using System;

namespace NuclearOptionDetents.Core;

/// <summary>Inputs to the canonical throttle boundary hold.</summary>
public readonly struct ThrottleBoundaryHoldInput
{
    public ThrottleBoundaryHoldInput(
        double requestedThrottle,
        double simulatedThrottle,
        ThrottleCommand command,
        EndpointDetentState idleState,
        EndpointDetentState afterburnerState,
        double idleBoundary,
        double afterburnerBoundary,
        double endpointEpsilon,
        bool enabled = true,
        bool relativeThrottleMode = true,
        SimulatedThrottleRange throttleRange = SimulatedThrottleRange.NegativeOneToOne,
        bool controlsEnabled = true,
        bool paused = false,
        bool axisModifierHeld = false,
        bool idleApplies = true,
        bool afterburnerApplies = true)
    {
        (RequestedThrottle, SimulatedThrottle, Command, IdleState, AfterburnerState,
            IdleBoundary, AfterburnerBoundary, EndpointEpsilon, Enabled, RelativeThrottleMode,
            ThrottleRange, ControlsEnabled, Paused, AxisModifierHeld, IdleApplies, AfterburnerApplies) =
            (requestedThrottle, simulatedThrottle, command, idleState, afterburnerState,
                idleBoundary, afterburnerBoundary, endpointEpsilon, enabled, relativeThrottleMode,
                throttleRange, controlsEnabled, paused, axisModifierHeld, idleApplies, afterburnerApplies);
    }

    public double RequestedThrottle { get; }
    public double SimulatedThrottle { get; }
    public ThrottleCommand Command { get; }
    public EndpointDetentState IdleState { get; }
    public EndpointDetentState AfterburnerState { get; }
    public double IdleBoundary { get; }
    public double AfterburnerBoundary { get; }
    public double EndpointEpsilon { get; }
    public bool Enabled { get; }
    public bool RelativeThrottleMode { get; }
    /// <summary>Range of the accumulator to pin, which follows the player setting or an external integrator's mapping.</summary>
    public SimulatedThrottleRange ThrottleRange { get; }
    public bool ControlsEnabled { get; }
    public bool Paused { get; }
    public bool AxisModifierHeld { get; }
    public bool IdleApplies { get; }
    public bool AfterburnerApplies { get; }
}

/// <summary>Canonical and private throttle values to publish after one vanilla update.</summary>
public readonly struct ThrottleBoundaryHoldResult
{
    internal ThrottleBoundaryHoldResult(
        double effectiveThrottle,
        double simulatedThrottle,
        bool idleHeld,
        bool afterburnerHeld,
        bool shouldPinSimulatedThrottle)
    {
        (EffectiveThrottle, SimulatedThrottle, IdleHeld, AfterburnerHeld, ShouldPinSimulatedThrottle) =
            (effectiveThrottle, simulatedThrottle, idleHeld, afterburnerHeld, shouldPinSimulatedThrottle);
    }

    public double EffectiveThrottle { get; }
    public double SimulatedThrottle { get; }
    public bool IdleHeld { get; }
    public bool AfterburnerHeld { get; }
    public bool ShouldPinSimulatedThrottle { get; }
    public bool IsHeld => IdleHeld || AfterburnerHeld;
}

/// <summary>
/// Holds the game's canonical throttle just inside a locked changeover. The private
/// relative accumulator is pinned to the same value so input cannot build up behind it.
/// </summary>
public static class ThrottleBoundaryHold
{
    // A normal, non-subnormal float-sized offset avoids equality-triggered consumers
    // without producing a visible gap from idle or full dry power.
    public const double InwardOffset = 0.0001;
    private const double ParkedValueTolerance = 0.00005;

    /// <summary>
    /// Passes throttle through untouched unless a locked boundary applies and the command is still
    /// pushing into it; then the throttle is reported one <see cref="InwardOffset"/> inside the boundary.
    /// Only relative mode pins the accumulator, because absolute and HOTAS input stay vanilla.
    /// </summary>
    public static ThrottleBoundaryHoldResult Apply(in ThrottleBoundaryHoldInput input)
    {
        if (!input.Enabled || !input.RelativeThrottleMode || !input.ControlsEnabled ||
            input.Paused || input.AxisModifierHeld)
        {
            return PassThrough(input);
        }

        var requested = SimulatedThrottleMapping.ClampPublic(input.RequestedThrottle);
        var epsilon = Math.Max(0, input.EndpointEpsilon);
        var idleParked = Math.Abs(requested - (input.IdleBoundary + InwardOffset)) <= ParkedValueTolerance;
        var holdIdle = input.IdleApplies &&
                       input.IdleState != EndpointDetentState.Unlocked &&
                       (input.Command == ThrottleCommand.Decrease ||
                        input.Command == ThrottleCommand.Neutral && idleParked) &&
                       requested <= input.IdleBoundary + epsilon;
        if (holdIdle)
        {
            var effective = SimulatedThrottleMapping.ClampPublic(Math.Max(requested, input.IdleBoundary + InwardOffset));
            return new ThrottleBoundaryHoldResult(
                effective,
                input.RelativeThrottleMode
                    ? SimulatedThrottleMapping.ToSimulated(effective, input.ThrottleRange)
                    : input.SimulatedThrottle,
                idleHeld: true,
                afterburnerHeld: false,
                shouldPinSimulatedThrottle: input.RelativeThrottleMode);
        }

        var afterburnerParked = Math.Abs(requested - (input.AfterburnerBoundary - InwardOffset)) <= ParkedValueTolerance;
        var holdAfterburner = input.AfterburnerApplies &&
                              input.AfterburnerState != EndpointDetentState.Unlocked &&
                              (input.Command == ThrottleCommand.Increase ||
                               input.Command == ThrottleCommand.Neutral && afterburnerParked) &&
                              requested >= input.AfterburnerBoundary - epsilon;
        if (holdAfterburner)
        {
            var effective = SimulatedThrottleMapping.ClampPublic(Math.Min(requested, input.AfterburnerBoundary - InwardOffset));
            return new ThrottleBoundaryHoldResult(
                effective,
                input.RelativeThrottleMode
                    ? SimulatedThrottleMapping.ToSimulated(effective, input.ThrottleRange)
                    : input.SimulatedThrottle,
                idleHeld: false,
                afterburnerHeld: true,
                shouldPinSimulatedThrottle: input.RelativeThrottleMode);
        }

        return PassThrough(input);
    }

    private static ThrottleBoundaryHoldResult PassThrough(in ThrottleBoundaryHoldInput input) =>
        new(
            SimulatedThrottleMapping.ClampPublic(input.RequestedThrottle),
            input.SimulatedThrottle,
            idleHeld: false,
            afterburnerHeld: false,
            shouldPinSimulatedThrottle: false);

}
