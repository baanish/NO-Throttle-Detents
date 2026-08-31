using NuclearOptionDetents.Core;
using NuclearOptionDetents.Diagnostics;

namespace NuclearOptionDetents.Tests;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Test)[]
        {
            ("approaching lower endpoint does not count", ApproachingLowerDoesNotCount),
            ("approaching upper endpoint does not count", ApproachingUpperDoesNotCount),
            ("lower unlocks at exact dwell", LowerUnlocksAtExactDwell),
            ("upper unlocks at exact dwell", UpperUnlocksAtExactDwell),
            ("release before dwell cancels", EarlyReleaseCancels),
            ("second attempt starts at zero", SecondAttemptStartsAtZero),
            ("lower unlock remains latched", LowerLatchRemainsAfterRelease),
            ("moving away relocks", MovingAwayRelocks),
            ("reverse travel stays unlocked until clear", ReverseTravelStaysUnlockedUntilClear),
            ("upper detent uses preset boundary", UpperDetentUsesPresetBoundary),
            ("detents are independent", DetentsAreIndependent),
            ("zero duration unlocks immediately", ZeroDurationUnlocksImmediately),
            ("disabled detent is transparent", DisabledDetentIsTransparent),
            ("master disabled is vanilla", MasterDisabledIsVanilla),
            ("Auto Hover bypass restarts a pending hold", AutoHoverBypassRestartsPendingHold),
            ("lifecycle reset clears both", LifecycleResetClearsBoth),
            ("cadence does not change result", CadenceIsIndependent),
            ("pause cancels dwell and passes gates", PausedTimeDoesNotAdvance),
            ("Axis Modifier cancels dwell and passes gates", AxisModifierCancelsHold),
            ("Axis Modifier preserves unlocked latch", AxisModifierPreservesUnlockedLatch),
            ("disabled controls lock dwell and pass gates", DisabledControlsPreserveLockedGates),
            ("opposite lower command locks dwell and passes gate", OppositeLowerCommandPassesGate),
            ("opposite upper command locks dwell and passes gate", OppositeUpperCommandPassesGate),
            ("locked idle publishes inward throttle", LockedIdlePublishesInwardThrottle),
            ("locked afterburner publishes inward throttle", LockedAfterburnerPublishesInwardThrottle),
            ("neutral endpoint does not initiate a boundary hold", NeutralEndpointDoesNotInitiateBoundaryHold),
            ("neutral maintains a float-roundtripped parked hold", NeutralMaintainsFloatRoundtrippedParkedHold),
            ("boundary offset is pinned for network transport", BoundaryOffsetIsPinnedForNetworkTransport),
            ("early release stays behind boundary", EarlyReleaseStaysBehindBoundary),
            ("exact dwell releases boundary", ExactDwellReleasesBoundary),
            ("negative range accumulator follows held boundary", NegativeRangeAccumulatorFollowsBoundary),
            ("nonnegative accumulator follows held boundary", NonnegativeAccumulatorFollowsBoundary),
            ("absolute throttle bypasses boundary hold", AbsoluteThrottleBypassesBoundaryHold),
            ("raw button release becomes neutral", RawButtonReleaseBecomesNeutral),
            ("absolute throttle bypasses detent runtime", AbsoluteThrottleBypassesDetentRuntime),
            ("reverse travel bypasses boundary hold", ReverseTravelBypassesBoundaryHold),
            ("disabled boundary hold is vanilla", DisabledBoundaryHoldIsVanilla),
            ("large epsilon preserves safe idle request", LargeEpsilonPreservesSafeIdleRequest),
            ("large epsilon preserves safe dry-thrust request", LargeEpsilonPreservesSafeDryThrustRequest),
            ("collective inversion reverses command direction", CollectiveInversionReversesCommandDirection),
            ("airbrake remains inhibited while holding", AirbrakeInhibitedWhileHolding),
            ("runtime snapshot governs idle flow", RuntimeSnapshotGovernsIdleFlow),
            ("runtime snapshot governs afterburner flow", RuntimeSnapshotGovernsAfterburnerFlow),
            ("readiness reports disabled mod", ReadinessReportsDisabledMod),
            ("readiness excludes absolute throttle", ReadinessExcludesAbsoluteThrottle),
            ("readiness waits for patch installation", ReadinessWaitsForPatchInstallation),
            ("readiness reports observer failure", ReadinessReportsObserverFailure),
            ("readiness reports one failed detent", ReadinessReportsOneFailedDetent),
            ("readiness waits for player aircraft", ReadinessWaitsForPlayerAircraft),
            ("readiness excludes unsupported aircraft", ReadinessExcludesUnsupportedAircraft),
            ("readiness excludes collective aircraft", ReadinessExcludesCollectiveAircraft),
            ("readiness names unsupported aircraft before throttle mode", ReadinessNamesUnsupportedBeforeThrottleMode),
            ("readiness names collective aircraft before throttle mode", ReadinessNamesCollectiveBeforeThrottleMode),
            ("readiness excludes aircraft without matching systems", ReadinessExcludesAircraftWithoutMatchingSystems),
            ("readiness ignores a failed non-applicable gate", ReadinessIgnoresFailedNonApplicableGate),
            ("readiness becomes likely after sustained input", ReadinessBecomesLikelyAfterInput),
            ("readiness names the only enabled detent", ReadinessNamesOnlyEnabledDetent),
            ("airframe preset allowlist is pinned", AirframePresetAllowlistIsPinned),
            ("custom preset is an exact fail-open fallback", CustomPresetIsExactFailOpenFallback),
            ("custom dry detent percentages are validated", CustomDryDetentPercentagesAreValidated),
            ("detected aircraft profiles round trip independently", DetectedAircraftProfilesRoundTripIndependently),
            ("detected aircraft identity updates without duplicates", DetectedAircraftIdentityUpdatesWithoutDuplicates),
            ("malformed detected aircraft records are ignored", MalformedDetectedAircraftRecordsAreIgnored),
            ("interior detent holds in both directions", InteriorDetentHoldsInBothDirections),
            ("interior detent follows the cockpit percentage range", InteriorDetentFollowsCockpitPercentageRange),
            ("interior detent does not snap and catches the first crossing", InteriorDetentDoesNotSnapAndCatchesFirstCrossing),
            ("nearby interior detents unlock independently", NearbyInteriorDetentsUnlockIndependently),
            ("interior detent requires a continuous hold", InteriorDetentRequiresContinuousHold),
            ("interior detent interruption clears crossing history", InteriorDetentInterruptionClearsCrossingHistory),
            ("custom detent readiness and HUD are explicit", CustomDetentReadinessAndHudAreExplicit),
            ("sensitivity scope is limited to detented aircraft", SensitivityScopeIsLimitedToDetentedAircraft),
            ("all afterburner nozzles must match", AllAfterburnerNozzlesMustMatch),
            ("modded afterburner nozzle counts are pinned", ModdedAfterburnerNozzleCountsArePinned),
            ("AB-4 requires all four afterburner nozzles", Ab4RequiresFourAfterburnerNozzles),
            ("live afterburner start is the conservative boundary", LiveAfterburnerStartIsConservativeBoundary),
            ("unreadable afterburner nozzle rejects confirmation", UnreadableAfterburnerNozzleRejectsConfirmation),
            ("mixed afterburner capability rejects confirmation", MixedAfterburnerCapabilityRejectsConfirmation),
            ("airframe preset lookup accepts runtime casing", AirframePresetLookupAcceptsRuntimeCasing),
            ("unknown and missing airframes bypass", UnknownAndMissingAirframesBypass),
            ("collective and runtime collective bypass", CollectiveAndRuntimeCollectiveBypass),
            ("unsupported expected feature bypasses", UnsupportedExpectedFeatureBypasses),
            ("live confirmation is required", LiveConfirmationIsRequired),
            ("runtime reconfigure preserves lower holding state", RuntimeReconfigurePreservesLowerHoldingState),
            ("runtime reconfigure preserves upper unlocked state", RuntimeReconfigurePreservesUpperUnlockedState),
            ("afterburner retarget preserves idle state", AfterburnerRetargetPreservesIdleState),
            ("disabled capability does not block readiness", DisabledCapabilityDoesNotBlockReadiness),
            ("low-frequency observer gap counts elapsed dwell", LowFrequencyObserverGapCountsElapsedDwell),
            ("explicit cancellation resets pending holds", ExplicitCancellationResetsPendingHolds),
            ("output mapping follows configured mode", OutputMappingFollowsConfiguredMode),
            ("sensitivity scales observed vanilla movement", SensitivityScalesObservedVanillaMovement),
            ("vanilla deadzone remains unchanged", VanillaDeadzoneRemainsUnchanged),
            ("unexpected throttle write rebases sensitivity", UnexpectedThrottleWriteRebasesSensitivity),
            ("foreign throttle writer yields only while active", ForeignThrottleWriterYieldsOnlyWhileActive),
            ("external integrator owns sensitivity", ExternalIntegratorOwnsSensitivity),
            ("non-detent aircraft bypass sensitivity", NonDetentAircraftBypassesSensitivity),
            ("indicator hides when boundary hold bypasses", IndicatorHidesWhenBoundaryHoldBypasses),
            ("indicator is hidden while bypassed", IndicatorHiddenWhileBypassed),
            ("indicator shows idle lock progress", IndicatorShowsIdleLockProgress),
            ("indicator shows afterburner lock", IndicatorShowsAfterburnerLock),
            ("indicator text is empty while hidden", IndicatorTextIsEmptyWhileHidden),
            ("indicator text formats idle hold", IndicatorTextFormatsIdleHold),
            ("indicator text formats afterburner lock", IndicatorTextFormatsAfterburnerLock),
            ("indicator text prefers idle if both are visible", IndicatorTextPrefersIdle),
            ("network validation always observes local", NetworkValidationAlwaysObservesLocal),
            ("network validation requires a selected remote", NetworkValidationRequiresSelectedRemote),
            ("network validation observes only the selected remote", NetworkValidationObservesOnlySelectedRemote),
            ("network validation volume is capped", NetworkValidationVolumeIsCapped),
        };

        int failures = 0;
        foreach (var (name, test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed");
        return failures == 0 ? 0 : 1;
    }

    private static void NetworkValidationAlwaysObservesLocal()
    {
        True(NetworkValidationSelection.ShouldObserve(local: true, owner: 30, requestedRemoteOwner: -1));
        True(NetworkValidationSelection.ShouldObserve(local: true, owner: 30, requestedRemoteOwner: 8));
    }

    private static void NetworkValidationRequiresSelectedRemote()
    {
        False(NetworkValidationSelection.ShouldObserve(local: false, owner: 8, requestedRemoteOwner: -1));
    }

    private static void NetworkValidationObservesOnlySelectedRemote()
    {
        True(NetworkValidationSelection.ShouldObserve(local: false, owner: 8, requestedRemoteOwner: 8));
        False(NetworkValidationSelection.ShouldObserve(local: false, owner: 3, requestedRemoteOwner: 8));
    }

    private static void NetworkValidationVolumeIsCapped()
    {
        Equal(2, NetworkValidationSelection.MaximumObservedAircraft);
        Equal(10, NetworkValidationSelection.SamplesPerSecond);
    }

    private static EndpointDetent Lower(double milliseconds = 200)
        => new(DetentDirection.Lower, milliseconds / 1000.0);

    private static EndpointDetent Upper(double milliseconds = 200)
        => new(DetentDirection.Upper, milliseconds / 1000.0);

    private static void ApproachingLowerDoesNotCount()
    {
        var detent = Lower();
        detent.Update(new EndpointDetentInput(0, 0.5, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(1, 0.0011, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Locked, detent.State);
        detent.Update(new EndpointDetentInput(1.1, 0, ThrottleCommand.Neutral));
        detent.Update(new EndpointDetentInput(1.1, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(1.2, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(1.2999, 0, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Holding, detent.State);
    }

    private static void ApproachingUpperDoesNotCount()
    {
        var detent = Upper();
        detent.Update(new EndpointDetentInput(0, 0.5, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(1, 0.9989, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Locked, detent.State);
        detent.Update(new EndpointDetentInput(1.1, 1, ThrottleCommand.Neutral));
        detent.Update(new EndpointDetentInput(1.1, 1, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(1.2, 1, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(1.2999, 1, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Holding, detent.State);
    }

    private static void LowerUnlocksAtExactDwell()
    {
        var detent = Lower();
        detent.Update(new EndpointDetentInput(10, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(10.1, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(10.2, 0, ThrottleCommand.Decrease));
        True(detent.IsUnlocked);
    }

    private static void UpperUnlocksAtExactDwell()
    {
        var detent = Upper();
        detent.Update(new EndpointDetentInput(10, 1, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(10.1, 1, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(10.2, 1, ThrottleCommand.Increase));
        True(detent.IsUnlocked);
    }

    private static void EarlyReleaseCancels()
    {
        var detent = Lower();
        detent.Update(new EndpointDetentInput(0, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(0.199, 0, ThrottleCommand.Neutral));
        Equal(EndpointDetentState.Locked, detent.State);
        True(detent.ElapsedHoldSeconds == 0);
    }

    private static void SecondAttemptStartsAtZero()
    {
        var detent = Lower();
        detent.Update(new EndpointDetentInput(0, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(0.199, 0, ThrottleCommand.Neutral));
        detent.Update(new EndpointDetentInput(1, 0, ThrottleCommand.Decrease));
        Near(0, detent.ElapsedHoldSeconds);
        detent.Update(new EndpointDetentInput(1.1, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(1.199, 0, ThrottleCommand.Decrease));
        True(detent.IsHolding);
        detent.Update(new EndpointDetentInput(1.2, 0, ThrottleCommand.Decrease));
        True(detent.IsUnlocked);
    }

    private static void LowerLatchRemainsAfterRelease()
    {
        var detent = Lower();
        UnlockLower(detent);
        detent.Update(new EndpointDetentInput(1, 0, ThrottleCommand.Neutral));
        True(detent.IsUnlocked);
    }

    private static void MovingAwayRelocks()
    {
        var detent = Lower();
        UnlockLower(detent);
        detent.Update(new EndpointDetentInput(1, 0.021, ThrottleCommand.Neutral));
        True(detent.IsLocked);
    }

    private static void ReverseTravelStaysUnlockedUntilClear()
    {
        var detent = new EndpointDetent(DetentDirection.Upper, 0.2, 0.9, 0.001, 0.02);
        detent.Update(new EndpointDetentInput(0, 0.9, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(0.1, 0.9, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(0.2, 0.9, ThrottleCommand.Increase));
        True(detent.IsUnlocked);
        detent.Update(new EndpointDetentInput(0.3, 0.95, ThrottleCommand.Decrease));
        True(detent.IsUnlocked);
        detent.Update(new EndpointDetentInput(0.4, 0.879, ThrottleCommand.Decrease));
        True(detent.IsLocked);
    }

    private static void UpperDetentUsesPresetBoundary()
    {
        var detent = new EndpointDetent(DetentDirection.Upper, 2, 0.9, 0.000001, 0.02);
        detent.Update(new EndpointDetentInput(0, 0.899998, ThrottleCommand.Increase));
        True(detent.IsLocked);
        detent.Update(new EndpointDetentInput(1, 0.9, ThrottleCommand.Increase));
        True(detent.IsHolding);
        Near(0.9, detent.Boundary);
    }

    private static void DetentsAreIndependent()
    {
        var lower = Lower();
        var upper = Upper();
        UnlockLower(lower);
        True(lower.IsUnlocked);
        True(upper.IsLocked);
        UnlockUpper(upper);
        True(lower.IsUnlocked);
        True(upper.IsUnlocked);
    }

    private static void ZeroDurationUnlocksImmediately()
    {
        var detent = Lower(0);
        detent.Update(new EndpointDetentInput(4, 0, ThrottleCommand.Decrease));
        True(detent.IsUnlocked);
    }

    private static void DisabledDetentIsTransparent()
    {
        var runtime = new DetentRuntime();
        var result = runtime.Update(new DetentRuntimeInput(
            0,
            0,
            ThrottleCommand.Neutral,
            idleEnabled: false,
            afterburnerEnabled: false));
        False(result.AirbrakeInhibited);
        True(runtime.IdleDetent.IsUnlocked);
        True(result.AfterburnerUnlocked);
    }

    private static void MasterDisabledIsVanilla()
    {
        var runtime = new DetentRuntime();
        var result = runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Neutral, masterEnabled: false));
        True(result.IsBypassed);
        False(result.AirbrakeInhibited);
        True(result.AfterburnerUnlocked);
    }

    private static void AutoHoverBypassRestartsPendingHold()
    {
        var runtime = new DetentRuntime(200, 200);
        runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        runtime.Update(new DetentRuntimeInput(0.1, 0, ThrottleCommand.Decrease));

        var bypassed = runtime.Update(new DetentRuntimeInput(
            0.15,
            0.4,
            ThrottleCommand.Neutral,
            masterEnabled: false));
        True(bypassed.IsBypassed);
        Equal(EndpointDetentState.Unlocked, bypassed.IdleState);

        var resumed = runtime.Update(new DetentRuntimeInput(1, 0, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Holding, resumed.IdleState);
        Near(0, resumed.IdleElapsedSeconds);
    }

    private static void LifecycleResetClearsBoth()
    {
        var runtime = new DetentRuntime();
        var aircraftA = new object();
        var controlsA = new object();
        runtime.ObserveContext(aircraftA, controlsA);
        UnlockRuntimeLower(runtime);
        runtime.ObserveContext(new object(), new object());
        True(runtime.IdleDetent.IsLocked);
        True(runtime.AfterburnerDetent.IsLocked);
    }

    private static void CadenceIsIndependent()
    {
        var coarse = Lower();
        coarse.Update(new EndpointDetentInput(0, 0, ThrottleCommand.Decrease));
        coarse.Update(new EndpointDetentInput(0.1, 0, ThrottleCommand.Decrease));
        coarse.Update(new EndpointDetentInput(0.2, 0, ThrottleCommand.Decrease));

        var fine = Lower();
        fine.Update(new EndpointDetentInput(0, 0, ThrottleCommand.Decrease));
        fine.Update(new EndpointDetentInput(0.03, 0, ThrottleCommand.Decrease));
        fine.Update(new EndpointDetentInput(0.09, 0, ThrottleCommand.Decrease));
        fine.Update(new EndpointDetentInput(0.19, 0, ThrottleCommand.Decrease));
        fine.Update(new EndpointDetentInput(0.2, 0, ThrottleCommand.Decrease));
        True(coarse.IsUnlocked);
        True(fine.IsUnlocked);
    }

    private static void LowFrequencyObserverGapCountsElapsedDwell()
    {
        var detent = Lower();
        detent.Update(new EndpointDetentInput(0, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(
            0.15,
            0,
            ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Holding, detent.State);
        Near(0.15, detent.ElapsedHoldSeconds);
        detent.Update(new EndpointDetentInput(
            0.2,
            0,
            ThrottleCommand.Decrease));

        True(detent.IsUnlocked);
    }

    private static void ExplicitCancellationResetsPendingHolds()
    {
        var runtime = new DetentRuntime();
        runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        runtime.Update(new DetentRuntimeInput(0.1, 0, ThrottleCommand.Decrease));

        runtime.CancelPendingHolds();
        var resumed = runtime.Update(new DetentRuntimeInput(1, 0, ThrottleCommand.Decrease));

        Equal(EndpointDetentState.Holding, resumed.IdleState);
        Near(0, resumed.IdleElapsedSeconds);
    }

    private static void PausedTimeDoesNotAdvance()
    {
        var lower = new DetentRuntime();
        lower.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        var lowerSnapshot = lower.Update(new DetentRuntimeInput(
            1,
            0,
            ThrottleCommand.Decrease,
            paused: true));
        True(lower.IdleDetent.IsLocked);
        False(lowerSnapshot.AirbrakeInhibited);

        var upper = new DetentRuntime();
        upper.Update(new DetentRuntimeInput(0, 1, ThrottleCommand.Increase));
        var upperSnapshot = upper.Update(new DetentRuntimeInput(
            1,
            1,
            ThrottleCommand.Increase,
            paused: true));
        True(upper.AfterburnerDetent.IsLocked);
        True(upperSnapshot.AfterburnerUnlocked);
    }

    private static void AxisModifierCancelsHold()
    {
        var lower = new DetentRuntime();
        lower.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        var lowerSnapshot = lower.Update(new DetentRuntimeInput(
            0.1,
            0,
            ThrottleCommand.Decrease,
            axisModifierHeld: true));
        True(lower.IdleDetent.IsLocked);
        False(lowerSnapshot.AirbrakeInhibited);

        var upper = new DetentRuntime();
        upper.Update(new DetentRuntimeInput(0, 1, ThrottleCommand.Increase));
        var upperSnapshot = upper.Update(new DetentRuntimeInput(
            0.1,
            1,
            ThrottleCommand.Increase,
            axisModifierHeld: true));
        True(upper.AfterburnerDetent.IsLocked);
        True(upperSnapshot.AfterburnerUnlocked);
    }

    private static void AxisModifierPreservesUnlockedLatch()
    {
        var runtime = new DetentRuntime();
        UnlockRuntimeLower(runtime);
        var result = runtime.Update(new DetentRuntimeInput(
            0.3,
            0,
            ThrottleCommand.Increase,
            axisModifierHeld: true));
        True(runtime.IdleDetent.IsUnlocked);
        False(result.AirbrakeInhibited);
    }

    private static void DisabledControlsPreserveLockedGates()
    {
        var runtime = new DetentRuntime();
        var lower = runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        lower = runtime.Update(new DetentRuntimeInput(
            0.1,
            0,
            ThrottleCommand.Decrease,
            controlsEnabled: false));
        True(runtime.IdleDetent.IsLocked);
        False(lower.AirbrakeInhibited);

        var upperRuntime = new DetentRuntime();
        upperRuntime.Update(new DetentRuntimeInput(1, 1, ThrottleCommand.Increase));
        var upper = upperRuntime.Update(new DetentRuntimeInput(
            1.1,
            1,
            ThrottleCommand.Increase,
            controlsEnabled: false));
        True(upperRuntime.AfterburnerDetent.IsLocked);
        True(upper.AfterburnerUnlocked);
    }

    private static void OppositeLowerCommandPassesGate()
    {
        var runtime = new DetentRuntime();
        runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        var snapshot = runtime.Update(new DetentRuntimeInput(0.1, 0, ThrottleCommand.Increase));
        True(runtime.IdleDetent.IsLocked);
        False(snapshot.AirbrakeInhibited);
    }

    private static void OppositeUpperCommandPassesGate()
    {
        var runtime = new DetentRuntime();
        runtime.Update(new DetentRuntimeInput(0, 1, ThrottleCommand.Increase));
        var snapshot = runtime.Update(new DetentRuntimeInput(0.1, 1, ThrottleCommand.Decrease));
        True(runtime.AfterburnerDetent.IsLocked);
        True(snapshot.AfterburnerUnlocked);
    }

    private static void LockedIdlePublishesInwardThrottle()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0, -1, ThrottleCommand.Decrease,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            afterburnerApplies: false));
        Near(ThrottleBoundaryHold.InwardOffset, result.EffectiveThrottle);
        True(result.IdleHeld);
        False(result.AfterburnerHeld);
    }

    private static void LockedAfterburnerPublishesInwardThrottle()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.91, 0.82, ThrottleCommand.Increase,
            EndpointDetentState.Locked, EndpointDetentState.Holding,
            0, 0.9, 0.001,
            idleApplies: false));
        Near(0.9 - ThrottleBoundaryHold.InwardOffset, result.EffectiveThrottle);
        False(result.IdleHeld);
        True(result.AfterburnerHeld);
    }

    private static void NeutralEndpointDoesNotInitiateBoundaryHold()
    {
        var idle = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0, -1, ThrottleCommand.Neutral,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            afterburnerApplies: false));
        Near(0, idle.EffectiveThrottle);
        False(idle.IsHeld);

        var afterburner = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            1, 1, ThrottleCommand.Neutral,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            idleApplies: false));
        Near(1, afterburner.EffectiveThrottle);
        False(afterburner.IsHeld);
    }

    private static void BoundaryOffsetIsPinnedForNetworkTransport()
    {
        Near(0.0001, ThrottleBoundaryHold.InwardOffset);
        var idlePin = (Half)ThrottleBoundaryHold.InwardOffset;
        var idleBits = (ushort)BitConverter.HalfToInt16Bits(idlePin);
        True((idleBits & 0x7c00) != 0);
        True((double)idlePin > 0);

        var afterburnerPin = (Half)(0.9 - ThrottleBoundaryHold.InwardOffset);
        True((double)afterburnerPin < 0.9);
    }

    private static void NeutralMaintainsFloatRoundtrippedParkedHold()
    {
        var simulated = (float)SimulatedThrottleMapping.ToSimulated(
            ThrottleBoundaryHold.InwardOffset,
            SimulatedThrottleRange.NegativeOneToOne);
        var requested = (float)SimulatedThrottleMapping.ToPublic(
            simulated,
            SimulatedThrottleRange.NegativeOneToOne);
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            requested, simulated, ThrottleCommand.Neutral,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            afterburnerApplies: false,
            throttleRange: SimulatedThrottleRange.NegativeOneToOne));

        True(result.IdleHeld);
        Near(ThrottleBoundaryHold.InwardOffset, result.EffectiveThrottle);
    }

    private static void EarlyReleaseStaysBehindBoundary()
    {
        var runtime = new DetentRuntime(2000, 2000, 0.001, 0.02, 0, 0.9);
        runtime.Update(new DetentRuntimeInput(0, 0.91, ThrottleCommand.Increase));
        var snapshot = runtime.Update(new DetentRuntimeInput(
            0.5,
            0.9 - ThrottleBoundaryHold.InwardOffset,
            ThrottleCommand.Neutral));
        Equal(EndpointDetentState.Locked, snapshot.AfterburnerState);

        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.9 - ThrottleBoundaryHold.InwardOffset, 0.8, ThrottleCommand.Neutral,
            snapshot.IdleState, snapshot.AfterburnerState,
            0, 0.9, 0.001,
            idleApplies: false));
        Near(0.9 - ThrottleBoundaryHold.InwardOffset, result.EffectiveThrottle);
        True(result.AfterburnerHeld);
    }

    private static void ExactDwellReleasesBoundary()
    {
        var runtime = new DetentRuntime(200, 200, 0.001, 0.02, 0, 0.9);
        runtime.Update(new DetentRuntimeInput(0, 0.91, ThrottleCommand.Increase));
        runtime.Update(new DetentRuntimeInput(0.1, 0.91, ThrottleCommand.Increase));
        var snapshot = runtime.Update(new DetentRuntimeInput(0.2, 0.91, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Unlocked, snapshot.AfterburnerState);

        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.91, 0.82, ThrottleCommand.Increase,
            snapshot.IdleState, snapshot.AfterburnerState,
            0, 0.9, 0.001,
            idleApplies: false));
        Near(0.91, result.EffectiveThrottle);
        False(result.IsHeld);
    }

    private static void NegativeRangeAccumulatorFollowsBoundary()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.91, 0.82, ThrottleCommand.Increase,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            throttleRange: SimulatedThrottleRange.NegativeOneToOne,
            idleApplies: false));
        Near((0.9 - ThrottleBoundaryHold.InwardOffset) * 2 - 1, result.SimulatedThrottle);
    }

    private static void NonnegativeAccumulatorFollowsBoundary()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.91, 0.91, ThrottleCommand.Increase,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            throttleRange: SimulatedThrottleRange.ZeroToOne,
            idleApplies: false));
        Near(0.9 - ThrottleBoundaryHold.InwardOffset, result.SimulatedThrottle);
    }

    private static void AbsoluteThrottleBypassesBoundaryHold()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.95, 0.9, ThrottleCommand.Increase,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            relativeThrottleMode: false,
            idleApplies: false));
        Near(0.95, result.EffectiveThrottle);
        Near(0.9, result.SimulatedThrottle);
        False(result.AfterburnerHeld);
        False(result.ShouldPinSimulatedThrottle);
    }

    private static void RawButtonReleaseBecomesNeutral()
    {
        Equal(ThrottleCommand.Increase, ThrottleCommands.FromRawAxis(1, 0.5));
        Equal(ThrottleCommand.Neutral, ThrottleCommands.FromRawAxis(0, 0.5));
        Equal(ThrottleCommand.Decrease, ThrottleCommands.FromRawAxis(-1, 0.5));
    }

    private static void AbsoluteThrottleBypassesDetentRuntime()
    {
        var runtime = new DetentRuntime(200, 200, 0.001, 0.02, 0, 0.9);
        var result = runtime.Update(new DetentRuntimeInput(
            0,
            0,
            ThrottleCommand.Decrease,
            relativeThrottleMode: false));
        True(result.IsBypassed);
        False(result.AirbrakeInhibited);
        True(result.AfterburnerUnlocked);
    }

    private static void ReverseTravelBypassesBoundaryHold()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.95, 0.9, ThrottleCommand.Decrease,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            idleApplies: false));
        Near(0.95, result.EffectiveThrottle);
        False(result.IsHeld);
    }

    private static void DisabledBoundaryHoldIsVanilla()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0, -1, ThrottleCommand.Decrease,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            enabled: false));
        Near(0, result.EffectiveThrottle);
        Near(-1, result.SimulatedThrottle);
        False(result.IsHeld);
    }

    private static void LargeEpsilonPreservesSafeIdleRequest()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.03, -0.94, ThrottleCommand.Neutral,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.05,
            afterburnerApplies: false));
        Near(0.03, result.EffectiveThrottle);
        False(result.IdleHeld);
    }

    private static void LargeEpsilonPreservesSafeDryThrustRequest()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.85, 0.7, ThrottleCommand.Neutral,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.05,
            idleApplies: false));
        Near(0.85, result.EffectiveThrottle);
        False(result.AfterburnerHeld);
    }

    private static void CollectiveInversionReversesCommandDirection()
    {
        Equal(ThrottleCommand.Increase, ThrottleCommands.FromRawAxis(1, 0.5));
        Equal(ThrottleCommand.Decrease, ThrottleCommands.FromRawAxis(-1, 0.5));
        Equal(ThrottleCommand.Decrease, ThrottleCommands.FromRawAxis(1, 0.5, reverseDirection: true));
        Equal(ThrottleCommand.Increase, ThrottleCommands.FromRawAxis(-1, 0.5, reverseDirection: true));
    }

    private static void AirbrakeInhibitedWhileHolding()
    {
        var spawnRuntime = new DetentRuntime();
        var spawnSnapshot = spawnRuntime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Neutral));
        True(spawnRuntime.IdleDetent.IsLocked);
        True(spawnSnapshot.AirbrakeInhibited);

        var runtime = new DetentRuntime();
        var result = runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        True(runtime.IdleDetent.IsHolding);
        True(result.AirbrakeInhibited);
    }

    private static void RuntimeSnapshotGovernsIdleFlow()
    {
        var runtime = new DetentRuntime();
        var snapshot = runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Holding, snapshot.IdleState);
        Near(0, snapshot.IdleElapsedSeconds);
        True(snapshot.AirbrakeInhibited);

        snapshot = runtime.Update(new DetentRuntimeInput(0.1, 0, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Holding, snapshot.IdleState);
        Near(0.1, snapshot.IdleElapsedSeconds);
        True(snapshot.AirbrakeInhibited);

        snapshot = runtime.Update(new DetentRuntimeInput(0.2, 0, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Unlocked, snapshot.IdleState);
        Near(0, snapshot.IdleElapsedSeconds);
        False(snapshot.AirbrakeInhibited);

        snapshot = runtime.Update(new DetentRuntimeInput(0.3, 0, ThrottleCommand.Neutral));
        Equal(EndpointDetentState.Unlocked, snapshot.IdleState);
        False(snapshot.AirbrakeInhibited);

        snapshot = runtime.Update(new DetentRuntimeInput(0.4, 0.03, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Locked, snapshot.IdleState);
        False(snapshot.AirbrakeInhibited);
    }

    private static void RuntimeSnapshotGovernsAfterburnerFlow()
    {
        var runtime = new DetentRuntime();
        var snapshot = runtime.Update(new DetentRuntimeInput(0, 1, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Holding, snapshot.AfterburnerState);
        Near(0, snapshot.AfterburnerElapsedSeconds);
        False(snapshot.AfterburnerUnlocked);

        snapshot = runtime.Update(new DetentRuntimeInput(0.1, 1, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Holding, snapshot.AfterburnerState);
        Near(0.1, snapshot.AfterburnerElapsedSeconds);
        False(snapshot.AfterburnerUnlocked);

        snapshot = runtime.Update(new DetentRuntimeInput(0.2, 1, ThrottleCommand.Increase));
        Equal(EndpointDetentState.Unlocked, snapshot.AfterburnerState);
        Near(0, snapshot.AfterburnerElapsedSeconds);
        True(snapshot.AfterburnerUnlocked);

        snapshot = runtime.Update(new DetentRuntimeInput(0.3, 1, ThrottleCommand.Neutral));
        Equal(EndpointDetentState.Unlocked, snapshot.AfterburnerState);
        True(snapshot.AfterburnerUnlocked);

        snapshot = runtime.Update(new DetentRuntimeInput(0.4, 0.8, ThrottleCommand.Decrease));
        Equal(EndpointDetentState.Locked, snapshot.AfterburnerState);
        True(snapshot.AfterburnerUnlocked);
    }

    private static void OutputMappingFollowsConfiguredMode()
    {
        Near(0, SimulatedThrottleMapping.ToPublic(-0.016, SimulatedThrottleRange.ZeroToOne));
        Near(0.492, SimulatedThrottleMapping.ToPublic(-0.016, SimulatedThrottleRange.NegativeOneToOne));
        Near(0, SimulatedThrottleMapping.ToSimulated(0, SimulatedThrottleRange.ZeroToOne));
        Near(-1, SimulatedThrottleMapping.ToSimulated(0, SimulatedThrottleRange.NegativeOneToOne));
    }

    private static void SensitivityScalesObservedVanillaMovement()
    {
        Near(-0.032, RelativeThrottleSensitivity.Apply(0, -0.016, -1, 0.016, 2, enabled: true));
        Near(0.908, RelativeThrottleSensitivity.Apply(0.9, 0.916, 1, 0.016, 0.5, enabled: true));
        Near(1, RelativeThrottleSensitivity.Apply(0.99, 1, 1, 0.016, 4, enabled: true));
        Near(-1, RelativeThrottleSensitivity.Apply(-0.99, -1, -1, 0.016, 4, enabled: true));
        Near(-0.016, RelativeThrottleSensitivity.Apply(0, -0.016, -1, 0.016, 1, enabled: true));
    }

    private static void VanillaDeadzoneRemainsUnchanged()
    {
        Near(0.4, RelativeThrottleSensitivity.Apply(0.4, 0.4, 0.04, 0.016, 4, enabled: true));
        Near(0.4, RelativeThrottleSensitivity.Apply(0.4, 0.4, 0.1, 0.016, 4, enabled: true));
    }

    private static void UnexpectedThrottleWriteRebasesSensitivity()
    {
        Near(0.7, RelativeThrottleSensitivity.Apply(0.4, 0.7, 1, 0.016, 4, enabled: true));
    }

    private static void ForeignThrottleWriterYieldsOnlyWhileActive()
    {
        False(ThrottleOutputOwnership.IsForeignControlActive(
            foreignThrottlePatchPresent: false,
            publicThrottle: 1,
            simulatedThrottle: 0,
            SimulatedThrottleRange.NegativeOneToOne));
        False(ThrottleOutputOwnership.IsForeignControlActive(
            foreignThrottlePatchPresent: true,
            publicThrottle: 0.5,
            simulatedThrottle: 0,
            SimulatedThrottleRange.NegativeOneToOne));
        False(ThrottleOutputOwnership.IsForeignControlActive(
            foreignThrottlePatchPresent: true,
            publicThrottle: 0,
            simulatedThrottle: -0.4,
            SimulatedThrottleRange.ZeroToOne));
        False(ThrottleOutputOwnership.IsForeignControlActive(
            foreignThrottlePatchPresent: true,
            publicThrottle: 0.5009,
            simulatedThrottle: 0,
            SimulatedThrottleRange.NegativeOneToOne));
        True(ThrottleOutputOwnership.IsForeignControlActive(
            foreignThrottlePatchPresent: true,
            publicThrottle: 0.5011,
            simulatedThrottle: 0,
            SimulatedThrottleRange.NegativeOneToOne));
        False(ThrottleOutputOwnership.IsForeignControlActive(
            foreignThrottlePatchPresent: true,
            publicThrottle: 1,
            simulatedThrottle: -1,
            SimulatedThrottleRange.NegativeOneToOne,
            invertOutput: true));
    }

    private static void ExternalIntegratorOwnsSensitivity()
    {
        False(RelativeThrottleSensitivity.ShouldApply(
            enabled: true,
            relativeThrottleMode: true,
            detentedAircraft: true,
            controlsEnabled: true,
            paused: false,
            axisModifierHeld: false,
            externalIntegratorActive: true,
            hasPreviousValue: true));
    }

    private static void NonDetentAircraftBypassesSensitivity()
    {
        False(RelativeThrottleSensitivity.ShouldApply(
            enabled: true,
            relativeThrottleMode: true,
            detentedAircraft: false,
            controlsEnabled: true,
            paused: false,
            axisModifierHeld: false,
            externalIntegratorActive: false,
            hasPreviousValue: true));
    }

    private static void IndicatorHidesWhenBoundaryHoldBypasses()
    {
        var runtime = new DetentRuntimeSnapshot(
            bypassed: false,
            EndpointDetentState.Holding,
            idleElapsedSeconds: 0.1,
            airbrakeInhibited: false,
            EndpointDetentState.Locked,
            afterburnerElapsedSeconds: 0,
            afterburnerUnlocked: false);
        var indicator = DetentIndicatorPolicy.Evaluate(
            runtime, 0, 0, 0.9, 0.001, 200, 200,
            enabled: true, boundaryHeld: false, idleApplies: true, afterburnerApplies: false);

        False(indicator.Visible);
    }
    private static void IndicatorHiddenWhileBypassed()
    {
        var runtime = new DetentRuntimeSnapshot(
            bypassed: true,
            EndpointDetentState.Holding,
            idleElapsedSeconds: 0.1,
            airbrakeInhibited: false,
            EndpointDetentState.Locked,
            afterburnerElapsedSeconds: 0,
            afterburnerUnlocked: false);
        var indicator = DetentIndicatorPolicy.Evaluate(
            runtime, 0, 0, 0.9, 0.001, 200, 200,
            enabled: true, boundaryHeld: true, idleApplies: true, afterburnerApplies: true);

        False(indicator.Visible);
    }

    private static void IndicatorShowsIdleLockProgress()
    {
        var runtime = new DetentRuntimeSnapshot(
            bypassed: false,
            EndpointDetentState.Holding,
            idleElapsedSeconds: 0.1,
            airbrakeInhibited: true,
            EndpointDetentState.Locked,
            afterburnerElapsedSeconds: 0,
            afterburnerUnlocked: false);
        var indicator = DetentIndicatorPolicy.Evaluate(
            runtime, 0.000001, 0, 0.9, 0.001, 200, 200,
            enabled: true, boundaryHeld: true, idleApplies: true, afterburnerApplies: true);

        True(indicator.Idle.Visible);
        Equal(EndpointDetentState.Holding, indicator.Idle.State);
        Near(0.5, indicator.Idle.Progress);
        False(indicator.Afterburner.Visible);
    }

    private static void IndicatorShowsAfterburnerLock()
    {
        var runtime = new DetentRuntimeSnapshot(
            bypassed: false,
            EndpointDetentState.Locked,
            idleElapsedSeconds: 0,
            airbrakeInhibited: false,
            EndpointDetentState.Locked,
            afterburnerElapsedSeconds: 0,
            afterburnerUnlocked: false);
        var indicator = DetentIndicatorPolicy.Evaluate(
            runtime, 0.899999, 0, 0.9, 0.001, 200, 200,
            enabled: true, boundaryHeld: true, idleApplies: false, afterburnerApplies: true);

        True(indicator.Afterburner.Visible);
        Equal(EndpointDetentState.Locked, indicator.Afterburner.State);
    }

    private static void IndicatorTextIsEmptyWhileHidden()
    {
        Equal(string.Empty, DetentIndicatorText.Format(DetentIndicatorSnapshot.Hidden));
    }

    private static void IndicatorTextFormatsIdleHold()
    {
        var snapshot = new DetentIndicatorSnapshot(
            new DetentIndicatorLine(true, EndpointDetentState.Holding, 0.5),
            default);

        Equal("IDLE HOLD 50%", DetentIndicatorText.Format(snapshot));
    }

    private static void IndicatorTextFormatsAfterburnerLock()
    {
        var snapshot = new DetentIndicatorSnapshot(
            default,
            new DetentIndicatorLine(true, EndpointDetentState.Locked, 0));

        Equal("AB LOCK", DetentIndicatorText.Format(snapshot));
    }

    private static void IndicatorTextPrefersIdle()
    {
        var snapshot = new DetentIndicatorSnapshot(
            new DetentIndicatorLine(true, EndpointDetentState.Locked, 0),
            new DetentIndicatorLine(true, EndpointDetentState.Locked, 0));

        Equal("IDLE LOCK", DetentIndicatorText.Format(snapshot));
    }
    private static void ReadinessReportsDisabledMod()
    {
        var result = Readiness(masterEnabled: false);
        Equal(RuntimeReadinessState.Off, result.State);
        Equal("OFF - Mod disabled", result.DisplayText);
    }

    private static void ReadinessExcludesAbsoluteThrottle()
    {
        var result = Readiness(relativeThrottleMode: false);
        Equal(RuntimeReadinessState.NotApplicable, result.State);
        Equal("NOT APPLICABLE - Relative throttle only", result.DisplayText);
    }

    private static void ReadinessWaitsForPatchInstallation()
    {
        var result = Readiness(patchStatusKnown: false);
        Equal(RuntimeReadinessState.Waiting, result.State);
        Equal("WAITING - Mod is starting", result.DisplayText);
    }

    private static void ReadinessReportsObserverFailure()
    {
        var result = Readiness(throttleObserverActive: false);
        Equal(RuntimeReadinessState.No, result.State);
        Equal("NO - Throttle observer unavailable", result.DisplayText);
    }

    private static void ReadinessReportsOneFailedDetent()
    {
        var result = Readiness(idleGateActive: false);
        Equal(RuntimeReadinessState.Partial, result.State);
        Equal("PARTIAL - Idle detent unavailable", result.DisplayText);
    }

    private static void ReadinessWaitsForPlayerAircraft()
    {
        var result = Readiness(hasPlayerAircraft: false);
        Equal(RuntimeReadinessState.Waiting, result.State);
        Equal("WAITING - Start or resume a flight", result.DisplayText);
    }

    private static void ReadinessExcludesCollectiveAircraft()
    {
        var result = Readiness(isCollective: true);
        Equal(RuntimeReadinessState.NotApplicable, result.State);
        Equal("NOT APPLICABLE - Collective aircraft", result.DisplayText);
    }

    private static void ReadinessExcludesUnsupportedAircraft()
    {
        var result = Readiness(airframeSupported: false);
        Equal(RuntimeReadinessState.Unsupported, result.State);
        Equal("UNSUPPORTED - Not in preset", result.DisplayText);
    }

    private static void ReadinessNamesUnsupportedBeforeThrottleMode()
    {
        var result = Readiness(airframeSupported: false, relativeThrottleMode: false);
        Equal(RuntimeReadinessState.Unsupported, result.State);
        Equal("UNSUPPORTED - Not in preset", result.DisplayText);
    }

    private static void ReadinessNamesCollectiveBeforeThrottleMode()
    {
        var result = Readiness(isCollective: true, relativeThrottleMode: false);
        Equal(RuntimeReadinessState.NotApplicable, result.State);
        Equal("NOT APPLICABLE - Collective aircraft", result.DisplayText);
    }

    private static void ReadinessExcludesAircraftWithoutMatchingSystems()
    {
        var result = Readiness(hasAirbrake: false, hasAfterburner: false);
        Equal(RuntimeReadinessState.NotApplicable, result.State);
        Equal("NOT APPLICABLE - No matching capability", result.DisplayText);
    }

    private static void ReadinessIgnoresFailedNonApplicableGate()
    {
        var result = Readiness(
            idleGateActive: false,
            hasAirbrake: false,
            hasAfterburner: true);
        Equal(RuntimeReadinessState.Likely, result.State);
        Equal("LIKELY - Afterburner detent", result.DisplayText);
    }

    private static void ReadinessBecomesLikelyAfterInput()
    {
        var result = Readiness();
        Equal(RuntimeReadinessState.Likely, result.State);
        Equal("LIKELY - Airbrake and afterburner", result.DisplayText);
    }

    private static void ReadinessNamesOnlyEnabledDetent()
    {
        var result = Readiness(afterburnerEnabled: false);
        Equal(RuntimeReadinessState.Likely, result.State);
        Equal("LIKELY - Airbrake detent", result.DisplayText);
    }

    private static void AirframePresetAllowlistIsPinned()
    {
        var expected = new (string Id, string Name, bool Collective, AirbrakePath AirbrakePath, bool Afterburner, float? IdleBoundary, int? Nozzles, float? AbStart, float? AbEnd)[]
        {
            ("AttackHelo1", "SAH-46 Chicane", true, AirbrakePath.None, false, null, null, null, null),
            ("Aryx_CargoPlane1", "MC-260 Chimera", false, AirbrakePath.Split, false, 0f, null, null, null),
            ("Aryx_F16M_KingViper", "F-16M King Viper", false, AirbrakePath.Component, true, 0f, 1, 0.9f, 1f),
            ("Aryx_Interceptor1", "FS-41 Eclipse", false, AirbrakePath.Component, true, 0f, 2, 0.9f, 1f),
            ("Aryx_LightFighter1", "F-99 Shrike", false, AirbrakePath.Component, true, 0f, 2, 0.9f, 1f),
            ("Aryx_PropAttacker1", "OA-27 Cavalier", false, AirbrakePath.Split, false, 0f, null, null, null),
            ("CAS1", "A-19 Brawler", false, AirbrakePath.Split, false, 0f, null, null, null),
            ("COIN", "CI-22 Cricket", false, AirbrakePath.None, false, null, null, null, null),
            ("Darkreach", "SFB-81 Darkreach", false, AirbrakePath.Split, false, 0f, null, null, null),
            ("EW1", "EW-25 Medusa", false, AirbrakePath.None, false, null, null, null, null),
            ("FastBomber1", "Alkyon AB-4", false, AirbrakePath.Split, true, 0f, 4, 0.9f, 1f),
            ("Multirole1", "KR-67 Ifrit", false, AirbrakePath.Split, true, 0f, 2, 0.9f, 1f),
            ("P_Trisurface1", "FS-3 Ternion", false, AirbrakePath.Split, true, 0f, 2, 0.9f, 1f),
            ("QuadVTOL1", "VL-49 Tarantula", true, AirbrakePath.None, false, null, null, null, null),
            ("Fighter1", "FS-12 Revoker", false, AirbrakePath.Component, true, 0f, 1, 0.9f, 1f),
            ("SmallFighter1", "FS-20 Vortex", false, AirbrakePath.Component, true, 0f, 1, 0.9f, 1f),
            ("trainer", "T/A-30 Compass", false, AirbrakePath.Component, false, 0f, null, null, null),
            ("UtilityHelo1", "UH-90 Ibis", true, AirbrakePath.None, false, null, null, null, null),
            ("VTOLTrainer1", "VT-7 Vagrant", false, AirbrakePath.Component, false, 0f, null, null, null),
        };

        Equal(expected.Length, AirframePresetCatalog.All.Count);
        foreach (var row in expected)
        {
            True(AirframePresetCatalog.TryGet(row.Id, out var preset));
            Equal(row.Id, preset.Id);
            Equal(row.Name, preset.DisplayName);
            Equal(row.Collective, preset.Collective);
            Equal(row.AirbrakePath, preset.AirbrakePath);
            Equal(row.Afterburner, preset.HasAfterburner);
            Equal(row.IdleBoundary, preset.IdleAirbrakeBoundary);
            Equal(row.Nozzles, preset.AfterburnerNozzleCount);
            Equal(row.AbStart, preset.AfterburnerStart);
            Equal(row.AbEnd, preset.AfterburnerEnd);
        }

        True(AirframePresetCatalog.TryGet("Multirole1", out var ifrit));
        True(AirframePresetCatalog.AfterburnerRangeMatches(ifrit, 0.9f, 1f));
        False(AirframePresetCatalog.AfterburnerRangeMatches(ifrit, 0.85f, 1f));
        True(AirframePresetCatalog.TryGet("Fighter1", out var revoker));
        True(AirframePresetCatalog.AfterburnerRangeMatches(revoker, 0.9f, 1f));
    }

    private static void SensitivityScopeIsLimitedToDetentedAircraft()
    {
        var detentedIds = new[]
        {
            "CAS1", "Darkreach", "FastBomber1", "Multirole1",
            "Fighter1", "SmallFighter1", "trainer", "VTOLTrainer1",
            "Aryx_CargoPlane1", "Aryx_F16M_KingViper", "Aryx_Interceptor1",
            "Aryx_LightFighter1", "Aryx_PropAttacker1", "P_Trisurface1",
        };
        foreach (var id in detentedIds)
        {
            True(AirframePresetCatalog.TryGet(id, out var preset));
            True(AirframePresetCatalog.SupportsDetents(preset, runtimeCollective: false));
        }

        foreach (var id in new[] { "COIN", "EW1", "AttackHelo1", "UtilityHelo1", "QuadVTOL1" })
        {
            True(AirframePresetCatalog.TryGet(id, out var preset));
            False(AirframePresetCatalog.SupportsDetents(preset, runtimeCollective: false));
        }

        True(AirframePresetCatalog.TryGet("Multirole1", out var ifrit));
        False(AirframePresetCatalog.SupportsDetents(ifrit, runtimeCollective: true));
        False(AirframePresetCatalog.SupportsDetents(null, runtimeCollective: false));
    }

    private static void AllAfterburnerNozzlesMustMatch()
    {
        True(AirframePresetCatalog.TryGet("Multirole1", out var ifrit));
        var nozzles = new[]
        {
            NozzleWithRange(0.9f, 1f),
            NozzleWithRange(0.9f, 1f),
        };

        True(AfterburnerCompatibility.TryAggregatePinnedRanges(ifrit, nozzles, out var start, out var end));
        Near(0.9, start);
        Near(1, end);

        nozzles[1] = NozzleWithRange(0.85f, 1f);
        False(AfterburnerCompatibility.TryAggregatePinnedRanges(ifrit, nozzles, out _, out _));
    }

    private static void UnreadableAfterburnerNozzleRejectsConfirmation()
    {
        True(AirframePresetCatalog.TryGet("Multirole1", out var ifrit));
        var nozzles = new[]
        {
            NozzleWithRange(0.9f, 1f),
            new AfterburnerNozzleSample(
                capabilityReadable: true,
                hasAfterburner: true,
                rangesReadable: false,
                ranges: Array.Empty<AfterburnerRangeSample>()),
        };

        False(AfterburnerCompatibility.TryAggregatePinnedRanges(ifrit, nozzles, out _, out _));
    }

    private static void CustomPresetIsExactFailOpenFallback()
    {
        var custom = CustomAirframe(
            "Test_CustomJet",
            AirbrakePath.Component,
            hasAfterburner: true,
            nozzleCount: 2,
            afterburnerStart: 0.82f);

        True(AirframePresetCatalog.TryGet("Test_CustomJet", custom, out var resolved));
        Equal("Test_CustomJet", resolved.Id);
        Equal(AirbrakePath.Component, resolved.AirbrakePath);
        Equal(2, resolved.AfterburnerNozzleCount);
        Equal(0.82f, resolved.AfterburnerStart);
        False(AirframePresetCatalog.TryGet("Test_CustomJet_2", custom, out _));

        var collision = CustomAirframe(
            "MULTIROLE1",
            AirbrakePath.Component,
            hasAfterburner: true,
            nozzleCount: 1,
            afterburnerStart: 0.8f);
        True(AirframePresetCatalog.TryGet("Multirole1", collision, out var ifrit));
        Equal("KR-67 Ifrit", ifrit.DisplayName);
        Equal(AirbrakePath.Split, ifrit.AirbrakePath);
        Equal(2, ifrit.AfterburnerNozzleCount);

        var invalid = CustomAirframe(
            "BrokenJet",
            AirbrakePath.None,
            hasAfterburner: true,
            nozzleCount: 0,
            afterburnerStart: 1f);
        False(AirframePresetCatalog.TryGet("BrokenJet", invalid, out _));
    }

    private static void CustomDryDetentPercentagesAreValidated()
    {
        var custom = CustomAirframe(
            "Test_CustomJet",
            AirbrakePath.None,
            hasAfterburner: false,
            dryDetents: "67, 82.5,67");
        True(custom.TryGetDryDetentFractions(out var fractions));
        Equal(2, fractions.Length);
        Near(0.67, fractions[0]);
        Near(0.825, fractions[1]);

        False(CustomAirframe("Test", AirbrakePath.None, false, dryDetents: "67,nope")
            .TryGetDryDetentFractions(out _));
        False(CustomAirframe("Test", AirbrakePath.None, false, dryDetents: "0")
            .TryGetDryDetentFractions(out _));
        False(CustomAirframe("Test", AirbrakePath.None, false, dryDetents: "100")
            .TryGetDryDetentFractions(out _));
        False(CustomAirframe("Test", AirbrakePath.None, false, dryDetents: "10,20,30,40,50,60,70,80,90")
            .TryGetDryDetentFractions(out _));
    }

    private static void DetectedAircraftProfilesRoundTripIndependently()
    {
        var catalog = new DetectedAircraftCatalog(string.Empty);
        True(catalog.Register("aryx.f16m", "F-16M Viper"));
        True(catalog.Register("blueprinter.ternion", "FS-3 Ternion / Prototype"));

        var restored = new DetectedAircraftCatalog(catalog.Serialize());

        Equal(2, restored.All.Count);
        Equal("F-16M Viper", restored.DisplayNameFor("ARYX.F16M"));
        Equal("FS-3 Ternion / Prototype", restored.DisplayNameFor("blueprinter.ternion"));
    }

    private static void DetectedAircraftIdentityUpdatesWithoutDuplicates()
    {
        var catalog = new DetectedAircraftCatalog(string.Empty);
        True(catalog.Register("aryx.f99", "F-99"));
        True(catalog.Register("ARYX.F99", "F-99 Shrike"));
        False(catalog.Register("aryx.f99", "F-99 Shrike"));

        Equal(1, catalog.All.Count);
        Equal("F-99 Shrike", catalog.DisplayNameFor("aryx.f99"));
    }

    private static void MalformedDetectedAircraftRecordsAreIgnored()
    {
        var catalog = new DetectedAircraftCatalog("broken;%=bad;valid=Valid%20Aircraft;=missing-id");

        Equal(1, catalog.All.Count);
        True(catalog.Contains("valid"));
        Equal("Valid Aircraft", catalog.DisplayNameFor("valid"));
    }

    private static void InteriorDetentHoldsInBothDirections()
    {
        var upward = new InteriorDetentRuntime(new[] { 0.67 }, 0, 0.9, 200, 0.001, 0.02);
        upward.Update(InteriorInput(0, 0.59, ThrottleCommand.Neutral));
        var heldUp = upward.Update(InteriorInput(0.01, 0.61, ThrottleCommand.Increase));
        True(heldUp.IsHeld);
        Near(0.603 - ThrottleBoundaryHold.InwardOffset, heldUp.EffectiveThrottle);
        Near((heldUp.EffectiveThrottle * 2) - 1, heldUp.SimulatedThrottle);
        Near(67, heldUp.DryPercent);

        var downward = new InteriorDetentRuntime(new[] { 0.67 }, 0, 0.9, 200, 0.001, 0.02);
        downward.Update(InteriorInput(0, 0.62, ThrottleCommand.Neutral));
        var heldDown = downward.Update(InteriorInput(
            0.01,
            0.59,
            ThrottleCommand.Decrease,
            SimulatedThrottleRange.ZeroToOne));
        True(heldDown.IsHeld);
        Near(0.603 + ThrottleBoundaryHold.InwardOffset, heldDown.EffectiveThrottle);
        Near(heldDown.EffectiveThrottle, heldDown.SimulatedThrottle);
    }

    private static void InteriorDetentFollowsCockpitPercentageRange()
    {
        const double displayStart = 0.05;
        const double displayEnd = 0.95;
        var runtime = new InteriorDetentRuntime(
            new[] { 0.67 },
            displayStart,
            displayEnd,
            200,
            0.001,
            0.02);
        runtime.Update(InteriorInput(0, 0.64, ThrottleCommand.Neutral));

        var held = runtime.Update(InteriorInput(0.01, 0.67, ThrottleCommand.Increase));

        True(held.IsHeld);
        var boundary = displayStart + ((displayEnd - displayStart) * 0.67);
        Near(boundary - ThrottleBoundaryHold.InwardOffset, held.EffectiveThrottle);
        Equal(67, (int)Math.Round(
            ((held.EffectiveThrottle - displayStart) / (displayEnd - displayStart)) * 100));
        Near(67, held.DryPercent);
    }

    private static void InteriorDetentDoesNotSnapAndCatchesFirstCrossing()
    {
        var above = new InteriorDetentRuntime(new[] { 0.67 }, 0, 1, 200, 0.001, 0.02);
        above.Update(InteriorInput(0, 0.75, ThrottleCommand.Neutral));
        False(above.Update(InteriorInput(0.01, 0.76, ThrottleCommand.Increase)).IsHeld);

        var multiple = new InteriorDetentRuntime(new[] { 0.4, 0.67, 0.8 }, 0, 1, 200, 0.001, 0.02);
        multiple.Update(InteriorInput(0, 0.3, ThrottleCommand.Neutral));
        var first = multiple.Update(InteriorInput(0.01, 0.9, ThrottleCommand.Increase));
        True(first.IsHeld);
        Near(0.4 - ThrottleBoundaryHold.InwardOffset, first.EffectiveThrottle);
        Near(40, first.DryPercent);
    }

    private static void InteriorDetentRequiresContinuousHold()
    {
        var runtime = new InteriorDetentRuntime(new[] { 0.67 }, 0, 1, 200, 0.001, 0.02);
        runtime.Update(InteriorInput(0, 0.65, ThrottleCommand.Neutral));
        var first = runtime.Update(InteriorInput(0.01, 0.69, ThrottleCommand.Increase));
        True(first.IsHeld);
        var parked = first.EffectiveThrottle;

        False(runtime.Update(InteriorInput(0.11, parked, ThrottleCommand.Neutral)).IsHeld);
        var restarted = runtime.Update(InteriorInput(0.12, 0.69, ThrottleCommand.Increase));
        True(restarted.IsHeld);
        Near(0, restarted.ElapsedHoldSeconds);
        True(runtime.Update(InteriorInput(0.22, 0.69, ThrottleCommand.Increase)).IsHeld);
        True(runtime.Update(InteriorInput(0.319, 0.69, ThrottleCommand.Increase)).IsHeld);
        False(runtime.Update(InteriorInput(0.32, 0.69, ThrottleCommand.Increase)).IsHeld);

        var gap = new InteriorDetentRuntime(new[] { 0.67 }, 0, 1, 200, 0.001, 0.02);
        gap.Update(InteriorInput(0, 0.65, ThrottleCommand.Neutral));
        gap.Update(InteriorInput(0.01, 0.69, ThrottleCommand.Increase));
        var afterGap = gap.Update(InteriorInput(0.21, 0.69, ThrottleCommand.Increase));
        True(afterGap.IsHeld);
        Near(0, afterGap.ElapsedHoldSeconds);
    }

    private static void NearbyInteriorDetentsUnlockIndependently()
    {
        var runtime = new InteriorDetentRuntime(new[] { 0.67, 0.68 }, 0, 1, 10, 0.001, 0.02);
        runtime.Update(InteriorInput(0, 0.66, ThrottleCommand.Neutral));
        True(runtime.Update(InteriorInput(0.01, 0.675, ThrottleCommand.Increase)).IsHeld);
        False(runtime.Update(InteriorInput(0.02, 0.675, ThrottleCommand.Increase)).IsHeld);

        var second = runtime.Update(InteriorInput(0.03, 0.69, ThrottleCommand.Increase));
        True(second.IsHeld);
        Near(68, second.DryPercent);
    }

    private static void InteriorDetentInterruptionClearsCrossingHistory()
    {
        var runtime = new InteriorDetentRuntime(new[] { 0.67 }, 0, 1, 200, 0.001, 0.02);
        runtime.Update(InteriorInput(0, 0.65, ThrottleCommand.Neutral));
        True(runtime.Update(InteriorInput(0.01, 0.69, ThrottleCommand.Increase)).IsHeld);
        runtime.CancelPendingHold();
        False(runtime.Update(InteriorInput(0.02, 0.8, ThrottleCommand.Increase)).IsHeld);

        var bypassed = runtime.Update(new InteriorDetentInput(
            0.03,
            0.6,
            0.6,
            ThrottleCommand.Decrease,
            SimulatedThrottleRange.NegativeOneToOne,
            axisModifierHeld: true));
        False(bypassed.IsHeld);
        False(runtime.Update(InteriorInput(0.04, 0.59, ThrottleCommand.Decrease)).IsHeld);
    }

    private static void CustomDetentReadinessAndHudAreExplicit()
    {
        var runtime = new InteriorDetentSnapshot(
            isHeld: true,
            EndpointDetentState.Holding,
            elapsedHoldSeconds: 0.1,
            dryPercent: 67,
            effectiveThrottle: 0.67,
            simulatedThrottle: 0.34,
            shouldPinSimulatedThrottle: true);
        var indicator = DetentIndicatorPolicy.EvaluateInterior(runtime, 200, enabled: true);
        Equal("67% HOLD 50%", DetentIndicatorText.Format(indicator));

        var locked = new InteriorDetentSnapshot(
            isHeld: true,
            EndpointDetentState.Locked,
            elapsedHoldSeconds: 0,
            dryPercent: 82.5,
            effectiveThrottle: 0.825,
            simulatedThrottle: 0.65,
            shouldPinSimulatedThrottle: true);
        Equal("82.5% LOCK", DetentIndicatorText.Format(
            DetentIndicatorPolicy.EvaluateInterior(locked, 200, enabled: true)));

        var readiness = RuntimeReadinessPolicy.Evaluate(new RuntimeReadinessInput(
            masterEnabled: true,
            idleEnabled: false,
            afterburnerEnabled: false,
            patchStatusKnown: true,
            throttleObserverActive: true,
            idleGateActive: false,
            afterburnerGateActive: false,
            hasPlayerAircraft: true,
            airframeSupported: true,
            isCollective: false,
            relativeThrottleMode: true,
            aircraftCapabilitiesKnown: true,
            hasAirbrake: false,
            hasAfterburner: false,
            interiorDetentsEnabled: true));
        Equal(RuntimeReadinessState.Likely, readiness.State);
        Equal("LIKELY - Custom detents", readiness.DisplayText);
    }

    private static CustomAirframeConfig CustomAirframe(
        string id,
        AirbrakePath airbrakePath,
        bool hasAfterburner,
        int nozzleCount = 1,
        float afterburnerStart = 0.9f,
        string dryDetents = "") =>
        new(
            enabled: true,
            id,
            airbrakePath,
            hasAfterburner,
            nozzleCount,
            afterburnerStart,
            afterburnerEnd: 1f,
            dryDetents,
            dryDetentHoldMilliseconds: 200);

    private static InteriorDetentInput InteriorInput(
        double time,
        double throttle,
        ThrottleCommand command,
        SimulatedThrottleRange range = SimulatedThrottleRange.NegativeOneToOne) =>
        new(time, throttle, SimulatedThrottleMapping.ToSimulated(throttle, range), command, range);

    private static void ModdedAfterburnerNozzleCountsArePinned()
    {
        var expectedCounts = new (string Id, int Count)[]
        {
            ("Aryx_F16M_KingViper", 1),
            ("Aryx_Interceptor1", 2),
            ("Aryx_LightFighter1", 2),
            ("P_Trisurface1", 2),
        };

        foreach (var (id, count) in expectedCounts)
        {
            True(AirframePresetCatalog.TryGet(id, out var preset));
            var nozzles = Enumerable.Repeat(NozzleWithRange(0.9f, 1f), count).ToArray();
            True(AfterburnerCompatibility.TryAggregatePinnedRanges(preset, nozzles, out _, out _));
            False(AfterburnerCompatibility.TryAggregatePinnedRanges(
                preset,
                nozzles.Append(NozzleWithRange(0.9f, 1f)).ToArray(),
                out _,
                out _));

            nozzles[0] = NozzleWithRange(0.85f, 1f);
            False(AfterburnerCompatibility.TryAggregatePinnedRanges(preset, nozzles, out _, out _));
        }
    }

    private static void Ab4RequiresFourAfterburnerNozzles()
    {
        True(AirframePresetCatalog.TryGet("FastBomber1", out var ab4));
        var nozzles = new[]
        {
            NozzleWithRange(0.9f, 1f),
            NozzleWithRange(0.9f, 1f),
            NozzleWithRange(0.9f, 1f),
            NozzleWithRange(0.9f, 1f),
        };

        True(AfterburnerCompatibility.TryAggregatePinnedRanges(ab4, nozzles, out var start, out var end));
        Near(0.9, start);
        Near(1, end);

        False(AfterburnerCompatibility.TryAggregatePinnedRanges(ab4, nozzles[..3], out _, out _));
        nozzles[3] = NozzleWithRange(0.85f, 1f);
        False(AfterburnerCompatibility.TryAggregatePinnedRanges(ab4, nozzles, out _, out _));
    }

    private static void MixedAfterburnerCapabilityRejectsConfirmation()
    {
        True(AirframePresetCatalog.TryGet("Multirole1", out var ifrit));
        var nozzles = new[]
        {
            NozzleWithRange(0.9f, 1f),
            new AfterburnerNozzleSample(
                capabilityReadable: true,
                hasAfterburner: false,
                rangesReadable: true,
                ranges: Array.Empty<AfterburnerRangeSample>()),
        };

        False(AfterburnerCompatibility.TryAggregatePinnedRanges(ifrit, nozzles, out _, out _));
    }

    private static void LiveAfterburnerStartIsConservativeBoundary()
    {
        True(AirframePresetCatalog.TryGet("Multirole1", out var ifrit));
        Near(0.9, AfterburnerCompatibility.ResolveDetentBoundary(ifrit, liveRangeConfirmed: false, 0.8996f));
        Near(0.8996, AfterburnerCompatibility.ResolveDetentBoundary(ifrit, liveRangeConfirmed: true, 0.8996f));
        Near(0.9, AfterburnerCompatibility.ResolveDetentBoundary(ifrit, liveRangeConfirmed: true, 0.9004f));
    }

    private static AfterburnerNozzleSample NozzleWithRange(float start, float end) =>
        new(
            capabilityReadable: true,
            hasAfterburner: true,
            rangesReadable: true,
            ranges: new[] { new AfterburnerRangeSample(start, end) });

    private static void UnknownAndMissingAirframesBypass()
    {
        False(AirframePresetCatalog.TryGet("NewAircraft_0", out _));
        False(AirframePresetCatalog.TryGet(string.Empty, out _));
        False(AirframePresetCatalog.CanGate(null, AirframeFeature.Airbrake, runtimeCollective: false, liveFeatureConfirmed: true));
    }

    private static void AirframePresetLookupAcceptsRuntimeCasing()
    {
        True(AirframePresetCatalog.TryGet("trainer", out var trainer));
        Equal("trainer", trainer.Id);
        True(AirframePresetCatalog.TryGet("MULTIROLE1", out var multirole));
        Equal("Multirole1", multirole.Id);
        True(AirframePresetCatalog.TryGet("fighter1", out var fighter));
        Equal("Fighter1", fighter.Id);
    }

    private static void CollectiveAndRuntimeCollectiveBypass()
    {
        True(AirframePresetCatalog.TryGet("AttackHelo1", out var helo));
        False(AirframePresetCatalog.CanGate(helo, AirframeFeature.Airbrake, runtimeCollective: false, liveFeatureConfirmed: true));

        True(AirframePresetCatalog.TryGet("Multirole1", out var jet));
        False(AirframePresetCatalog.CanGate(jet, AirframeFeature.Airbrake, runtimeCollective: true, liveFeatureConfirmed: true));
    }

    private static void UnsupportedExpectedFeatureBypasses()
    {
        True(AirframePresetCatalog.TryGet("COIN", out var prop));
        False(AirframePresetCatalog.CanGate(prop, AirframeFeature.Airbrake, runtimeCollective: false, liveFeatureConfirmed: true));
        True(AirframePresetCatalog.TryGet("trainer", out var trainer));
        False(AirframePresetCatalog.CanGate(trainer, AirframeFeature.Afterburner, runtimeCollective: false, liveFeatureConfirmed: true));
    }

    private static void LiveConfirmationIsRequired()
    {
        True(AirframePresetCatalog.TryGet("Multirole1", out var jet));
        False(AirframePresetCatalog.CanGate(jet, AirframeFeature.Airbrake, runtimeCollective: false, liveFeatureConfirmed: false));
        True(AirframePresetCatalog.CanGate(jet, AirframeFeature.Airbrake, runtimeCollective: false, liveFeatureConfirmed: true));
        True(AirframePresetCatalog.CanGate(jet, AirframeFeature.Afterburner, runtimeCollective: false, liveFeatureConfirmed: true));
    }

    private static void RuntimeReconfigurePreservesLowerHoldingState()
    {
        var runtime = new DetentRuntime();
        runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        runtime.Update(new DetentRuntimeInput(0.1, 0, ThrottleCommand.Decrease));
        var elapsed = runtime.IdleDetent.ElapsedHoldSeconds;

        runtime.Reconfigure(500, 500, 0.002, 0.03);

        True(runtime.IdleDetent.IsHolding);
        Near(elapsed, runtime.IdleDetent.ElapsedHoldSeconds);
        Near(0.002, runtime.IdleDetent.EndpointEpsilon);
        Near(0.03, runtime.IdleDetent.ResetHysteresis);
    }

    private static void RuntimeReconfigurePreservesUpperUnlockedState()
    {
        var runtime = new DetentRuntime();
        UnlockRuntimeUpper(runtime);

        runtime.Reconfigure(500, 500, 0.002, 0.03);

        True(runtime.AfterburnerDetent.IsUnlocked);
        Near(0.002, runtime.AfterburnerDetent.EndpointEpsilon);
        Near(0.03, runtime.AfterburnerDetent.ResetHysteresis);
    }

    private static void AfterburnerRetargetPreservesIdleState()
    {
        var runtime = new DetentRuntime(afterburnerBoundary: 0.9);
        UnlockRuntimeLower(runtime);

        runtime.RetargetAfterburnerBoundary(0.8996);

        True(runtime.IdleDetent.IsUnlocked);
        Near(0.8996, runtime.AfterburnerDetent.Boundary);
    }

    private static void DisabledCapabilityDoesNotBlockReadiness()
    {
        var capabilitiesKnown = RuntimeReadinessPolicy.AreEnabledCapabilitiesKnown(
            hasPreset: true,
            idleEnabled: false,
            afterburnerEnabled: true,
            presetHasAirbrake: true,
            presetHasAfterburner: true,
            airbrakeConfirmed: false,
            afterburnerConfirmed: true);

        True(capabilitiesKnown);
        var result = Readiness(
            idleEnabled: false,
            afterburnerEnabled: true,
            aircraftCapabilitiesKnown: capabilitiesKnown,
            hasAirbrake: false,
            hasAfterburner: true);
        Equal(RuntimeReadinessState.Likely, result.State);
        Equal("LIKELY - Afterburner detent", result.DisplayText);
    }

    private static RuntimeReadinessResult Readiness(
        bool masterEnabled = true,
        bool idleEnabled = true,
        bool afterburnerEnabled = true,
        bool patchStatusKnown = true,
        bool throttleObserverActive = true,
        bool idleGateActive = true,
        bool afterburnerGateActive = true,
        bool hasPlayerAircraft = true,
        bool airframeSupported = true,
        bool isCollective = false,
        bool relativeThrottleMode = true,
        bool aircraftCapabilitiesKnown = true,
        bool hasAirbrake = true,
        bool hasAfterburner = true) =>
        RuntimeReadinessPolicy.Evaluate(new RuntimeReadinessInput(
            masterEnabled,
            idleEnabled,
            afterburnerEnabled,
            patchStatusKnown,
            throttleObserverActive,
            idleGateActive,
            afterburnerGateActive,
            hasPlayerAircraft,
            airframeSupported,
            isCollective,
            relativeThrottleMode,
            aircraftCapabilitiesKnown,
            hasAirbrake,
            hasAfterburner));

    private static void UnlockLower(EndpointDetent detent)
    {
        detent.Update(new EndpointDetentInput(0, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(0.1, 0, ThrottleCommand.Decrease));
        detent.Update(new EndpointDetentInput(0.2, 0, ThrottleCommand.Decrease));
    }

    private static void UnlockUpper(EndpointDetent detent)
    {
        detent.Update(new EndpointDetentInput(0, 1, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(0.1, 1, ThrottleCommand.Increase));
        detent.Update(new EndpointDetentInput(0.2, 1, ThrottleCommand.Increase));
    }

    private static void UnlockRuntimeLower(DetentRuntime runtime)
    {
        runtime.Update(new DetentRuntimeInput(0, 0, ThrottleCommand.Decrease));
        runtime.Update(new DetentRuntimeInput(0.1, 0, ThrottleCommand.Decrease));
        runtime.Update(new DetentRuntimeInput(0.2, 0, ThrottleCommand.Decrease));
    }

    private static void UnlockRuntimeUpper(DetentRuntime runtime)
    {
        runtime.Update(new DetentRuntimeInput(0, 1, ThrottleCommand.Increase));
        runtime.Update(new DetentRuntimeInput(0.1, 1, ThrottleCommand.Increase));
        runtime.Update(new DetentRuntimeInput(0.2, 1, ThrottleCommand.Increase));
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"expected {expected}, got {actual}");
        }
    }

    private static void True(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException("expected true");
        }
    }

    private static void False(bool value)
    {
        if (value)
        {
            throw new InvalidOperationException("expected false");
        }
    }

    private static void Near(double expected, double actual)
    {
        if (Math.Abs(expected - actual) > 0.000001)
        {
            throw new InvalidOperationException($"expected {expected}, got {actual}");
        }
    }
}
