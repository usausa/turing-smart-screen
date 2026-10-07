namespace LcdDriver.TrofeoVision;

public readonly record struct DeviceInfo(
    PanelType Pm,
    byte Sub)
{
    public RotateOption GetRotateOption(ScreenOrientation orientation)
    {
        var rotate = Pm switch
        {
            // 1280x480
            PanelType.TrofeoVision686 => Sub == 2 ? RotateOption.Rotate90 : RotateOption.None,
            _ => RotateOption.None
        };
        return (RotateOption)(((int)rotate - (int)orientation + 4) % 4);
    }
}
