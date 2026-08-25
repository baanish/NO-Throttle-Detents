using System;
using BepInEx.Configuration;
using NuclearOptionDetents.Core;
using UnityEngine;

namespace NuclearOptionDetents.Config;

/// <summary>Binds the BepInEx config entries and reduces them to an <see cref="EffectiveSettings"/> snapshot the runtime can compare.</summary>
internal sealed class ModConfig
{
    private const string StatusSection = "Status";
    private const string GeneralSection = "General";
    private const string IndicatorSection = "Indicator";
    private const string SensitivitySection = "Throttle Sensitivity";
    private const string IdleSection = "Idle / Airbrake Detent";
    private const string AfterburnerSection = "Full Dry / Afterburner Detent";
    private const string AdvancedSection = "Advanced";

    public ModConfig(ConfigFile config)
    {
        RuntimeStatus = config.Bind(
            StatusSection,
            "RuntimeStatus",
            "Open the in-game Configuration Manager to see the live check.",
            Describe(
                "Best-effort compatibility check for the current aircraft; LIKELY is not an in-flight test.",
                "Current Aircraft",
                100,
                customDrawer: DrawRuntimeStatus,
                readOnly: true,
                hideDefaultButton: true));
        Enabled = config.Bind(
            GeneralSection,
            "Enabled",
            true,
            Describe(
                "Enables all detents; off restores Nuclear Option's normal behavior.",
                "Enable Mod",
                100));
        DebugLogging = config.Bind(
            GeneralSection,
            "DebugLogging",
            false,
            Describe(
                "Logs aircraft attachment and lifecycle resets to BepInEx\\LogOutput.log.",
                "Debug Logging",
                90));
        NetworkValidation = config.Bind(
            GeneralSection,
            "NetworkValidation",
            false,
            Describe(
                "With Debug Logging, samples the local aircraft and one selected remote for multiplayer checks.",
                "Network Validation",
                80));
        NetworkValidationOwner = config.Bind(
            GeneralSection,
            "NetworkValidationOwner",
            -1,
            Describe(
                "Remote session player index; -1 samples only the local aircraft and lists available owners.",
                "Network Validation Owner",
                70,
                new AcceptableValueRange<int>(-1, 255)));
        IndicatorEnabled = config.Bind(
            IndicatorSection,
            "Enabled",
            true,
            Describe(
                "Shows the active detent below the HUD throttle gauge.",
                "Show Detent Indicator",
                100));
        ThrottleSensitivity = config.Bind(
            SensitivitySection,
            "Multiplier",
            1f,
            Describe(
                "Scales relative throttle on aircraft with a supported detent. Use PauelsRandomFixes for other aircraft.",
                "Throttle Sensitivity",
                100,
                new AcceptableValueRange<float>(0.25f, 4f)));
        IdleEnabled = config.Bind(
            IdleSection,
            "Enabled",
            true,
            Describe(
                "Holds at idle before airbrake; off restores normal idle behavior.",
                "Enable Idle Detent",
                100));
        IdleHoldMilliseconds = config.Bind(
            IdleSection,
            "HoldMilliseconds",
            200,
            Describe(
                "Continuous Decrease Throttle hold required at idle; releasing early resets it.",
                "Idle Hold Time (ms)",
                90,
                new AcceptableValueRange<int>(0, 2000)));
        AfterburnerEnabled = config.Bind(
            AfterburnerSection,
            "Enabled",
            true,
            Describe(
                "Holds at this aircraft's captured full-dry boundary before afterburner.",
                "Enable Afterburner Detent",
                100));
        AfterburnerHoldMilliseconds = config.Bind(
            AfterburnerSection,
            "HoldMilliseconds",
            200,
            Describe(
                "Hold Increase at the full-dry boundary; releasing early resets it.",
                "Afterburner Hold Time (ms)",
                90,
                new AcceptableValueRange<int>(0, 2000)));
        CommandThreshold = config.Bind(
            AdvancedSection,
            "CommandThreshold",
            0.5f,
            Describe(
                "Minimum raw input counted as a hold; 1.0 requires full-scale input.",
                "Command Threshold",
                100,
                new AcceptableValueRange<float>(0.1f, 1f)));
        EndpointEpsilon = config.Bind(
            AdvancedSection,
            "EndpointEpsilon",
            0.001f,
            Describe(
                "Distance from a preset boundary that can start a hold.",
                "Endpoint Tolerance",
                90,
                new AcceptableValueRange<float>(0.00001f, 0.05f)));
        ResetHysteresis = config.Bind(
            AdvancedSection,
            "ResetHysteresis",
            0.02f,
            Describe(
                "Inward travel before relock; uses at least Endpoint Tolerance.",
                "Relock Distance",
                80,
                new AcceptableValueRange<float>(0.001f, 0.10f)));
    }

