using System;
using NuclearOptionDetents.Compatibility;
using NuclearOptionDetents.Core;

namespace NuclearOptionDetents.Patches;

internal static class AirbrakePatch
{
    public static void Prefix(Airbrake __instance, out ThrottleRestoreState __state)
    {
        __state = default;
        try
        {
            if (!RuntimeController.ShouldInspectComponentAirbrake)
            {
                return;
            }

            var inputsAccessor = RuntimeCompatibility.AirbrakeControlInputs;
            if (inputsAccessor is null)
            {
                return;
            }

            var inputs = inputsAccessor(__instance);
            if (ReferenceEquals(inputs, null))
            {
                return;
            }

            var originalThrottle = inputs.throttle;
            RuntimeController.ObserveAirbrake(__instance);
            if (originalThrottle != 0f)
            {
                return;
            }

            if (!RuntimeController.ShouldInhibitAirbrake(__instance, inputs, originalThrottle))
            {
                return;
            }

            __state = new ThrottleRestoreState(inputs, originalThrottle);
            inputs.throttle = (float)ThrottleBoundaryHold.InwardOffset;
        }
        catch (Exception exception)
        {
            __state.Restore();
            RuntimeController.ReportPatchFailure("Airbrake", exception);
        }
    }

    public static void Postfix(ref ThrottleRestoreState __state) => __state.Restore();

    public static void Finalizer(ref ThrottleRestoreState __state) => __state.Restore();
}

internal struct ThrottleRestoreState
{
    private readonly ControlInputs? _inputs;
    private readonly float _originalThrottle;
    private bool _restored;

    public ThrottleRestoreState(ControlInputs inputs, float originalThrottle)
    {
        _inputs = inputs;
        _originalThrottle = originalThrottle;
        _restored = false;
    }

    public void Restore()
    {
        if (_restored || ReferenceEquals(_inputs, null))
        {
            return;
        }

        _inputs.throttle = _originalThrottle;
        _restored = true;
    }
}
