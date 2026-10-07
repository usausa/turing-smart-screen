namespace LcdDriver.TrofeoVision;

public readonly record struct DeviceInfo(
    PanelType Pm,
    byte Sub)
{
    public RotateOption RotateOption => Pm switch
    {
        // 1280x480
        PanelType.TrofeoVision686 => Sub == 2 ? RotateOption.Rotate90 : RotateOption.None,
        _ => RotateOption.None
    };
}
