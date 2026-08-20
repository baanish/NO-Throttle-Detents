namespace NuclearOptionDetents.Core;

internal static class ComponentGatePolicy
{
    // Update and FixedUpdate can observe the same input at different points in a
    // frame. Simulation time tolerates that ordering without coupling safety to FPS.
    public const double MaximumObserverAgeSeconds = DetentTiming.MaximumObservationGapSeconds;

    public static bool AllowsBlock(
        bool controlsEnabled,
        bool paused,
        bool axisModifierHeld,
        ThrottleCommand command,
        DetentDirection direction,
        double observerAgeSeconds)
    {
        if (!controlsEnabled || paused || axisModifierHeld)
        {
            return false;
        }

        if (ThrottleCommands.IsOppositeDirection(command, direction))
        {
            return false;
        }

        var observerFresh = observerAgeSeconds >= 0 &&
                            observerAgeSeconds <= MaximumObserverAgeSeconds;
        return observerFresh;
    }
}
