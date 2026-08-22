using System;
using System.Collections.Generic;
using BepInEx.Logging;
using NuclearOptionDetents.Compatibility;
using NuclearOptionDetents.Config;
using Rewired;
using UnityEngine;

namespace NuclearOptionDetents.Core;

/// <summary>
/// Single owner of the local pilot's detent state. Harmony patches only report what they saw here,
/// which keeps every gating decision (preset, live components, player settings, hold timers) in one place
/// and keeps remote aircraft out of it entirely.
/// </summary>
internal static class RuntimeController
{
    private static ModConfig? _config;
    private static ManualLogSource? _log;
    private static DetentRuntime _runtime = new();
    private static readonly List<JetNozzle> AfterburnerCandidates = new();
    private static readonly List<AfterburnerNozzleSample> AfterburnerSamples = new();
    private static EffectiveSettings _settings;
    private static bool _hasSettings;
    private static DetentIndicatorSnapshot _indicator = DetentIndicatorSnapshot.Hidden;
    private static bool _hasEffectiveSimulatedThrottle;
    private static double _effectiveSimulatedThrottle;
    private static PilotPlayerState? _localState;
    private static Aircraft? _localAircraft;
    private static ControlInputs? _localInputs;
    private static bool _localCollective;
    private static AirframePreset? _activePreset;
    private static string _airframeId = string.Empty;
    private static string _airframeName = string.Empty;
    private static bool _aircraftCapabilitiesKnown;
    private static bool _liveAirbrakeComponentConfirmed;
    private static bool _liveSplitAirbrakeConfirmed;
    private static bool _liveAfterburnerConfirmed;
    private static float _liveAfterburnerStart;
    private static float _liveAfterburnerEnd;
    private static bool _afterburnerRangeMatchesPreset;
    private static int _airbrakeConfirmedFrame = -1;
    private static int _afterburnerConfirmedFrame = -1;
    private static bool _hasAirbrake;
    private static bool _hasAfterburner;
    private static int _lastObservedFrame = -1;
    private static float _lastObservedSimulationTime;
    private static bool _hasLastObservedSimulationTime;
    private static int _componentGateInputFrame = -1;
    private static bool _componentGateInputValid;
    private static bool _componentGateControlsEnabled;
    private static bool _componentGatePaused;
    private static bool _componentGateAxisModifierHeld;
    private static ThrottleCommand _componentGateCommand;
    private static bool _patchStatusKnown;
    private static bool _throttleObserverActive;
    private static bool _airbrakeComponentGateActive;
    private static bool _splitAirbrakeGateActive;
    private static bool _idleGateActive;
    private static bool _afterburnerGateActive;
    private static readonly HashSet<string> ReportedFailures = new();

    /// <summary>Called once at plugin load; the reset leaves the runtime in the same state as leaving an aircraft.</summary>
    public static void Initialize(ModConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
        _patchStatusKnown = false;
        ResetAll("initialization");
    }

    /// <summary>Status line for the config UI. Never throws: a failed check reports caution rather than breaking the settings window.</summary>
    public static RuntimeReadinessResult BestEffortReadiness
    {
        get
        {
            try
            {
                var settings = ReadSettings();
                var hasPlayerAircraft = !ReferenceEquals(_localState, null) &&
                                        !ReferenceEquals(_localAircraft, null) &&
                                        !ReferenceEquals(_localInputs, null);
                return RuntimeReadinessPolicy.Evaluate(new RuntimeReadinessInput(
                    settings.Enabled,
                    settings.IdleEnabled,
                    settings.AfterburnerEnabled,
                    _patchStatusKnown,
                    _throttleObserverActive,
                    _idleGateActive,
                    _afterburnerGateActive,
                    hasPlayerAircraft,
                    _activePreset is not null,
                    _localCollective,
                    PlayerSettings.throttleUseRelative,
                    _aircraftCapabilitiesKnown,
                    _hasAirbrake,
                    _hasAfterburner));
            }
            catch
            {
                return new RuntimeReadinessResult(
                    RuntimeReadinessState.Caution,
                    "CAUTION - Status check failed");
            }
        }
    }

    public static string CurrentAircraftDisplayName =>
        string.IsNullOrWhiteSpace(_airframeName) ? "No aircraft" : _airframeName;

