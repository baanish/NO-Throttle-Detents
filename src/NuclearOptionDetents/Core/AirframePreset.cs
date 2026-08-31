using System;
using System.Collections.Generic;

namespace NuclearOptionDetents.Core;

/// <summary>
/// Explicit compatibility data for one installed Nuclear Option airframe.
/// Unknown IDs are intentionally not represented, so a new airframe remains vanilla.
/// </summary>
internal sealed class AirframePreset
{
    public AirframePreset(
        string id,
        string displayName,
        bool collective,
        AirbrakePath airbrakePath,
        bool hasAfterburner,
        float? idleAirbrakeBoundary,
        int? afterburnerNozzleCount,
        float? afterburnerStart,
        float? afterburnerEnd)
    {
        (Id, DisplayName, Collective, AirbrakePath, HasAfterburner,
            IdleAirbrakeBoundary, AfterburnerNozzleCount, AfterburnerStart, AfterburnerEnd) =
            (id, displayName, collective, airbrakePath, hasAfterburner,
            idleAirbrakeBoundary, afterburnerNozzleCount, afterburnerStart, afterburnerEnd);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool Collective { get; }

    public AirbrakePath AirbrakePath { get; }

    public bool HasAirbrake => AirbrakePath != NuclearOptionDetents.Core.AirbrakePath.None;

    public bool HasAfterburner { get; }

    public float? IdleAirbrakeBoundary { get; }

    public int? AfterburnerNozzleCount { get; }

    public float? AfterburnerStart { get; }

    public float? AfterburnerEnd { get; }
}

internal enum AirframeFeature
{
    Airbrake,
    Afterburner,
}

internal enum AirbrakePath
{
    None = 0,
    Component = 1,
    Split = 2,
}

/// <summary>
/// Read-only afterburner data for one local JetNozzle. A range is deliberately
/// represented per stage so compatibility cannot be established by an aggregate
/// min/max that hides a mismatched or unpinned stage.
/// </summary>
internal readonly struct AfterburnerRangeSample
{
    public AfterburnerRangeSample(float start, float end)
    {
        (Start, End) = (start, end);
    }

    public float Start { get; }
    public float End { get; }
}

internal readonly struct AfterburnerNozzleSample
{
    public AfterburnerNozzleSample(
        bool capabilityReadable,
        bool hasAfterburner,
        bool rangesReadable,
        IReadOnlyList<AfterburnerRangeSample> ranges)
    {
        (CapabilityReadable, HasAfterburner, RangesReadable, Ranges) =
            (capabilityReadable, hasAfterburner, rangesReadable, ranges);
    }

    public bool CapabilityReadable { get; }
    public bool HasAfterburner { get; }
    public bool RangesReadable { get; }
    public IReadOnlyList<AfterburnerRangeSample> Ranges { get; }
}

internal static class AfterburnerCompatibility
{
    public static float ResolveDetentBoundary(
        AirframePreset? preset,
        bool liveRangeConfirmed,
        float liveStart)
    {
        var presetStart = preset?.AfterburnerStart ?? 1f;
        return liveRangeConfirmed && RangeIsFinite(liveStart)
            ? Math.Min(presetStart, liveStart)
            : presetStart;
    }

    /// <summary>
    /// Requires every local nozzle to expose the pinned preset range. Any
    /// unreadable, unpinned, mixed-capability, or mismatched nozzle rejects
    /// confirmation and leaves vanilla behavior unchanged.
    /// </summary>
    public static bool TryAggregatePinnedRanges(
        AirframePreset? preset,
        IReadOnlyList<AfterburnerNozzleSample> nozzles,
        out float start,
        out float end)
    {
        start = 0f;
        end = 0f;
        if (preset?.AfterburnerStart is not float expectedStart ||
            preset.AfterburnerEnd is not float expectedEnd ||
            preset.AfterburnerNozzleCount is not int expectedNozzleCount ||
            nozzles.Count != expectedNozzleCount)
        {
            return false;
        }

        var foundRange = false;
        var sawAfterburnerNozzle = false;
        var sawNonAfterburnerNozzle = false;
        foreach (var nozzle in nozzles)
        {
            if (!nozzle.CapabilityReadable)
            {
                return false;
            }

            if (!nozzle.HasAfterburner)
            {
                sawNonAfterburnerNozzle = true;
                continue;
            }

            sawAfterburnerNozzle = true;
            if (!nozzle.RangesReadable || nozzle.Ranges.Count == 0)
            {
                return false;
            }

            foreach (var range in nozzle.Ranges)
            {
                if (!RangeIsFinite(range.Start) ||
                    !RangeIsFinite(range.End) ||
                    Math.Abs(range.Start - expectedStart) > 0.0005f ||
                    Math.Abs(range.End - expectedEnd) > 0.0005f)
                {
                    return false;
                }

                if (!foundRange)
                {
                    start = range.Start;
                    end = range.End;
                    foundRange = true;
                }
                else
                {
                    start = Math.Min(start, range.Start);
                    end = Math.Max(end, range.End);
                }
            }
        }

        return sawAfterburnerNozzle &&
               !sawNonAfterburnerNozzle &&
               foundRange;
    }

    private static bool RangeIsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);
}

