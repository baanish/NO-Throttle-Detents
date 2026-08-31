namespace NuclearOptionDetents.Core;

internal enum RuntimeReadinessState
{
    Off,
    No,
    Unsupported,
    NotApplicable,
    Partial,
    Waiting,
    Caution,
    Likely,
}

internal readonly struct RuntimeReadinessInput
{
    public RuntimeReadinessInput(
        bool masterEnabled,
        bool idleEnabled,
        bool afterburnerEnabled,
        bool patchStatusKnown,
        bool throttleObserverActive,
        bool idleGateActive,
        bool afterburnerGateActive,
        bool hasPlayerAircraft,
        bool airframeSupported,
        bool isCollective,
        bool relativeThrottleMode,
        bool aircraftCapabilitiesKnown,
        bool hasAirbrake,
        bool hasAfterburner,
        bool interiorDetentsEnabled = false)
    {
        (MasterEnabled, IdleEnabled, AfterburnerEnabled, PatchStatusKnown,
            ThrottleObserverActive, IdleGateActive, AfterburnerGateActive,
            HasPlayerAircraft, AirframeSupported, IsCollective, RelativeThrottleMode,
            AircraftCapabilitiesKnown, HasAirbrake, HasAfterburner, InteriorDetentsEnabled) =
            (masterEnabled, idleEnabled, afterburnerEnabled, patchStatusKnown,
                throttleObserverActive, idleGateActive, afterburnerGateActive,
                hasPlayerAircraft, airframeSupported, isCollective, relativeThrottleMode,
                aircraftCapabilitiesKnown, hasAirbrake, hasAfterburner, interiorDetentsEnabled);
    }

    public bool MasterEnabled { get; }
    public bool IdleEnabled { get; }
    public bool AfterburnerEnabled { get; }
    public bool PatchStatusKnown { get; }
    public bool ThrottleObserverActive { get; }
    public bool IdleGateActive { get; }
    public bool AfterburnerGateActive { get; }
    public bool HasPlayerAircraft { get; }
    public bool AirframeSupported { get; }
    public bool IsCollective { get; }
    public bool RelativeThrottleMode { get; }
    public bool AircraftCapabilitiesKnown { get; }
    public bool HasAirbrake { get; }
    public bool HasAfterburner { get; }
    public bool InteriorDetentsEnabled { get; }
}

internal readonly struct RuntimeReadinessResult
{
    public RuntimeReadinessResult(RuntimeReadinessState state, string displayText)
    {
        (State, DisplayText) = (state, displayText);
    }

    public RuntimeReadinessState State { get; }
    public string DisplayText { get; }
}

internal static class RuntimeReadinessPolicy
{
    public static bool AreEnabledCapabilitiesKnown(
        bool hasPreset,
        bool idleEnabled,
        bool afterburnerEnabled,
        bool presetHasAirbrake,
        bool presetHasAfterburner,
        bool airbrakeConfirmed,
        bool afterburnerConfirmed) =>
        hasPreset &&
        (!idleEnabled || !presetHasAirbrake || airbrakeConfirmed) &&
        (!afterburnerEnabled || !presetHasAfterburner || afterburnerConfirmed);

    public static RuntimeReadinessResult Evaluate(RuntimeReadinessInput input)
    {
        if (!input.MasterEnabled)
        {
            return Result(RuntimeReadinessState.Off, "OFF - Mod disabled");
        }

        if (!input.IdleEnabled && !input.AfterburnerEnabled && !input.InteriorDetentsEnabled)
        {
            return Result(RuntimeReadinessState.Off, "OFF - Both detents disabled");
        }

        if (!input.PatchStatusKnown)
        {
            return Result(RuntimeReadinessState.Waiting, "WAITING - Mod is starting");
        }

        if (!input.ThrottleObserverActive)
        {
            return Result(RuntimeReadinessState.No, "NO - Throttle observer unavailable");
        }

        if (!input.HasPlayerAircraft)
        {
            return Result(RuntimeReadinessState.Waiting, "WAITING - Start or resume a flight");
        }

        if (!input.AirframeSupported)
        {
            return Result(RuntimeReadinessState.Unsupported, "UNSUPPORTED - Not in preset");
        }

        if (input.IsCollective)
        {
            return Result(RuntimeReadinessState.NotApplicable, "NOT APPLICABLE - Collective aircraft");
        }

        if (!input.RelativeThrottleMode)
        {
            return Result(RuntimeReadinessState.NotApplicable, "NOT APPLICABLE - Relative throttle only");
        }

        if (!input.AircraftCapabilitiesKnown)
        {
            return Result(RuntimeReadinessState.Caution, "CAUTION - Systems not confirmed");
        }

        var idleApplies = input.IdleEnabled && input.HasAirbrake;
        var afterburnerApplies = input.AfterburnerEnabled && input.HasAfterburner;
        if (!idleApplies && !afterburnerApplies && !input.InteriorDetentsEnabled)
        {
            if (!input.HasAirbrake && !input.HasAfterburner)
            {
                return Result(RuntimeReadinessState.NotApplicable, "NOT APPLICABLE - No matching capability");
            }

            return Result(RuntimeReadinessState.Off, "OFF - Matching detent disabled");
        }

        var idleUnavailable = idleApplies && !input.IdleGateActive;
        var afterburnerUnavailable = afterburnerApplies && !input.AfterburnerGateActive;
        if (idleUnavailable && afterburnerUnavailable)
        {
            return Result(RuntimeReadinessState.No, "NO - Required detent patches unavailable");
        }

        if (idleUnavailable)
        {
            var state = afterburnerApplies ? RuntimeReadinessState.Partial : RuntimeReadinessState.No;
            var prefix = state == RuntimeReadinessState.Partial ? "PARTIAL" : "NO";
            return Result(state, $"{prefix} - Idle detent unavailable");
        }

        if (afterburnerUnavailable)
        {
            var state = idleApplies ? RuntimeReadinessState.Partial : RuntimeReadinessState.No;
            var prefix = state == RuntimeReadinessState.Partial ? "PARTIAL" : "NO";
            return Result(state, $"{prefix} - Afterburner detent unavailable");
        }

        if (input.InteriorDetentsEnabled)
        {
            return idleApplies || afterburnerApplies
                ? Result(RuntimeReadinessState.Likely, "LIKELY - Preset and custom detents")
                : Result(RuntimeReadinessState.Likely, "LIKELY - Custom detents");
        }

        if (idleApplies && afterburnerApplies)
        {
            return Result(RuntimeReadinessState.Likely, "LIKELY - Airbrake and afterburner");
        }

        return idleApplies
            ? Result(RuntimeReadinessState.Likely, "LIKELY - Airbrake detent")
            : Result(RuntimeReadinessState.Likely, "LIKELY - Afterburner detent");
    }

    private static RuntimeReadinessResult Result(RuntimeReadinessState state, string displayText) =>
        new(state, displayText);
}
