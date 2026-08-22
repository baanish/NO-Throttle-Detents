using System.Reflection;
using HarmonyLib;
using NuclearOptionDetents.Core;

namespace NuclearOptionDetents.Patches;

/// <summary>
/// Observes the local pilot's throttle after vanilla (or a replacement) has updated it. Running at
/// last priority means this sees the final value another mod produced instead of racing it.
/// </summary>
internal static class ThrottleInputPatch
{
    // The one external relative-throttle integrator known to keep vanilla's signed accumulator.
    private const string PauelsThrottlePatchType = "PRF.Fixes.ThrottleRelativeVelocity";
    private const string PauelsThrottlePatchMethod = "ThrottleAxis1ControlsReplacer";

    private static bool _originalRanLastCall = true;
    private static bool _externalUsesSignedMapping;

    /// <summary>
    /// Tells the runtime whether another mod skipped the original method this frame, and if so whether
    /// that mod still writes vanilla's signed accumulator. The patch lookup is cached until the original
    /// runs again, because reading Harmony's patch info every frame is not free.
    /// </summary>
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(
        PilotPlayerState __instance,
        MethodBase __originalMethod,
        bool __runOriginal)
    {
        if (__runOriginal)
        {
            _originalRanLastCall = true;
            _externalUsesSignedMapping = false;
        }
        else if (_originalRanLastCall)
        {
            _originalRanLastCall = false;
            _externalUsesSignedMapping = HasPauelsThrottlePatch(__originalMethod);
        }

        RuntimeController.ObserveThrottle(
            __instance,
            externalRelativeThrottleIntegrator: !__runOriginal,
            externalUsesSignedMapping: _externalUsesSignedMapping);
    }

    /// <summary>Identifies PauelsRandomFixes by patch type and method name; any other replacement is treated as an unknown mapping and disables the detents.</summary>
    private static bool HasPauelsThrottlePatch(MethodBase method)
    {
        var patchInfo = Harmony.GetPatchInfo(method);
        if (patchInfo is null)
        {
            return false;
        }

        if (patchInfo.Prefixes.Count != 1)
        {
            return false;
        }

        var patchMethod = patchInfo.Prefixes[0].PatchMethod;
        return patchMethod.Name == PauelsThrottlePatchMethod &&
               patchMethod.DeclaringType?.FullName == PauelsThrottlePatchType;
    }
}

/// <summary>Drops all cached local-player state when the pilot leaves the seat.</summary>
internal static class PilotStateLeavePatch
{
    public static void Prefix(PilotPlayerState __instance) =>
        RuntimeController.ResetIfLocalState(__instance);
}

/// <summary>Catches frames where the throttle observer did not run, so pending holds are cancelled when controls are interrupted.</summary>
internal static class PlayerControlsPatch
{
    public static void Postfix(PilotPlayerState __instance) =>
        RuntimeController.ObserveControlFrame(__instance);
}
