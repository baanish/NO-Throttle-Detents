using System;

namespace NuclearOptionDetents.Core;

/// <summary>Detects when another patch is actively publishing throttle outside vanilla's relative accumulator.</summary>
public static class ThrottleOutputOwnership
{
    private const double AlignmentTolerance = 0.001;

    public static bool IsForeignControlActive(
        bool foreignThrottlePatchPresent,
        double publicThrottle,
        double simulatedThrottle,
        SimulatedThrottleRange range,
        bool invertOutput = false)
    {
        if (!foreignThrottlePatchPresent ||
            double.IsNaN(publicThrottle) || double.IsInfinity(publicThrottle) ||
            double.IsNaN(simulatedThrottle) || double.IsInfinity(simulatedThrottle))
        {
            return false;
        }

        var expected = SimulatedThrottleMapping.ToPublic(simulatedThrottle, range);
        if (invertOutput)
        {
            expected = 1 - expected;
        }

        return Math.Abs(publicThrottle - expected) > AlignmentTolerance;
    }
}
