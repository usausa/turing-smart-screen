# TuringSmartScreenLib

| Package | Info | Description |
|-|-|-|
| TuringSmartScreenLib | [![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib)](https://www.nuget.org/packages/TuringSmartScreenLib/) | Core |
| TuringSmartScreenLib.Helpers.SkiaSharp | [![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib.Helpers.SkiaSharp)](https://www.nuget.org/packages/TuringSmartScreenLib.Helpers.SkiaSharp/) | Helpers |

## What is this?

* Turing Smart Screen controller library.

## Usage

```csharp
using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

using var screen = ScreenFactory.Create(ScreenType.RevisionB, "COM10");
screen.SetBrightness(100);
screen.Orientation = ScreenOrientation.Landscape;

using var bitmap = SKBitmap.Decode(File.OpenRead("genbaneko.png"));
var buffer = screen.CreateBufferFrom(bitmap);

screen.DisplayBuffer(0, 0, buffer);
```

<img src="Images/image.jpg" width="50%" title="image">
