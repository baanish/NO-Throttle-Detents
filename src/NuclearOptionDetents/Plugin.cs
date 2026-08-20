using BepInEx;
using NuclearOptionDetents.Config;
using NuclearOptionDetents.Core;
using NuclearOptionDetents.Patches;
using UnityEngine.SceneManagement;

namespace NuclearOptionDetents;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.baanish.nuclearoption.detents";
    public const string PluginName = "Nuclear Option Detents";
    public const string PluginVersion = "0.1.0";

    private PatchInstaller? _patchInstaller;
    private void Awake()
    {
        var modConfig = new ModConfig(Config);
        RuntimeController.Initialize(modConfig, Logger);
        _patchInstaller = new PatchInstaller(Logger);
        _patchInstaller.Install();
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;

        RuntimeController.SetPatchStatus(
            _patchInstaller.ThrottleObserver.IsActive,
            _patchInstaller.AirbrakeComponentGate.IsActive,
            _patchInstaller.SplitAirbrakeGate.IsActive,
            _patchInstaller.AfterburnerGate.IsActive);
        Logger.LogInfo(
            $"{PluginName} {PluginVersion} loaded. " +
            $"Throttle={_patchInstaller.ThrottleObserver}; airbrake={_patchInstaller.AirbrakeComponentGate}; " +
            $"split airbrake={_patchInstaller.SplitAirbrakeGate}; afterburner={_patchInstaller.AfterburnerGate}.");
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        RuntimeController.ResetAll("plugin unload");
        _patchInstaller?.Uninstall();
    }

    private static void HandleActiveSceneChanged(Scene previous, Scene current) =>
        RuntimeController.ResetAll($"scene changed from '{previous.name}' to '{current.name}'");
}
