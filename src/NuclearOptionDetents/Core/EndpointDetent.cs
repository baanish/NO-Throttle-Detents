using System;

namespace NuclearOptionDetents.Core;

public enum DetentDirection
{
    Lower = 0,
    Upper = 1,
}

public enum EndpointDetentState
{
    Locked = 0,
    Holding = 1,
    Unlocked = 2,
}

/// <summary>Inputs to one endpoint detent. Time is simulation time, in seconds.</summary>
public readonly struct EndpointDetentInput
{
    public EndpointDetentInput(
        double simulationTime,
        double throttle,
        ThrottleCommand command,
        bool controlsEnabled = true,
        bool paused = false,
        bool cancelHold = false,
        bool enabled = true)
    {
        (SimulationTime, Throttle, Command, ControlsEnabled, Paused, CancelHold, Enabled) =
            (simulationTime, throttle, command, controlsEnabled, paused, cancelHold, enabled);
    }

    public double SimulationTime { get; }
    public double Throttle { get; }
    public ThrottleCommand Command { get; }
    public bool ControlsEnabled { get; }
    public bool Paused { get; }
    public bool CancelHold { get; }
    public bool Enabled { get; }
}

/// <summary>
/// Pure state machine for one virtual endpoint detent.
/// The dwell is accumulated from elapsed simulation time, never from update count.
/// </summary>
public sealed class EndpointDetent
{
    private readonly DetentDirection _direction;
    private double _boundary;
    private double _endpointEpsilon;
    private double _resetHysteresis;
    private double _holdDurationSeconds;
    private EndpointDetentState _state = EndpointDetentState.Locked;
    private double _elapsedSeconds;
    private double _lastSimulationTime;
    private bool _hasLastSimulationTime;
    private bool _wasDisabled;

    public EndpointDetent(
        DetentDirection direction,
        double holdDurationSeconds,
        double endpointEpsilon = 0.001,
        double resetHysteresis = 0.02)
        : this(
            direction,
            holdDurationSeconds,
            direction == DetentDirection.Lower ? 0 : 1,
            endpointEpsilon,
            resetHysteresis)
    {
    }

    public EndpointDetent(
        DetentDirection direction,
        double holdDurationSeconds,
        double boundary,
        double endpointEpsilon,
        double resetHysteresis)
    {
        ValidateBoundary(boundary);
        ValidateConfiguration(endpointEpsilon, resetHysteresis);

        _direction = direction;
        _boundary = boundary;
        _holdDurationSeconds = Math.Max(0, holdDurationSeconds);
        _endpointEpsilon = endpointEpsilon;
        _resetHysteresis = resetHysteresis;
    }

    public double Boundary => _boundary;
    public double EndpointEpsilon => _endpointEpsilon;
    public double ResetHysteresis => _resetHysteresis;
    public EndpointDetentState State => _state;
    public bool IsLocked => _state == EndpointDetentState.Locked;
    public bool IsHolding => _state == EndpointDetentState.Holding;
    public bool IsUnlocked => _state == EndpointDetentState.Unlocked;
    public double ElapsedHoldSeconds => _state == EndpointDetentState.Holding ? _elapsedSeconds : 0;

    /// <summary>Changes the endpoint boundary without discarding either detent's state.</summary>
    public void RetargetBoundary(double boundary)
    {
        ValidateBoundary(boundary);
        _boundary = boundary;
    }

    /// <summary>Updates live timing settings without changing the endpoint state.</summary>
    public void Reconfigure(
        double holdDurationSeconds,
        double endpointEpsilon,
        double resetHysteresis)
    {
        ValidateConfiguration(endpointEpsilon, resetHysteresis);
        _holdDurationSeconds = Math.Max(0, holdDurationSeconds);
        _endpointEpsilon = endpointEpsilon;
        _resetHysteresis = resetHysteresis;
    }

    public bool IsAtEndpoint(double throttle)
    {
        return _direction == DetentDirection.Lower
            ? throttle <= _boundary + _endpointEpsilon
            : throttle >= _boundary - _endpointEpsilon;
    }

    /// <summary>Advance the state machine by one simulation observation.</summary>
    public EndpointDetentState Update(in EndpointDetentInput input)
    {
        if (!input.Enabled)
        {
            // Disabled detents are deliberately transparent to vanilla behavior.
            _state = EndpointDetentState.Unlocked;
            _elapsedSeconds = 0;
            _hasLastSimulationTime = false;
            _wasDisabled = true;
            return _state;
        }

        if (_wasDisabled)
        {
            _wasDisabled = false;
            Reset();
        }

        bool atEndpoint = IsAtEndpoint(input.Throttle);
        bool movedAway = _direction == DetentDirection.Lower
            ? input.Throttle > _boundary + _resetHysteresis
            : input.Throttle < _boundary - _resetHysteresis;
        bool oppositeCommand = ThrottleCommands.IsOppositeDirection(input.Command, _direction);

        if (_state == EndpointDetentState.Unlocked)
        {
            // Crossing back inward is always free. Relock only after the value itself
            // has cleared the boundary hysteresis, not merely on the first reverse input.
            if (movedAway)
            {
                Reset();
            }

            return _state;
        }

        if (movedAway || oppositeCommand || !atEndpoint || !input.ControlsEnabled || input.Paused || input.CancelHold)
        {
            Reset();
            return _state;
        }

        if (!ThrottleCommands.IsDirection(input.Command, _direction))
        {
            Reset();
            return _state;
        }

        if (_state == EndpointDetentState.Locked)
        {
            _state = EndpointDetentState.Holding;
            _elapsedSeconds = 0;
            _lastSimulationTime = input.SimulationTime;
            _hasLastSimulationTime = true;
        }
        else
        {
            if (!_hasLastSimulationTime)
            {
                _lastSimulationTime = input.SimulationTime;
                _hasLastSimulationTime = true;
            }

            double delta = input.SimulationTime - _lastSimulationTime;
            _lastSimulationTime = input.SimulationTime;
            if (delta > 0)
            {
                // The input is sampled at the current simulation time. Once the
                // qualifying command is observed again, elapsed simulation time is
                // the only cadence-independent measure of the held dwell.
                _elapsedSeconds += delta;
            }
        }

        // Simulation timestamps are commonly single-precision Unity values; the tiny
        // tolerance makes an exact configured dwell deterministic at its boundary.
        if (_elapsedSeconds + 1e-12 >= _holdDurationSeconds)
        {
            _state = EndpointDetentState.Unlocked;
            _elapsedSeconds = 0;
        }

        return _state;
    }

    /// <summary>Clears both a pending hold and an unlocked latch.</summary>
    public void Reset()
    {
        _state = EndpointDetentState.Locked;
        _elapsedSeconds = 0;
        _lastSimulationTime = 0;
        _hasLastSimulationTime = false;
    }

    public void CancelPendingHold()
    {
        if (_state == EndpointDetentState.Holding)
        {
            Reset();
        }
    }

    private static void ValidateConfiguration(double endpointEpsilon, double resetHysteresis)
    {
        if (endpointEpsilon < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endpointEpsilon));
        }

        if (resetHysteresis < endpointEpsilon)
        {
            throw new ArgumentOutOfRangeException(nameof(resetHysteresis));
        }
    }

    private static void ValidateBoundary(double boundary)
    {
        if (double.IsNaN(boundary) || double.IsInfinity(boundary) || boundary < 0 || boundary > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(boundary));
        }
    }
}
