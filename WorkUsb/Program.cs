// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global
#pragma warning disable CA1031
#pragma warning disable CA1303
#pragma warning disable CA1515
namespace WorkUsb;

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

using LibUsbDotNet;
using LibUsbDotNet.Main;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

internal static class Program
{
    public static void Main()
    {
        //var finder = new UsbDeviceFinder(0x1cbe, 0x0088);
        //var device = UsbDevice.OpenUsbDevice(finder);

        using var device = new TuringDevice();
        device.Initialize();
        device.SendBrightnessCommand(0);
    }
}

public sealed class TuringDevice : IDisposable
{
    private const int VendorId = 0x1cbe;
    private const int ProductId = 0x0088;
    private const int CmdPacketSize = 500;
    private const int FullPacketSize = 512;

    private static readonly byte[] DesKeyBytes = "slv3tuzx"u8.ToArray();
    private static readonly byte[] MagicBytes = [161, 26];

    private readonly BufferedBlockCipher cipher = new(new CbcBlockCipher(new DesEngine()));

    private UsbDevice? device;
    private UsbEndpointReader? reader;
    private UsbEndpointWriter? writer;

    public void Dispose()
    {
        // Clean up resources
        if (reader != null)
        {
            reader.Dispose();
            reader = null;
        }

        if (writer != null)
        {
            writer.Dispose();
            writer = null;
        }

        if (device != null)
        {
            if (device is IUsbDevice wholeUsbDevice)
            {
                // Release interface
                wholeUsbDevice.ReleaseInterface(0);
            }

            ((IDisposable)device).Dispose();
            device = null;
        }

        // Force garbage collection to ensure device resources are released
        UsbDevice.Exit();
    }

    public bool Initialize()
    {
        Trace.WriteLine("Initializing Turing Device...");

        try
        {
            // Find and open the USB device
            var finder = new UsbDeviceFinder(VendorId, ProductId);
            device = UsbDevice.OpenUsbDevice(finder);

            if (device == null)
            {
                Trace.WriteLine("Device not found.");
                return false;
            }

            Trace.WriteLine("Device found.");
            // If this is a "whole" USB device (like a composite device),
            // it needs to be properly configured first
            if (device is IUsbDevice wholeUsbDevice)
            {
                // Select the first configuration and claim interface 0
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            // Get endpoints for reading and writing
            reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);

            if (reader == null || writer == null)
            {
                Console.WriteLine("Failed to open endpoints.");
                return false;
            }

            Trace.WriteLine("Device initialized.");
            return true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error initializing device: {ex.Message}");
            return false;
        }
    }

    public static byte[] BuildCommandPacketHeader(byte commandId)
    {
        var packet = ArrayPool<byte>.Shared.Rent(CmdPacketSize);
        try
        {
            Array.Clear(packet, 0, CmdPacketSize);

            packet[0] = commandId;
            packet[2] = 0x1A;
            packet[3] = 0x6D;

            // Optimize timestamp calculation - avoid creating two DateTimeOffset objects
            var today = DateTime.UtcNow.Date;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var dayStart = new DateTimeOffset(today).ToUnixTimeMilliseconds();
            var timestamp = now - dayStart;

            BinaryPrimitives.WriteUInt32LittleEndian(
                packet.AsSpan(4, sizeof(uint)),
                unchecked((uint)timestamp));

            // Create a copy to return (since we need to return the rented array)
            var result = new byte[CmdPacketSize];
            Buffer.BlockCopy(packet, 0, result, 0, CmdPacketSize);
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(packet);
        }
    }

