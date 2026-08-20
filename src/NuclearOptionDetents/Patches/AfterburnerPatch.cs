using System;
using NuclearOptionDetents.Compatibility;
using NuclearOptionDetents.Core;

namespace NuclearOptionDetents.Patches;

internal static class AfterburnerPatch
{
    public static void Prefix(JetNozzle __instance, ref bool allowAfterburner)
    {
        try
        {
            var needsConfirmation = RuntimeController.NeedsAfterburnerConfirmation;
            if (!allowAfterburner && !needsConfirmation)
            {
                return;
            }

            if (!RuntimeController.ShouldInspectAfterburnerCandidates)
            {
                return;
            }

            var aircraftAccessor = RuntimeCompatibility.JetNozzleAircraft;
            if (aircraftAccessor is null)
            {
                return;
            }

            var aircraft = aircraftAccessor(__instance);
            if (ReferenceEquals(aircraft, null))
            {
                return;
            }

            if (needsConfirmation)
            {
                RuntimeController.ObserveAfterburner(__instance, aircraft);
            }
            if (!allowAfterburner)
            {
                return;
            }

            if (RuntimeController.ShouldBlockAfterburner(aircraft, allowAfterburner))
            {
                allowAfterburner = false;
            }
        }
        catch (Exception exception)
        {
            RuntimeController.ReportPatchFailure("Afterburner", exception);
        }
    }
}
