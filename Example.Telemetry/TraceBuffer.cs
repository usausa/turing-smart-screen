namespace Example.Telemetry;

internal sealed class TraceBuffer
{
    public const float Interval = 0.05f;
    public const int Capacity = 240;

    private readonly float[] speed = new float[Capacity];
    private readonly float[] rpm = new float[Capacity];
    private readonly float[] throttle = new float[Capacity];
    private readonly float[] brake = new float[Capacity];

    private int head;

    public int Count { get; private set; }

    public void Add(VehicleSimulator simulator)
    {
        speed[head] = simulator.Speed / VehicleSimulator.TopSpeed;
        rpm[head] = simulator.Rpm / VehicleSimulator.MaxRpm;
        throttle[head] = simulator.Throttle;
        brake[head] = simulator.Brake;
        head = (head + 1) % Capacity;
        Count = Math.Min(Count + 1, Capacity);
    }

    public float Speed(int index) => Get(speed, index);

    public float Rpm(int index) => Get(rpm, index);

    public float Throttle(int index) => Get(throttle, index);

    public float Brake(int index) => Get(brake, index);

    private float Get(float[] series, int index) => series[(head - Count + index + Capacity) % Capacity];
}
