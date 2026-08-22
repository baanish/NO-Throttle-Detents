using System;

namespace NuclearOptionDetents.Core;

/// <summary>
/// Scales vanilla's relative-throttle integration on detented aircraft. Vanilla steps its private
/// accumulator by deltaTime toward the axis direction each frame; this recomputes that step with a
/// multiplied deltaTime, and only when it can prove vanilla just took the step it expected.
/// </summary>
public static class RelativeThrottleSensitivity
{
    // Vanilla ignores axis input below this magnitude; matching it keeps a resting stick vanilla.
    private const double VanillaInputDeadzone = 0.1;
    // Slack for the float accumulator round-tripping through double arithmetic.
    private const double ObservationTolerance = 0.0001;

    /// <summary>
    /// True only for the local pilot's relative-throttle input on an aircraft with a live detent,
    /// with controls active, no axis modifier, no foreign integrator, and a previous frame to step from.
    /// Aircraft without a detent stay vanilla so this mod does not become a global sensitivity mod.
    /// </summary>
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

    /// <summary>
    /// Returns the observed accumulator unchanged unless every input is finite, the axis is outside the
    /// vanilla deadzone, and the observed value matches the step vanilla should have taken. That match is
    /// the safety check: if another mod or a game update moved the accumulator differently, the frame is
    /// left alone instead of fighting over it.
    /// </summary>
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
