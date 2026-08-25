namespace NuclearOptionDetents.Diagnostics;

internal static class NetworkValidationSelection
{
    public const int MaximumObservedAircraft = 2;
    public const int SamplesPerSecond = 10;

    public static bool ShouldObserve(bool local, int owner, int requestedRemoteOwner) =>
        local || requestedRemoteOwner >= 0 && owner == requestedRemoteOwner;
}
