namespace TuringSmartScreenLib;

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0032
// ReSharper disable ConvertToAutoProperty
public sealed class ScreenBufferRgb353 : IScreenBuffer
{
    private readonly int width;

    private readonly int height;

    private byte[] buffer;

    public int Width => width;

    public int Height => height;

    internal byte[] Buffer => buffer;

    public ScreenBufferRgb353(int width, int height)
    {
        this.width = width;
        this.height = height;
        buffer = ArrayPool<byte>.Shared.Rent(width * height * 2);
        buffer.AsSpan().Clear();
    }

    public void Dispose()
    {
        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var rgb = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        ref var p = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), (y * width + x) * 2);
        Unsafe.WriteUnaligned(ref p, BitConverter.IsLittleEndian ? rgb : (ushort)((rgb << 8) | (rgb >> 8)));
    }

    public void Clear() => Clear(0, 0, 0);

    public void Clear(byte r, byte g, byte b)
    {
        var pattern = (Span<byte>)stackalloc byte[2];
        var rgb = ((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3);
        BinaryPrimitives.WriteInt16LittleEndian(pattern, (short)rgb);
        Helper.Fill(buffer.AsSpan(0, width * height * 2), pattern);
    }
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
