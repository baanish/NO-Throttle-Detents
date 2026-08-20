using System;
using NuclearOptionDetents.Compatibility;
using NuclearOptionDetents.Core;

namespace NuclearOptionDetents.Patches;

internal static class SplitAirbrakePatch
{
    public static void Prefix(ControlSurface __instance, out ThrottleRestoreState __state)
    {
        __state = default;
        try
        {
            if (!RuntimeController.ShouldInspectSplitAirbrake)
            {
                return;
            }

            var inputsAccessor = RuntimeCompatibility.ControlSurfaceInputs;
            var maxSplitAccessor = RuntimeCompatibility.ControlSurfaceMaxSplit;
            if (inputsAccessor is null || maxSplitAccessor is null || maxSplitAccessor(__instance) <= 0f)
            {
                return;
            }

            var inputs = inputsAccessor(__instance);
            if (ReferenceEquals(inputs, null))
            {
                return;
            }

            var originalThrottle = inputs.throttle;
            RuntimeController.ObserveSplitAirbrake(__instance);
            if (originalThrottle != 0f)
            {
                return;
            }

            if (!RuntimeController.ShouldInhibitSplitAirbrake(__instance, inputs, originalThrottle))
            {
                return;
            }

            __state = new ThrottleRestoreState(inputs, originalThrottle);
            inputs.throttle = (float)ThrottleBoundaryHold.InwardOffset;
        }
        catch (Exception exception)
        {
            __state.Restore();
            RuntimeController.ReportPatchFailure("Split-airbrake", exception);
        }
    }

    public static void Postfix(ref ThrottleRestoreState __state) => __state.Restore();

    public static void Finalizer(ref ThrottleRestoreState __state) => __state.Restore();
}
