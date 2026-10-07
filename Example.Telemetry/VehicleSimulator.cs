namespace Example.Telemetry;

internal sealed class VehicleSimulator
{
    public const float MaxRpm = 19000f;
    public const float RedlineRpm = 17000f;
    public const float ShiftRpm = 17800f;
    public const float IdleRpm = 1400f;
    public const int TopGear = 8;
    public const float TopSpeed = 680f;

    // Speed [km/h] = Rpm * SpeedPerRpm / Ratio
    private const float SpeedPerRpm = 0.028f;

    private static readonly float[] Ratios = [0f, 2.90f, 2.20f, 1.80f, 1.52f, 1.30f, 1.12f, 0.97f, 0.86f];

    // FL, FR, RL, RR
    private readonly float[] tyreTemps = [96f, 97f, 99f, 100f];

    private uint seed = 7;

    private Phase phase = Phase.Straight;
    private float phaseTime;
    private float phaseLength = 7f;
    private float cornerDirection = 1f;
    private float shiftCut;
    private float autoBoostTime;
    private float lapLength = 79.4f;

    private enum Phase
    {
        Straight,
        Brake,
        Corner
    }

    public float Rpm { get; private set; }

    public float Speed { get; private set; }

    public int Gear { get; private set; } = 1;

    public float Throttle { get; private set; }

    public float Brake { get; private set; }

    public float BoostCharge { get; private set; } = 1f;

    public bool BoostActive { get; private set; }

    public float TurboPressure { get; private set; }

    public float WaterTemp { get; private set; } = 84f;

    public float OilTemp { get; private set; } = 102f;

    public float Fuel { get; private set; } = 0.68f;

    public float ErsOutput { get; private set; }

    public float GLong { get; private set; }

    public float GLat { get; private set; }

    public int Lap { get; private set; } = 23;

    public int TotalLaps { get; } = 51;

    public int Position { get; } = 1;

    public float LapTime { get; private set; } = 21.348f;

    public float BestLap { get; private set; } = 77.892f;

    public float Delta { get; private set; }

    public float SessionTime { get; private set; }

    public ReadOnlySpan<float> TyreTemps => tyreTemps;

