using BepInEx;
using NuclearOptionDetents.Config;
using NuclearOptionDetents.Core;
using NuclearOptionDetents.Patches;
using NuclearOptionDetents.UI;
using UnityEngine.SceneManagement;

namespace NuclearOptionDetents;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.baanish.nuclearoption.detents";
    public const string PluginName = "Nuclear Option Detents";
    public const string PluginVersion = "0.2.0";

    private PatchInstaller? _patchInstaller;
    private DetentHudIndicator? _hudIndicator;
    private bool _hudFailureLogged;

    private void Awake()
    {
        var modConfig = new ModConfig(Config);
        RuntimeController.Initialize(modConfig, Logger);
        _hudIndicator = new DetentHudIndicator();
        if (!_hudIndicator.IsAvailable)
        {
            Logger.LogWarning("HUD indicator unavailable: ThrottleGauge.throttleLabel was not found with the expected TextMeshProUGUI type.");
        }
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
        _hudIndicator?.Reset();
        RuntimeController.ResetAll("plugin unload");
        _patchInstaller?.Uninstall();
    }

    private void LateUpdate()
    {
        try
        {
            _hudIndicator?.Render(RuntimeController.IndicatorSnapshot);
        }
        catch (System.Exception exception)
        {
            _hudIndicator?.Reset();
            if (!_hudFailureLogged)
            {
                Logger.LogWarning($"HUD indicator unavailable: {exception.Message}");
                _hudFailureLogged = true;
            }
        }
    }

    private void HandleActiveSceneChanged(Scene previous, Scene current)
    {
        _hudIndicator?.Reset();
        RuntimeController.ResetAll($"scene changed from '{previous.name}' to '{current.name}'");
    }
}
