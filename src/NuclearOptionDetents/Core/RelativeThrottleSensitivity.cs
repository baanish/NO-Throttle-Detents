using System;

namespace NuclearOptionDetents.Core;

public static class RelativeThrottleSensitivity
{
    private const double VanillaInputDeadzone = 0.1;
    private const double ObservationTolerance = 0.0001;

    public static bool ShouldApply(
        bool enabled,
        bool relativeThrottleMode,
        bool detentedAircraft,
        bool controlsEnabled,
        bool paused,
        bool axisModifierHeld,
        bool externalIntegratorActive,
        bool hasPreviousValue) =>
        enabled && relativeThrottleMode && detentedAircraft && controlsEnabled &&
        !paused && !axisModifierHeld && !externalIntegratorActive && hasPreviousValue;

    public static double Apply(
        double previousSimulatedThrottle,
        double observedSimulatedThrottle,
        double rawThrottle,
        double deltaTime,
        double multiplier,
        bool enabled)
    {
        if (!enabled || multiplier == 1 ||
            double.IsNaN(previousSimulatedThrottle) || double.IsInfinity(previousSimulatedThrottle) ||
            double.IsNaN(observedSimulatedThrottle) || double.IsInfinity(observedSimulatedThrottle) ||
            double.IsNaN(rawThrottle) || double.IsInfinity(rawThrottle) ||
            double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime <= 0 ||
            double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier <= 0)
        {
            return observedSimulatedThrottle;
        }

        if (Math.Abs(rawThrottle) <= VanillaInputDeadzone)
        {
            return observedSimulatedThrottle;
        }

        var target = Math.Sign(rawThrottle);
        var expectedVanillaStep = ClampStep(target - previousSimulatedThrottle, deltaTime);
        var expectedObserved = previousSimulatedThrottle + expectedVanillaStep;
        if (Math.Abs(observedSimulatedThrottle - expectedObserved) > ObservationTolerance)
        {
            return observedSimulatedThrottle;
        }

        var scaledStep = ClampStep(target - previousSimulatedThrottle, deltaTime * multiplier);
        var scaled = previousSimulatedThrottle + scaledStep;
        return SimulatedThrottleMapping.ClampSimulated(scaled);
    }

    private static double ClampStep(double value, double maximumMagnitude) =>
        Math.Max(-maximumMagnitude, Math.Min(maximumMagnitude, value));
}
