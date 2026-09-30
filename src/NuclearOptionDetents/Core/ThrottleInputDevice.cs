namespace NuclearOptionDetents.Core;

/// <summary>
/// The last kind of input seen driving the local player's Throttle action. Detents run only for
/// <see cref="Digital"/>: the game ramps a key or button throttle the same way whether or not Use Throttle
/// Relative Axis is on, while an analog axis (HOTAS lever, slider, stick) is not supported and stays vanilla.
/// <see cref="Unknown"/> until the first input, which is vanilla too.
/// </summary>
internal enum ThrottleInputDevice
{
    Unknown,
    Digital,
    Analog,
}