    /// <summary>Last evaluated HUD state; hidden whenever the runtime resets, so a stale line cannot survive an aircraft or scene change.</summary>
    public static DetentIndicatorSnapshot IndicatorSnapshot => _indicator;

    /// <summary>Records which patches actually installed; a gate whose patch is missing never claims to be active.</summary>
    public static void SetPatchStatus(
        bool throttleObserverActive,
        bool airbrakeComponentGateActive,
        bool splitAirbrakeGateActive,
        bool afterburnerGateActive)
    {
        _throttleObserverActive = throttleObserverActive;
        _airbrakeComponentGateActive = airbrakeComponentGateActive;
        _splitAirbrakeGateActive = splitAirbrakeGateActive;
        _afterburnerGateActive = afterburnerGateActive;
        _patchStatusKnown = true;
        RefreshApplicableCapabilities();
    }

    /// <summary>
    /// The per-frame entry point: resolves the local aircraft, applies throttle sensitivity, advances the
    /// detent state machine, pins the boundary hold, and publishes the HUD snapshot.
    /// <paramref name="externalRelativeThrottleIntegrator"/> means another mod replaced vanilla's throttle
    /// integration this frame; detents then run only if that mod is known to keep vanilla's signed
    /// accumulator (<paramref name="externalUsesSignedMapping"/>), because pinning an unknown mapping
    /// would move the throttle somewhere the player did not ask for.
    /// </summary>
    public static void ObserveThrottle(
        PilotPlayerState state,
        bool externalRelativeThrottleIntegrator,
        bool externalUsesSignedMapping)
    {
        try
        {
            var playerAccessor = RuntimeCompatibility.PilotPlayer;
            var collectiveAccessor = RuntimeCompatibility.PilotCollective;
            var pilotAccessor = RuntimeCompatibility.PilotStatePilot;
            var aircraftAccessor = RuntimeCompatibility.PilotOwnedAircraft;
            var inputsAccessor = RuntimeCompatibility.PilotControlInputs;
            var simulatedThrottleAccessor = RuntimeCompatibility.PilotSimulatedThrottle;
            if (playerAccessor is null || collectiveAccessor is null || pilotAccessor is null ||
                aircraftAccessor is null || inputsAccessor is null || simulatedThrottleAccessor is null)
            {
                ResetAll("throttle accessors unavailable");
                return;
            }

            var player = playerAccessor(state);
            var collective = collectiveAccessor(state);
            var pilot = pilotAccessor(state);
            var aircraft = ReferenceEquals(pilot, null) ? null : aircraftAccessor(pilot);
            var inputs = inputsAccessor(state);
            if (ReferenceEquals(player, null) || !IsLiveUnityObject(aircraft) || ReferenceEquals(inputs, null))
            {
                ResetAll("local input reference lost");
                return;
            }

            var settings = ReadSettings();
            EnsureSettings(settings);
            var simulationTime = Time.time;
            if (!ReferenceEquals(_localState, state) ||
                !ReferenceEquals(_localAircraft, aircraft) ||
                !ReferenceEquals(_localInputs, inputs) ||
                collective != _localCollective)
            {
                _localState = state;
                _localAircraft = aircraft;
                _localInputs = inputs;
                _localCollective = collective;
                _lastObservedFrame = -1;
                _lastObservedSimulationTime = 0f;
                _hasLastObservedSimulationTime = false;
                _hasEffectiveSimulatedThrottle = false;
                _indicator = DetentIndicatorSnapshot.Hidden;
                ResolveAirframePreset(aircraft!);
                ResetAircraftCapabilities();
                RefreshApplicableCapabilities();
                RebuildRuntime(settings);
                _runtime.ObserveContext(aircraft, inputs);
                if (settings.DebugLogging && _log is not null)
                {
                    _log.LogInfo(
                        $"Detents attached to {_airframeName} ({_airframeId}): " +
                        $"allowlisted={_activePreset is not null}, airbrake={_hasAirbrake}, afterburner={_hasAfterburner}, " +
                        $"throttleUseNegative={PlayerSettings.throttleUseNegative}");
                }
            }

            var frame = Time.frameCount;
            var rawThrottle = player.GetAxisRaw("Throttle");
            var axisModifierHeld = player.GetButton("Axis Modifier");
            var requestedThrottle = inputs.throttle;
            var relativeThrottle = PlayerSettings.throttleUseRelative;
            var reverseDirection = collective && PlayerSettings.invertCollective;
            var controlsEnabled = GameManager.flightControlsEnabled;
            var paused = Time.timeScale <= 0f;
            var command = ThrottleCommands.FromRawAxis(
                rawThrottle,
                settings.CommandThreshold,
                reverseDirection);
            CacheComponentGateInput(frame, controlsEnabled, paused, axisModifierHeld, command);
            var airframeAllowsDetents = AirframeAllowsDetents();
            var compatibleIntegrator = !externalRelativeThrottleIntegrator || externalUsesSignedMapping;
            var detentsAllowed = airframeAllowsDetents && relativeThrottle && compatibleIntegrator;
            var idleApplies = settings.IdleEnabled && _hasAirbrake;
            var afterburnerApplies = settings.AfterburnerEnabled && _hasAfterburner;

            var simulatedThrottleBefore = simulatedThrottleAccessor(state);
            var simulatedThrottleRange = externalUsesSignedMapping || PlayerSettings.throttleUseNegative
                ? SimulatedThrottleRange.NegativeOneToOne
                : SimulatedThrottleRange.ZeroToOne;
            var hasLiveDetentCapability = _hasAirbrake || _hasAfterburner;
            var sensitivityEnabled = settings.ThrottleSensitivity != 1f &&
                                     RelativeThrottleSensitivity.ShouldApply(
                settings.Enabled,
                relativeThrottle,
                airframeAllowsDetents && hasLiveDetentCapability,
                controlsEnabled,
                paused,
                axisModifierHeld,
                externalRelativeThrottleIntegrator,
                _hasEffectiveSimulatedThrottle);
            var effectiveSimulatedThrottle = RelativeThrottleSensitivity.Apply(
                _effectiveSimulatedThrottle,
                simulatedThrottleBefore,
                rawThrottle,
                Time.deltaTime,
                settings.ThrottleSensitivity,
                sensitivityEnabled);
            if (sensitivityEnabled)
            {
                simulatedThrottleAccessor(state) = (float)effectiveSimulatedThrottle;
                requestedThrottle = (float)SimulatedThrottleMapping.ToPublic(
                    effectiveSimulatedThrottle,
                    simulatedThrottleRange);
                inputs.throttle = requestedThrottle;
            }

            var snapshot = _runtime.Update(new DetentRuntimeInput(
                simulationTime,
                requestedThrottle,
                command,
                settings.Enabled && detentsAllowed,
                idleApplies,
                afterburnerApplies,
                controlsEnabled,
                paused,
                axisModifierHeld,
                relativeThrottle));
            var boundaryHold = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
                requestedThrottle,
                effectiveSimulatedThrottle,
                command,
                snapshot.IdleState,
                snapshot.AfterburnerState,
                _runtime.IdleDetent.Boundary,
                _runtime.AfterburnerDetent.Boundary,
                settings.EndpointEpsilon,
                settings.Enabled && detentsAllowed,
                relativeThrottle,
                simulatedThrottleRange,
                controlsEnabled,
                paused,
                axisModifierHeld,
                idleApplies,
                afterburnerApplies));
            if (boundaryHold.IsHeld)
            {
                inputs.throttle = (float)boundaryHold.EffectiveThrottle;
            }
            if (boundaryHold.ShouldPinSimulatedThrottle)
            {
                effectiveSimulatedThrottle = boundaryHold.SimulatedThrottle;
                simulatedThrottleAccessor(state) = (float)effectiveSimulatedThrottle;
            }

