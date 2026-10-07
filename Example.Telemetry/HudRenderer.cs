namespace Example.Telemetry;

using System.Globalization;

using SkiaSharp;

internal static class HudRenderer
{
    public const int Width = 1920;
    public const int Height = 480;

    private static readonly SKPaint Fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint Stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };

    private static readonly SKColor[] BackgroundColors = [new(0x0B, 0x1A, 0x2E), HudColors.Background];
    private static readonly SKColor[] TachColors = [HudColors.Azure, HudColors.Cyan, HudColors.Amber, HudColors.Red, HudColors.Red];
    private static readonly float[] TachPositions = [0f, 0.36f, 0.52f, 0.596f, 0.667f];
    private static readonly (string Label, SKColor Color)[] Legends =
    [
        ("BRAKE", HudColors.Red),
        ("THROTTLE", HudColors.Green),
        ("RPM", HudColors.Amber),
        ("SPEED", HudColors.Cyan)
    ];

    private static SKShader? backgroundShader;
    private static SKShader? tachShader;

    public static void Render(SKCanvas c, VehicleSimulator simulator, TraceBuffer trace, float t)
    {
        c.Clear(HudColors.Background);
        DrawBackground(c, t);
        DrawHeader(c, t);
        DrawShiftLights(c, simulator, t);
        DrawTachometer(c, simulator);
        DrawLapPanel(c, simulator);
        DrawTyres(c, simulator);
        DrawSpeed(c, simulator);
        DrawGear(c, simulator, t);
        DrawErs(c, simulator);
        DrawGForce(c, simulator);
        DrawTrace(c, trace);
        DrawGauges(c, simulator, t);
    }

    // --------------------------------------------------------------------------------
    // Background / Header
    // --------------------------------------------------------------------------------

    private static void DrawBackground(SKCanvas c, float t)
    {
        backgroundShader ??= SKShader.CreateRadialGradient(new SKPoint(Width / 2f, Height * 0.35f), Width * 0.6f, BackgroundColors, null, SKShaderTileMode.Clamp);
        Fill.Shader = backgroundShader;
        Fill.Color = SKColors.White;
        Fill.MaskFilter = null;
        c.DrawRect(0, 0, Width, Height, Fill);
        Fill.Shader = null;

        // Perspective grid
        const float horizon = 330f;
        c.Save();
        c.ClipRect(new SKRect(0, horizon, Width, Height));
        Stroke.StrokeWidth = 1.2f;
        Stroke.MaskFilter = null;
        Stroke.Color = HudColors.Azure.WithAlpha(24);
        for (var k = -24; k <= 24; k++)
        {
            c.DrawLine((Width / 2f) + (k * 46f), horizon, (Width / 2f) + (k * 210f), Height + 40f, Stroke);
        }
        for (var i = 0; i < 6; i++)
        {
            var u = ((i / 6f) + (t * 0.14f)) % 1f;
            Stroke.Color = HudColors.Azure.WithAlpha((byte)(12 + (50 * u)));
            var y = horizon + ((Height + 20f - horizon) * u * u);
            c.DrawLine(0, y, Width, y, Stroke);
        }
        c.Restore();
    }

    private static void DrawHeader(SKCanvas c, float t)
    {
        HudParts.DrawGlowText(c, "TYPE-19 STRATOS", 34f, 46f, 32f, HudColors.Cyan, SKTextAlign.Left, null, 8f);
        HudParts.DrawText(c, "FORMULA COCKPIT TELEMETRY", 36f, 70f, 14f, HudColors.Dim, SKTextAlign.Left);

        HudParts.DrawLine(c, 440f, 34f, 700f, 34f, 2f, HudColors.Azure.WithAlpha(110));
        HudParts.DrawLine(c, 700f, 34f, 722f, 54f, 2f, HudColors.Azure.WithAlpha(110));
        HudParts.DrawLine(c, 1480f, 34f, 1220f, 34f, 2f, HudColors.Azure.WithAlpha(110));
        HudParts.DrawLine(c, 1220f, 34f, 1198f, 54f, 2f, HudColors.Azure.WithAlpha(110));
        HudParts.FillCircle(c, 1500f, 34f, 5f, MathF.Sin(t * 4f) > 0f ? HudColors.Green : HudColors.Green.WithAlpha(70), 5f);

        HudParts.DrawText(c, "SYS", 1730f, 44f, 18f, HudColors.Dim, SKTextAlign.Right);
        HudParts.DrawGlowText(c, "ONLINE", 1886f, 46f, 26f, HudColors.Green, SKTextAlign.Right, HudParts.MonoFace, 4f);
        var link = 96.5f + (2.5f * MathF.Sin(t * 1.7f));
        HudParts.DrawText(c, Format($"LINK {link:F1}%"), 1820f, 72f, 14f, HudColors.Dim.WithAlpha(190), SKTextAlign.Right, HudParts.MonoFace);
        Fill.MaskFilter = null;
        for (var i = 0; i < 5; i++)
        {
            Fill.Color = (i < 4) || (MathF.Sin(t * 6f) > 0f) ? HudColors.Green.WithAlpha(170) : HudColors.PanelLine;
            c.DrawRect(1832f + (i * 11f), 61f, 7f, 12f, Fill);
        }
    }

    private static void DrawShiftLights(SKCanvas c, VehicleSimulator simulator, float t)
    {
        const int count = 15;
        const float y = 42f;
        const float spacing = 30f;
        const float x0 = (Width / 2f) - ((count - 1) * spacing / 2f);
        var lit = (simulator.Rpm - 11000f) / (VehicleSimulator.ShiftRpm - 11000f) * count;
        var over = simulator.Rpm > 17600f;
        var flash = MathF.Sin(t * 45f) > 0f;
        for (var i = 0; i < count; i++)
        {
            var x = x0 + (i * spacing);
            if (over ? flash : i < lit)
            {
                HudParts.FillCircle(c, x, y, 9f, i < 5 ? HudColors.Cyan : i < 10 ? HudColors.Amber : HudColors.Red, 7f);
            }
            else
            {
                Fill.Color = HudColors.Panel;
                Fill.MaskFilter = null;
                c.DrawCircle(x, y, 7f, Fill);
                Stroke.Color = HudColors.PanelLine;
                Stroke.StrokeWidth = 1.5f;
                Stroke.MaskFilter = null;
                c.DrawCircle(x, y, 7f, Stroke);
            }
        }
    }

    // --------------------------------------------------------------------------------
    // Tachometer
    // --------------------------------------------------------------------------------

    private static void DrawTachometer(SKCanvas c, VehicleSimulator simulator)
    {
        const float cx = 250f;
        const float cy = 282f;
        const float r = 150f;
        const float start = 150f;
        const float sweep = 240f;
        const float redFrac = VehicleSimulator.RedlineRpm / VehicleSimulator.MaxRpm;
        var frac = Math.Clamp(simulator.Rpm / VehicleSimulator.MaxRpm, 0f, 1f);
        var inRed = simulator.Rpm >= VehicleSimulator.RedlineRpm;

        HudParts.DrawArc(c, cx, cy, r + 30f, start, sweep, 2f, HudColors.PanelLine);
        HudParts.DrawArc(c, cx, cy, r + 21f, start + (sweep * redFrac), sweep * (1f - redFrac), 6f, HudColors.Red.WithAlpha(210), 5f);
        HudParts.DrawArc(c, cx, cy, r, start, sweep, 20f, new SKColor(0x0E, 0x1C, 0x2C, 230));

        // Value arc with the sweep gradient (rotated to start at the arc start)
        if (frac > 0.004f)
        {
            tachShader ??= SKShader.CreateSweepGradient(new SKPoint(cx, cy), TachColors, TachPositions);
            var rect = new SKRect(cx - r, cy - r, cx + r, cy + r);
            c.Save();
            c.RotateDegrees(start, cx, cy);
            Stroke.Shader = tachShader;
            Stroke.StrokeWidth = 20f;
            Stroke.StrokeCap = SKStrokeCap.Butt;
            Stroke.Color = SKColors.White.WithAlpha(130);
            Stroke.MaskFilter = HudParts.Blur(8f);
            c.DrawArc(rect, 0f, sweep * frac, false, Stroke);
            Stroke.Color = SKColors.White;
            Stroke.MaskFilter = null;
            c.DrawArc(rect, 0f, sweep * frac, false, Stroke);
            Stroke.Shader = null;
            c.Restore();
        }

        for (var rpm = 0; rpm <= 19000; rpm += 500)
        {
            var angle = start + (sweep * rpm / VehicleSimulator.MaxRpm);
            var major = (rpm % 1000) == 0;
            var color = rpm >= VehicleSimulator.RedlineRpm ? HudColors.Red.WithAlpha(220) : HudColors.Dim.WithAlpha(major ? (byte)230 : (byte)120);
            var p0 = HudParts.Polar(cx, cy, r + 12f, angle);
            var p1 = HudParts.Polar(cx, cy, r + (major ? 26f : 19f), angle);
            HudParts.DrawLine(c, p0.X, p0.Y, p1.X, p1.Y, major ? 2.5f : 1.4f, color, 0f, SKStrokeCap.Butt);
            if ((rpm % 2000) == 0)
            {
                var p = HudParts.Polar(cx, cy, r - 34f, angle);
                HudParts.DrawText(c, (rpm / 1000).ToString(CultureInfo.InvariantCulture), p.X, p.Y + 8f, 21f, rpm >= 18000 ? HudColors.Red : HudColors.Dim, SKTextAlign.Center, HudParts.MonoFace);
            }
        }
        HudParts.DrawText(c, "x1000 r/min", cx, cy - 42f, 14f, HudColors.Dim);

        // Needle
        c.Save();
        c.RotateDegrees(start + (sweep * frac), cx, cy);
        using (var builder = new SKPathBuilder())
        {
            builder.MoveTo(cx + 46f, cy - 5.5f);
            builder.LineTo(cx + r - 8f, cy - 1.5f);
            builder.LineTo(cx + r - 8f, cy + 1.5f);
            builder.LineTo(cx + 46f, cy + 5.5f);
            builder.Close();
            using var needle = builder.Detach();
            Fill.Color = (inRed ? HudColors.Red : HudColors.Cyan).WithAlpha(160);
            Fill.MaskFilter = HudParts.Blur(6f);
            c.DrawPath(needle, Fill);
            Fill.Color = HudColors.White;
            Fill.MaskFilter = null;
            c.DrawPath(needle, Fill);
        }
        c.Restore();

        // Hub
        Fill.Color = HudColors.Panel;
        c.DrawCircle(cx, cy, 24f, Fill);
        Stroke.Color = HudColors.PanelLine;
        Stroke.StrokeWidth = 2f;
        c.DrawCircle(cx, cy, 24f, Stroke);
        HudParts.FillCircle(c, cx, cy, 6f, inRed ? HudColors.Red : HudColors.Cyan, 5f);

        // Digital RPM
        HudParts.DrawCutPanel(c, cx - 92f, cy + 62f, 184f, 86f, 12f, HudColors.Panel.WithAlpha(220), HudColors.PanelLine);
        HudParts.DrawText(c, "ENGINE RPM", cx, cy + 84f, 14f, HudColors.Dim);
        var rpmText = ((int)simulator.Rpm).ToString(CultureInfo.InvariantCulture).PadLeft(5, ' ');
        HudParts.DrawSevenSegment(c, cx - (HudParts.SevenSegmentWidth(36f, 5) / 2f), cy + 96f, 36f, rpmText, inRed ? HudColors.Red : HudColors.Cyan);
    }

    // --------------------------------------------------------------------------------
    // Lap / Tyres
    // --------------------------------------------------------------------------------

    private static void DrawLapPanel(SKCanvas c, VehicleSimulator simulator)
    {
        const float x = 466f;
        const float y = 98f;
        const float w = 284f;
        HudParts.DrawCutPanel(c, x, y, w, 250f, 14f, HudColors.Panel.WithAlpha(200), HudColors.PanelLine);
        DrawRow(c, "LAP", Format($"{simulator.Lap:00} / {simulator.TotalLaps:00}"), y + 42f, HudColors.White);
        DrawRow(c, "TIME", FormatLap(simulator.LapTime), y + 88f, HudColors.Cyan);
        DrawRow(c, "BEST", FormatLap(simulator.BestLap), y + 134f, HudColors.Amber);
        DrawRow(c, "DELTA", Format($"{simulator.Delta:+0.000;-0.000}"), y + 180f, simulator.Delta <= 0f ? HudColors.Green : HudColors.Red);
        DrawRow(c, "POS", Format($"P{simulator.Position:00}"), y + 226f, HudColors.White);

        static void DrawRow(SKCanvas c, string label, string value, float baseline, SKColor color)
        {
            HudParts.DrawText(c, label, x + 22f, baseline - 2f, 18f, HudColors.Dim, SKTextAlign.Left);
            HudParts.DrawGlowText(c, value, x + w - 22f, baseline, 28f, color, SKTextAlign.Right, HudParts.MonoFace, 4f);
        }
    }

    private static void DrawTyres(SKCanvas c, VehicleSimulator simulator)
    {
        const float x = 466f;
        const float y = 358f;
        const float left = x + 112f;
        const float right = x + 208f;
        const float boxWidth = 64f;
        const float boxHeight = 34f;
        const float spine = (left + boxWidth + right) / 2f;
        HudParts.DrawCutPanel(c, x, y, 284f, 104f, 14f, HudColors.Panel.WithAlpha(200), HudColors.PanelLine);
        HudParts.DrawText(c, "TYRE", x + 22f, y + 46f, 18f, HudColors.Dim, SKTextAlign.Left);
        HudParts.DrawText(c, "°C", x + 22f, y + 72f, 15f, HudColors.Dim, SKTextAlign.Left);

        // Axles
        HudParts.DrawLine(c, left + boxWidth, y + 31f, right, y + 31f, 2f, HudColors.PanelLine);
        HudParts.DrawLine(c, left + boxWidth, y + 73f, right, y + 73f, 2f, HudColors.PanelLine);
        HudParts.DrawLine(c, spine, y + 31f, spine, y + 73f, 2f, HudColors.PanelLine);

        var temps = simulator.TyreTemps;
        for (var i = 0; i < temps.Length; i++)
        {
            var bx = (i % 2) == 0 ? left : right;
            var by = y + 14f + ((i / 2) * 42f);
            var temp = temps[i];
            var color = temp < 85f ? HudColors.Azure : temp < 105f ? HudColors.Green : temp < 115f ? HudColors.Amber : HudColors.Red;
            var rect = new SKRect(bx, by, bx + boxWidth, by + boxHeight);
            Fill.Color = color.WithAlpha(50);
            Fill.MaskFilter = null;
            c.DrawRoundRect(rect, 6f, 6f, Fill);
            Stroke.Color = color;
            Stroke.StrokeWidth = 2f;
            Stroke.MaskFilter = null;
            c.DrawRoundRect(rect, 6f, 6f, Stroke);
            HudParts.DrawText(c, ((int)temp).ToString(CultureInfo.InvariantCulture), bx + (boxWidth / 2f), by + 25f, 21f, HudColors.White, SKTextAlign.Center, HudParts.MonoFace);
        }
    }

    // --------------------------------------------------------------------------------
    // Speed / Gear / ERS / G-force
    // --------------------------------------------------------------------------------

    private static void DrawSpeed(SKCanvas c, VehicleSimulator simulator)
    {
        const float cx = Width / 2f;
        const float digitHeight = 132f;
        const int cells = 30;
        const float cellWidth = 9f;
        const float gap = 3f;
        const float total = (cells * (cellWidth + gap)) - gap;
        const float x0 = cx - (total / 2f);
        const float y = 288f;

        HudParts.DrawText(c, "SPEED", cx, 100f, 17f, HudColors.Dim);
        var text = ((int)MathF.Round(simulator.Speed)).ToString(CultureInfo.InvariantCulture).PadLeft(3, ' ');
        var digitWidth = HudParts.SevenSegmentWidth(digitHeight, 3);
        HudParts.DrawSevenSegment(c, cx - (digitWidth / 2f), 112f, digitHeight, text, simulator.BoostActive ? HudColors.Amber : HudColors.Cyan);
        HudParts.DrawText(c, "km/h", cx, 278f, 22f, HudColors.Azure);

        // Speed ribbon
        var litCount = (int)((simulator.Speed / VehicleSimulator.TopSpeed * cells) + 0.5f);
        Fill.MaskFilter = null;
        for (var i = 0; i < cells; i++)
        {
            var color = i < (cells * 0.6f) ? HudColors.Cyan : i < (cells * 0.85f) ? HudColors.Amber : HudColors.Red;
            Fill.Color = i < litCount ? color : HudColors.PanelLine.WithAlpha(80);
            c.DrawRect(x0 + (i * (cellWidth + gap)), y, cellWidth, 12f, Fill);
        }
        HudParts.DrawText(c, "0", x0, y + 28f, 13f, HudColors.Dim);
        HudParts.DrawText(c, "680", x0 + total, y + 28f, 13f, HudColors.Dim);
    }

    private static void DrawGear(SKCanvas c, VehicleSimulator simulator, float t)
    {
        const float cx = Width / 2f;
        const float cy = 392f;
        HudParts.DrawCutPanel(c, cx - 60f, cy - 64f, 120f, 128f, 20f, HudColors.Panel.WithAlpha(225), HudColors.PanelLine, 2f);

        var color = simulator.Rpm > VehicleSimulator.RedlineRpm ? HudColors.Red : simulator.Rpm > 15200f ? HudColors.Amber : HudColors.White;
        if ((simulator.Rpm > 17600f) && (MathF.Sin(t * 30f) < 0f))
        {
            color = color.WithAlpha(120);
        }
        HudParts.DrawGlowText(c, simulator.Gear.ToString(CultureInfo.InvariantCulture), cx, cy + 34f, 100f, color, SKTextAlign.Center, HudParts.MonoFace, 12f);
        HudParts.DrawText(c, "GEAR", cx, cy + 56f, 15f, HudColors.Dim);

        for (var gear = 1; gear <= VehicleSimulator.TopGear; gear++)
        {
            var current = gear == simulator.Gear;
            var y = cy + 44f - ((gear - 1) * 14f);
            Fill.Color = current ? HudColors.Cyan : HudColors.PanelLine.WithAlpha(120);
            Fill.MaskFilter = current ? HudParts.Blur(4f) : null;
            c.DrawRect(cx - 76f, y, 7f, 8f, Fill);
            c.DrawRect(cx + 69f, y, 7f, 8f, Fill);
            Fill.MaskFilter = null;
        }
    }

    private static void DrawErs(SKCanvas c, VehicleSimulator simulator)
    {
        const float cx = 816f;
        const float cy = 388f;
        const float r = 44f;
        var frac = Math.Clamp((simulator.ErsOutput + 100f) / 400f, 0f, 1f);
        var color = simulator.ErsOutput < -5f ? HudColors.Green : simulator.BoostActive ? HudColors.Amber : HudColors.Cyan;
        HudParts.DrawArc(c, cx, cy, r, 135f, 270f, 7f, HudColors.PanelLine.WithAlpha(140));
        if (frac > 0.01f)
        {
            HudParts.DrawArc(c, cx, cy, r, 135f, 270f * frac, 7f, color, 5f);
        }
        var p = HudParts.Polar(cx, cy, r, 135f + (270f * frac));
        HudParts.FillCircle(c, p.X, p.Y, 3.5f, HudColors.White, 4f);
        HudParts.DrawText(c, "ERS", cx, cy - 14f, 13f, HudColors.Dim);
        HudParts.DrawGlowText(c, Format($"{simulator.ErsOutput:+0;-0;0}"), cx, cy + 9f, 23f, HudColors.White, SKTextAlign.Center, HudParts.MonoFace, 4f);
        HudParts.DrawText(c, "kW", cx, cy + 26f, 12f, HudColors.Dim);
        HudParts.DrawText(c, "ERS OUTPUT", cx, cy + r + 24f, 14f, HudColors.Dim);
    }

    private static void DrawGForce(SKCanvas c, VehicleSimulator simulator)
    {
        const float cx = 1104f;
        const float cy = 388f;
        const float r = 44f;
        Stroke.Color = HudColors.PanelLine;
        Stroke.StrokeWidth = 2f;
        Stroke.MaskFilter = null;
        c.DrawCircle(cx, cy, r, Stroke);
        Stroke.Color = HudColors.PanelLine.WithAlpha(120);
        Stroke.StrokeWidth = 1.2f;
        c.DrawCircle(cx, cy, r / 2f, Stroke);
        HudParts.DrawLine(c, cx - r, cy, cx + r, cy, 1f, HudColors.PanelLine.WithAlpha(140));
        HudParts.DrawLine(c, cx, cy - r, cx, cy + r, 1f, HudColors.PanelLine.WithAlpha(140));

        var bx = Math.Clamp(simulator.GLat / 3.5f, -1f, 1f) * (r - 10f);
        var by = Math.Clamp(simulator.GLong / 3.5f, -1f, 1f) * (r - 10f);
        var total = MathF.Sqrt((simulator.GLat * simulator.GLat) + (simulator.GLong * simulator.GLong));
        var color = total > 3.2f ? HudColors.Red : total > 2.2f ? HudColors.Amber : HudColors.Cyan;
        HudParts.DrawLine(c, cx, cy, cx + bx, cy + by, 1.5f, color.WithAlpha(110));
        HudParts.FillCircle(c, cx + bx, cy + by, 5.5f, color, 6f);
        HudParts.DrawText(c, Format($"G-FORCE  {total:0.0} G"), cx, cy + r + 24f, 14f, HudColors.Dim);
    }

    // --------------------------------------------------------------------------------
    // Trace / Gauges
    // --------------------------------------------------------------------------------

    private static void DrawTrace(SKCanvas c, TraceBuffer trace)
    {
        const float x = 1200f;
        const float y = 98f;
        const float w = 690f;
        const float h = 218f;
        const float px = x + 22f;
        const float py = y + 42f;
        const float pw = w - 44f;
        const float ph = h - 62f;
        HudParts.DrawCutPanel(c, x, y, w, h, 14f, HudColors.Panel.WithAlpha(200), HudColors.PanelLine);
        HudParts.DrawText(c, "LIVE TELEMETRY", x + 22f, y + 28f, 16f, HudColors.Dim, SKTextAlign.Left);

        var lx = x + w - 22f;
        foreach (var (label, color) in Legends)
        {
            HudParts.DrawText(c, label, lx, y + 28f, 14f, color, SKTextAlign.Right);
            lx -= (label.Length * 9f) + 34f;
            HudParts.DrawLine(c, lx + 8f, y + 23f, lx + 24f, y + 23f, 3f, color);
        }

        // Grid (12 seconds)
        for (var i = 0; i <= 4; i++)
        {
            var gy = py + (ph * i / 4f);
            HudParts.DrawLine(c, px, gy, px + pw, gy, 1f, HudColors.PanelLine.WithAlpha(i is 0 or 4 ? (byte)200 : (byte)110), 0f, SKStrokeCap.Butt);
        }
        for (var i = 0; i <= 6; i++)
        {
            var gx = px + (pw * i / 6f);
            HudParts.DrawLine(c, gx, py, gx, py + ph, 1f, HudColors.PanelLine.WithAlpha(90), 0f, SKStrokeCap.Butt);
        }
        HudParts.DrawText(c, "-12s", px, py + ph + 15f, 12f, HudColors.Dim, SKTextAlign.Left, HudParts.MonoFace);
        HudParts.DrawText(c, "NOW", px + pw, py + ph + 15f, 12f, HudColors.Dim, SKTextAlign.Right, HudParts.MonoFace);

        if (trace.Count < 2)
        {
            return;
        }

        Span<SKPoint> points = stackalloc SKPoint[trace.Count];
        DrawSeries(c, trace, points, trace.Throttle, HudColors.Green.WithAlpha(210), 2f, 0f);
        DrawSeries(c, trace, points, trace.Brake, HudColors.Red.WithAlpha(230), 2f, 0f);
        DrawSeries(c, trace, points, trace.Rpm, HudColors.Amber.WithAlpha(190), 1.5f, 0f);
        DrawSeries(c, trace, points, trace.Speed, HudColors.Cyan, 2.5f, 4f);

        static void DrawSeries(SKCanvas c, TraceBuffer trace, Span<SKPoint> points, Func<int, float> value, SKColor color, float width, float glow)
        {
            var offset = TraceBuffer.Capacity - trace.Count;
            for (var i = 0; i < trace.Count; i++)
            {
                var v = 0.04f + (0.92f * Math.Clamp(value(i), 0f, 1f));
                points[i] = new SKPoint(px + (pw * (offset + i) / (TraceBuffer.Capacity - 1)), py + (ph * (1f - v)));
            }
            HudParts.DrawPolyline(c, points, width, color, glow);
        }
    }

    private static void DrawGauges(SKCanvas c, VehicleSimulator simulator, float t)
    {
        HudParts.DrawMiniGauge(c, GaugeBounds(0), "WATER TEMP", Format($"{simulator.WaterTemp:0}"), "°C", (simulator.WaterTemp - 50f) / 80f, simulator.WaterTemp > 110f, HudColors.Azure, t);
        HudParts.DrawMiniGauge(c, GaugeBounds(1), "OIL TEMP", Format($"{simulator.OilTemp:0}"), "°C", (simulator.OilTemp - 60f) / 100f, simulator.OilTemp > 145f, HudColors.Amber, t);
        HudParts.DrawMiniGauge(c, GaugeBounds(2), "FUEL", Format($"{simulator.Fuel * 100f:0}"), "%", simulator.Fuel, simulator.Fuel < 0.12f, HudColors.Green, t);
        HudParts.DrawMiniGauge(c, GaugeBounds(3), "TURBO", Format($"{simulator.TurboPressure:0.00}"), "bar", simulator.TurboPressure / 3f, simulator.TurboPressure > 2.6f, HudColors.Amber, t);

        static SKRect GaugeBounds(int index) => SKRect.Create(1206f + (index * 172f), 328f, 160f, 140f);
    }

    // --------------------------------------------------------------------------------
    // Helper
    // --------------------------------------------------------------------------------

    private static string Format(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatLap(float seconds)
    {
        var minutes = (int)(seconds / 60f);
        return Format($"{minutes}:{seconds - (minutes * 60f):00.000}");
    }
}
