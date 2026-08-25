using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOptionDetents.Compatibility;
using NuclearOptionDetents.Config;
using NuclearOptionDetents.Core;
using UnityEngine;

namespace NuclearOptionDetents.Diagnostics;

/// <summary>Opt-in, read-only sampling of human aircraft for multiplayer validation logs.</summary>
internal sealed class NetworkValidationObserver
{
    private const float ScanIntervalSeconds = 1f;
    private const float SampleIntervalSeconds = 1f / NetworkValidationSelection.SamplesPerSecond;
    private readonly ModConfig _config;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, ObservedAircraft> _aircraft = new();
    private bool _active;
    private bool _resolutionAttempted;
    private bool _available;
    private int _requestedRemoteOwner = int.MinValue;
    private string _lastRoster = string.Empty;
    private float _nextScanTime;
    private float _nextSampleTime;

    private MethodInfo? _aircraftPlayerGetter;
    private PropertyInfo? _playerIndex;
    private PropertyInfo? _playerAircraft;
    private PropertyInfo? _playerIsLocal;
    private FieldInfo? _aircraftInputs;
    private FieldInfo? _airbrakeAircraft;
    private FieldInfo? _airbrakeAttachedAircraft;
    private FieldInfo? _airbrakeActive;
    private FieldInfo? _airbrakeOpenAmount;
    private FieldInfo? _controlSurfaceAircraft;
    private FieldInfo? _controlSurfaceMaxSplit;
    private FieldInfo? _controlSurfaceSplitAmount;
    private FieldInfo? _jetNozzleAircraft;
    private FieldInfo? _jetNozzleAfterburners;
    private FieldInfo? _afterburnerAmount;

    public NetworkValidationObserver(ModConfig config, ManualLogSource log)
    {
        _config = config;
        _log = log;
    }

    public void Update()
    {
        try
        {
            UpdateCore();
        }
        catch (Exception exception)
        {
            _log.LogWarning($"Network validation stopped after an error: {exception.Message}");
            Stop(_config.DebugLogging.Value);
            _available = false;
        }
    }

    private void UpdateCore()
    {
        if (!_config.DebugLogging.Value || !_config.NetworkValidation.Value)
        {
            Stop(_config.DebugLogging.Value);
            return;
        }

        if (!_resolutionAttempted)
        {
            _resolutionAttempted = true;
            try
            {
                ResolveFields();
                _available = true;
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Network validation unavailable: {exception.Message}");
            }
        }
        if (!_available)
        {
            return;
        }

        var now = Time.unscaledTime;
        if (!_active)
        {
            _active = true;
            _nextScanTime = now;
            _nextSampleTime = now;
            _log.LogInfo("NOD-NET|v=1|event=start|mode=observer");
        }
        if (_requestedRemoteOwner != _config.NetworkValidationOwner.Value)
        {
            _nextScanTime = now;
        }
        if (now >= _nextScanTime)
        {
            RefreshAircraft();
            _nextScanTime = now + ScanIntervalSeconds;
        }
        if (now >= _nextSampleTime)
        {
            SampleAircraft();
            _nextSampleTime = now + SampleIntervalSeconds;
        }
    }

    public void Reset()
    {
        Stop(_config.DebugLogging.Value);
        _resolutionAttempted = false;
        _available = false;
    }

    private void Stop(bool logDetaches)
    {
        if (!_active)
        {
            return;
        }

        if (logDetaches)
        {
            foreach (var observed in _aircraft.Values)
            {
                LogDetach(observed, "disabled");
            }
        }
        _aircraft.Clear();
        _active = false;
        _requestedRemoteOwner = int.MinValue;
        _lastRoster = string.Empty;
    }

