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
    private const int MaxCapabilityDiscoveryAttempts = 12;
    private const float CapabilityDiscoveryRetrySeconds = 0.25f;
    private static ModConfig? _config;
    private static ManualLogSource? _log;
    private static DetentRuntime _runtime = new();
    private static InteriorDetentRuntime _interiorDetents = new(
        Array.Empty<double>(),
        displayStart: 0,
        displayEnd: 1,
        holdMilliseconds: 200,
        crossingEpsilon: 0.001,
        resetHysteresis: 0.02);
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
    private static double[] _customDryDetentFractions = Array.Empty<double>();
    private static bool _customDisplayRangeKnown;
    private static double _customDisplayStart;
    private static double _customDisplayEnd = 1;
    private static string _airframeId = string.Empty;
    private static string _airframeName = string.Empty;
    private static bool _aircraftCapabilitiesKnown;
    private static bool _liveAirbrakeComponentConfirmed;
    private static bool _liveSplitAirbrakeConfirmed;
    private static bool _liveAfterburnerConfirmed;
    private static float _liveAfterburnerStart;
    private static float _liveAfterburnerEnd;
    private static bool _afterburnerRangeMatchesPreset;
    private static bool _hasAirbrake;
    private static bool _hasAfterburner;
    private static int _capabilityDiscoveryAttempts;
    private static float _nextCapabilityDiscoveryTime;
    private static int _lastObservedFrame = -1;
    private static bool _patchStatusKnown;
    private static bool _throttleObserverActive;
    private static bool _idleGateActive;
    private static bool _afterburnerGateActive;
    private static bool _foreignThrottleBypassActive;
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
                    _activePreset is not null || _customDryDetentFractions.Length > 0,
                    _localCollective,
                    PlayerSettings.throttleUseRelative,
                    _aircraftCapabilitiesKnown,
                    _hasAirbrake,
                    _hasAfterburner,
                    CustomDetentsApply(),
                    _customDryDetentFractions.Length > 0));
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
    public static string CurrentAircraftId => _airframeId;

    /// <summary>Last evaluated HUD state; hidden whenever the runtime resets, so a stale line cannot survive an aircraft or scene change.</summary>
    public static DetentIndicatorSnapshot IndicatorSnapshot => _indicator;
    internal static Aircraft? LocalAircraft => _localAircraft;
    internal static bool HasCustomThrottleDisplayRange(Aircraft aircraft) =>
        ReferenceEquals(_localAircraft, aircraft) && _customDisplayRangeKnown;
    internal static DetentRuntimeSnapshot DetentSnapshot => _runtime.Snapshot;
    internal static bool HasEffectiveSimulatedThrottle => _hasEffectiveSimulatedThrottle;
    internal static double EffectiveSimulatedThrottle => _effectiveSimulatedThrottle;

    /// <summary>Updates custom detents from the local cockpit gauge's percentage range.</summary>
    internal static void SetCustomThrottleDisplayRange(Aircraft aircraft, double start, double end)
    {
        if (!ReferenceEquals(_localAircraft, aircraft) ||
            double.IsNaN(start) || double.IsInfinity(start) ||
            double.IsNaN(end) || double.IsInfinity(end) || end <= start)
        {
            return;
        }

        start = SimulatedThrottleMapping.ClampPublic(start);
        end = SimulatedThrottleMapping.ClampPublic(end);
        if (end <= start ||
            _customDisplayRangeKnown &&
            Math.Abs(start - _customDisplayStart) <= 0.000001 &&
            Math.Abs(end - _customDisplayEnd) <= 0.000001)
        {
            return;
        }

        _customDisplayRangeKnown = true;
        _customDisplayStart = start;
        _customDisplayEnd = end;
        RebuildRuntime(ReadSettings());
        RefreshApplicableCapabilities();
        if (_settings.DebugLogging && _log is not null && _customDryDetentFractions.Length > 0)
        {
            _log.LogInfo(
                $"Custom detent HUD range for {_airframeId}: " +
                $"{_customDisplayStart:0.####}..{_customDisplayEnd:0.####}");
        }
    }

    internal static void ClearCustomThrottleDisplayRange(Aircraft aircraft)
    {
        if (!ReferenceEquals(_localAircraft, aircraft) || !_customDisplayRangeKnown)
        {
            return;
        }

        _customDisplayRangeKnown = false;
        _customDisplayStart = 0;
        _customDisplayEnd = 1;
        RebuildRuntime(ReadSettings());
        RefreshApplicableCapabilities();
    }

    /// <summary>Records which patches actually installed; a gate whose patch is missing never claims to be active.</summary>
    public static void SetPatchStatus(bool throttleObserverActive)
    {
        _throttleObserverActive = throttleObserverActive;
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
    /// <paramref name="foreignThrottlePatchPresent"/> allows the runtime to yield only while a foreign
    /// postfix or transpiler is actually publishing a value outside that accumulator.
    /// </summary>
    public static void ObserveThrottle(
        PilotPlayerState state,
        bool externalRelativeThrottleIntegrator,
        bool externalUsesSignedMapping,
        bool foreignThrottlePatchPresent)
    {
        try
        {
            var playerAccessor = RuntimeCompatibility.PilotPlayer;
            var collectiveAccessor = RuntimeCompatibility.PilotCollective;
            var pilotAccessor = RuntimeCompatibility.PilotStatePilot;
            var aircraftAccessor = RuntimeCompatibility.PilotOwnedAircraft;
            var inputsAccessor = RuntimeCompatibility.PilotControlInputs;
            var simulatedThrottleAccessor = RuntimeCompatibility.PilotSimulatedThrottle;
            var autoHoverAccessor = RuntimeCompatibility.AircraftAutoHoverEnabled;
            var localAircraftAccessor = RuntimeCompatibility.IsLocalAircraft;
            if (playerAccessor is null || collectiveAccessor is null || pilotAccessor is null ||
                aircraftAccessor is null || inputsAccessor is null || simulatedThrottleAccessor is null ||
                autoHoverAccessor is null || localAircraftAccessor is null)
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
            if (!localAircraftAccessor(aircraft!))
            {
                ResetAll("throttle observer was not attached to the local aircraft");
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
                _hasEffectiveSimulatedThrottle = false;
                _indicator = DetentIndicatorSnapshot.Hidden;
                _customDisplayRangeKnown = false;
                _customDisplayStart = 0;
                _customDisplayEnd = 1;
                ResolveAirframeIdentity(aircraft!);
                settings = ReadSettings();
                _settings = settings;
                _hasSettings = true;
                ResolveAirframePreset();
                ResetAircraftCapabilities();
                DiscoverLocalCapabilities(aircraft!);
                _nextCapabilityDiscoveryTime = simulationTime + CapabilityDiscoveryRetrySeconds;
                RefreshApplicableCapabilities();
                RebuildRuntime(settings);
                _runtime.ObserveContext(aircraft, inputs);
                if (settings.DebugLogging && _log is not null)
                {
                    _log.LogInfo(
                        $"Detents attached to {_airframeName} ({_airframeId}): " +
                        $"allowlisted={_activePreset is not null}, airbrake={_hasAirbrake}, afterburner={_hasAfterburner}, " +
                        $"customDetents={_customDryDetentFractions.Length}, throttleUseNegative={PlayerSettings.throttleUseNegative}");
                }
            }

            if (NeedsCapabilityDiscovery() &&
                _capabilityDiscoveryAttempts < MaxCapabilityDiscoveryAttempts &&
                simulationTime >= _nextCapabilityDiscoveryTime)
            {
                var hadAirbrake = _hasAirbrake;
                var hadAfterburner = _hasAfterburner;
                DiscoverLocalCapabilities(aircraft!);
                _nextCapabilityDiscoveryTime = simulationTime + CapabilityDiscoveryRetrySeconds;
                RefreshApplicableCapabilities();
                if (hadAirbrake != _hasAirbrake || hadAfterburner != _hasAfterburner)
                {
                    RebuildRuntime(settings);
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
            var simulatedThrottleBefore = simulatedThrottleAccessor(state);
            var simulatedThrottleRange = externalUsesSignedMapping || PlayerSettings.throttleUseNegative
                ? SimulatedThrottleRange.NegativeOneToOne
                : SimulatedThrottleRange.ZeroToOne;
            var foreignThrottleControl = ThrottleOutputOwnership.IsForeignControlActive(
                foreignThrottlePatchPresent,
                requestedThrottle,
                simulatedThrottleBefore,
                simulatedThrottleRange,
                invertOutput: reverseDirection);
            if (foreignThrottleControl != _foreignThrottleBypassActive)
            {
                _foreignThrottleBypassActive = foreignThrottleControl;
                if (settings.DebugLogging && _log is not null)
                {
                    _log.LogInfo(foreignThrottleControl
                        ? "Detents bypassed: another mod is controlling throttle"
                        : "Detents resumed: relative throttle control returned to the game");
                }
            }
            var airframeAllowsDetents = AirframeAllowsDetents();
            var compatibleIntegrator = !externalRelativeThrottleIntegrator || externalUsesSignedMapping;
            var autoHoverEnabled = autoHoverAccessor(aircraft!);
            var detentsAllowed = airframeAllowsDetents && relativeThrottle && compatibleIntegrator &&
                                 !autoHoverEnabled && !foreignThrottleControl;
            var idleApplies = settings.IdleEnabled && _hasAirbrake;
            var afterburnerApplies = settings.AfterburnerEnabled && _hasAfterburner;
            var customDetentsApply = CustomDetentsApply();

            var hasLiveDetentCapability = _hasAirbrake || _hasAfterburner || customDetentsApply;
            var sensitivityEnabled = settings.ThrottleSensitivity != 1f &&
                                     RelativeThrottleSensitivity.ShouldApply(
                settings.Enabled,
                relativeThrottle,
                airframeAllowsDetents && hasLiveDetentCapability && !autoHoverEnabled && !foreignThrottleControl,
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

            var interiorHold = _interiorDetents.Update(new InteriorDetentInput(
                simulationTime,
                requestedThrottle,
                effectiveSimulatedThrottle,
                command,
                simulatedThrottleRange,
                enabled: settings.Enabled && detentsAllowed && customDetentsApply,
                relativeThrottleMode: relativeThrottle,
                controlsEnabled,
                paused,
                axisModifierHeld));
            if (interiorHold.IsHeld)
            {
                requestedThrottle = (float)interiorHold.EffectiveThrottle;
                inputs.throttle = requestedThrottle;
            }
            if (interiorHold.ShouldPinSimulatedThrottle)
            {
                effectiveSimulatedThrottle = interiorHold.SimulatedThrottle;
                simulatedThrottleAccessor(state) = (float)effectiveSimulatedThrottle;
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
                relativeThrottle,
                suspendState: interiorHold.IsHeld));
            var boundaryHold = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
                requestedThrottle,
                effectiveSimulatedThrottle,
                command,
                snapshot.IdleState,
                snapshot.AfterburnerState,
                _runtime.IdleDetent.Boundary,
                _runtime.AfterburnerDetent.Boundary,
                settings.EndpointEpsilon,
                settings.Enabled && detentsAllowed && !interiorHold.IsHeld,
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
            if (interiorHold.IsHeld)
            {
                _indicator = DetentIndicatorPolicy.EvaluateInterior(
                    interiorHold,
                    settings.CustomAirframe.DryDetentHoldMilliseconds,
                    settings.IndicatorEnabled);
            }
            _lastObservedFrame = frame;
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
            _interiorDetents.CancelPendingHold();
            _indicator = DetentIndicatorSnapshot.Hidden;
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
        _interiorDetents.ResetLifecycle();
        _localState = null;
        _localAircraft = null;
        _localInputs = null;
        _localCollective = false;
        _activePreset = null;
        _customDryDetentFractions = Array.Empty<double>();
        _customDisplayRangeKnown = false;
        _customDisplayStart = 0;
        _customDisplayEnd = 1;
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
        _hasAirbrake = false;
        _hasAfterburner = false;
        _capabilityDiscoveryAttempts = 0;
        _nextCapabilityDiscoveryTime = 0f;
        _indicator = DetentIndicatorSnapshot.Hidden;
        _hasEffectiveSimulatedThrottle = false;
        _effectiveSimulatedThrottle = 0;
        _lastObservedFrame = -1;
        _foreignThrottleBypassActive = false;
        if (shouldLog)
        {
            _log!.LogInfo($"Detent state reset: {reason}");
        }
    }

    public static void ReportPatchFailure(string patchName, Exception exception) =>
        LogFailureOnce($"{patchName} patch", exception);

    private static EffectiveSettings ReadSettings()
    {
        if (_config is null)
        {
            throw new InvalidOperationException("Runtime controller is not initialized.");
        }

        return _config.ReadEffective(_airframeId);
    }

    private static void EnsureSettings(EffectiveSettings settings)
    {
        if (_hasSettings && _settings.Equals(settings))
        {
            return;
        }

        var customAirframeChanged = !_hasSettings ||
                                    !_settings.CustomAirframe.Equals(settings.CustomAirframe);
        var timingChanged = !_hasSettings ||
                            _settings.IdleHoldMilliseconds != settings.IdleHoldMilliseconds ||
                            _settings.AfterburnerHoldMilliseconds != settings.AfterburnerHoldMilliseconds ||
                            !_settings.EndpointEpsilon.Equals(settings.EndpointEpsilon) ||
                            !_settings.ResetHysteresis.Equals(settings.ResetHysteresis);
        var interiorTimingChanged = !_hasSettings ||
                                    !_settings.EndpointEpsilon.Equals(settings.EndpointEpsilon) ||
                                    !_settings.ResetHysteresis.Equals(settings.ResetHysteresis);
        _settings = settings;
        _hasSettings = true;
        if (customAirframeChanged && !ReferenceEquals(_localAircraft, null))
        {
            ResolveAirframePreset();
            ResetAircraftCapabilities();
            _nextCapabilityDiscoveryTime = Time.time + CapabilityDiscoveryRetrySeconds;
            RefreshApplicableCapabilities();
            RebuildRuntime(settings);
            return;
        }

        if (timingChanged)
        {
            _runtime.Reconfigure(
                settings.IdleHoldMilliseconds,
                settings.AfterburnerHoldMilliseconds,
                settings.EndpointEpsilon,
                settings.ResetHysteresis);
        }

        if (interiorTimingChanged)
        {
            _interiorDetents.Reconfigure(settings.EndpointEpsilon, settings.ResetHysteresis);
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
        _interiorDetents = new InteriorDetentRuntime(
            _customDisplayRangeKnown ? _customDryDetentFractions : Array.Empty<double>(),
            _customDisplayStart,
            _customDisplayEnd,
            settings.CustomAirframe.DryDetentHoldMilliseconds,
            settings.EndpointEpsilon,
            settings.ResetHysteresis);
        _lastObservedFrame = -1;
        _indicator = DetentIndicatorSnapshot.Hidden;
        _hasEffectiveSimulatedThrottle = false;
        if (!ReferenceEquals(_localAircraft, null) && !ReferenceEquals(_localInputs, null))
        {
            _runtime.ObserveContext(_localAircraft, _localInputs);
        }
    }

    private static void ResolveAirframeIdentity(Aircraft aircraft)
    {
        _airframeId = string.Empty;
        _airframeName = string.Empty;
        if (!RuntimeCompatibility.TryGetAirframeIdentity(aircraft, out _airframeId, out _airframeName))
        {
            return;
        }

        _config?.RegisterDetectedAircraft(_airframeId, _airframeName);
    }

    private static void ResolveAirframePreset()
    {
        _activePreset = null;
        AirframePresetCatalog.TryGet(_airframeId, _settings.CustomAirframe, out _activePreset!);
        _customDryDetentFractions = Array.Empty<double>();
        if (_settings.CustomAirframe.Matches(_airframeId) &&
            _settings.CustomAirframe.TryGetDryDetentFractions(out var fractions))
        {
            _customDryDetentFractions = fractions;
        }
    }

    /// <summary>Preset-level eligibility only; live component confirmation is tracked separately in the capability flags.</summary>
    private static bool AirframeAllowsDetents() =>
        !_localCollective &&
        (AirframePresetCatalog.SupportsDetents(_activePreset, runtimeCollective: false) ||
         CustomDetentsApply());

    private static bool CustomDetentsApply() =>
        _customDisplayRangeKnown && _customDryDetentFractions.Length > 0;

    private static void RefreshApplicableCapabilities()
    {
        var airbrakeConfirmed = _activePreset?.AirbrakePath switch
        {
            AirbrakePath.Component => _liveAirbrakeComponentConfirmed,
            AirbrakePath.Split => _liveSplitAirbrakeConfirmed,
            _ => false,
        };
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
        _idleGateActive = _throttleObserverActive && _hasAirbrake;
        _afterburnerGateActive = _throttleObserverActive && _hasAfterburner;
        _aircraftCapabilitiesKnown = _hasSettings &&
            (_activePreset is null
                ? CustomDetentsApply()
                : RuntimeReadinessPolicy.AreEnabledCapabilitiesKnown(
                    hasPreset: true,
                    _settings.IdleEnabled,
                    _settings.AfterburnerEnabled,
                    _activePreset.HasAirbrake,
                    _activePreset.HasAfterburner,
                    airbrakeConfirmed,
                    _afterburnerRangeMatchesPreset));
    }

    private static void ResetAircraftCapabilities()
    {
        _liveAirbrakeComponentConfirmed = false;
        _liveSplitAirbrakeConfirmed = false;
        _liveAfterburnerConfirmed = false;
        _liveAfterburnerStart = 0f;
        _liveAfterburnerEnd = 0f;
        _afterburnerRangeMatchesPreset = false;
        AfterburnerCandidates.Clear();
        AfterburnerSamples.Clear();
        _capabilityDiscoveryAttempts = 0;
        _nextCapabilityDiscoveryTime = 0f;
    }

    private static bool NeedsCapabilityDiscovery() =>
        _activePreset is not null &&
        !_activePreset.Collective &&
        !_localCollective &&
        (_activePreset.HasAirbrake && !_hasAirbrake ||
         _activePreset.HasAfterburner && !_hasAfterburner);

    /// <summary>Confirms preset capabilities only on components whose owner is the selected local aircraft.</summary>
    private static void DiscoverLocalCapabilities(Aircraft aircraft)
    {
        if (_activePreset is null || _activePreset.Collective || _localCollective)
        {
            return;
        }

        _capabilityDiscoveryAttempts++;
        var ownedAirbrakes = 0;
        var ownedSplitSurfaces = 0;
        var ownedNozzles = 0;
        try
        {
            if (_activePreset.AirbrakePath == AirbrakePath.Component)
            {
                var serializedAircraft = RuntimeCompatibility.AirbrakeSerializedAircraft;
                var attachedAircraft = RuntimeCompatibility.AirbrakeAttachedAircraft;
                if (serializedAircraft is not null && attachedAircraft is not null)
                {
                    foreach (var airbrake in Resources.FindObjectsOfTypeAll<Airbrake>())
                    {
                        if (ReferenceEquals(serializedAircraft(airbrake), aircraft) ||
                            ReferenceEquals(attachedAircraft(airbrake), aircraft))
                        {
                            ownedAirbrakes++;
                        }
                    }

                    _liveAirbrakeComponentConfirmed = ownedAirbrakes > 0;
                }
            }
            else if (_activePreset.AirbrakePath == AirbrakePath.Split)
            {
                var aircraftAccessor = RuntimeCompatibility.ControlSurfaceAircraft;
                var maxSplitAccessor = RuntimeCompatibility.ControlSurfaceMaxSplit;
                if (aircraftAccessor is not null && maxSplitAccessor is not null)
                {
                    foreach (var surface in Resources.FindObjectsOfTypeAll<ControlSurface>())
                    {
                        if (ReferenceEquals(aircraftAccessor(surface), aircraft) &&
                            maxSplitAccessor(surface) > 0f)
                        {
                            ownedSplitSurfaces++;
                        }
                    }

                    _liveSplitAirbrakeConfirmed = ownedSplitSurfaces > 0;
                }
            }
        }
        catch (Exception exception)
        {
            LogFailureOnce("Local airbrake discovery", exception);
        }

        if (_activePreset.HasAfterburner)
        {
            try
            {
                var aircraftAccessor = RuntimeCompatibility.JetNozzleAircraft;
                if (aircraftAccessor is not null)
                {
                    AfterburnerCandidates.Clear();
                    foreach (var nozzle in Resources.FindObjectsOfTypeAll<JetNozzle>())
                    {
                        if (ReferenceEquals(aircraftAccessor(nozzle), aircraft))
                        {
                            AfterburnerCandidates.Add(nozzle);
                            ownedNozzles++;
                        }
                    }

                    RefreshAfterburnerSamples();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Local afterburner discovery", exception);
            }
        }

        if (_hasSettings && _settings.DebugLogging && _log is not null)
        {
            _log.LogInfo(
                $"Capability scan {_capabilityDiscoveryAttempts}/{MaxCapabilityDiscoveryAttempts} for {_airframeId}: " +
                $"airbrakes={ownedAirbrakes}, splitSurfaces={ownedSplitSurfaces}, nozzles={ownedNozzles}, " +
                $"afterburnerAggregateConfirmed={_liveAfterburnerConfirmed}");
        }
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

    private static bool IsLiveUnityObject(object? value) =>
        !ReferenceEquals(value, null) &&
        (value is not UnityEngine.Object unityObject || unityObject != null);

    private static void LogFailureOnce(string operation, Exception exception)
    {
        if (_log is null || !ReportedFailures.Add(operation))
        {
            return;
        }

        _log.LogWarning($"{operation} failed open: {exception.GetType().Name}: {exception.Message}");
    }
}