/// <summary>
/// The allowlist and its fail-open gating policy.
/// </summary>
internal static class AirframePresetCatalog
{
    private static readonly AirframePreset[] Presets =
    {
        new("AttackHelo1", "SAH-46 Chicane", true, AirbrakePath.None, false, null, null, null, null),
        new("Aryx_CargoPlane1", "MC-260 Chimera", false, AirbrakePath.Split, false, 0f, null, null, null),
        new("Aryx_F16M_KingViper", "F-16M King Viper", false, AirbrakePath.Component, true, 0f, 1, 0.9f, 1f),
        new("Aryx_Interceptor1", "FS-41 Eclipse", false, AirbrakePath.Component, true, 0f, 2, 0.9f, 1f),
        new("Aryx_LightFighter1", "F-99 Shrike", false, AirbrakePath.Component, true, 0f, 2, 0.9f, 1f),
        new("Aryx_PropAttacker1", "OA-27 Cavalier", false, AirbrakePath.Split, false, 0f, null, null, null),
        new("CAS1", "A-19 Brawler", false, AirbrakePath.Split, false, 0f, null, null, null),
        new("COIN", "CI-22 Cricket", false, AirbrakePath.None, false, null, null, null, null),
        new("Darkreach", "SFB-81 Darkreach", false, AirbrakePath.Split, false, 0f, null, null, null),
        new("EW1", "EW-25 Medusa", false, AirbrakePath.None, false, null, null, null, null),
        new("FastBomber1", "Alkyon AB-4", false, AirbrakePath.Split, true, 0f, 4, 0.9f, 1f),
        new("Multirole1", "KR-67 Ifrit", false, AirbrakePath.Split, true, 0f, 2, 0.9f, 1f),
        new("P_Trisurface1", "FS-3 Ternion", false, AirbrakePath.Split, true, 0f, 2, 0.9f, 1f),
        new("QuadVTOL1", "VL-49 Tarantula", true, AirbrakePath.None, false, null, null, null, null),
        new("Fighter1", "FS-12 Revoker", false, AirbrakePath.Component, true, 0f, 1, 0.9f, 1f),
        new("SmallFighter1", "FS-20 Vortex", false, AirbrakePath.Component, true, 0f, 1, 0.9f, 1f),
        new("trainer", "T/A-30 Compass", false, AirbrakePath.Component, false, 0f, null, null, null),
        new("UtilityHelo1", "UH-90 Ibis", true, AirbrakePath.None, false, null, null, null, null),
        new("VTOLTrainer1", "VT-7 Vagrant", false, AirbrakePath.Component, false, 0f, null, null, null),
    };

    private static readonly Dictionary<string, AirframePreset> PresetsById = CreateIndex();

    public static IReadOnlyList<AirframePreset> All => Presets;

    public static bool TryGet(string id, out AirframePreset preset)
    {
        if (string.IsNullOrEmpty(id))
        {
            preset = null!;
            return false;
        }

        return PresetsById.TryGetValue(id, out preset!);
    }

    /// <summary>Built-ins win; a matching custom profile is only a fallback for an otherwise unknown ID.</summary>
    public static bool TryGet(string id, CustomAirframeConfig custom, out AirframePreset preset)
    {
        if (TryGet(id, out preset))
        {
            return true;
        }

        return custom.Matches(id) && custom.TryCreatePreset(out preset);
    }

    /// <summary>
    /// Returns true for a pinned, non-collective aircraft with at least one detent.
    /// Live component checks still decide whether each individual gate can run.
    /// </summary>
    public static bool SupportsDetents(AirframePreset? preset, bool runtimeCollective) =>
        preset is not null &&
        !preset.Collective &&
        !runtimeCollective &&
        (preset.HasAirbrake || preset.HasAfterburner);

    /// <summary>
    /// Returns true only after the allowlisted preset and live component agree.
    /// This preserves vanilla behavior for unknown, collective, or unsupported systems.
    /// </summary>
    public static bool CanGate(
        AirframePreset? preset,
        AirframeFeature feature,
        bool runtimeCollective,
        bool liveFeatureConfirmed)
    {
        if (preset is null || preset.Collective || runtimeCollective || !liveFeatureConfirmed)
        {
            return false;
        }

        return feature switch
        {
            AirframeFeature.Airbrake => preset.HasAirbrake,
            AirframeFeature.Afterburner => preset.HasAfterburner,
            _ => false,
        };
    }

    public static bool AfterburnerRangeMatches(
        AirframePreset? preset,
        float liveStart,
        float liveEnd,
        float tolerance = 0.0005f)
    {
        if (preset?.AfterburnerStart is not float expectedStart ||
            preset.AfterburnerEnd is not float expectedEnd ||
            float.IsNaN(liveStart) || float.IsNaN(liveEnd) ||
            float.IsInfinity(liveStart) || float.IsInfinity(liveEnd))
        {
            return false;
        }

        var allowedError = Math.Max(0f, tolerance);
        return Math.Abs(liveStart - expectedStart) <= allowedError &&
               Math.Abs(liveEnd - expectedEnd) <= allowedError;
    }

    private static Dictionary<string, AirframePreset> CreateIndex()
    {
        // The installed game emits some jsonKey values with different casing at runtime
        // (for example, the Trainer definition is reported as "trainer"). The key remains
        // explicit; only casing is normalized so new IDs remain outside the allowlist.
        var index = new Dictionary<string, AirframePreset>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in Presets)
        {
            index.Add(preset.Id, preset);
        }

        return index;
    }
}
