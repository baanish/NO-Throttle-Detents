using System.Reflection;
using HarmonyLib;
using NuclearOptionDetents.Core;

namespace NuclearOptionDetents.Patches;

internal static class ThrottleInputPatch
{
    private const string PauelsThrottlePatchType = "PRF.Fixes.ThrottleRelativeVelocity";
    private const string PauelsThrottlePatchMethod = "ThrottleAxis1ControlsReplacer";

    private static bool _originalRanLastCall = true;
    private static bool _externalUsesSignedMapping;

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

    private static bool HasPauelsThrottlePatch(MethodBase method)
    {
        var patchInfo = Harmony.GetPatchInfo(method);
        if (patchInfo is null)
        {
            return false;
        }

        foreach (var patch in patchInfo.Prefixes)
        {
            var patchMethod = patch.PatchMethod;
            if (patchMethod.Name == PauelsThrottlePatchMethod &&
                patchMethod.DeclaringType?.FullName == PauelsThrottlePatchType)
            {
                return true;
            }
        }

        return false;
    }
}

internal static class PilotStateLeavePatch
{
    public static void Prefix(PilotPlayerState __instance) =>
        RuntimeController.ResetIfLocalState(__instance);
}

internal static class PlayerControlsPatch
{
    public static void Postfix(PilotPlayerState __instance) =>
        RuntimeController.ObserveControlFrame(__instance);
}
