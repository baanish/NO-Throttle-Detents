using System;

namespace NuclearOptionDetents.Core;

public static class RelativeThrottleSensitivity
{
    public static bool ShouldApply(
        bool enabled,
        bool relativeThrottleMode,
        bool detentedAircraft,
        bool controlsEnabled,
        bool paused,
        bool axisModifierHeld,
        bool inputActive,
        bool externalIntegratorActive,
        bool hasPreviousValue) =>
        enabled && relativeThrottleMode && detentedAircraft && controlsEnabled &&
        !paused && !axisModifierHeld && inputActive &&
        !externalIntegratorActive && hasPreviousValue;
    public static double Apply(
        double previousSimulatedThrottle,
        double observedSimulatedThrottle,
        double inputDelta,
        double multiplier,
        SimulatedThrottleRange range,
        bool enabled)
    {
        if (!enabled || multiplier == 1 ||
            double.IsNaN(previousSimulatedThrottle) || double.IsInfinity(previousSimulatedThrottle) ||
            double.IsNaN(observedSimulatedThrottle) || double.IsInfinity(observedSimulatedThrottle) ||
            double.IsNaN(inputDelta) || double.IsInfinity(inputDelta) ||
            double.IsNaN(multiplier) || double.IsInfinity(multiplier))
        {
            return observedSimulatedThrottle;
        }

        var scaled = previousSimulatedThrottle + (inputDelta * multiplier);
        return SimulatedThrottleMapping.ClampSimulated(scaled, range);
    }
}
