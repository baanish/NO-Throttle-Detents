using System;
using System.Collections.Generic;
using System.Globalization;

namespace NuclearOptionDetents.Core;

/// <summary>An opt-in user profile for one exact aircraft ID. Invalid fields fail open to vanilla behavior.</summary>
internal sealed class CustomAirframeConfig : IEquatable<CustomAirframeConfig>
{
    private const int MaximumDryDetents = 8;

    public static CustomAirframeConfig Disabled { get; } = new(
        false,
        string.Empty,
        AirbrakePath.None,
        false,
        1,
        0.9f,
        1f,
        string.Empty,
        200);

    public CustomAirframeConfig(
        bool enabled,
        string aircraftId,
        AirbrakePath airbrakePath,
        bool hasAfterburner,
        int afterburnerNozzleCount,
        float afterburnerStart,
        float afterburnerEnd,
        string dryDetentPercentages,
        int dryDetentHoldMilliseconds)
    {
        Enabled = enabled;
        AircraftId = (aircraftId ?? string.Empty).Trim();
        AirbrakePath = airbrakePath;
        HasAfterburner = hasAfterburner;
        AfterburnerNozzleCount = afterburnerNozzleCount;
        AfterburnerStart = afterburnerStart;
        AfterburnerEnd = afterburnerEnd;
        DryDetentPercentages = (dryDetentPercentages ?? string.Empty).Trim();
        DryDetentHoldMilliseconds = dryDetentHoldMilliseconds;
    }

    public bool Enabled { get; }
    public string AircraftId { get; }
    public AirbrakePath AirbrakePath { get; }
    public bool HasAfterburner { get; }
    public int AfterburnerNozzleCount { get; }
    public float AfterburnerStart { get; }
    public float AfterburnerEnd { get; }
    public string DryDetentPercentages { get; }
    public int DryDetentHoldMilliseconds { get; }

    public bool Matches(string aircraftId) =>
        Enabled &&
        AircraftId.Length > 0 &&
        string.Equals(AircraftId, aircraftId, StringComparison.OrdinalIgnoreCase);

    public bool TryCreatePreset(out AirframePreset preset)
    {
        preset = null!;
        if (!Enabled || AircraftId.Length == 0 ||
            AirbrakePath < AirbrakePath.None || AirbrakePath > AirbrakePath.Split)
        {
            return false;
        }

        if (!HasAfterburner && AirbrakePath == AirbrakePath.None)
        {
            return false;
        }

        int? nozzleCount = null;
        float? afterburnerStart = null;
        float? afterburnerEnd = null;
        if (HasAfterburner)
        {
            if (AfterburnerNozzleCount <= 0 ||
                !IsFinite(AfterburnerStart) || !IsFinite(AfterburnerEnd) ||
                AfterburnerStart < 0f || AfterburnerEnd > 1f ||
                AfterburnerStart >= AfterburnerEnd)
            {
                return false;
            }

            nozzleCount = AfterburnerNozzleCount;
            afterburnerStart = AfterburnerStart;
            afterburnerEnd = AfterburnerEnd;
        }

        preset = new AirframePreset(
            AircraftId,
            AircraftId,
            collective: false,
            AirbrakePath,
            HasAfterburner,
            AirbrakePath == AirbrakePath.None ? null : 0f,
            nozzleCount,
            afterburnerStart,
            afterburnerEnd);
        return true;
    }

    /// <summary>Parses a compact comma-separated list such as "67,82.5" into sorted cockpit-display fractions.</summary>
    public bool TryGetDryDetentFractions(out double[] fractions)
    {
        fractions = Array.Empty<double>();
        if (!Enabled || AircraftId.Length == 0)
        {
            return false;
        }

        if (DryDetentPercentages.Length == 0)
        {
            return true;
        }

        var tokens = DryDetentPercentages.Split(',');
        if (tokens.Length > MaximumDryDetents)
        {
            return false;
        }

        var parsed = new List<double>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!double.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ||
                double.IsNaN(percent) || double.IsInfinity(percent) ||
                percent <= 0 || percent >= 100)
            {
                return false;
            }

            parsed.Add(percent / 100.0);
        }

        parsed.Sort();
        for (var index = parsed.Count - 1; index > 0; index--)
        {
            if (Math.Abs(parsed[index] - parsed[index - 1]) <= 0.000001)
            {
                parsed.RemoveAt(index);
            }
        }

        fractions = parsed.ToArray();
        return true;
    }

    public bool Equals(CustomAirframeConfig? other) =>
        other is not null &&
        Enabled == other.Enabled &&
        string.Equals(AircraftId, other.AircraftId, StringComparison.Ordinal) &&
        AirbrakePath == other.AirbrakePath &&
        HasAfterburner == other.HasAfterburner &&
        AfterburnerNozzleCount == other.AfterburnerNozzleCount &&
        AfterburnerStart.Equals(other.AfterburnerStart) &&
        AfterburnerEnd.Equals(other.AfterburnerEnd) &&
        string.Equals(DryDetentPercentages, other.DryDetentPercentages, StringComparison.Ordinal) &&
        DryDetentHoldMilliseconds == other.DryDetentHoldMilliseconds;

    public override bool Equals(object? obj) => Equals(obj as CustomAirframeConfig);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Enabled ? 1 : 0;
            hash = (hash * 397) ^ AircraftId.GetHashCode();
            hash = (hash * 397) ^ (int)AirbrakePath;
            hash = (hash * 397) ^ (HasAfterburner ? 1 : 0);
            hash = (hash * 397) ^ AfterburnerNozzleCount;
            hash = (hash * 397) ^ AfterburnerStart.GetHashCode();
            hash = (hash * 397) ^ AfterburnerEnd.GetHashCode();
            hash = (hash * 397) ^ DryDetentPercentages.GetHashCode();
            return (hash * 397) ^ DryDetentHoldMilliseconds;
        }
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
