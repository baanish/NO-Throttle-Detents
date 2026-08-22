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
    private string _lastText = string.Empty;

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

        var text = DetentIndicatorText.Format(snapshot);
        if (text.Length == 0)
        {
            SetVisible(false);
            return;
        }

        UpdateStyleAndPosition();
        if (!string.Equals(_lastText, text, System.StringComparison.Ordinal))
        {
            _indicatorLabel!.text = text;
            _lastText = text;
        }

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
        _lastText = string.Empty;
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

        label.enabled = true;
        label.font = source.font;
        label.fontSharedMaterial = source.fontSharedMaterial;
        label.color = source.color;
        label.fontStyle = source.fontStyle;
        label.fontSize = Mathf.Max(12f, source.fontSize * 0.65f);
        label.transform.localScale = source.transform.localScale;

        var sourcePosition = hudCenter.InverseTransformPoint(source.transform.position);
        label.transform.localPosition = sourcePosition +
                                        new Vector3(0f, -Mathf.Max(22f, source.fontSize * 0.65f), 0f);
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
