namespace Example.Telemetry;

using SkiaSharp;

internal static class HudParts
{
    public static readonly SKTypeface LabelFace = SKTypeface.FromFamilyName("Bahnschrift", SKFontStyle.Bold);
    public static readonly SKTypeface MonoFace = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);

    private static readonly SKPaint Fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint Stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private static readonly SKPaint Text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKFont Font = new() { Edging = SKFontEdging.Antialias };
    private static readonly Dictionary<int, SKMaskFilter> Blurs = [];

    // Segment bits: A=1, B=2, C=4, D=8, E=16, F=32, G=64
    private static readonly Dictionary<char, int> Segments = new()
    {
        ['0'] = 0b0111111,
        ['1'] = 0b0000110,
        ['2'] = 0b1011011,
        ['3'] = 0b1001111,
        ['4'] = 0b1100110,
        ['5'] = 0b1101101,
        ['6'] = 0b1111101,
        ['7'] = 0b0000111,
        ['8'] = 0b1111111,
        ['9'] = 0b1101111,
        ['-'] = 0b1000000
    };

    public static SKMaskFilter Blur(float sigma)
    {
        var key = (int)(sigma * 4f);
        if (!Blurs.TryGetValue(key, out var filter))
        {
            filter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, MathF.Max(0.5f, sigma));
            Blurs[key] = filter;
        }
        return filter;
    }

    public static SKPoint Polar(float cx, float cy, float r, float degree)
    {
        var radian = degree * MathF.PI / 180f;
        return new SKPoint(cx + (r * MathF.Cos(radian)), cy + (r * MathF.Sin(radian)));
    }

    // --------------------------------------------------------------------------------
    // Text
    // --------------------------------------------------------------------------------

    public static void DrawText(SKCanvas c, string s, float x, float y, float size, SKColor color, SKTextAlign align = SKTextAlign.Center, SKTypeface? face = null)
    {
        Font.Typeface = face ?? LabelFace;
        Font.Size = size;
        Text.Color = color;
        Text.MaskFilter = null;
        c.DrawText(s, x, y, align, Font, Text);
    }

    public static void DrawGlowText(SKCanvas c, string s, float x, float y, float size, SKColor color, SKTextAlign align = SKTextAlign.Center, SKTypeface? face = null, float glow = 0f)
    {
        Font.Typeface = face ?? LabelFace;
        Font.Size = size;
        Text.Color = color.WithAlpha(150);
        Text.MaskFilter = Blur(glow > 0f ? glow : size * 0.18f);
        c.DrawText(s, x, y, align, Font, Text);
        Text.Color = color;
        Text.MaskFilter = null;
        c.DrawText(s, x, y, align, Font, Text);
    }

    // --------------------------------------------------------------------------------
    // Line / Arc / Panel
    // --------------------------------------------------------------------------------

    public static void DrawArc(SKCanvas c, float cx, float cy, float r, float startAngle, float sweepAngle, float width, SKColor color, float glow = 0f)
    {
        var rect = new SKRect(cx - r, cy - r, cx + r, cy + r);
        Stroke.StrokeWidth = width;
        Stroke.StrokeCap = SKStrokeCap.Butt;
        if (glow > 0f)
        {
            Stroke.Color = color.WithAlpha(120);
            Stroke.MaskFilter = Blur(glow);
            c.DrawArc(rect, startAngle, sweepAngle, false, Stroke);
        }
        Stroke.Color = color;
        Stroke.MaskFilter = null;
        c.DrawArc(rect, startAngle, sweepAngle, false, Stroke);
    }

    public static void DrawLine(SKCanvas c, float x0, float y0, float x1, float y1, float width, SKColor color, float glow = 0f, SKStrokeCap cap = SKStrokeCap.Round)
    {
        Stroke.StrokeWidth = width;
        Stroke.StrokeCap = cap;
        if (glow > 0f)
        {
            Stroke.Color = color.WithAlpha(120);
            Stroke.MaskFilter = Blur(glow);
            c.DrawLine(x0, y0, x1, y1, Stroke);
        }
        Stroke.Color = color;
        Stroke.MaskFilter = null;
        c.DrawLine(x0, y0, x1, y1, Stroke);
    }

    public static void DrawPolyline(SKCanvas c, ReadOnlySpan<SKPoint> points, float width, SKColor color, float glow = 0f)
    {
        if (points.Length < 2)
        {
            return;
        }

        using var builder = new SKPathBuilder();
        builder.MoveTo(points[0]);
        for (var i = 1; i < points.Length; i++)
        {
            builder.LineTo(points[i]);
        }
        using var path = builder.Detach();

        Stroke.StrokeWidth = width;
        Stroke.StrokeCap = SKStrokeCap.Round;
        Stroke.StrokeJoin = SKStrokeJoin.Round;
        if (glow > 0f)
        {
            Stroke.Color = color.WithAlpha(110);
            Stroke.MaskFilter = Blur(glow);
            c.DrawPath(path, Stroke);
        }
        Stroke.Color = color;
        Stroke.MaskFilter = null;
        c.DrawPath(path, Stroke);
        Stroke.StrokeJoin = SKStrokeJoin.Miter;
    }

    public static void FillCircle(SKCanvas c, float cx, float cy, float r, SKColor color, float glow = 0f)
    {
        if (glow > 0f)
        {
            Fill.Color = color.WithAlpha(130);
            Fill.MaskFilter = Blur(glow);
            c.DrawCircle(cx, cy, r, Fill);
        }
        Fill.Color = color;
        Fill.MaskFilter = null;
        c.DrawCircle(cx, cy, r, Fill);
    }

    // Octagon panel with cut corners
    public static void DrawCutPanel(SKCanvas c, float x, float y, float w, float h, float cut, SKColor fill, SKColor border, float borderWidth = 1.5f)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(x + cut, y);
        builder.LineTo(x + w - cut, y);
        builder.LineTo(x + w, y + cut);
        builder.LineTo(x + w, y + h - cut);
        builder.LineTo(x + w - cut, y + h);
        builder.LineTo(x + cut, y + h);
        builder.LineTo(x, y + h - cut);
        builder.LineTo(x, y + cut);
        builder.Close();
        using var path = builder.Detach();

        Fill.Color = fill;
        Fill.MaskFilter = null;
        c.DrawPath(path, Fill);
        Stroke.Color = border;
        Stroke.StrokeWidth = borderWidth;
        Stroke.StrokeCap = SKStrokeCap.Butt;
        Stroke.MaskFilter = null;
        c.DrawPath(path, Stroke);
    }

    // --------------------------------------------------------------------------------
    // 7 segment
    // --------------------------------------------------------------------------------

    public static float SevenSegmentWidth(float h, int chars) => (chars * ((h * 0.58f) + (h * 0.17f))) - (h * 0.17f);

    public static void DrawSevenSegment(SKCanvas c, float x, float y, float h, string s, SKColor color)
    {
        var w = h * 0.58f;
        var gap = h * 0.17f;
        var thickness = h * 0.115f;

        c.Save();
        c.Translate(x + (h * 0.06f), y);
        c.Skew(-0.07f, 0f);

        var dx = 0f;
        foreach (var ch in s)
        {
            DrawSegments(c, dx, w, h, thickness, 0b1111111, color.WithAlpha(16), 0f);
            if (Segments.TryGetValue(ch, out var bits))
            {
                DrawSegments(c, dx, w, h, thickness, bits, color.WithAlpha(110), h * 0.075f);
                DrawSegments(c, dx, w, h, thickness, bits, color, 0f);
            }
            dx += w + gap;
        }
        c.Restore();
    }

    private static void DrawSegments(SKCanvas c, float x, float w, float h, float thickness, int bits, SKColor color, float glow)
    {
        Stroke.StrokeWidth = thickness;
        Stroke.StrokeCap = SKStrokeCap.Round;
        Stroke.Color = color;
        Stroke.MaskFilter = glow > 0f ? Blur(glow) : null;
        var m = thickness * 0.78f;
        var hy = h / 2f;
        if ((bits & 1) != 0)
        {
            c.DrawLine(x + m, 0, x + w - m, 0, Stroke);
        }
        if ((bits & 2) != 0)
        {
            c.DrawLine(x + w, m, x + w, hy - m, Stroke);
        }
        if ((bits & 4) != 0)
        {
            c.DrawLine(x + w, hy + m, x + w, h - m, Stroke);
        }
        if ((bits & 8) != 0)
        {
            c.DrawLine(x + m, h, x + w - m, h, Stroke);
        }
        if ((bits & 16) != 0)
        {
            c.DrawLine(x, hy + m, x, h - m, Stroke);
        }
        if ((bits & 32) != 0)
        {
            c.DrawLine(x, m, x, hy - m, Stroke);
        }
        if ((bits & 64) != 0)
        {
            c.DrawLine(x + m, hy, x + w - m, hy, Stroke);
        }
        Stroke.MaskFilter = null;
    }

    // --------------------------------------------------------------------------------
    // Mini gauge
    // --------------------------------------------------------------------------------

    public static void DrawMiniGauge(SKCanvas c, SKRect bounds, string label, string value, string unit, float frac, bool warn, SKColor accent, float t)
    {
        const float r = 40f;
        var flash = warn && (MathF.Sin(t * 12f) > -0.2f);
        var color = warn ? HudColors.Red : accent;
        var border = warn ? HudColors.Red.WithAlpha(flash ? (byte)200 : (byte)90) : HudColors.PanelLine;

        DrawCutPanel(c, bounds.Left, bounds.Top, bounds.Width, bounds.Height, 12f, HudColors.Panel.WithAlpha(215), border);
        DrawLine(c, bounds.Left + 5f, bounds.Top + 12f, bounds.Left + 5f, bounds.Top + 32f, 2.5f, color.WithAlpha(160), 0f, SKStrokeCap.Butt);
        DrawLine(c, bounds.Right - 5f, bounds.Bottom - 32f, bounds.Right - 5f, bounds.Bottom - 12f, 2.5f, color.WithAlpha(160), 0f, SKStrokeCap.Butt);

        var cx = bounds.MidX;
        var cy = bounds.MidY + 24f;
        DrawText(c, label, cx, bounds.Top + 24f, 15f, HudColors.Dim);

        frac = Math.Clamp(frac, 0f, 1f);
        DrawArc(c, cx, cy, r, 180f, 180f, 6f, HudColors.PanelLine.WithAlpha(140));
        if (frac > 0.005f)
        {
            DrawArc(c, cx, cy, r, 180f, 180f * frac, 6f, flash ? HudColors.Red : color, 5f);
        }

        var p = Polar(cx, cy, r, 180f + (180f * frac));
        FillCircle(c, p.X, p.Y, 3.8f, HudColors.White, 4f);

        for (var i = 0; i <= 4; i++)
        {
            var p0 = Polar(cx, cy, r + 7f, 180f + (45f * i));
            var p1 = Polar(cx, cy, r + 12f, 180f + (45f * i));
            DrawLine(c, p0.X, p0.Y, p1.X, p1.Y, 1.6f, HudColors.Dim.WithAlpha(150), 0f, SKStrokeCap.Butt);
        }

        DrawGlowText(c, value, cx, cy + 4f, 27f, flash ? HudColors.Red : HudColors.White, SKTextAlign.Center, MonoFace, 4f);
        DrawText(c, unit, cx, cy + 22f, 13f, HudColors.Dim);
    }
}
