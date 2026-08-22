using System;

namespace NuclearOptionDetents.Core;

/// <summary>Range used to map vanilla's private throttle accumulator, selected from the player setting or a verified external integrator.</summary>
public enum SimulatedThrottleRange
{
    ZeroToOne = 0,
    NegativeOneToOne = 1,
}

/// <summary>The one place the public 0..1 throttle and vanilla's private accumulator are converted, so both ranges stay in sync.</summary>
public static class SimulatedThrottleMapping
{
    /// <summary>Accumulator to the 0..1 value gameplay reads; the result is clamped, so out-of-range input is safe.</summary>
    public static double ToPublic(double simulatedThrottle, SimulatedThrottleRange range) =>
        ClampPublic(range == SimulatedThrottleRange.NegativeOneToOne
            ? 0.5 * (simulatedThrottle + 1)
            : simulatedThrottle);

    /// <summary>Inverse of <see cref="ToPublic"/>; the public value is clamped first so the accumulator cannot be driven out of range.</summary>
    public static double ToSimulated(double publicThrottle, SimulatedThrottleRange range)
    {
        var normalized = ClampPublic(publicThrottle);
        return range == SimulatedThrottleRange.NegativeOneToOne
            ? (normalized * 2) - 1
            : normalized;
    }

    /// <summary>Clamps to the accumulator range; non-finite input falls back to idle rather than propagating NaN into vanilla.</summary>
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

    /// <summary>Clamps to the public range; non-finite input becomes idle.</summary>
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
