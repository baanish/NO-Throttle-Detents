using System;

namespace NuclearOptionDetents.Core;

/// <summary>One local-player throttle observation consumed by both detents.</summary>
public readonly struct DetentRuntimeInput
{
    public DetentRuntimeInput(
        double simulationTime,
        double throttle,
        ThrottleCommand command,
        bool masterEnabled = true,
        bool idleEnabled = true,
        bool afterburnerEnabled = true,
        bool controlsEnabled = true,
        bool paused = false,
        bool axisModifierHeld = false,
        bool relativeThrottleMode = true)
    {
        (SimulationTime, Throttle, Command, MasterEnabled, IdleEnabled,
            AfterburnerEnabled, ControlsEnabled, Paused, AxisModifierHeld, RelativeThrottleMode) =
            (simulationTime, throttle, command, masterEnabled, idleEnabled,
                afterburnerEnabled, controlsEnabled, paused, axisModifierHeld, relativeThrottleMode);
    }

    public double SimulationTime { get; }
    public double Throttle { get; }
    public ThrottleCommand Command { get; }
    public bool MasterEnabled { get; }
    public bool IdleEnabled { get; }
    public bool AfterburnerEnabled { get; }
    public bool ControlsEnabled { get; }
    public bool Paused { get; }
    public bool AxisModifierHeld { get; }
    public bool RelativeThrottleMode { get; }
}

/// <summary>Read-only result of one coordinated detent update.</summary>
public readonly struct DetentRuntimeSnapshot
{
    internal DetentRuntimeSnapshot(
        bool bypassed,
        EndpointDetentState idleState,
        double idleElapsedSeconds,
        bool airbrakeInhibited,
        EndpointDetentState afterburnerState,
        double afterburnerElapsedSeconds,
        bool afterburnerUnlocked)
    {
        (IsBypassed, IdleState, IdleElapsedSeconds, AirbrakeInhibited,
            AfterburnerState, AfterburnerElapsedSeconds, AfterburnerUnlocked) =
            (bypassed, idleState, idleElapsedSeconds, airbrakeInhibited,
                afterburnerState, afterburnerElapsedSeconds, afterburnerUnlocked);
    }

    public bool IsBypassed { get; }
    public EndpointDetentState IdleState { get; }
    public double IdleElapsedSeconds { get; }
    public bool AirbrakeInhibited { get; }
    public EndpointDetentState AfterburnerState { get; }
    public double AfterburnerElapsedSeconds { get; }
    public bool AfterburnerUnlocked { get; }
}

/// <summary>
/// Pure coordination for the independent lower and upper endpoint detents.
/// The caller remains responsible for scoping input and gates to the local aircraft.
/// </summary>
public sealed class DetentRuntime
{
    private bool _hasContext;
    private object? _aircraft;
    private object? _controls;
    private DetentRuntimeSnapshot _snapshot;

    public DetentRuntime(
        double idleHoldMilliseconds = 200,
        double afterburnerHoldMilliseconds = 200,
        double endpointEpsilon = 0.001,
        double resetHysteresis = 0.02,
        double idleBoundary = 0,
        double afterburnerBoundary = 1)
    {
        IdleDetent = new EndpointDetent(
            DetentDirection.Lower,
            Math.Max(0, idleHoldMilliseconds) / 1000.0,
            idleBoundary,
            endpointEpsilon,
            resetHysteresis);
        AfterburnerDetent = new EndpointDetent(
            DetentDirection.Upper,
            Math.Max(0, afterburnerHoldMilliseconds) / 1000.0,
            afterburnerBoundary,
            endpointEpsilon,
            resetHysteresis);
        _snapshot = CreateSnapshot(bypassed: true, airbrakeInhibited: false, afterburnerUnlocked: true);
    }

    public EndpointDetent IdleDetent { get; }
    public EndpointDetent AfterburnerDetent { get; }
    public DetentRuntimeSnapshot Snapshot => _snapshot;
    /// <summary>Reset state when the local aircraft/control object or scene changes.</summary>
    public void ResetLifecycle()
    {
        IdleDetent.Reset();
        AfterburnerDetent.Reset();
        _hasContext = false;
        _aircraft = null;
        _controls = null;
        _snapshot = CreateSnapshot(bypassed: true, airbrakeInhibited: false, afterburnerUnlocked: true);
    }

