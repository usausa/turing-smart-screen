namespace LcdDriver.TrofeoVisionLy;

public readonly record struct DeviceInfo(
    PanelType Pm,
    byte Sub)
{
    public RotateOption RotateOption => Pm switch
    {
        // 1920x462 (9.16 inch) / 1920x440 (11.3 inch)
        PanelType.TrofeoVision916 or PanelType.TrofeoVision113 => Sub is 2 or 4 ? RotateOption.None : RotateOption.Rotate180,
        // 1280x480
        PanelType.TrofeoVision686 => Sub == 2 ? RotateOption.Rotate90 : RotateOption.None,
        _ => RotateOption.None
    };
}
