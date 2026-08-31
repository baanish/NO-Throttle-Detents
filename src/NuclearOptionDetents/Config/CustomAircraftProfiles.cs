using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using NuclearOptionDetents.Compatibility;
using NuclearOptionDetents.Core;
using UnityEngine;

namespace NuclearOptionDetents.Config;

/// <summary>Persists one profile per installed or observed aircraft and exposes one compact Configuration Manager editor.</summary>
internal sealed class CustomAircraftProfiles
{
    private const string EditorSection = "Custom Aircraft";
    private const string ProfileSectionPrefix = "Custom Aircraft Profile ";
    private readonly ConfigFile _config;
    private readonly ConfigEntry<string> _detectedAircraft;
    private readonly ConfigEntry<string> _selectedAircraft;
    private readonly Dictionary<string, ProfileEntries> _profiles =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _editBuffers = new(StringComparer.Ordinal);
    private readonly DetectedAircraftCatalog _catalog;
    private bool _selectorOpen;

    public CustomAircraftProfiles(ConfigFile config)
    {
        _config = config;
        _detectedAircraft = config.Bind(
            EditorSection,
            "DetectedAircraft",
            string.Empty,
            Hidden("Aircraft IDs found in the installed catalog or during seat entry."));
        _catalog = new DetectedAircraftCatalog(_detectedAircraft.Value);
        foreach (var identity in _catalog.All)
        {
            BindProfile(identity.Id, identity.DisplayName);
        }

        _selectedAircraft = config.Bind(
            EditorSection,
            "SelectedAircraftId",
            string.Empty,
            new ConfigDescription(
                "Choose an installed aircraft, then edit only that aircraft's profile.",
                null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = DrawEditor,
                    DispName = "Aircraft Profile",
                    HideDefaultButton = true,
                    Order = 100,
                }));