    public void Update(float dt)
    {
        SessionTime += dt;
        UpdateDriver(dt);
        UpdateLap(dt);

        // Boost
        BoostActive = (autoBoostTime > 0f) && (BoostCharge > 0.04f) && (Throttle > 0.25f);
        BoostCharge = BoostActive
            ? MathF.Max(0f, BoostCharge - (dt * 0.16f))
            : MathF.Min(1f, BoostCharge + (dt * (Throttle < 0.2f ? 0.085f : 0.045f)));

        // Longitudinal [km/h/s]
        var thrust = Throttle * (95f + (360f / Gear));
        if (BoostActive)
        {
            thrust *= 1.65f;
        }
        if (shiftCut > 0f)
        {
            thrust *= 0.15f;
            shiftCut -= dt;
        }
        var drag = (Speed * Speed * 0.00045f) + 9f;
        var accel = MathF.Min(thrust, 165f) - drag - (Brake * 175f);
        if ((Speed <= 0.01f) && (accel < 0f))
        {
            accel = 0f;
        }
        Speed = Math.Clamp(Speed + (accel * dt), 0f, TopSpeed);

        // RPM
        var rpmTarget = MathF.Max(IdleRpm + (Throttle * 900f), Speed * Ratios[Gear] / SpeedPerRpm);
        Rpm = Math.Clamp(Rpm + ((rpmTarget - Rpm) * MathF.Min(1f, dt * 14f)), 0f, MaxRpm);

        // Shift
        if ((Rpm > ShiftRpm) && (Gear < TopGear) && (Throttle > 0.4f))
        {
            Gear++;
            shiftCut = 0.13f;
        }
        else if ((Gear > 1) && (Rpm < 9000f) && ((Brake > 0.2f) || (Throttle < 0.15f)) && ((Speed * Ratios[Gear - 1] / SpeedPerRpm) < 15500f))
        {
            Gear--;
            shiftCut = 0.10f;
        }

        // Turbo
        var boostTarget = 0.25f + (Throttle * 1.45f) + (BoostActive ? 0.95f : 0f) - (shiftCut > 0f ? 0.8f : 0f);
        TurboPressure = MathF.Max(0f, TurboPressure + ((boostTarget - TurboPressure) * MathF.Min(1f, dt * 5f)));

        // ERS (negative while regenerating)
        var ersTarget = BoostActive ? 280f : (Throttle * 120f) - (Brake * 90f);
        ErsOutput += (ersTarget - ErsOutput) * MathF.Min(1f, dt * 4f);

        // G
        GLong += ((accel / 35.3f) - GLong) * MathF.Min(1f, dt * 6f);
        var latTarget = phase == Phase.Corner
            ? cornerDirection * (2.2f + (1.4f * MathF.Sin(phaseTime / phaseLength * MathF.PI))) * MathF.Min(1f, Speed / 200f)
            : 0f;
        GLat += (latTarget - GLat) * MathF.Min(1f, dt * 5f);

        // Temperature and fuel
        var load = (Throttle * 0.7f) + (BoostActive ? 0.55f : 0f);
        WaterTemp += (82f + (load * 17f) + (Speed * 0.013f) - WaterTemp) * dt * 0.05f;
        OilTemp += (95f + (load * 36f) + (Speed * 0.01f) - OilTemp) * dt * 0.04f;
        Fuel -= dt * (0.00022f + (0.0012f * Throttle) + (BoostActive ? 0.0015f : 0f));
        if (Fuel < 0.04f)
        {
            Fuel = 0.95f;
        }

        // Tyres (front heats under braking, rear under traction, outer side in corners)
        for (var i = 0; i < tyreTemps.Length; i++)
        {
            var front = i < 2;
            var outer = (i % 2) == 0 ? GLat > 0f : GLat < 0f;
            var target = 90f + (Throttle * (front ? 4f : 11f)) + (Brake * (front ? 16f : 6f)) + (MathF.Abs(GLat) * (outer ? 6f : 2.5f)) + (Speed * 0.008f);
            tyreTemps[i] += (target - tyreTemps[i]) * dt * 0.35f;
        }

        Delta = -0.214f + (0.16f * MathF.Sin(SessionTime * 0.21f)) + (0.04f * MathF.Sin(SessionTime * 1.3f));
    }

    private void UpdateDriver(float dt)
    {
        phaseTime += dt;
        switch (phase)
        {
            case Phase.Straight:
                Throttle = MathF.Min(1f, Throttle + (dt * 2.5f));
                Brake = 0f;
                if ((autoBoostTime <= 0f) && (phaseTime > 3.5f) && (BoostCharge > 0.75f) && (Gear >= 6))
                {
                    autoBoostTime = 2.6f;
                }
                if (phaseTime > phaseLength)
                {
                    Next(Phase.Brake, 1.1f + (NextRandom() * 0.5f));
                }
                break;

            case Phase.Brake:
                Throttle = 0f;
                Brake = MathF.Min(1f, Brake + (dt * 6f));
                if ((phaseTime > phaseLength) || (Speed < 130f))
                {
                    Next(Phase.Corner, 1.8f + (NextRandom() * 1.4f));
                }
                break;

            case Phase.Corner:
                Brake = MathF.Max(0f, Brake - (dt * 4f));
                Throttle = 0.45f + (0.1f * MathF.Sin(SessionTime * 3f));
                if (phaseTime > phaseLength)
                {
                    Next(Phase.Straight, 5f + (NextRandom() * 3f));
                }
                break;
        }

        if (autoBoostTime > 0f)
        {
            autoBoostTime -= dt;
        }
    }

    private void Next(Phase nextPhase, float length)
    {
        phase = nextPhase;
        phaseTime = 0f;
        phaseLength = length;
        if (nextPhase == Phase.Corner)
        {
            cornerDirection = NextRandom() < 0.5f ? -1f : 1f;
        }
    }

    private void UpdateLap(float dt)
    {
        LapTime += dt;
        if (LapTime < lapLength)
        {
            return;
        }

        BestLap = MathF.Min(BestLap, LapTime);
        LapTime = 0f;
        Lap = (Lap % TotalLaps) + 1;
        lapLength = 76f + (NextRandom() * 6f);
    }

    // Xorshift (deterministic demo sequence)
    private float NextRandom()
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return (seed & 0xFFFFFF) / 16777216f;
    }
}
