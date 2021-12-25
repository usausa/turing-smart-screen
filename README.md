# TuringSmartScreenLib

[![NuGet Badge](https://buildstats.info/nuget/TuringSmartScreenLib)](https://www.nuget.org/packages/TuringSmartScreenLib/)

## What is this?

* Turing Smart Screen controller library.

## Usage

```csharp
using System.IO;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers;

// Load RGB565 bytes
var bytes = BitmapLoader.Load(File.OpenRead("test.png"), 0, 0, 320, 480);

// Display bitmap
using var screen = new TuringSmartScreen("COM10");
screen.Open();
screen.DisplayBitmap(0, 0, 320, 480, bytes);
```

<img src="Images/image.png" width="75%" title="image">