        EnsureSelection();
    }

    public void Register(string id, string displayName)
    {
        var changed = RegisterIdentity(id, displayName);
        if (!_catalog.Contains(id))
        {
            return;
        }

        if (changed)
        {
            _detectedAircraft.Value = _catalog.Serialize();
        }

        _selectedAircraft.Value = id.Trim();
    }

    public void RefreshInstalledAircraft()
    {
        var definitions = Encyclopedia.i?.aircraft;
        if (definitions is null)
        {
            return;
        }

        var changed = false;
        foreach (var definition in definitions)
        {
            if (RuntimeCompatibility.TryGetDefinitionIdentity(definition, out var id, out var displayName))
            {
                changed |= RegisterIdentity(id, displayName);
            }
        }

        if (changed)
        {
            _detectedAircraft.Value = _catalog.Serialize();
        }

        EnsureSelection();
    }

    public CustomAirframeConfig Read(string aircraftId) =>
        _profiles.TryGetValue((aircraftId ?? string.Empty).Trim(), out var profile)
            ? profile.Read()
            : CustomAirframeConfig.Disabled;

    private ProfileEntries BindProfile(string id, string displayName)
    {
        if (_profiles.TryGetValue(id, out var existing))
        {
            return existing;
        }

        var section = ProfileSectionPrefix + Uri.EscapeDataString(id);
        var profile = new ProfileEntries(
            id,
            _config.Bind(section, "DisplayName", displayName, Hidden("Last observed aircraft name.")),
            _config.Bind(section, "Enabled", false, Hidden("Enable this custom profile.")),
            _config.Bind(section, "AirbrakePath", AirbrakePath.None, Hidden("Idle airbrake implementation.")),
            _config.Bind(section, "HasAfterburner", false, Hidden("Whether this aircraft has afterburner.")),
            _config.Bind(section, "AfterburnerNozzleCount", 1, Hidden("Afterburner nozzle count.", new AcceptableValueRange<int>(1, 16))),
            _config.Bind(section, "AfterburnerStart", 0.9f, Hidden("Throttle where afterburner begins.", new AcceptableValueRange<float>(0f, 1f))),
            _config.Bind(section, "AfterburnerEnd", 1f, Hidden("Throttle where the afterburner stage ends.", new AcceptableValueRange<float>(0f, 1f))),
            _config.Bind(section, "DryDetentPercentages", string.Empty, Hidden("Cockpit percentages, comma-separated.")),
            _config.Bind(section, "DryDetentHoldMilliseconds", 200, Hidden("Hold required to cross custom detents.", new AcceptableValueRange<int>(0, 2000))));
        _profiles.Add(id, profile);
        return profile;
    }

    private void DrawEditor(ConfigEntryBase _)
    {
        var identities = _catalog.All;
        if (identities.Count == 0)
        {
            GUILayout.Label("No aircraft found. Enter a flight once to retry.", GUILayout.ExpandWidth(true));
            return;
        }

        EnsureSelection();
        var selectedId = _selectedAircraft.Value;
        var selectedName = _catalog.DisplayNameFor(selectedId);
        var currentSuffix = string.Equals(selectedId, RuntimeController.CurrentAircraftId, StringComparison.OrdinalIgnoreCase)
            ? "  [CURRENT]"
            : string.Empty;

        GUILayout.BeginVertical();
        var disclosure = _selectorOpen ? "▲" : "▼";
        if (GUILayout.Button(
                new GUIContent($"{selectedName} ({selectedId}){currentSuffix}  {disclosure}", "Select a detected aircraft profile."),
                GUILayout.ExpandWidth(true)))
        {
            _selectorOpen = !_selectorOpen;
            if (_selectorOpen)
            {
                RefreshInstalledAircraft();
                identities = _catalog.All;
            }
        }

        if (_selectorOpen)
        {
            foreach (var identity in identities)
            {
                var suffix = string.Equals(identity.Id, RuntimeController.CurrentAircraftId, StringComparison.OrdinalIgnoreCase)
                    ? "  [CURRENT]"
                    : string.Empty;
                if (GUILayout.Button($"{identity.DisplayName} ({identity.Id}){suffix}", GUILayout.ExpandWidth(true)))
                {
                    _selectedAircraft.Value = identity.Id;
                    _selectorOpen = false;
                    selectedId = identity.Id;
                }
            }
        }

        if (_profiles.TryGetValue(selectedId, out var profile))
        {
            DrawProfile(profile);
        }
        GUILayout.EndVertical();
    }

    private bool RegisterIdentity(string id, string displayName)
    {
        var changed = _catalog.Register(id, displayName);
        if (!_catalog.Contains(id))
        {
            return false;
        }

        var profile = BindProfile(id.Trim(), _catalog.DisplayNameFor(id));
        if (!string.Equals(profile.DisplayName.Value, displayName, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(displayName))
        {
            profile.DisplayName.Value = displayName.Trim();
        }

        return changed;
    }

    private void DrawProfile(ProfileEntries profile)
    {
        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        var enabled = GUILayout.Toggle(
            profile.Enabled.Value,
            new GUIContent("Enable profile", "Use these custom settings for this aircraft."));
        if (enabled != profile.Enabled.Value)
        {
            profile.Enabled.Value = enabled;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(
                new GUIContent("Reset profile", "Restore this aircraft's custom settings to their defaults."),
                GUILayout.Width(90)))
        {
            profile.Reset();
            ClearEditBuffers(profile.Id);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Airbrake", "How idle airbrake is implemented."), GUILayout.Width(105));
        var airbrake = GUILayout.SelectionGrid(
            (int)profile.AirbrakePath.Value,
            new[] { "None", "Component", "Split" },
            3,
            GUILayout.ExpandWidth(true));
        if (airbrake != (int)profile.AirbrakePath.Value)
        {
            profile.AirbrakePath.Value = (AirbrakePath)airbrake;
        }
        GUILayout.EndHorizontal();

        var hasAfterburner = GUILayout.Toggle(
            profile.HasAfterburner.Value,
            new GUIContent("Has afterburner", "Validate an afterburner detent for this aircraft."));
        if (hasAfterburner != profile.HasAfterburner.Value)
        {
            profile.HasAfterburner.Value = hasAfterburner;
        }

        var controlsEnabled = GUI.enabled;
        GUI.enabled = controlsEnabled && hasAfterburner;
        DrawInt(profile, "Afterburner nozzles", "nozzles", profile.AfterburnerNozzleCount, 1, 16);
        DrawFloat(profile, "Afterburner start", "ab-start", profile.AfterburnerStart, 0f, 1f);
        DrawFloat(profile, "Afterburner end", "ab-end", profile.AfterburnerEnd, 0f, 1f);
        GUI.enabled = controlsEnabled;

        DrawText(profile, "Custom detents (%)", "detents", profile.DryDetentPercentages);
        DrawInt(profile, "Detent hold (ms)", "hold", profile.DryDetentHoldMilliseconds, 0, 2000);
    }

    private void ClearEditBuffers(string profileId)
    {
        _editBuffers.Remove(profileId + "\nnozzles");
        _editBuffers.Remove(profileId + "\nab-start");
        _editBuffers.Remove(profileId + "\nab-end");
        _editBuffers.Remove(profileId + "\ndetents");
        _editBuffers.Remove(profileId + "\nhold");
    }

    private void DrawInt(
        ProfileEntries profile,
        string label,
        string field,
        ConfigEntry<int> entry,
        int minimum,
        int maximum)
    {
        var text = DrawField(profile, label, field, entry.Value.ToString(CultureInfo.InvariantCulture));
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            entry.Value = Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    private void DrawFloat(
        ProfileEntries profile,
        string label,
        string field,
        ConfigEntry<float> entry,
        float minimum,
        float maximum)
    {
        var text = DrawField(profile, label, field, entry.Value.ToString("0.####", CultureInfo.InvariantCulture));
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            !float.IsNaN(value) && !float.IsInfinity(value))
        {
            entry.Value = Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    private void DrawText(ProfileEntries profile, string label, string field, ConfigEntry<string> entry)
    {
        var text = DrawField(profile, label, field, entry.Value);
        if (!string.Equals(text, entry.Value, StringComparison.Ordinal))
        {
            entry.Value = text;
        }
    }

    private string DrawField(
        ProfileEntries profile,
        string label,
        string field,
        string currentValue)
    {
        var key = profile.Id + "\n" + field;
        if (!_editBuffers.TryGetValue(key, out var buffered))
        {
            buffered = currentValue;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(105));
        buffered = GUILayout.TextField(buffered, GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        _editBuffers[key] = buffered;
        return buffered;
    }

    private void EnsureSelection()
    {
        if (_catalog.Contains(_selectedAircraft.Value))
        {
            return;
        }

        var identities = _catalog.All;
        _selectedAircraft.Value = identities.Count == 0 ? string.Empty : identities[0].Id;
    }

    private static ConfigDescription Hidden(
        string description,
        AcceptableValueBase? acceptableValues = null) =>
        new(
            description,
            acceptableValues,
            new ConfigurationManagerAttributes { Browsable = false });

    private sealed class ProfileEntries
    {
        public ProfileEntries(
            string id,
            ConfigEntry<string> displayName,
            ConfigEntry<bool> enabled,
            ConfigEntry<AirbrakePath> airbrakePath,
            ConfigEntry<bool> hasAfterburner,
            ConfigEntry<int> afterburnerNozzleCount,
            ConfigEntry<float> afterburnerStart,
            ConfigEntry<float> afterburnerEnd,
            ConfigEntry<string> dryDetentPercentages,
            ConfigEntry<int> dryDetentHoldMilliseconds)
        {
            Id = id;
            DisplayName = displayName;
            Enabled = enabled;
            AirbrakePath = airbrakePath;
            HasAfterburner = hasAfterburner;
            AfterburnerNozzleCount = afterburnerNozzleCount;
            AfterburnerStart = afterburnerStart;
            AfterburnerEnd = afterburnerEnd;
            DryDetentPercentages = dryDetentPercentages;
            DryDetentHoldMilliseconds = dryDetentHoldMilliseconds;
        }

        public string Id { get; }
        public ConfigEntry<string> DisplayName { get; }
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<AirbrakePath> AirbrakePath { get; }
        public ConfigEntry<bool> HasAfterburner { get; }
        public ConfigEntry<int> AfterburnerNozzleCount { get; }
        public ConfigEntry<float> AfterburnerStart { get; }
        public ConfigEntry<float> AfterburnerEnd { get; }
        public ConfigEntry<string> DryDetentPercentages { get; }
        public ConfigEntry<int> DryDetentHoldMilliseconds { get; }

        public CustomAirframeConfig Read() =>
            new(
                Enabled.Value,
                Id,
                AirbrakePath.Value,
                HasAfterburner.Value,
                Math.Max(1, Math.Min(16, AfterburnerNozzleCount.Value)),
                Clamp(AfterburnerStart.Value, 0f, 1f),
                Clamp(AfterburnerEnd.Value, 0f, 1f),
                DryDetentPercentages.Value,
                Math.Max(0, Math.Min(2000, DryDetentHoldMilliseconds.Value)));

        public void Reset()
        {
            Enabled.BoxedValue = Enabled.DefaultValue;
            AirbrakePath.BoxedValue = AirbrakePath.DefaultValue;
            HasAfterburner.BoxedValue = HasAfterburner.DefaultValue;
            AfterburnerNozzleCount.BoxedValue = AfterburnerNozzleCount.DefaultValue;
            AfterburnerStart.BoxedValue = AfterburnerStart.DefaultValue;
            AfterburnerEnd.BoxedValue = AfterburnerEnd.DefaultValue;
            DryDetentPercentages.BoxedValue = DryDetentPercentages.DefaultValue;
            DryDetentHoldMilliseconds.BoxedValue = DryDetentHoldMilliseconds.DefaultValue;
        }

        private static float Clamp(float value, float minimum, float maximum) =>
            float.IsNaN(value) || float.IsInfinity(value)
                ? minimum
                : Math.Max(minimum, Math.Min(maximum, value));
    }

}