    public byte[] EncryptWithDes(byte[] data)
    {
        var keyParam = new KeyParameter(DesKeyBytes);
        cipher.Init(true, new ParametersWithIV(keyParam, DesKeyBytes));

        var paddedLen = (data.Length + 7) & ~7;    // round up to multiple of 8
        var padded = ArrayPool<byte>.Shared.Rent(paddedLen);
        try
        {
            Array.Clear(padded, 0, paddedLen);     // Ensure padding bytes are zeroed
            data.CopyTo(padded, 0);

            var outputSize = cipher.GetOutputSize(paddedLen);
            var encrypted = ArrayPool<byte>.Shared.Rent(outputSize);
            try
            {
                var len = cipher.ProcessBytes(padded, 0, paddedLen, encrypted, 0);
                var finalLen = len + cipher.DoFinal(encrypted, len);

                // Return only the actual encrypted data
                var result = new byte[finalLen];
                Buffer.BlockCopy(encrypted, 0, result, 0, finalLen);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(encrypted);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(padded);
        }
    }

    public byte[] EncryptCommandPacket(byte[] data)
    {
        var encrypted = EncryptWithDes(data);

        var finalPacket = ArrayPool<byte>.Shared.Rent(FullPacketSize);
        Array.Clear(finalPacket, 0, FullPacketSize);

        Buffer.BlockCopy(encrypted, 0, finalPacket, 0, Math.Min(encrypted.Length, FullPacketSize - 2));

        // Add magic bytes at the end
        finalPacket[FullPacketSize - 2] = MagicBytes[0];  // 161
        finalPacket[FullPacketSize - 1] = MagicBytes[1];  // 26

        // Create a copy to return (since we need to return the rented array)
        var result = new byte[FullPacketSize];
        Buffer.BlockCopy(finalPacket, 0, result, 0, FullPacketSize);

        ArrayPool<byte>.Shared.Return(finalPacket);
        return result;
    }

    public void SendSyncCommand()
    {
        var cmdPacket = BuildCommandPacketHeader(10);
        WriteToDevice(EncryptCommandPacket(cmdPacket));
    }

    public void SendRestartDeviceCommand()
    {
        var cmdPacket = BuildCommandPacketHeader(11);
        WriteToDevice(EncryptCommandPacket(cmdPacket));
    }

    public void SendBrightnessCommand(byte brightness)
    {
        var cmdPacket = BuildCommandPacketHeader(14);
        cmdPacket[8] = brightness;
        WriteToDevice(EncryptCommandPacket(cmdPacket));
    }

    public void SendClearImageCommand()
    {
        // Minimal transparent PNG for 480x1920 (copied from Python clear_image)
        byte[] imgData = [
            0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x01, 0xe0, 0x00, 0x00, 0x07, 0x80, 0x08, 0x06, 0x00, 0x00, 0x00, 0x16, 0xf0, 0x84,
                0xf5, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, 0x00, 0xae, 0xce, 0x1c, 0xe9, 0x00, 0x00,
                0x00, 0x04, 0x67, 0x41, 0x4d, 0x41, 0x00, 0x00, 0xb1, 0x8f, 0x0b, 0xfc, 0x61, 0x05, 0x00, 0x00,
                0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0e, 0xc3, 0x00, 0x00, 0x0e, 0xc3, 0x01, 0xc7,
                0x6f, 0xa8, 0x64, 0x00, 0x00, 0x0e, 0x0c, 0x49, 0x44, 0x41, 0x54, 0x78, 0x5e, 0xed, 0xc1, 0x01,
                0x0d, 0x00, 0x00, 0x00, 0xc2, 0xa0, 0xf7, 0x4f, 0x6d, 0x0f, 0x07, 0x14, 0x00, 0x00, 0x00, 0x00
            ];
        // Add 3568 zero bytes
        Array.Resize(ref imgData, imgData.Length + 3568);
        // Add PNG end chunk
        var endChunk = new byte[]
        {
            0x00, 0xf0, 0x66, 0x4a, 0xc8, 0x00, 0x01, 0x11, 0x9d, 0x82, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82
        };
        Array.Resize(ref imgData, imgData.Length + endChunk.Length);
        Array.Copy(endChunk, 0, imgData, imgData.Length - endChunk.Length, endChunk.Length);

        var imgSize = imgData.Length;
        var cmdPacket = BuildCommandPacketHeader(102);
        cmdPacket[8] = (byte)((imgSize >> 24) & 0xFF);
        cmdPacket[9] = (byte)((imgSize >> 16) & 0xFF);
        cmdPacket[10] = (byte)((imgSize >> 8) & 0xFF);
        cmdPacket[11] = (byte)(imgSize & 0xFF);

        var encryptedPacket = EncryptCommandPacket(cmdPacket);
        var fullPayload = new byte[encryptedPacket.Length + imgData.Length];
        Buffer.BlockCopy(encryptedPacket, 0, fullPayload, 0, encryptedPacket.Length);
        Buffer.BlockCopy(imgData, 0, fullPayload, encryptedPacket.Length, imgData.Length);

        WriteToDevice(fullPayload);
    }

    public bool WriteToDevice(byte[] data, int timeout = 2000)
    {
        if (writer == null || reader == null)
        {
            return false;
        }

        try
        {
            // Write the data
            var ec = writer.Write(data, timeout, out var transferLength);

            if (ec != ErrorCode.None)
            {
                Console.WriteLine($"Write Error: {ec}");
                return false;
            }

            Console.WriteLine($"Wrote {transferLength} bytes to device.");

            // Read the response
            var readBuffer = new byte[512];
            ec = reader.Read(readBuffer, timeout, out transferLength);

            if (ec != ErrorCode.None && ec != ErrorCode.IoTimedOut)
            {
                Console.WriteLine($"Read Error: {ec}");
                return false;
            }

            if (transferLength > 0)
            {
                Console.WriteLine($"Read {transferLength} bytes from device");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to device: {ex.Message}");
            return false;
        }
    }

    public void ReadFlush(int maxAttempts = 5)
    {
        if (reader == null)
        {
            return;
        }

        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                var readBuffer = new byte[512];
                var ec = reader.Read(readBuffer, 200, out var transferLength);

                if (ec == ErrorCode.IoTimedOut || transferLength == 0)
                {
                    break;
                }

                Console.WriteLine($"Flushed {transferLength} bytes");
            }
            catch
            {
                break;
            }
        }
    }

    public void DelaySync()
    {
        SendSyncCommand();
        Thread.Sleep(200);
    }
}
