using System;

namespace NuclearOptionDetents.Core;

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

public static class DetentIndicatorPolicy
{
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

public static class DetentIndicatorText
{
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

    internal static int RoundedPercent(double progress)
    {
        var clamped = Math.Max(0, Math.Min(1, progress));
        return (int)Math.Round(clamped * 100, MidpointRounding.AwayFromZero);
    }
}
