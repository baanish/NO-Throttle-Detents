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
        bool throttleUsesNegativeRange = true,
        bool controlsEnabled = true,
        bool paused = false,
        bool axisModifierHeld = false,
        bool idleApplies = true,
        bool afterburnerApplies = true)
    {
        (RequestedThrottle, SimulatedThrottle, Command, IdleState, AfterburnerState,
            IdleBoundary, AfterburnerBoundary, EndpointEpsilon, Enabled, RelativeThrottleMode,
            ThrottleUsesNegativeRange, ControlsEnabled, Paused, AxisModifierHeld, IdleApplies, AfterburnerApplies) =
            (requestedThrottle, simulatedThrottle, command, idleState, afterburnerState,
                idleBoundary, afterburnerBoundary, endpointEpsilon, enabled, relativeThrottleMode,
                throttleUsesNegativeRange, controlsEnabled, paused, axisModifierHeld, idleApplies, afterburnerApplies);
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
    public bool ThrottleUsesNegativeRange { get; }
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
    public const double InwardOffset = 0.000001;

    public static ThrottleBoundaryHoldResult Apply(in ThrottleBoundaryHoldInput input)
    {
        if (!input.Enabled || !input.RelativeThrottleMode || !input.ControlsEnabled ||
            input.Paused || input.AxisModifierHeld)
        {
            return PassThrough(input);
        }

        var requested = Clamp01(input.RequestedThrottle);
        var epsilon = Math.Max(0, input.EndpointEpsilon);
        var holdIdle = input.IdleApplies &&
                       input.IdleState != EndpointDetentState.Unlocked &&
                       input.Command != ThrottleCommand.Increase &&
                       requested <= input.IdleBoundary + epsilon;
        if (holdIdle)
        {
            var effective = Clamp01(Math.Max(requested, input.IdleBoundary + InwardOffset));
            return new ThrottleBoundaryHoldResult(
                effective,
                input.RelativeThrottleMode
                    ? PublicToSimulated(effective, input.ThrottleUsesNegativeRange)
                    : input.SimulatedThrottle,
                idleHeld: true,
                afterburnerHeld: false,
                shouldPinSimulatedThrottle: input.RelativeThrottleMode);
        }

        var holdAfterburner = input.AfterburnerApplies &&
                              input.AfterburnerState != EndpointDetentState.Unlocked &&
                              input.Command != ThrottleCommand.Decrease &&
                              requested >= input.AfterburnerBoundary - epsilon;
        if (holdAfterburner)
        {
            var effective = Clamp01(Math.Min(requested, input.AfterburnerBoundary - InwardOffset));
            return new ThrottleBoundaryHoldResult(
                effective,
                input.RelativeThrottleMode
                    ? PublicToSimulated(effective, input.ThrottleUsesNegativeRange)
                    : input.SimulatedThrottle,
                idleHeld: false,
                afterburnerHeld: true,
                shouldPinSimulatedThrottle: input.RelativeThrottleMode);
        }

        return PassThrough(input);
    }

    public static double PublicToSimulated(double publicThrottle, bool throttleUsesNegativeRange)
    {
        var normalized = Clamp01(publicThrottle);
        return throttleUsesNegativeRange ? (normalized * 2) - 1 : normalized;
    }

    private static ThrottleBoundaryHoldResult PassThrough(in ThrottleBoundaryHoldInput input) =>
        new(
            Clamp01(input.RequestedThrottle),
            input.SimulatedThrottle,
            idleHeld: false,
            afterburnerHeld: false,
            shouldPinSimulatedThrottle: false);

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        return Math.Max(0, Math.Min(1, value));
    }
}
