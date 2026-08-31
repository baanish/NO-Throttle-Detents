using System;
using System.Reflection;
using HarmonyLib;
using NuclearOptionDetents.Core;
using TMPro;
using UnityEngine;

namespace NuclearOptionDetents.UI;

/// <summary>
/// Draws the detent line under the HUD throttle gauge by cloning the gauge's own label, so the text
/// inherits the game's font, material, and color instead of hardcoding a style that HUD changes would break.
/// The clone is owned by this class: every failure path destroys it rather than leaving an orphan on the HUD.
/// </summary>
internal sealed class DetentHudIndicator
{
    private const float DisplayRangeRetrySeconds = 0.25f;
    private static readonly FieldInfo? ThrottleLabelField =
        AccessTools.DeclaredField(typeof(ThrottleGauge), "throttleLabel");
    private static readonly FieldInfo? ThrottleRegionsField =
        AccessTools.DeclaredField(typeof(ThrottleGauge), "throttleRegions");
    private static readonly Type? ThrottleRegionType = ThrottleRegionsField?.FieldType.GetElementType();
    private static readonly FieldInfo? RegionShowPercentField =
        ThrottleRegionType is null ? null : AccessTools.DeclaredField(ThrottleRegionType, "showPercent");
    private static readonly FieldInfo? RegionStartField =
        ThrottleRegionType is null ? null : AccessTools.DeclaredField(ThrottleRegionType, "start");
    private static readonly FieldInfo? RegionEndField =
        ThrottleRegionType is null ? null : AccessTools.DeclaredField(ThrottleRegionType, "end");

    private FlightHud? _hud;
    private Aircraft? _displayAircraft;
    private ThrottleGauge? _displayGauge;
    private float _nextDisplayRangeProbeTime;
    private Transform? _hudCenter;
    private TextMeshProUGUI? _sourceLabel;
    private GameObject? _indicatorObject;
    private TextMeshProUGUI? _indicatorLabel;
    private bool _hasTextKey;
    private bool _lastWasIdle;
    private bool _lastWasInterior;
    private EndpointDetentState _lastState;
    private int _lastPercent;
    private double _lastBoundaryPercent;
    private bool _hasStyle;
    private TMP_FontAsset? _lastFont;
    private Material? _lastMaterial;
    private Color _lastColor;
    private FontStyles _lastFontStyle;
    private float _lastFontSize;
    private Vector3 _lastScale;

    /// <summary>False when the game build no longer exposes the throttle label with the expected type; the plugin then runs without a HUD indicator.</summary>
    public bool IsAvailable =>
        ThrottleLabelField is not null && ThrottleLabelField.FieldType == typeof(TextMeshProUGUI);

    /// <summary>Reads the local gauge once per aircraft so custom percentages use the same linear region the cockpit displays.</summary>
    public void SyncThrottleDisplayRange()
    {
        var aircraft = RuntimeController.LocalAircraft;
        if (ReferenceEquals(aircraft, null))
        {
            _displayAircraft = null;
            _displayGauge = null;
            return;
        }

        var hud = SceneSingleton<FlightHud>.i;
        if (!IsAlive(hud))
        {
            RuntimeController.ClearCustomThrottleDisplayRange(aircraft!);
            return;
        }

        var cachedPair = ReferenceEquals(_displayAircraft, aircraft) && IsAlive(_displayGauge);
        if (cachedPair && RuntimeController.HasCustomThrottleDisplayRange(aircraft!))
        {
            return;
        }
        if (cachedPair && Time.unscaledTime < _nextDisplayRangeProbeTime)
        {
            return;
        }

        var gauge = hud!.GetComponentInChildren<ThrottleGauge>(true);
        if (!IsAlive(gauge))
        {
            _nextDisplayRangeProbeTime = Time.unscaledTime + DisplayRangeRetrySeconds;
            RuntimeController.ClearCustomThrottleDisplayRange(aircraft!);
            return;
        }

        _displayAircraft = aircraft;
        _displayGauge = gauge;
        if (TryGetDryDisplayRange(gauge!, out var start, out var end))
        {
            _nextDisplayRangeProbeTime = 0f;
            RuntimeController.SetCustomThrottleDisplayRange(aircraft!, start, end);
            return;
        }

        _nextDisplayRangeProbeTime = Time.unscaledTime + DisplayRangeRetrySeconds;
        RuntimeController.ClearCustomThrottleDisplayRange(aircraft!);
    }

