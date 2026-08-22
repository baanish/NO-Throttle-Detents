using System;

namespace NuclearOptionDetents.Core;

public enum SimulatedThrottleRange
{
    ZeroToOne = 0,
    NegativeOneToOne = 1,
}

public static class SimulatedThrottleMapping
{
    private const double ComparisonTolerance = 0.0001;

    public static SimulatedThrottleRange Resolve(
        double simulatedThrottle,
        double publicThrottle,
        SimulatedThrottleRange fallback)
    {
        if (!IsFinite(simulatedThrottle) || !IsFinite(publicThrottle))
        {
            return fallback;
        }

        var zeroRangeValid = simulatedThrottle >= -ComparisonTolerance &&
                             simulatedThrottle <= 1 + ComparisonTolerance;
        var signedRangeValid = simulatedThrottle >= -1 - ComparisonTolerance &&
                               simulatedThrottle <= 1 + ComparisonTolerance;
        if (!zeroRangeValid && signedRangeValid)
        {
            return SimulatedThrottleRange.NegativeOneToOne;
        }

        if (!signedRangeValid)
        {
            return fallback;
        }

        var output = Clamp01(publicThrottle);
        var zeroError = Math.Abs(Clamp01(simulatedThrottle) - output);
        var signedError = Math.Abs(Clamp01(0.5 * (simulatedThrottle + 1)) - output);
        if (zeroError + ComparisonTolerance < signedError)
        {
            return SimulatedThrottleRange.ZeroToOne;
        }

        if (signedError + ComparisonTolerance < zeroError)
        {
            return SimulatedThrottleRange.NegativeOneToOne;
        }

        return fallback;
    }

    public static double ToPublic(double simulatedThrottle, SimulatedThrottleRange range) =>
        Clamp01(range == SimulatedThrottleRange.NegativeOneToOne
            ? 0.5 * (simulatedThrottle + 1)
            : simulatedThrottle);

    public static double ToSimulated(double publicThrottle, SimulatedThrottleRange range)
    {
        var normalized = Clamp01(publicThrottle);
        return range == SimulatedThrottleRange.NegativeOneToOne
            ? (normalized * 2) - 1
            : normalized;
    }

    public static double ClampSimulated(double value, SimulatedThrottleRange range)
    {
        var minimum = range == SimulatedThrottleRange.NegativeOneToOne ? -1 : 0;
        if (!IsFinite(value))
        {
            return minimum;
        }

        return Math.Max(minimum, Math.Min(1, value));
    }

    private static double Clamp01(double value)
    {
        if (!IsFinite(value))
        {
            return 0;
        }

        return Math.Max(0, Math.Min(1, value));
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
