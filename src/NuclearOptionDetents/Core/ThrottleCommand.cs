namespace NuclearOptionDetents.Core;

/// <summary>The outward detent intent observed for one simulation update.</summary>
public enum ThrottleCommand
{
    Neutral = 0,
    Increase = 1,
    Decrease = 2,
}

/// <summary>Converts Rewired's raw Increase/Decrease input into detent intent.</summary>
public static class ThrottleCommands
{
    /// <summary>
    /// The game's own throttle input deadzone, compared as a float like the game does. Detents only run for
    /// keys and buttons, which read 0 or +-1, so no separate command threshold is needed.
    /// </summary>
    public const float VanillaInputDeadzone = 0.1f;

    public static ThrottleCommand FromRawAxis(double rawAxis, bool reverseDirection = false)
    {
        if (rawAxis > VanillaInputDeadzone)
        {
            return reverseDirection ? ThrottleCommand.Decrease : ThrottleCommand.Increase;
        }

        if (rawAxis < -VanillaInputDeadzone)
        {
            return reverseDirection ? ThrottleCommand.Increase : ThrottleCommand.Decrease;
        }

        return ThrottleCommand.Neutral;
    }

    public static bool IsDirection(ThrottleCommand command, DetentDirection direction)
    {
        return direction == DetentDirection.Lower
            ? command == ThrottleCommand.Decrease
            : command == ThrottleCommand.Increase;
    }

    public static bool IsOppositeDirection(ThrottleCommand command, DetentDirection direction)
    {
        return direction == DetentDirection.Lower
            ? command == ThrottleCommand.Increase
            : command == ThrottleCommand.Decrease;
    }
}
