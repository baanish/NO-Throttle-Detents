using System.Reflection;
using HarmonyLib;
using NuclearOptionDetents.Core;
using UnityEngine;

namespace NuclearOptionDetents.Patches;

internal static class ThrottleInputPatch
{
    private const float PatchRefreshSeconds = 1f;
    private const string PauelsThrottlePatchType = "PRF.Fixes.ThrottleRelativeVelocity";
    private const string PauelsThrottlePatchMethod = "ThrottleAxis1ControlsReplacer";

    private static MethodBase? _observedMethod;
    private static float _nextPatchRefresh;
    private static bool _pauelsThrottlePatchActive;

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(PilotPlayerState __instance, MethodBase __originalMethod)
    {
        var now = Time.unscaledTime;
        if (!ReferenceEquals(_observedMethod, __originalMethod) || now >= _nextPatchRefresh)
        {
            _observedMethod = __originalMethod;
            _nextPatchRefresh = now + PatchRefreshSeconds;
            _pauelsThrottlePatchActive = HasPauelsThrottlePatch(__originalMethod);
        }

        RuntimeController.ObserveThrottle(__instance, _pauelsThrottlePatchActive);
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
