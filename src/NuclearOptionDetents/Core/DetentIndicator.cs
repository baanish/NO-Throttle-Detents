using System;

namespace NuclearOptionDetents.Core;

/// <summary>One boundary's HUD row: whether to draw it, its lock state, and release-hold progress in 0..1.</summary>
public readonly struct DetentIndicatorLine
{
    internal DetentIndicatorLine(bool visible, EndpointDetentState state, double progress)
    {
        Visible = visible;
        State = state;
        Progress = progress;
    }

    public bool Visible { get; }
    public EndpointDetentState State { get; }
    public double Progress { get; }
}

/// <summary>What the HUD should show this frame for both boundaries.</summary>
public readonly struct DetentIndicatorSnapshot
{
    internal DetentIndicatorSnapshot(DetentIndicatorLine idle, DetentIndicatorLine afterburner)
    {
        Idle = idle;
        Afterburner = afterburner;
    }

    public DetentIndicatorLine Idle { get; }
    public DetentIndicatorLine Afterburner { get; }
    public bool Visible => Idle.Visible || Afterburner.Visible;

    public static DetentIndicatorSnapshot Hidden => new(default, default);
}

/// <summary>Derives HUD visibility from runtime state only; it touches no Unity object, so it stays testable off-engine.</summary>
public static class DetentIndicatorPolicy
{
    /// <summary>
    /// Shows a boundary only while the throttle is parked against it and the detent is still locked.
    /// Anything that makes this frame vanilla (indicator off, bypassed runtime, no boundary hold)
    /// hides both rows, so the HUD never claims a detent the runtime is not applying.
    /// </summary>
    public static DetentIndicatorSnapshot Evaluate(
        in DetentRuntimeSnapshot runtime,
        double throttle,
        double idleBoundary,
        double afterburnerBoundary,
        double endpointEpsilon,
        double idleHoldMilliseconds,
        double afterburnerHoldMilliseconds,
        bool enabled,
        bool boundaryHeld,
        bool idleApplies,
        bool afterburnerApplies)
    {
        if (!enabled || !boundaryHeld || runtime.IsBypassed)
        {
            return DetentIndicatorSnapshot.Hidden;
        }

        var epsilon = Math.Max(0, endpointEpsilon);
        var idleVisible = idleApplies &&
                          runtime.IdleState != EndpointDetentState.Unlocked &&
                          throttle <= idleBoundary + epsilon;
        var afterburnerVisible = afterburnerApplies &&
                                 runtime.AfterburnerState != EndpointDetentState.Unlocked &&
                                 throttle >= afterburnerBoundary - epsilon;
        return new DetentIndicatorSnapshot(
            new DetentIndicatorLine(
                idleVisible,
                runtime.IdleState,
                Progress(runtime.IdleElapsedSeconds, idleHoldMilliseconds)),
            new DetentIndicatorLine(
                afterburnerVisible,
                runtime.AfterburnerState,
                Progress(runtime.AfterburnerElapsedSeconds, afterburnerHoldMilliseconds)));
    }

    /// <summary>Elapsed fraction of the configured release hold; a zero-length hold reports no progress rather than a full bar.</summary>
    private static double Progress(double elapsedSeconds, double holdMilliseconds)
    {
        var durationSeconds = Math.Max(0, holdMilliseconds) / 1000.0;
        if (durationSeconds <= 0)
        {
            return 0;
        }

        return Math.Max(0, Math.Min(1, elapsedSeconds / durationSeconds));
    }
}

/// <summary>Renders a snapshot as the one short HUD line, idle taking precedence when both boundaries qualify.</summary>
public static class DetentIndicatorText
{
    /// <summary>Empty when nothing is held, so callers can treat the returned text as the whole HUD state.</summary>
    public static string Format(in DetentIndicatorSnapshot snapshot)
    {
        if (snapshot.Idle.Visible)
        {
            return FormatLine("IDLE", snapshot.Idle);
        }

        return snapshot.Afterburner.Visible
            ? FormatLine("AB", snapshot.Afterburner)
            : string.Empty;
    }

    private static string FormatLine(string boundary, in DetentIndicatorLine line)
    {
        if (line.State != EndpointDetentState.Holding)
        {
            return $"{boundary} LOCK";
        }

        return $"{boundary} HOLD {RoundedPercent(line.Progress)}%";
    }

    /// <summary>Shared by the text formatter and the HUD's change detection so both agree on when the number moved.</summary>
    internal static int RoundedPercent(double progress)
    {
        var clamped = Math.Max(0, Math.Min(1, progress));
        return (int)Math.Round(clamped * 100, MidpointRounding.AwayFromZero);
    }
}