    /// <summary>
    /// Called every frame. Text is rewritten only when the displayed line actually changes, and the clone
    /// mirrors the source label's visibility so the indicator disappears with the rest of the HUD.
    /// </summary>
    public void Render(in DetentIndicatorSnapshot snapshot)
    {
        if (!IsAvailable || !snapshot.Visible)
        {
            SetVisible(false);
            return;
        }

        var hud = SceneSingleton<FlightHud>.i;
        if (!IsAlive(hud) || !EnsureLabel(hud!))
        {
            Reset();
            return;
        }

        var wasIdle = snapshot.Idle.Visible;
        var wasInterior = snapshot.Interior.Visible;
        var line = wasIdle
            ? snapshot.Idle
            : wasInterior
                ? snapshot.Interior
                : snapshot.Afterburner;
        var percent = line.State == EndpointDetentState.Holding
            ? DetentIndicatorText.RoundedPercent(line.Progress)
            : -1;
        if (!_hasTextKey || wasIdle != _lastWasIdle || wasInterior != _lastWasInterior ||
            line.State != _lastState || percent != _lastPercent ||
            !line.BoundaryPercent.Equals(_lastBoundaryPercent))
        {
            _indicatorLabel!.text = DetentIndicatorText.Format(snapshot);
            _hasTextKey = true;
            _lastWasIdle = wasIdle;
            _lastWasInterior = wasInterior;
            _lastState = line.State;
            _lastPercent = percent;
            _lastBoundaryPercent = line.BoundaryPercent;
        }

        UpdateStyleAndPosition();
        SetVisible(_sourceLabel!.enabled && _sourceLabel.gameObject.activeInHierarchy);
    }

    /// <summary>Destroys the clone and forgets every cached reference, leaving the HUD exactly as vanilla left it.</summary>
    public void Reset()
    {
        if (IsAlive(_indicatorObject))
        {
            UnityEngine.Object.Destroy(_indicatorObject);
        }

        _hud = null;
        _displayAircraft = null;
        _displayGauge = null;
        _nextDisplayRangeProbeTime = 0f;
        _hudCenter = null;
        _sourceLabel = null;
        _indicatorObject = null;
        _indicatorLabel = null;
        _hasTextKey = false;
        _lastWasIdle = false;
        _lastWasInterior = false;
        _lastState = default;
        _lastPercent = 0;
        _lastBoundaryPercent = 0;
        _hasStyle = false;
        _lastFont = null;
        _lastMaterial = null;
        _lastColor = default;
        _lastFontStyle = default;
        _lastFontSize = 0f;
        _lastScale = default;
    }

