namespace LcdDriver.TuringSmartScreen;

/// <summary>
/// Storage capacity information returned by the device (command ID 100).
/// All values are in kilobytes (KB).
/// </summary>
/// <param name="Total">Total storage capacity in KB.</param>
/// <param name="Used">Used storage in KB.</param>
/// <param name="Valid">Available (free) storage in KB.</param>
public readonly record struct CapacityInfo(uint Total, uint Used, uint Valid);

