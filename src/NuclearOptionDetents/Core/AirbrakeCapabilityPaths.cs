namespace NuclearOptionDetents.Core;

internal static class AirbrakeCapabilityPaths
{
    public static bool IsConfirmed(
        AirbrakePath path,
        bool componentConfirmed,
        bool splitSurfaceConfirmed) =>
        path switch
        {
            AirbrakePath.Component => componentConfirmed,
            AirbrakePath.Split => splitSurfaceConfirmed,
            _ => false,
        };

    public static bool HasActiveGate(
        AirbrakePath path,
        bool componentConfirmed,
        bool componentGateActive,
        bool splitSurfaceConfirmed,
        bool splitSurfaceGateActive) =>
        path switch
        {
            AirbrakePath.Component => componentConfirmed && componentGateActive,
            AirbrakePath.Split => splitSurfaceConfirmed && splitSurfaceGateActive,
            _ => false,
        };
}
