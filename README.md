# LCD Driver Library

| Package | Info | Description |
|-|-|-|
| TuringSmartScreenLib | [![NuGet](https://img.shields.io/nuget/v/TuringSmartScreenLib.svg)](https://www.nuget.org/packages/TuringSmartScreenLib/) | Core |
| TuringSmartScreenLib.Helpers.SkiaSharp | [![NuGet](https://img.shields.io/nuget/v/TuringSmartScreenLib.Helpers.SkiaSharp.svg)](https://www.nuget.org/packages/TuringSmartScreenLib.Helpers.SkiaSharp/) | Helpers |
| LcdDriver.TrofeoVision | [![NuGet](https://img.shields.io/nuget/v/LcdDriver.TrofeoVision.svg)](https://www.nuget.org/packages/LcdDriver.TrofeoVision/) | Thermalright Trofeo Vision usb lcd controller |
| LcdDriver.TuringSmartScreen | [![NuGet](https://img.shields.io/nuget/v/LcdDriver.TuringSmartScreen.svg)](https://www.nuget.org/packages/LcdDriver.TuringSmartScreen/) | Turing-Smart-Screen usb lcd controller |

## 👉What is this?

LCD controller libraries for the following devices:

* [Turing Smart Screen](https://www.turzx.com/) 3.5 inch / 5 inch / 8 inch (Serial)
* [Turing Smart Screen](https://www.turzx.com/) 8 inch USB Revision 1.1 (USB)
* [Thermalright Trofeo Vision](https://www.thermalright.com/product/trofeo-vision-lcd-white/) (USB HID)

## 🔲TuringSmartScreenLib

Turing Smart Screen 3.5 inch, 5 inch, 8 inch serial connection models.

| Revision | Screen Size | Resolution |
|-|-|-|
| RevisionA | 3.5 inch | 320x480 |
| RevisionB | 3.5 inch | 320x480 |
| RevisionC | 5 inch | 800x480 |
| RevisionE | 8 inch | 480x1920 |

<img src="Images/tss.jpg" width="50%" title="image">

## 🔲LcdDriver.TrofeoVision

[Thermalright Trofeo Vision](https://www.thermalright.com/product/trofeo-vision-lcd-white/) USB HID LCD controller (1280x480).

<img src="Images/trofeo.jpg" width="50%" title="image">

### 🌐Link

- [MacStatDisplay](https://github.com/usausa/mac-stat-display) : macOS system monitor

## 🔲LcdDriver.TuringSmartScreen

[Turing Smart Screen](https://www.turzx.com/) 8 inch USB Revision 1.1 LCD controller.

<img src="Images/tss8usb.jpg" width="50%" title="image">

## 🛠️TuringSmartScreenTool

CLI for turing smart screen.

### 📦Install

```
> dotnet tool install -g TuringSmartScreenTool
```

### 📘Usage

```
> tsstool reset -r a -p COM10
> tsstool clear -r a -p COM10
> tsstool on -p COM10
> tsstool off -p COM10
> tsstool bright -p COM10 -l 192
> tsstool image -p COM10 -f genbaneko.png
> tsstool fill -p COM10 -c ff0000
> tsstool text -p COM10 -t TEST -x 80 -y 40 -s 96 -f Arial -c ff0000 -b 0000ff
```