    public ConfigEntry<string> RuntimeStatus { get; }
    public ConfigEntry<bool> Enabled { get; }
    public ConfigEntry<bool> DebugLogging { get; }
    public ConfigEntry<bool> NetworkValidation { get; }
    public ConfigEntry<int> NetworkValidationOwner { get; }
    public ConfigEntry<bool> IndicatorEnabled { get; }
    /// <summary>Applied only to aircraft with a supported, live detent; every other aircraft stays vanilla, which is why the description points elsewhere for a global sensitivity change.</summary>
    public ConfigEntry<float> ThrottleSensitivity { get; }
    public ConfigEntry<bool> IdleEnabled { get; }
    public ConfigEntry<int> IdleHoldMilliseconds { get; }
    public ConfigEntry<bool> AfterburnerEnabled { get; }
    public ConfigEntry<int> AfterburnerHoldMilliseconds { get; }
    public ConfigEntry<float> CommandThreshold { get; }
    public ConfigEntry<float> EndpointEpsilon { get; }
    public ConfigEntry<float> ResetHysteresis { get; }

    private static ConfigDescription Describe(
        string description,
        string displayName,
        int order,
        AcceptableValueBase? acceptableValues = null,
        Action<ConfigEntryBase>? customDrawer = null,
        bool readOnly = false,
        bool hideDefaultButton = false) =>
        new(
            description,
            acceptableValues,
            new ConfigurationManagerAttributes
            {
                CustomDrawer = customDrawer,
                DispName = displayName,
                HideDefaultButton = hideDefaultButton,
                Order = order,
                ReadOnly = readOnly,
            });

    private static void DrawRuntimeStatus(ConfigEntryBase _)
    {
        var readiness = RuntimeController.BestEffortReadiness;
        GUILayout.Label(
            $"{RuntimeController.CurrentAircraftDisplayName} | {readiness.DisplayText}",
            GUILayout.ExpandWidth(true));
    }

    /// <summary>Clamps every entry on read, so a hand-edited config file cannot push the runtime outside its tested ranges.</summary>
    public EffectiveSettings ReadEffective()
    {
        var epsilon = Clamp(EndpointEpsilon.Value, 0.00001f, 0.05f);
        var hysteresis = Math.Max(epsilon, Clamp(ResetHysteresis.Value, 0.001f, 0.10f));
        return new EffectiveSettings(
            Enabled.Value,
            DebugLogging.Value,
            IndicatorEnabled.Value,
            Clamp(ThrottleSensitivity.Value, 0.25f, 4f),
            IdleEnabled.Value,
            Clamp(IdleHoldMilliseconds.Value, 0, 2000),
            AfterburnerEnabled.Value,
            Clamp(AfterburnerHoldMilliseconds.Value, 0, 2000),
            Clamp(CommandThreshold.Value, 0.1f, 1f),
            epsilon,
            hysteresis);
    }

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return minimum;
        }

        return Math.Max(minimum, Math.Min(maximum, value));
    }
}
