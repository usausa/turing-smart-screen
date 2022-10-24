# TuringSmartScreenLib

| Package | Info | Description |
|-|-|-|
| TuringSmartScreenLib | [![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib)](https://www.nuget.org/packages/TuringSmartScreenLib/) | Core |
| TuringSmartScreenLib.Helpers.SkiaSharp | [![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib.Helpers)](https://www.nuget.org/packages/TuringSmartScreenLib.Helpers.SkiaSharp/) | Helpers |

## What is this?

* Turing Smart Screen controller library.

## Usage

```csharp
using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

using var screen = ScreenFactory.Create(ScreenType.RevisionB, "COM10");
screen.SetBrightness(100);
screen.SetOrientation(ScreenOrientation.Landscape);

var buffer = screen.CreateBuffer(480, 320);

using var bitmap = SKBitmap.Decode(File.OpenRead("genbaneko.png"));
buffer.ReadFrom(bitmap);

screen.DisplayBuffer(0, 0, buffer);
```

<img src="Images/image.jpg" width="50%" title="image">

# TuringSmartScreenTool

Command line tool.

## Install

```
> dotnet tool install -g TuringSmartScreenTool
```

## Usage

```
> tsstool reset -p COM10
> tsstool clear -p COM10
> tsstool on -p COM10
> tsstool off -p COM10
> tsstool bright -p COM10 -l 192
> tsstool display -p COM10 -f genbaneko.png
```
