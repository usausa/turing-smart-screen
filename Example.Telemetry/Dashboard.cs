namespace Example.Telemetry;

using SkiaSharp;

internal sealed class Dashboard
{
    private const float StepTime = 1f / 60f;

    private readonly VehicleSimulator simulator = new();

    private readonly TraceBuffer trace = new();

    private float time;

    private float sampleTime;

    public Dashboard()
    {
        // Warm up so that the traces are filled
        Advance(43.7f);
    }

    public void Advance(float seconds)
    {
        while (seconds > 0f)
        {
            var step = MathF.Min(seconds, StepTime);
            simulator.Update(step);
            time += step;
            sampleTime += step;
            if (sampleTime >= TraceBuffer.Interval)
            {
                sampleTime -= TraceBuffer.Interval;
                trace.Add(simulator);
            }
            seconds -= step;
        }
    }

    public void Render(SKCanvas canvas) => HudRenderer.Render(canvas, simulator, trace, time);
}