    private void ResolveFields()
    {
        _aircraftPlayerGetter = AccessTools.PropertyGetter(typeof(Aircraft), "Player") ??
                                throw new MissingMemberException(typeof(Aircraft).FullName, "Player");
        var playerType = _aircraftPlayerGetter.ReturnType;
        _playerIndex = AccessTools.Property(playerType, "PlayerIndex") ??
                       throw new MissingMemberException(playerType.FullName, "PlayerIndex");
        _playerAircraft = AccessTools.Property(playerType, "Aircraft") ??
                          throw new MissingMemberException(playerType.FullName, "Aircraft");
        _playerIsLocal = AccessTools.Property(playerType, "IsLocalPlayer") ??
                         throw new MissingMemberException(playerType.FullName, "IsLocalPlayer");
        _aircraftInputs = RequireField(typeof(Aircraft), "controlInputs", typeof(ControlInputs));
        _airbrakeAircraft = RequireField(typeof(Airbrake), "aircraft", typeof(Aircraft));
        _airbrakeAttachedAircraft = RequireField(typeof(Airbrake), "attachedAircraft", typeof(Aircraft));
        _airbrakeActive = RequireField(typeof(Airbrake), "active", typeof(bool));
        _airbrakeOpenAmount = RequireField(typeof(Airbrake), "openAmount", typeof(float));
        _controlSurfaceAircraft = RequireField(typeof(ControlSurface), "aircraft", typeof(Aircraft));
        _controlSurfaceMaxSplit = RequireField(typeof(ControlSurface), "maxSplit", typeof(float));
        _controlSurfaceSplitAmount = RequireField(typeof(ControlSurface), "splitAmount", typeof(float));
        _jetNozzleAircraft = RequireField(typeof(JetNozzle), "aircraft", typeof(Aircraft));
        _jetNozzleAfterburners = AccessTools.DeclaredField(typeof(JetNozzle), "afterburners") ??
                                   throw new MissingFieldException(typeof(JetNozzle).FullName, "afterburners");
        var afterburnerType = _jetNozzleAfterburners.FieldType.GetElementType() ??
                              throw new InvalidOperationException("JetNozzle.afterburners is not an array.");
        _afterburnerAmount = RequireField(afterburnerType, "afterburnerAmount", typeof(float));
    }

    private void RefreshAircraft()
    {
        var requestedOwner = _config.NetworkValidationOwner.Value;
        if (_requestedRemoteOwner != requestedOwner)
        {
            foreach (var key in _aircraft.Where(entry => !entry.Value.Local).Select(entry => entry.Key).ToArray())
            {
                LogDetach(_aircraft[key], "selection-changed");
                _aircraft.Remove(key);
            }
            _requestedRemoteOwner = requestedOwner;
        }

        var candidates = new List<HumanAircraft>();
        foreach (var candidate in UnityEngine.Object.FindObjectsOfType(typeof(Aircraft)))
        {
            var aircraft = (object)candidate as Aircraft;
            if (aircraft is null || !IsLive(aircraft) ||
                !TryGetHumanOwner(aircraft, out var owner, out var local))
            {
                continue;
            }
            candidates.Add(new HumanAircraft(aircraft, owner, local));
        }

        LogRoster(candidates, requestedOwner);
        var selected = new List<HumanAircraft>(NetworkValidationSelection.MaximumObservedAircraft);
        var runtimeLocal = RuntimeController.LocalAircraft;
        var localAircraft = candidates.FirstOrDefault(candidate =>
            candidate.Local && ReferenceEquals(candidate.Aircraft, runtimeLocal));
        if (localAircraft is not null)
        {
            selected.Add(localAircraft);
            if (requestedOwner >= 0)
            {
                var remoteAircraft = candidates.FirstOrDefault(candidate =>
                    !candidate.Local && candidate.Owner == requestedOwner);
                if (remoteAircraft is not null)
                {
                    selected.Add(remoteAircraft);
                }
            }
        }

        var seen = new HashSet<int>();
        foreach (var candidate in selected)
        {
            var aircraft = candidate.Aircraft;
            var owner = candidate.Owner;
            var local = candidate.Local;
            if (!NetworkValidationSelection.ShouldObserve(local, owner, requestedOwner))
            {
                continue;
            }

            var key = ((UnityEngine.Object)(object)aircraft).GetInstanceID();
            seen.Add(key);
            if (_aircraft.TryGetValue(key, out var existing) &&
                ReferenceEquals(existing.Aircraft, aircraft) &&
                existing.Owner == owner && existing.Local == local)
            {
                continue;
            }
            if (existing is not null)
            {
                LogDetach(existing, "owner-changed");
                _aircraft.Remove(key);
            }

            try
            {
                var observed = CreateObservedAircraft(aircraft, owner, local);
                _aircraft[key] = observed;
                LogAttach(observed);
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Network validation skipped an aircraft: {exception.Message}");
            }
        }

        foreach (var key in _aircraft.Keys.Where(key => !seen.Contains(key)).ToArray())
        {
            LogDetach(_aircraft[key], "gone");
            _aircraft.Remove(key);
        }
    }

