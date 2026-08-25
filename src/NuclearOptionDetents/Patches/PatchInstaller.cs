using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOptionDetents.Compatibility;

namespace NuclearOptionDetents.Patches;

internal sealed class PatchInstaller
{
    internal const string HarmonyId = "com.baanish.nuclearoption.detents";
    private readonly Harmony _harmony = new(HarmonyId);
    private readonly ManualLogSource _log;

    public PatchInstaller(ManualLogSource log)
    {
        _log = log;
    }

    public FeatureStatus ThrottleObserver { get; private set; }

    public void Install()
    {
        ThrottleObserver = InstallThrottleObserver();
    }

    public void Uninstall()
    {
        _harmony.UnpatchSelf();
    }

    private FeatureStatus InstallThrottleObserver()
    {
        var patched = new List<MethodBase>();
        try
        {
            RuntimeCompatibility.ResolveThrottleObserverFields();
            ResolveOptionalCapability("airbrake", RuntimeCompatibility.ResolveAirbrakeFields);
            ResolveOptionalCapability("split airbrake", RuntimeCompatibility.ResolveSplitAirbrakeFields);
            ResolveOptionalCapability("afterburner", RuntimeCompatibility.ResolveJetNozzleFields);
            var target = ResolveThrottleTarget();
            var postfixMethod = AccessTools.DeclaredMethod(typeof(ThrottleInputPatch), nameof(ThrottleInputPatch.Postfix)) ??
                                throw new MissingMethodException(nameof(ThrottleInputPatch), nameof(ThrottleInputPatch.Postfix));
            _harmony.Patch(target, postfix: new HarmonyMethod(postfixMethod) { priority = Priority.Last });
            patched.Add(target);

            var controlFrameTarget = AccessTools.DeclaredMethod(
                                         typeof(PilotPlayerState),
                                         "PlayerControls",
                                         Type.EmptyTypes) ??
                                     throw new MissingMethodException(nameof(PilotPlayerState), "PlayerControls");
            var controlFramePostfix = AccessTools.DeclaredMethod(
                                          typeof(PlayerControlsPatch),
                                          nameof(PlayerControlsPatch.Postfix)) ??
                                      throw new MissingMethodException(nameof(PlayerControlsPatch), nameof(PlayerControlsPatch.Postfix));
            _harmony.Patch(controlFrameTarget, postfix: new HarmonyMethod(controlFramePostfix) { priority = Priority.Last });
            patched.Add(controlFrameTarget);

            var leaveTarget = AccessTools.DeclaredMethod(typeof(PilotPlayerState), "LeaveState", Type.EmptyTypes);
            if (leaveTarget is not null)
            {
                var leavePrefix = AccessTools.DeclaredMethod(typeof(PilotStateLeavePatch), nameof(PilotStateLeavePatch.Prefix)) ??
                                  throw new MissingMethodException(nameof(PilotStateLeavePatch), nameof(PilotStateLeavePatch.Prefix));
                _harmony.Patch(leaveTarget, prefix: new HarmonyMethod(leavePrefix));
                patched.Add(leaveTarget);
            }
            else
            {
                _log.LogWarning("PilotPlayerState.LeaveState() was not found. Scene, identity, and observer-gap resets remain active.");
            }

            return FeatureStatus.Active(RuntimeCompatibility.FormatMethod(target));
        }
        catch (Exception exception)
        {
            RollBack(patched);
            return FeatureStatus.Unavailable(exception.Message);
        }
    }

    private void ResolveOptionalCapability(string name, Action resolver)
    {
        try
        {
            resolver();
        }
        catch (Exception exception)
        {
            _log.LogWarning($"{name} capability discovery unavailable: {exception.Message}");
        }
    }

    private static MethodBase ResolveThrottleTarget()
    {
        return AccessTools.DeclaredMethod(
                   typeof(PilotPlayerState),
                   "PlayerThrottleAxis1Controls",
                   Type.EmptyTypes)
               ?? throw new MissingMethodException(
                   nameof(PilotPlayerState),
                   "PlayerThrottleAxis1Controls");
    }

    private void RollBack(IEnumerable<MethodBase> methods)
    {
        foreach (var method in methods)
        {
            _harmony.Unpatch(method, HarmonyPatchType.All, HarmonyId);
        }
    }
}

internal readonly struct FeatureStatus
{
    private FeatureStatus(bool isActive, string? target, string? reason)
    {
        IsActive = isActive;
        Target = target;
        Reason = reason;
    }

    public bool IsActive { get; }
    public string? Target { get; }
    public string? Reason { get; }

    public static FeatureStatus Active(string target) => new(true, target, null);

    public static FeatureStatus Unavailable(string reason) => new(false, null, reason);

    public override string ToString() => IsActive ? $"active: {Target}" : $"unavailable: {Reason}";
}
