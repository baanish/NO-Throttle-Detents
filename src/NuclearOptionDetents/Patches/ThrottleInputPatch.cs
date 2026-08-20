using HarmonyLib;
using NuclearOptionDetents.Core;

namespace NuclearOptionDetents.Patches;

internal static class ThrottleInputPatch
{
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(PilotPlayerState __instance) =>
        RuntimeController.ObserveThrottle(__instance);
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