    private void LogRoster(IEnumerable<HumanAircraft> candidates, int requestedOwner)
    {
        var localOwners = string.Join(",", candidates
            .Where(candidate => candidate.Local)
            .Select(candidate => candidate.Owner)
            .Distinct()
            .OrderBy(owner => owner));
        var remoteOwners = string.Join(",", candidates
            .Where(candidate => !candidate.Local)
            .Select(candidate => candidate.Owner)
            .Distinct()
            .OrderBy(owner => owner));
        var roster = $"local={ValueOrNone(localOwners)}|remote={ValueOrNone(remoteOwners)}|selectedRemote={requestedOwner}";
        if (roster == _lastRoster)
        {
            return;
        }

        _lastRoster = roster;
        _log.LogInfo($"NOD-NET|v=1|event=owners|{roster}");
    }

    private ObservedAircraft CreateObservedAircraft(Aircraft aircraft, int owner, bool local)
    {
        RuntimeCompatibility.TryGetAirframeIdentity(aircraft, out var id, out var name);
        var airbrakes = Resources.FindObjectsOfTypeAll<Airbrake>()
            .Where(airbrake => IsLive(airbrake) &&
                               (ReferenceEquals(_airbrakeAircraft!.GetValue(airbrake), aircraft) ||
                                ReferenceEquals(_airbrakeAttachedAircraft!.GetValue(airbrake), aircraft)))
            .ToArray();
        var splitSurfaces = Resources.FindObjectsOfTypeAll<ControlSurface>()
            .Where(surface => IsLive(surface) &&
                              ReferenceEquals(_controlSurfaceAircraft!.GetValue(surface), aircraft) &&
                              (float)_controlSurfaceMaxSplit!.GetValue(surface) > 0f)
            .ToArray();
        var nozzles = Resources.FindObjectsOfTypeAll<JetNozzle>()
            .Where(nozzle => IsLive(nozzle) &&
                             ReferenceEquals(_jetNozzleAircraft!.GetValue(nozzle), aircraft))
            .ToArray();
        return new ObservedAircraft(
            aircraft,
            owner,
            local,
            id,
            name,
            airbrakes,
            splitSurfaces,
            nozzles);
    }

