using System;
using System.Collections.Generic;

namespace NuclearOptionDetents.Core;

internal readonly struct InteriorDetentInput
{
    public InteriorDetentInput(
        double simulationTime,
        double requestedThrottle,
        double simulatedThrottle,
        ThrottleCommand command,
        SimulatedThrottleRange throttleRange,
        bool enabled = true,
        bool relativeThrottleMode = true,
        bool controlsEnabled = true,
        bool paused = false,
        bool axisModifierHeld = false)
    {
        (SimulationTime, RequestedThrottle, SimulatedThrottle, Command, ThrottleRange,
            Enabled, RelativeThrottleMode, ControlsEnabled, Paused, AxisModifierHeld) =
            (simulationTime, requestedThrottle, simulatedThrottle, command, throttleRange,
                enabled, relativeThrottleMode, controlsEnabled, paused, axisModifierHeld);
    }

    public double SimulationTime { get; }
    public double RequestedThrottle { get; }
    public double SimulatedThrottle { get; }
    public ThrottleCommand Command { get; }
    public SimulatedThrottleRange ThrottleRange { get; }
    public bool Enabled { get; }
    public bool RelativeThrottleMode { get; }
    public bool ControlsEnabled { get; }
    public bool Paused { get; }
    public bool AxisModifierHeld { get; }
}

internal readonly struct InteriorDetentSnapshot
{
    public InteriorDetentSnapshot(
        bool isHeld,
        EndpointDetentState state,
        double elapsedHoldSeconds,
        double dryPercent,
        double effectiveThrottle,
        double simulatedThrottle,
        bool shouldPinSimulatedThrottle)
    {
        (IsHeld, State, ElapsedHoldSeconds, DryPercent, EffectiveThrottle,
            SimulatedThrottle, ShouldPinSimulatedThrottle) =
            (isHeld, state, elapsedHoldSeconds, dryPercent, effectiveThrottle,
                simulatedThrottle, shouldPinSimulatedThrottle);
    }

    public bool IsHeld { get; }
    public EndpointDetentState State { get; }
    public double ElapsedHoldSeconds { get; }
    public double DryPercent { get; }
    public double EffectiveThrottle { get; }
    public double SimulatedThrottle { get; }
    public bool ShouldPinSimulatedThrottle { get; }
}

/// <summary>Pure bidirectional state machine for user-defined detents on the cockpit's displayed throttle scale.</summary>
internal sealed class InteriorDetentRuntime
{
    private readonly double[] _boundaries;
    private readonly double[] _dryPercents;
    private readonly bool[] _unlocked;
    private readonly double _holdDurationSeconds;
    private double _crossingEpsilon;
    private double _resetHysteresis;
    private bool _hasLastThrottle;
    private double _lastThrottle;
    private int _activeIndex = -1;
    private DetentDirection _activeDirection;
    private bool _holding;
    private double _elapsedSeconds;
    private double _lastSimulationTime;

    public InteriorDetentRuntime(
        IReadOnlyList<double> displayedFractions,
        double displayStart,
        double displayEnd,
        double holdMilliseconds,
        double crossingEpsilon,
        double resetHysteresis)
    {
        displayStart = SimulatedThrottleMapping.ClampPublic(displayStart);
        displayEnd = SimulatedThrottleMapping.ClampPublic(displayEnd);
        if (displayEnd < displayStart)
        {
            (displayStart, displayEnd) = (displayEnd, displayStart);
        }

        _holdDurationSeconds = Math.Max(0, holdMilliseconds) / 1000.0;
        _crossingEpsilon = Math.Max(0, crossingEpsilon);
        _resetHysteresis = Math.Max(_crossingEpsilon, resetHysteresis);
        _boundaries = new double[displayedFractions.Count];
        _dryPercents = new double[displayedFractions.Count];
        _unlocked = new bool[displayedFractions.Count];
        for (var index = 0; index < displayedFractions.Count; index++)
        {
            var fraction = Math.Max(0, Math.Min(1, displayedFractions[index]));
            _boundaries[index] = displayStart + ((displayEnd - displayStart) * fraction);
            _dryPercents[index] = fraction * 100;
        }
    }

    public int Count => _boundaries.Length;

    public void Reconfigure(double crossingEpsilon, double resetHysteresis)
    {
        _crossingEpsilon = Math.Max(0, crossingEpsilon);
        _resetHysteresis = Math.Max(_crossingEpsilon, resetHysteresis);
    }

