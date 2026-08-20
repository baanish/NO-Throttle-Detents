using System;

namespace NuclearOptionDetents.Core;

internal static class ReferenceOwnership
{
    public static bool AirbrakeMatches<TAircraft>(
        TAircraft? localAircraft,
        TAircraft? serializedAircraft,
        TAircraft? attachedAircraft)
        where TAircraft : class
        =>
        localAircraft is not null &&
        (ReferenceEquals(localAircraft, serializedAircraft) ||
         ReferenceEquals(localAircraft, attachedAircraft));

    public static bool AircraftMatches<TAircraft>(
        TAircraft? localAircraft,
        TAircraft? candidateAircraft)
        where TAircraft : class
        =>
        localAircraft is not null &&
        ReferenceEquals(localAircraft, candidateAircraft);
}