    private bool TryGetHumanOwner(Aircraft aircraft, out int owner, out bool local)
    {
        owner = -1;
        local = false;
        try
        {
            var player = _aircraftPlayerGetter!.Invoke(aircraft, null);
            if (player is null || !ReferenceEquals(_playerAircraft!.GetValue(player), aircraft))
            {
                return false;
            }

            owner = (int)_playerIndex!.GetValue(player);
            local = (bool)_playerIsLocal!.GetValue(player);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SampleAircraft()
    {
        foreach (var observed in _aircraft.Values
                     .OrderBy(observed => observed.Local ? 0 : 1)
                     .ThenBy(observed => observed.Owner))
        {
            if (!IsLive(observed.Aircraft) ||
                !TryGetHumanOwner(observed.Aircraft, out var owner, out var local) ||
                owner != observed.Owner || local != observed.Local)
            {
                continue;
            }

            try
            {
                LogSample(observed);
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Network validation sample failed: {exception.Message}");
            }
        }
    }

    private void LogAttach(ObservedAircraft observed)
    {
        _log.LogInfo(
            $"NOD-NET|v=1|event=attach|scope={Scope(observed)}|owner={observed.Owner}" +
            $"|aircraft={Clean(observed.Id)}|name={Clean(observed.Name)}" +
            $"|airbrakes={observed.Airbrakes.Length}|splitSurfaces={observed.SplitSurfaces.Length}" +
            $"|nozzles={observed.Nozzles.Length}");
    }

    private void LogDetach(ObservedAircraft observed, string reason)
    {
        _log.LogInfo(
            $"NOD-NET|v=1|event=detach|scope={Scope(observed)}|owner={observed.Owner}" +
            $"|aircraft={Clean(observed.Id)}|reason={reason}");
    }

    private void LogSample(ObservedAircraft observed)
    {
        var inputs = (ControlInputs?)_aircraftInputs!.GetValue(observed.Aircraft);
        var throttle = inputs?.throttle;
        var autoHover = TryAutoHover(observed.Aircraft);
        var airbrakeActive = observed.Airbrakes.Length == 0
            ? (bool?)null
            : observed.Airbrakes.Any(airbrake => (bool)_airbrakeActive!.GetValue(airbrake));
        var airbrakeOpen = observed.Airbrakes.Length == 0
            ? (float?)null
            : observed.Airbrakes.Max(airbrake => (float)_airbrakeOpenAmount!.GetValue(airbrake));
        var split = observed.SplitSurfaces.Length == 0
            ? (float?)null
            : observed.SplitSurfaces.Max(surface => Math.Abs((float)_controlSurfaceSplitAmount!.GetValue(surface)));
        var afterburner = MaxAfterburnerAmount(observed.Nozzles);

        var localRuntime = observed.Local && ReferenceEquals(RuntimeController.LocalAircraft, observed.Aircraft);
        var snapshot = RuntimeController.DetentSnapshot;
        var indicator = RuntimeController.IndicatorSnapshot;
        var simulated = localRuntime && RuntimeController.HasEffectiveSimulatedThrottle
            ? RuntimeController.EffectiveSimulatedThrottle
            : (double?)null;

        _log.LogInfo(
            $"NOD-NET|v=1|event=sample|scope={Scope(observed)}|owner={observed.Owner}" +
            $"|aircraft={Clean(observed.Id)}|t={Number(Time.unscaledTime)}|frame={Time.frameCount}" +
            $"|throttle={Number(throttle)}|sim={Number(simulated)}|autoh={Flag(autoHover)}" +
            $"|airbrakeActive={Flag(airbrakeActive)}|airbrakeOpen={Number(airbrakeOpen)}" +
            $"|split={Number(split)}|ab={Number(afterburner)}" +
            $"|idleState={(localRuntime ? snapshot.IdleState.ToString() : "na")}" +
            $"|abState={(localRuntime ? snapshot.AfterburnerState.ToString() : "na")}" +
            $"|idleHeld={(localRuntime ? Flag(indicator.Idle.Visible) : "na")}" +
            $"|abHeld={(localRuntime ? Flag(indicator.Afterburner.Visible) : "na")}");
    }

    private float? MaxAfterburnerAmount(IEnumerable<JetNozzle> nozzles)
    {
        var found = false;
        var maximum = 0f;
        foreach (var nozzle in nozzles)
        {
            if (_jetNozzleAfterburners!.GetValue(nozzle) is not Array afterburners)
            {
                continue;
            }
            foreach (var afterburner in afterburners)
            {
                if (afterburner is null)
                {
                    continue;
                }
                found = true;
                maximum = Math.Max(maximum, (float)_afterburnerAmount!.GetValue(afterburner));
            }
        }
        return found ? maximum : null;
    }

    private bool? TryAutoHover(Aircraft aircraft)
    {
        try
        {
            return RuntimeCompatibility.AircraftAutoHoverEnabled?.Invoke(aircraft);
        }
        catch
        {
            return null;
        }
    }

    private static FieldInfo RequireField(Type type, string name, Type expectedType)
    {
        var field = AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
        if (field.FieldType != expectedType)
        {
            throw new InvalidOperationException($"{type.FullName}.{name} is not {expectedType.FullName}.");
        }
        return field;
    }

    private static bool IsLive(object? value) =>
        !ReferenceEquals(value, null) &&
        (value is not UnityEngine.Object unityObject || unityObject != null);

    private static string Scope(ObservedAircraft observed) => observed.Local ? "local" : "remote";
    private static string Flag(bool value) => value ? "1" : "0";
    private static string Flag(bool? value) => value.HasValue ? Flag(value.Value) : "na";
    private static string Number(double? value) =>
        value.HasValue ? value.Value.ToString("0.000000", CultureInfo.InvariantCulture) : "na";
    private static string Clean(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Replace('|', '_').Replace('=', '_').Replace('\r', ' ').Replace('\n', ' ');

    private static string ValueOrNone(string value) => string.IsNullOrEmpty(value) ? "none" : value;

    private sealed class HumanAircraft
    {
        public HumanAircraft(Aircraft aircraft, int owner, bool local) =>
            (Aircraft, Owner, Local) = (aircraft, owner, local);

        public Aircraft Aircraft { get; }
        public int Owner { get; }
        public bool Local { get; }
    }

    private sealed class ObservedAircraft
    {
        public ObservedAircraft(
            Aircraft aircraft,
            int owner,
            bool local,
            string id,
            string name,
            Airbrake[] airbrakes,
            ControlSurface[] splitSurfaces,
            JetNozzle[] nozzles)
        {
            (Aircraft, Owner, Local, Id, Name, Airbrakes, SplitSurfaces, Nozzles) =
                (aircraft, owner, local, id, name, airbrakes, splitSurfaces, nozzles);
        }

        public Aircraft Aircraft { get; }
        public int Owner { get; }
        public bool Local { get; }
        public string Id { get; }
        public string Name { get; }
        public Airbrake[] Airbrakes { get; }
        public ControlSurface[] SplitSurfaces { get; }
        public JetNozzle[] Nozzles { get; }
    }
}
