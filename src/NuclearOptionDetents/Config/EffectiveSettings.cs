using System;

namespace NuclearOptionDetents.Config;

internal readonly struct EffectiveSettings : IEquatable<EffectiveSettings>
{
    public EffectiveSettings(
        bool enabled,
        bool debugLogging,
        bool indicatorEnabled,
        float throttleSensitivity,
        bool idleEnabled,
        int idleHoldMilliseconds,
        bool afterburnerEnabled,
        int afterburnerHoldMilliseconds,
        float commandThreshold,
        float endpointEpsilon,
        float resetHysteresis)
    {
        (Enabled, DebugLogging, IndicatorEnabled, ThrottleSensitivity, IdleEnabled, IdleHoldMilliseconds, AfterburnerEnabled,
            AfterburnerHoldMilliseconds, CommandThreshold, EndpointEpsilon, ResetHysteresis) =
            (enabled, debugLogging, indicatorEnabled, throttleSensitivity, idleEnabled, idleHoldMilliseconds, afterburnerEnabled,
                afterburnerHoldMilliseconds, commandThreshold, endpointEpsilon, resetHysteresis);
    }

    public bool Enabled { get; }
    public bool DebugLogging { get; }
    public bool IndicatorEnabled { get; }
    public float ThrottleSensitivity { get; }
    public bool IdleEnabled { get; }
    public int IdleHoldMilliseconds { get; }
    public bool AfterburnerEnabled { get; }
    public int AfterburnerHoldMilliseconds { get; }
    public float CommandThreshold { get; }
    public float EndpointEpsilon { get; }
    public float ResetHysteresis { get; }

    public bool Equals(EffectiveSettings other) =>
        Enabled == other.Enabled &&
        DebugLogging == other.DebugLogging &&
        IndicatorEnabled == other.IndicatorEnabled &&
        ThrottleSensitivity.Equals(other.ThrottleSensitivity) &&
        IdleEnabled == other.IdleEnabled &&
        IdleHoldMilliseconds == other.IdleHoldMilliseconds &&
        AfterburnerEnabled == other.AfterburnerEnabled &&
        AfterburnerHoldMilliseconds == other.AfterburnerHoldMilliseconds &&
        CommandThreshold.Equals(other.CommandThreshold) &&
        EndpointEpsilon.Equals(other.EndpointEpsilon) &&
        ResetHysteresis.Equals(other.ResetHysteresis);

    public override bool Equals(object? obj) => obj is EffectiveSettings other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Enabled ? 1 : 0;
            hash = (hash * 397) ^ (DebugLogging ? 1 : 0);
            hash = (hash * 397) ^ (IndicatorEnabled ? 1 : 0);
            hash = (hash * 397) ^ ThrottleSensitivity.GetHashCode();
            hash = (hash * 397) ^ (IdleEnabled ? 1 : 0);
            hash = (hash * 397) ^ IdleHoldMilliseconds;
            hash = (hash * 397) ^ (AfterburnerEnabled ? 1 : 0);
            hash = (hash * 397) ^ AfterburnerHoldMilliseconds;
            hash = (hash * 397) ^ CommandThreshold.GetHashCode();
            hash = (hash * 397) ^ EndpointEpsilon.GetHashCode();
            return (hash * 397) ^ ResetHysteresis.GetHashCode();
        }
    }

}
