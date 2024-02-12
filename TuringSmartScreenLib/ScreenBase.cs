namespace TuringSmartScreenLib;

internal abstract class ScreenBase : IScreen
{
    private readonly int width;

    private readonly int height;

    private ScreenOrientation orientation = ScreenOrientation.Portrait;

    public int Width
    {
        get
        {
            if ((orientation == ScreenOrientation.Portrait) || (orientation == ScreenOrientation.ReversePortrait))
            {
                return width;
            }
            return height;
        }
    }

    public int Height
    {
        get
        {
            if ((orientation == ScreenOrientation.Portrait) || (orientation == ScreenOrientation.ReversePortrait))
            {
                return height;
            }
            return width;
        }
    }

    public ScreenOrientation Orientation
    {
        get => orientation;
        set
        {
            if (SetOrientation(value))
            {
                orientation = value;
            }
        }
    }

    protected ScreenBase(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    protected abstract bool SetOrientation(ScreenOrientation orientation);

    public abstract void Dispose();

    public abstract void Reset();

    public abstract void Clear();

    public abstract void ScreenOff();

    public abstract void ScreenOn();

    public abstract void SetBrightness(byte level);

    public abstract IScreenBuffer CreateBuffer(int width, int height);

    public abstract void DisplayBuffer(int x, int y, IScreenBuffer buffer);
}
