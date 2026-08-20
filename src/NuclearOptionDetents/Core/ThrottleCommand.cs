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
    public static ThrottleCommand FromRawAxis(double rawAxis, double threshold, bool reverseDirection = false)
    {
        if (rawAxis >= threshold)
        {
            return reverseDirection ? ThrottleCommand.Decrease : ThrottleCommand.Increase;
        }

        if (rawAxis <= -threshold)
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