    public InteriorDetentSnapshot Update(in InteriorDetentInput input)
    {
        var requested = SimulatedThrottleMapping.ClampPublic(input.RequestedThrottle);
        if (!input.Enabled || !input.RelativeThrottleMode || !input.ControlsEnabled ||
            input.Paused || input.AxisModifierHeld || _boundaries.Length == 0)
        {
            ResetState();
            Remember(requested);
            return PassThrough(input, requested);
        }

        if (!_hasLastThrottle)
        {
            Remember(requested);
            return PassThrough(input, requested);
        }

        if (_holding)
        {
            if (!ThrottleCommands.IsDirection(input.Command, _activeDirection))
            {
                var cancelledIndex = _activeIndex;
                var cancelledDirection = _activeDirection;
                CancelHold();
                Remember(input.Command == ThrottleCommand.Neutral
                    ? ThrottleOnApproachSide(cancelledIndex, cancelledDirection)
                    : requested);
                return PassThrough(input, requested);
            }

            var delta = input.SimulationTime - _lastSimulationTime;
            _lastSimulationTime = input.SimulationTime;
            if (delta > DetentTiming.MaximumObservationGapSeconds + 0.000001)
            {
                _elapsedSeconds = 0;
            }
            else if (delta > 0)
            {
                _elapsedSeconds += delta;
            }

            if (_elapsedSeconds + 1e-12 >= _holdDurationSeconds)
            {
                _unlocked[_activeIndex] = true;
                CancelHold();
                Remember(requested);
                return PassThrough(input, requested);
            }

            return HoldAtActiveBoundary(input);
        }

        RelockClearedBoundaries(requested);
        var crossedIndex = FindCrossedBoundary(_lastThrottle, requested, input.Command);
        if (crossedIndex < 0)
        {
            Remember(requested);
            return PassThrough(input, requested);
        }

        _activeIndex = crossedIndex;
        _activeDirection = input.Command == ThrottleCommand.Increase
            ? DetentDirection.Upper
            : DetentDirection.Lower;
        _holding = true;
        _elapsedSeconds = 0;
        _lastSimulationTime = input.SimulationTime;
        if (_holdDurationSeconds <= 0)
        {
            _unlocked[_activeIndex] = true;
            CancelHold();
            Remember(requested);
            return PassThrough(input, requested);
        }

        return HoldAtActiveBoundary(input);
    }

    public void ResetLifecycle()
    {
        ResetState();
        _hasLastThrottle = false;
        _lastThrottle = 0;
    }

    public void CancelPendingHold()
    {
        ResetState();
        _hasLastThrottle = false;
    }

    private int FindCrossedBoundary(double previous, double current, ThrottleCommand command)
    {
        if (command == ThrottleCommand.Increase && current > previous)
        {
            for (var index = 0; index < _boundaries.Length; index++)
            {
                if (_unlocked[index])
                {
                    continue;
                }

                var boundary = _boundaries[index];
                if (previous <= boundary + _crossingEpsilon && current > boundary + _crossingEpsilon)
                {
                    return index;
                }
            }
        }
        else if (command == ThrottleCommand.Decrease && current < previous)
        {
            for (var index = _boundaries.Length - 1; index >= 0; index--)
            {
                if (_unlocked[index])
                {
                    continue;
                }

                var boundary = _boundaries[index];
                if (previous >= boundary - _crossingEpsilon && current < boundary - _crossingEpsilon)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private void RelockClearedBoundaries(double throttle)
    {
        for (var index = 0; index < _boundaries.Length; index++)
        {
            if (_unlocked[index] && Math.Abs(throttle - _boundaries[index]) > _resetHysteresis)
            {
                _unlocked[index] = false;
            }
        }
    }

    private InteriorDetentSnapshot HoldAtActiveBoundary(in InteriorDetentInput input)
    {
        var boundary = _boundaries[_activeIndex];
        var parked = SimulatedThrottleMapping.ClampPublic(
            _activeDirection == DetentDirection.Upper
                ? boundary - ThrottleBoundaryHold.InwardOffset
                : boundary + ThrottleBoundaryHold.InwardOffset);
        Remember(parked);
        return new InteriorDetentSnapshot(
            isHeld: true,
            EndpointDetentState.Holding,
            _elapsedSeconds,
            _dryPercents[_activeIndex],
            parked,
            SimulatedThrottleMapping.ToSimulated(parked, input.ThrottleRange),
            shouldPinSimulatedThrottle: true);
    }

    private double ThrottleOnApproachSide(int index, DetentDirection direction)
    {
        var crossingBand = _crossingEpsilon + ThrottleBoundaryHold.InwardOffset;
        return SimulatedThrottleMapping.ClampPublic(
            direction == DetentDirection.Upper
                ? _boundaries[index] - crossingBand
                : _boundaries[index] + crossingBand);
    }

    private static InteriorDetentSnapshot PassThrough(in InteriorDetentInput input, double requested) =>
        new(
            isHeld: false,
            EndpointDetentState.Locked,
            elapsedHoldSeconds: 0,
            dryPercent: 0,
            requested,
            input.SimulatedThrottle,
            shouldPinSimulatedThrottle: false);

    private void Remember(double throttle)
    {
        _hasLastThrottle = true;
        _lastThrottle = throttle;
    }

    private void ResetState()
    {
        Array.Clear(_unlocked, 0, _unlocked.Length);
        CancelHold();
    }

    private void CancelHold()
    {
        _activeIndex = -1;
        _holding = false;
        _elapsedSeconds = 0;
        _lastSimulationTime = 0;
    }
}