            _effectiveSimulatedThrottle = effectiveSimulatedThrottle;
            _hasEffectiveSimulatedThrottle = true;
            _indicator = DetentIndicatorPolicy.Evaluate(
                snapshot,
                inputs.throttle,
                _runtime.IdleDetent.Boundary,
                _runtime.AfterburnerDetent.Boundary,
                settings.EndpointEpsilon,
                settings.IdleHoldMilliseconds,
                settings.AfterburnerHoldMilliseconds,
                settings.IndicatorEnabled,
                boundaryHold.IsHeld,
                idleApplies,
                afterburnerApplies);
            _lastObservedFrame = frame;
            _lastObservedSimulationTime = simulationTime;
            _hasLastObservedSimulationTime = true;
        }
        catch (Exception exception)
        {
            ResetAll("throttle observation failed");
            LogFailureOnce("Throttle observation", exception);
        }
    }

    /// <summary>Covers frames the throttle observer missed: if controls are interrupted, pending holds are cancelled instead of resuming mid-count.</summary>
    public static void ObserveControlFrame(PilotPlayerState state)
    {
        if (!ReferenceEquals(_localState, state) || _lastObservedFrame == Time.frameCount)
        {
            return;
        }

        var strengthAccessor = RuntimeCompatibility.PilotStrength;
        var controlsInterrupted = !GameManager.flightControlsEnabled ||
                                  Time.timeScale <= 0f ||
                                  strengthAccessor is not null && strengthAccessor(state) < 0.2f;
        if (controlsInterrupted)
        {
            _runtime.CancelPendingHolds();
            _indicator = DetentIndicatorSnapshot.Hidden;
            InvalidateComponentGateInput();
        }
    }

    /// <summary>Suppresses the local aircraft's airbrake only while the idle detent is holding at zero throttle; anything unconfirmed returns false and stays vanilla.</summary>
    public static bool ShouldInhibitAirbrake(Airbrake airbrake, ControlInputs inputs, float originalThrottle)
    {
        try
        {
            if (!_hasSettings || !_hasAirbrake || originalThrottle != 0f ||
                !PlayerSettings.throttleUseRelative)
            {
                return false;
            }

            var local = IsLocalAirbrake(airbrake);
            if (!local)
            {
                return false;
            }

            var settings = _settings;
            var inputAllowsBlock = ComponentGateInputAllowsBlock(DetentDirection.Lower);
            var patchActive = _airbrakeComponentGateActive;
            var stateMachineBlocks = _airbrakeConfirmedFrame == Time.frameCount ||
                                     _runtime.Snapshot.AirbrakeInhibited;
            return patchActive && settings.Enabled && settings.IdleEnabled &&
                   inputAllowsBlock && stateMachineBlocks;
        }
        catch (Exception exception)
        {
            LogFailureOnce("Airbrake gate", exception);
            return false;
        }
    }

    /// <summary>Confirms the preset's component airbrake exists on the live aircraft; the gate stays inactive until this fires.</summary>
    public static void ObserveAirbrake(Airbrake airbrake)
    {
        if (_activePreset?.AirbrakePath != AirbrakePath.Component ||
            !IsLocalAirbrake(airbrake) || _liveAirbrakeComponentConfirmed)
        {
            return;
        }

        _liveAirbrakeComponentConfirmed = true;
        _airbrakeConfirmedFrame = Time.frameCount;
        RefreshApplicableCapabilities();
    }

    /// <summary>Split-surface counterpart of <see cref="ShouldInhibitAirbrake"/>, gated on its own patch and live confirmation.</summary>
    public static bool ShouldInhibitSplitAirbrake(
        ControlSurface controlSurface,
        ControlInputs inputs,
        float originalThrottle)
    {
        try
        {
            if (!_hasSettings || !_hasAirbrake || originalThrottle != 0f ||
                !PlayerSettings.throttleUseRelative)
            {
                return false;
            }

            var local = IsLocalControlSurface(controlSurface);
            if (!local)
            {
                return false;
            }

            var settings = _settings;
            var inputAllowsBlock = ComponentGateInputAllowsBlock(DetentDirection.Lower);
            var patchActive = _splitAirbrakeGateActive;
            var stateMachineBlocks = _airbrakeConfirmedFrame == Time.frameCount ||
                                     _runtime.Snapshot.AirbrakeInhibited;
            return patchActive && settings.Enabled && settings.IdleEnabled &&
                   inputAllowsBlock && stateMachineBlocks;
        }
        catch (Exception exception)
        {
            LogFailureOnce("Split-airbrake gate", exception);
            return false;
        }
    }

    /// <summary>Confirms the preset's split airbrake surfaces on the live aircraft.</summary>
    public static void ObserveSplitAirbrake(ControlSurface controlSurface)
    {
        if (_activePreset?.AirbrakePath != AirbrakePath.Split ||
            !IsLocalControlSurface(controlSurface) || _liveSplitAirbrakeConfirmed)
        {
            return;
        }

        _liveSplitAirbrakeConfirmed = true;
        _airbrakeConfirmedFrame = Time.frameCount;
        RefreshApplicableCapabilities();
    }

    /// <summary>
    /// Collects the local aircraft's nozzles until all expected ones match the preset's afterburner range.
    /// Only then is the detent boundary retargeted to the confirmed live start, so a mismatched airframe
    /// keeps vanilla afterburner behavior.
    /// </summary>
    public static void ObserveAfterburner(JetNozzle nozzle, Aircraft aircraft)
    {
        if (_afterburnerRangeMatchesPreset || _activePreset?.HasAfterburner != true ||
            _localCollective || !IsLocalAircraft(aircraft))
        {
            return;
        }

        if (!AddAfterburnerCandidate(nozzle))
        {
            return;
        }

        var previousBoundary = _runtime.AfterburnerDetent.Boundary;
        RefreshAfterburnerSamples();
        RefreshApplicableCapabilities();
        if (!_afterburnerRangeMatchesPreset)
        {
            return;
        }

        _afterburnerConfirmedFrame = Time.frameCount;

        var confirmedBoundary = AfterburnerCompatibility.ResolveDetentBoundary(
            _activePreset,
            liveRangeConfirmed: true,
            _liveAfterburnerStart);
        if (_hasSettings && Math.Abs(previousBoundary - confirmedBoundary) > 1e-9)
        {
            _runtime.RetargetAfterburnerBoundary(confirmedBoundary);
        }

    }

    /// <summary>Can only turn an existing vanilla allow into a block for the local aircraft; it never enables afterburner vanilla refused.</summary>
    public static bool ShouldBlockAfterburner(Aircraft aircraft, bool vanillaAllowAfterburner)
    {
        try
        {
            if (!_hasSettings || !_hasAfterburner || !vanillaAllowAfterburner ||
                !PlayerSettings.throttleUseRelative)
            {
                return false;
            }

            var local = IsLocalAircraft(aircraft);
            if (!local)
            {
                return false;
            }

            var settings = _settings;
            var inputAllowsBlock = ComponentGateInputAllowsBlock(DetentDirection.Upper);
            var patchActive = _afterburnerGateActive;
            var stateMachineBlocks = _afterburnerConfirmedFrame == Time.frameCount ||
                                     !_runtime.Snapshot.AfterburnerUnlocked;
            return patchActive && settings.Enabled && settings.AfterburnerEnabled &&
                   inputAllowsBlock && stateMachineBlocks;
        }
        catch (Exception exception)
        {
            LogFailureOnce("Afterburner gate", exception);
            return false;
        }
    }

    /// <summary>Ignores other pilots' states, so only the local player leaving the seat clears the runtime.</summary>
    public static void ResetIfLocalState(PilotPlayerState state)
    {
        if (ReferenceEquals(_localState, state))
        {
            ResetAll("leaving player pilot state");
        }
    }

    /// <summary>Returns every cached aircraft, capability, throttle, and indicator value to its unattached default; the reason is logged only under debug logging.</summary>
    public static void ResetAll(string reason)
    {
        var shouldLog = !ReferenceEquals(_localState, null) &&
                        _hasSettings && _settings.DebugLogging && _log is not null;
        _runtime.ResetLifecycle();
        _localState = null;
        _localAircraft = null;
        _localInputs = null;
        _localCollective = false;
        _activePreset = null;
        _airframeId = string.Empty;
        _airframeName = string.Empty;
        _aircraftCapabilitiesKnown = false;
        _liveAirbrakeComponentConfirmed = false;
        _liveSplitAirbrakeConfirmed = false;
        _liveAfterburnerConfirmed = false;
        _liveAfterburnerStart = 0f;
        _liveAfterburnerEnd = 0f;
        _afterburnerRangeMatchesPreset = false;
        AfterburnerCandidates.Clear();
        AfterburnerSamples.Clear();
        _airbrakeConfirmedFrame = -1;
        _afterburnerConfirmedFrame = -1;
        _hasAirbrake = false;
        _hasAfterburner = false;
        _indicator = DetentIndicatorSnapshot.Hidden;
        _hasEffectiveSimulatedThrottle = false;
        _effectiveSimulatedThrottle = 0;
        _lastObservedFrame = -1;
        _lastObservedSimulationTime = 0f;
        _hasLastObservedSimulationTime = false;
        InvalidateComponentGateInput();
        if (shouldLog)
        {
            _log!.LogInfo($"Detent state reset: {reason}");
        }
    }

    public static void ReportPatchFailure(string patchName, Exception exception) =>
        LogFailureOnce($"{patchName} patch", exception);

    public static bool ShouldInspectAirbrakeCandidates =>
        _hasSettings && _settings.Enabled && _settings.IdleEnabled &&
        _activePreset?.HasAirbrake == true && !_localCollective && PlayerSettings.throttleUseRelative;

    public static bool ShouldInspectComponentAirbrake =>
        ShouldInspectAirbrakeCandidates && _activePreset?.AirbrakePath == AirbrakePath.Component;

    public static bool ShouldInspectSplitAirbrake =>
        ShouldInspectAirbrakeCandidates && _activePreset?.AirbrakePath == AirbrakePath.Split;

    public static bool ShouldInspectAfterburnerCandidates =>
        _hasSettings && _settings.Enabled && _settings.AfterburnerEnabled &&
        _activePreset?.HasAfterburner == true && !_localCollective && PlayerSettings.throttleUseRelative;

    public static bool NeedsAfterburnerConfirmation =>
        ShouldInspectAfterburnerCandidates && !_afterburnerRangeMatchesPreset;

    private static EffectiveSettings ReadSettings()
    {
        if (_config is null)
        {
            throw new InvalidOperationException("Runtime controller is not initialized.");
        }

        return _config.ReadEffective();
    }

    private static void EnsureSettings(EffectiveSettings settings)
    {
        if (_hasSettings && _settings.Equals(settings))
        {
            return;
        }

        var timingChanged = !_hasSettings ||
                            _settings.IdleHoldMilliseconds != settings.IdleHoldMilliseconds ||
                            _settings.AfterburnerHoldMilliseconds != settings.AfterburnerHoldMilliseconds ||
                            !_settings.EndpointEpsilon.Equals(settings.EndpointEpsilon) ||
                            !_settings.ResetHysteresis.Equals(settings.ResetHysteresis);
        _settings = settings;
        _hasSettings = true;
        if (timingChanged)
        {
            _runtime.Reconfigure(
                settings.IdleHoldMilliseconds,
                settings.AfterburnerHoldMilliseconds,
                settings.EndpointEpsilon,
                settings.ResetHysteresis);
        }

        RefreshApplicableCapabilities();
    }

    private static void RebuildRuntime(EffectiveSettings settings)
    {
        var idleBoundary = _activePreset?.IdleAirbrakeBoundary ?? 0f;
        var afterburnerBoundary = AfterburnerCompatibility.ResolveDetentBoundary(
            _activePreset,
            _afterburnerRangeMatchesPreset,
            _liveAfterburnerStart);
        _runtime = new DetentRuntime(
            settings.IdleHoldMilliseconds,
            settings.AfterburnerHoldMilliseconds,
            settings.EndpointEpsilon,
            settings.ResetHysteresis,
            idleBoundary,
            afterburnerBoundary);
        _lastObservedFrame = -1;
        _lastObservedSimulationTime = 0f;
        _hasLastObservedSimulationTime = false;
        _indicator = DetentIndicatorSnapshot.Hidden;
        _hasEffectiveSimulatedThrottle = false;
        InvalidateComponentGateInput();
        if (!ReferenceEquals(_localAircraft, null) && !ReferenceEquals(_localInputs, null))
        {
            _runtime.ObserveContext(_localAircraft, _localInputs);
        }
    }

    private static void ResolveAirframePreset(Aircraft aircraft)
    {
        _activePreset = null;
        _airframeId = string.Empty;
        _airframeName = string.Empty;
        if (!RuntimeCompatibility.TryGetAirframeIdentity(aircraft, out _airframeId, out _airframeName))
        {
            return;
        }

        AirframePresetCatalog.TryGet(_airframeId, out _activePreset!);
    }

    /// <summary>Preset-level eligibility only; live component confirmation is tracked separately in the capability flags.</summary>
    private static bool AirframeAllowsDetents() =>
        AirframePresetCatalog.SupportsDetents(_activePreset, _localCollective);

    private static void RefreshApplicableCapabilities()
    {
        var airbrakeConfirmed = AirbrakeCapabilityPaths.IsConfirmed(
            _activePreset?.AirbrakePath ?? AirbrakePath.None,
            _liveAirbrakeComponentConfirmed,
            _liveSplitAirbrakeConfirmed);
        _idleGateActive = AirbrakeCapabilityPaths.HasActiveGate(
            _activePreset?.AirbrakePath ?? AirbrakePath.None,
            _liveAirbrakeComponentConfirmed,
            _airbrakeComponentGateActive,
            _liveSplitAirbrakeConfirmed,
            _splitAirbrakeGateActive);
        _hasAirbrake = AirframePresetCatalog.CanGate(
            _activePreset,
            AirframeFeature.Airbrake,
            _localCollective,
            airbrakeConfirmed);
        _afterburnerRangeMatchesPreset = _liveAfterburnerConfirmed &&
                                         AirframePresetCatalog.AfterburnerRangeMatches(
                                             _activePreset,
                                             _liveAfterburnerStart,
                                             _liveAfterburnerEnd);
        _hasAfterburner = AirframePresetCatalog.CanGate(
            _activePreset,
            AirframeFeature.Afterburner,
            _localCollective,
            _afterburnerRangeMatchesPreset);
        _aircraftCapabilitiesKnown = _hasSettings && RuntimeReadinessPolicy.AreEnabledCapabilitiesKnown(
            _activePreset is not null,
            _settings.IdleEnabled,
            _settings.AfterburnerEnabled,
            _activePreset?.HasAirbrake == true,
            _activePreset?.HasAfterburner == true,
            airbrakeConfirmed,
            _afterburnerRangeMatchesPreset);
    }

    private static void ResetAircraftCapabilities()
    {
        _airbrakeConfirmedFrame = -1;
        _afterburnerConfirmedFrame = -1;
        _liveAirbrakeComponentConfirmed = false;
        _liveSplitAirbrakeConfirmed = false;
        _liveAfterburnerConfirmed = false;
        _liveAfterburnerStart = 0f;
        _liveAfterburnerEnd = 0f;
        _afterburnerRangeMatchesPreset = false;
        AfterburnerCandidates.Clear();
        AfterburnerSamples.Clear();
    }

    private static bool AddAfterburnerCandidate(JetNozzle nozzle)
    {
        foreach (var candidate in AfterburnerCandidates)
        {
            if (ReferenceEquals(candidate, nozzle))
            {
                return false;
            }
        }

        AfterburnerCandidates.Add(nozzle);
        return true;
    }

    private static void RefreshAfterburnerSamples()
    {
        for (var index = AfterburnerCandidates.Count - 1; index >= 0; index--)
        {
            if (!IsLiveUnityObject(AfterburnerCandidates[index]))
            {
                AfterburnerCandidates.RemoveAt(index);
            }
        }

        AfterburnerSamples.Clear();
        foreach (var nozzle in AfterburnerCandidates)
        {
            if (!RuntimeCompatibility.TryGetAfterburnerCapability(nozzle, out var hasAfterburner))
            {
                AfterburnerSamples.Add(new AfterburnerNozzleSample(
                    capabilityReadable: false,
                    hasAfterburner: false,
                    rangesReadable: false,
                    ranges: Array.Empty<AfterburnerRangeSample>()));
                continue;
            }

            var ranges = new List<AfterburnerThrottleRange>();
            var rangesReadable = RuntimeCompatibility.TryGetAfterburnerThrottleRanges(nozzle, ranges);
            var rangeSamples = new List<AfterburnerRangeSample>(ranges.Count);
            foreach (var range in ranges)
            {
                rangeSamples.Add(new AfterburnerRangeSample(range.Start, range.End));
            }

            AfterburnerSamples.Add(new AfterburnerNozzleSample(
                capabilityReadable: true,
                hasAfterburner,
                rangesReadable,
                rangeSamples));
        }

        _liveAfterburnerConfirmed = AfterburnerCompatibility.TryAggregatePinnedRanges(
            _activePreset,
            AfterburnerSamples,
            out _liveAfterburnerStart,
            out _liveAfterburnerEnd);
    }

    private static bool IsLocalAirbrake(Airbrake airbrake)
    {
        var serializedAircraftAccessor = RuntimeCompatibility.AirbrakeSerializedAircraft;
        var attachedAircraftAccessor = RuntimeCompatibility.AirbrakeAttachedAircraft;
        return IsLiveUnityObject(_localAircraft) &&
               serializedAircraftAccessor is not null && attachedAircraftAccessor is not null &&
               ReferenceOwnership.AirbrakeMatches(
                   _localAircraft,
                   serializedAircraftAccessor(airbrake),
                   attachedAircraftAccessor(airbrake));
    }

    private static bool IsLocalControlSurface(ControlSurface controlSurface)
    {
        var aircraftAccessor = RuntimeCompatibility.ControlSurfaceAircraft;
        return IsLiveUnityObject(_localAircraft) && aircraftAccessor is not null &&
               ReferenceOwnership.AircraftMatches(
            _localAircraft,
            aircraftAccessor(controlSurface));
    }

    private static bool IsLocalAircraft(Aircraft? aircraft)
    {
        if (!IsLiveUnityObject(_localAircraft) || !IsLiveUnityObject(aircraft))
        {
            return false;
        }

        return ReferenceOwnership.AircraftMatches(
            _localAircraft,
            aircraft);
    }

    private static bool IsLiveUnityObject(object? value) =>
        !ReferenceEquals(value, null) &&
        (value is not UnityEngine.Object unityObject || unityObject != null);

    private static bool ComponentGateInputAllowsBlock(DetentDirection direction)
    {
        if (!TryGetComponentGateInput(out var controlsEnabled, out var paused, out var axisModifierHeld, out var command))
        {
            return false;
        }

        var observerAgeSeconds = _hasLastObservedSimulationTime
            ? Time.time - _lastObservedSimulationTime
            : double.PositiveInfinity;
        return ComponentGatePolicy.AllowsBlock(
            controlsEnabled,
            paused,
            axisModifierHeld,
            command,
            direction,
            observerAgeSeconds);
    }

    private static bool TryGetComponentGateInput(
        out bool controlsEnabled,
        out bool paused,
        out bool axisModifierHeld,
        out ThrottleCommand command)
    {
        var frame = Time.frameCount;
        if (_componentGateInputFrame != frame)
        {
            _componentGateInputFrame = frame;
            _componentGateInputValid = false;
            var playerAccessor = RuntimeCompatibility.PilotPlayer;
            if (playerAccessor is not null && !ReferenceEquals(_localState, null))
            {
                var player = playerAccessor(_localState);
                if (!ReferenceEquals(player, null))
                {
                    var rawThrottle = player.GetAxisRaw("Throttle");
                    var reverseDirection = _localCollective && PlayerSettings.invertCollective;
                    _componentGateControlsEnabled = GameManager.flightControlsEnabled;
                    _componentGatePaused = Time.timeScale <= 0f;
                    _componentGateAxisModifierHeld = player.GetButton("Axis Modifier");
                    _componentGateCommand = ThrottleCommands.FromRawAxis(
                        rawThrottle,
                        _settings.CommandThreshold,
                        reverseDirection);
                    _componentGateInputValid = true;
                }
            }
        }

        controlsEnabled = _componentGateControlsEnabled;
        paused = _componentGatePaused;
        axisModifierHeld = _componentGateAxisModifierHeld;
        command = _componentGateCommand;
        return _componentGateInputValid;
    }

    private static void CacheComponentGateInput(
        int frame,
        bool controlsEnabled,
        bool paused,
        bool axisModifierHeld,
        ThrottleCommand command)
    {
        _componentGateInputFrame = frame;
        _componentGateInputValid = true;
        _componentGateControlsEnabled = controlsEnabled;
        _componentGatePaused = paused;
        _componentGateAxisModifierHeld = axisModifierHeld;
        _componentGateCommand = command;
    }

    private static void InvalidateComponentGateInput()
    {
        _componentGateInputFrame = -1;
        _componentGateInputValid = false;
        _componentGateControlsEnabled = false;
        _componentGatePaused = false;
        _componentGateAxisModifierHeld = false;
        _componentGateCommand = ThrottleCommand.Neutral;
    }

    private static void LogFailureOnce(string operation, Exception exception)
    {
        if (_log is null || !ReportedFailures.Add(operation))
        {
            return;
        }

        _log.LogWarning($"{operation} failed open: {exception.GetType().Name}: {exception.Message}");
    }
}
