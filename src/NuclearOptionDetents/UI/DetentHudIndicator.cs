using System.Reflection;
using HarmonyLib;
using NuclearOptionDetents.Core;
using TMPro;
using UnityEngine;

namespace NuclearOptionDetents.UI;

internal sealed class DetentHudIndicator
{
    private static readonly FieldInfo? ThrottleLabelField =
        AccessTools.DeclaredField(typeof(ThrottleGauge), "throttleLabel");

    private FlightHud? _hud;
    private Transform? _hudCenter;
    private TextMeshProUGUI? _sourceLabel;
    private GameObject? _indicatorObject;
    private TextMeshProUGUI? _indicatorLabel;
    private bool _hasTextKey;
    private bool _lastWasIdle;
    private EndpointDetentState _lastState;
    private int _lastPercent;
    private bool _hasStyle;
    private TMP_FontAsset? _lastFont;
    private Material? _lastMaterial;
    private Color _lastColor;
    private FontStyles _lastFontStyle;
    private float _lastFontSize;
    private Vector3 _lastScale;

    public bool IsAvailable =>
        ThrottleLabelField is not null && ThrottleLabelField.FieldType == typeof(TextMeshProUGUI);

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
        var line = wasIdle ? snapshot.Idle : snapshot.Afterburner;
        var percent = line.State == EndpointDetentState.Holding
            ? DetentIndicatorText.RoundedPercent(line.Progress)
            : -1;
        if (!_hasTextKey || wasIdle != _lastWasIdle || line.State != _lastState || percent != _lastPercent)
        {
            _indicatorLabel!.text = DetentIndicatorText.Format(snapshot);
            _hasTextKey = true;
            _lastWasIdle = wasIdle;
            _lastState = line.State;
            _lastPercent = percent;
        }

        UpdateStyleAndPosition();
        SetVisible(_sourceLabel!.enabled && _sourceLabel.gameObject.activeInHierarchy);
    }

    public void Reset()
    {
        if (IsAlive(_indicatorObject))
        {
            UnityEngine.Object.Destroy(_indicatorObject);
        }

        _hud = null;
        _hudCenter = null;
        _sourceLabel = null;
        _indicatorObject = null;
        _indicatorLabel = null;
        _hasTextKey = false;
        _lastWasIdle = false;
        _lastState = default;
        _lastPercent = 0;
        _hasStyle = false;
        _lastFont = null;
        _lastMaterial = null;
        _lastColor = default;
        _lastFontStyle = default;
        _lastFontSize = 0f;
        _lastScale = default;
    }

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

    private static bool IsAlive(UnityEngine.Object? value) =>
        !ReferenceEquals(value, null) && value != null;
}
