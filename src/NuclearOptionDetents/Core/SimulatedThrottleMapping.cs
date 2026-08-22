using System;

namespace NuclearOptionDetents.Core;

public enum SimulatedThrottleRange
{
    ZeroToOne = 0,
    NegativeOneToOne = 1,
}

public static class SimulatedThrottleMapping
{
    public static double ToPublic(double simulatedThrottle, SimulatedThrottleRange range) =>
        ClampPublic(range == SimulatedThrottleRange.NegativeOneToOne
            ? 0.5 * (simulatedThrottle + 1)
            : simulatedThrottle);

    public static double ToSimulated(double publicThrottle, SimulatedThrottleRange range)
    {
        var normalized = ClampPublic(publicThrottle);
        return range == SimulatedThrottleRange.NegativeOneToOne
            ? (normalized * 2) - 1
            : normalized;
    }

    // Vanilla's private accumulator can travel to -1 even when public throttle
    // uses the zero-to-one mapping and clamps that negative travel to idle.
    public static double ClampSimulated(double value)
    {
        if (!IsFinite(value))
        {
            return -1;
        }

        return Math.Max(-1, Math.Min(1, value));
    }

    public static double ClampPublic(double value)
    {
        if (!IsFinite(value))
        {
            return 0;
        }

        return Math.Max(0, Math.Min(1, value));
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
