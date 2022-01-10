# TuringSmartScreenLib

| Package | Info | Description |
|-|-|-|
| TuringSmartScreenLib | [![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib)](https://www.nuget.org/packages/TuringSmartScreenLib/) | Core |
| TuringSmartScreenLib.Helpers | [![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib.Helpers)](https://www.nuget.org/packages/TuringSmartScreenLib.Helpers/) | Helpers |

## What is this?

* Turing Smart Screen controller library.

## Usage

```csharp
using System.IO;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers;

// Load RGB565 bytes
var bytes = BitmapLoader.Load(File.OpenRead("genbaneko.png"), 0, 0, 320, 480);

// Display bitmap
using var screen = new TuringSmartScreen("COM10");
screen.Open();
screen.DisplayBitmap(0, 0, 320, 480, bytes);
```

<img src="Images/image.jpg" width="50%" title="image">

## TuringSmartScreenTool

Command line tool.

### Install

```
> dotnet tool install -g TuringSmartScreenTool
```

### Usage

```
> tsstool reset -p COM10
> tsstool clear -p COM10
> tsstool on -p COM10
> tsstool off -p COM10
> tsstool bright -p COM10 -l 192
> tsstool display -p COM10 -f genbaneko.png
```