    /// <summary>Reset if either reference no longer identifies the same local control path.</summary>
    public void ObserveContext(object? aircraft, object? controls)
    {
        if (!_hasContext)
        {
            _hasContext = true;
            _aircraft = aircraft;
            _controls = controls;
            return;
        }

        if (!ReferenceEquals(_aircraft, aircraft) || !ReferenceEquals(_controls, controls))
        {
            IdleDetent.Reset();
            AfterburnerDetent.Reset();
            _aircraft = aircraft;
            _controls = controls;
            _snapshot = CreateSnapshot(bypassed: true, airbrakeInhibited: false, afterburnerUnlocked: true);
        }
    }

    public DetentRuntimeSnapshot Update(in DetentRuntimeInput input)
    {
        bool bypassed = !input.MasterEnabled || !input.RelativeThrottleMode;
        if (bypassed)
        {
            IdleDetent.Update(new EndpointDetentInput(input.SimulationTime, input.Throttle, input.Command, enabled: false));
            AfterburnerDetent.Update(new EndpointDetentInput(input.SimulationTime, input.Throttle, input.Command, enabled: false));
            _snapshot = CreateSnapshot(bypassed: true, airbrakeInhibited: false, afterburnerUnlocked: true);
            return _snapshot;
        }

        var command = input.AxisModifierHeld ? ThrottleCommand.Neutral : input.Command;
        IdleDetent.Update(new EndpointDetentInput(
            input.SimulationTime,
            input.Throttle,
            command,
            input.ControlsEnabled,
            input.Paused,
            input.AxisModifierHeld,
            input.IdleEnabled));
        AfterburnerDetent.Update(new EndpointDetentInput(
            input.SimulationTime,
            input.Throttle,
            command,
            input.ControlsEnabled,
            input.Paused,
            input.AxisModifierHeld,
            input.AfterburnerEnabled));

        bool commonInterruption = !input.ControlsEnabled || input.Paused || input.AxisModifierHeld;
        bool lowerInterrupted = commonInterruption ||
                                ThrottleCommands.IsOppositeDirection(input.Command, DetentDirection.Lower);
        bool upperInterrupted = commonInterruption ||
                                ThrottleCommands.IsOppositeDirection(input.Command, DetentDirection.Upper);
        bool atLowerEndpoint = IdleDetent.IsAtEndpoint(input.Throttle);
        bool airbrakeInhibited = input.IdleEnabled && !lowerInterrupted &&
                                 !IdleDetent.IsUnlocked && atLowerEndpoint;
        _snapshot = CreateSnapshot(
            bypassed: false,
            airbrakeInhibited: airbrakeInhibited,
            afterburnerUnlocked: !input.AfterburnerEnabled || upperInterrupted || AfterburnerDetent.IsUnlocked);
        return _snapshot;
    }

    public void CancelPendingHolds()
    {
        IdleDetent.CancelPendingHold();
        AfterburnerDetent.CancelPendingHold();
        _snapshot = CreateSnapshot(
            _snapshot.IsBypassed,
            airbrakeInhibited: false,
            afterburnerUnlocked: true);
    }

    public void RetargetAfterburnerBoundary(double boundary) =>
        AfterburnerDetent.RetargetBoundary(boundary);

    /// <summary>Updates live timing settings without replacing endpoint state.</summary>
    public void Reconfigure(
        double idleHoldMilliseconds,
        double afterburnerHoldMilliseconds,
        double endpointEpsilon,
        double resetHysteresis)
    {
        IdleDetent.Reconfigure(
            Math.Max(0, idleHoldMilliseconds) / 1000.0,
            endpointEpsilon,
            resetHysteresis);
        AfterburnerDetent.Reconfigure(
            Math.Max(0, afterburnerHoldMilliseconds) / 1000.0,
            endpointEpsilon,
            resetHysteresis);
        _snapshot = CreateSnapshot(
            _snapshot.IsBypassed,
            _snapshot.AirbrakeInhibited,
            _snapshot.AfterburnerUnlocked);
    }

    private DetentRuntimeSnapshot CreateSnapshot(
        bool bypassed,
        bool airbrakeInhibited,
        bool afterburnerUnlocked) =>
        new(
            bypassed,
            IdleDetent.State,
            IdleDetent.ElapsedHoldSeconds,
            airbrakeInhibited,
            AfterburnerDetent.State,
            AfterburnerDetent.ElapsedHoldSeconds,
            afterburnerUnlocked);
}