    /// <summary>Rebuilds the clone whenever any cached HUD object has been destroyed or replaced; false means the HUD is not ready and nothing should be drawn.</summary>
    private bool EnsureLabel(FlightHud hud)
    {
        if (ReferenceEquals(_hud, hud) && IsAlive(_hudCenter) && IsAlive(_sourceLabel) &&
            IsAlive(_indicatorObject) && IsAlive(_indicatorLabel))
        {
            return true;
        }

        var gauge = hud.GetComponentInChildren<ThrottleGauge>(true);
        if (!IsAlive(gauge))
        {
            return false;
        }

        var source = ThrottleLabelField?.GetValue(gauge) as TextMeshProUGUI;
        if (!IsAlive(source))
        {
            return false;
        }

        var hudCenter = hud.GetHUDCenter();
        if (!IsAlive(hudCenter))
        {
            return false;
        }

        Reset();
        _hud = hud;
        _hudCenter = hudCenter;
        _sourceLabel = source;
        _indicatorObject = UnityEngine.Object.Instantiate(source!.gameObject, hudCenter);
        _indicatorObject.name = "NuclearOptionDetents_HudIndicator";
        _indicatorLabel = _indicatorObject.GetComponent<TextMeshProUGUI>();
        if (!IsAlive(_indicatorLabel))
        {
            Reset();
            return false;
        }

        _indicatorLabel!.raycastTarget = false;
        _indicatorLabel.enabled = true;
        _indicatorLabel.richText = false;
        _indicatorLabel.enableAutoSizing = false;
        _indicatorLabel.enableWordWrapping = false;
        _indicatorLabel.overflowMode = TextOverflowModes.Overflow;
        _indicatorLabel.alignment = TextAlignmentOptions.Center;

        var rect = _indicatorObject.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 60f);
        _indicatorObject.transform.localRotation = Quaternion.identity;
        return true;
    }

    /// <summary>Re-applies the source label's style only when it changed, and parks the line just below the gauge so it tracks HUD scaling.</summary>
    private void UpdateStyleAndPosition()
    {
        var source = _sourceLabel!;
        var label = _indicatorLabel!;
        var hudCenter = _hudCenter!;

        var font = source.font;
        var material = source.fontSharedMaterial;
        var color = source.color;
        var fontStyle = source.fontStyle;
        var fontSize = Mathf.Max(12f, source.fontSize * 0.65f);
        var sourceScale = source.transform.localScale;
        if (!_hasStyle || !ReferenceEquals(_lastFont, font) || !ReferenceEquals(_lastMaterial, material) ||
            _lastColor != color || _lastFontStyle != fontStyle || _lastFontSize != fontSize ||
            _lastScale != sourceScale)
        {
            label.font = font;
            label.fontSharedMaterial = material;
            label.color = color;
            label.fontStyle = fontStyle;
            label.fontSize = fontSize;
            label.transform.localScale = sourceScale;
            _hasStyle = true;
            _lastFont = font;
            _lastMaterial = material;
            _lastColor = color;
            _lastFontStyle = fontStyle;
            _lastFontSize = fontSize;
            _lastScale = sourceScale;
        }

        var sourcePosition = hudCenter.InverseTransformPoint(source.transform.position);
        var labelPosition = sourcePosition +
                            new Vector3(0f, -Mathf.Max(22f, source.fontSize * 0.65f), 0f);
        if (label.transform.localPosition != labelPosition)
        {
            label.transform.localPosition = labelPosition;
        }
    }

    private void SetVisible(bool visible)
    {
        if (IsAlive(_indicatorObject) && _indicatorObject!.activeSelf != visible)
        {
            _indicatorObject.SetActive(visible);
        }
    }

    /// <summary>Guards against Unity's destroyed-object sentinel, which is non-null to the CLR but compares equal to null.</summary>
    private static bool IsAlive(UnityEngine.Object? value) =>
        !ReferenceEquals(value, null) && value != null;

    private static bool TryGetDryDisplayRange(ThrottleGauge gauge, out double start, out double end)
    {
        start = 0;
        end = 1;
        if (ThrottleRegionsField?.GetValue(gauge) is not Array regions ||
            RegionShowPercentField is null || RegionStartField is null || RegionEndField is null)
        {
            return false;
        }

        const double dryRangeProbe = 0.5;
        for (var index = 0; index < regions.Length; index++)
        {
            var region = regions.GetValue(index);
            if (region is null || RegionShowPercentField.GetValue(region) is not bool showPercent || !showPercent)
            {
                continue;
            }

            if (RegionStartField.GetValue(region) is not float candidateStart ||
                RegionEndField.GetValue(region) is not float candidateEnd)
            {
                continue;
            }
            if (candidateEnd > candidateStart &&
                dryRangeProbe >= candidateStart && dryRangeProbe <= candidateEnd)
            {
                start = candidateStart;
                end = candidateEnd;
                return true;
            }
        }

        return false;
    }
}
