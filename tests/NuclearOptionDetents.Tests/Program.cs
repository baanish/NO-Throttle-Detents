using NuclearOptionDetents.Core;

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
            ("lifecycle reset clears both", LifecycleResetClearsBoth),
            ("cadence does not change result", CadenceIsIndependent),
            ("pause cancels dwell and passes gates", PausedTimeDoesNotAdvance),
            ("Axis Modifier cancels dwell and passes gates", AxisModifierCancelsHold),
            ("Axis Modifier preserves unlocked latch", AxisModifierPreservesUnlockedLatch),
            ("observer age policy rejects stale input", ObserverAgePolicyRejectsStaleInput),
            ("disabled controls lock dwell and pass gates", DisabledControlsPreserveLockedGates),
            ("opposite lower command locks dwell and passes gate", OppositeLowerCommandPassesGate),
            ("opposite upper command locks dwell and passes gate", OppositeUpperCommandPassesGate),
            ("locked idle publishes inward throttle", LockedIdlePublishesInwardThrottle),
            ("locked afterburner publishes inward throttle", LockedAfterburnerPublishesInwardThrottle),
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
            ("all afterburner nozzles must match", AllAfterburnerNozzlesMustMatch),
            ("AB-4 requires all four afterburner nozzles", Ab4RequiresFourAfterburnerNozzles),
            ("live afterburner start is the conservative boundary", LiveAfterburnerStartIsConservativeBoundary),
            ("unreadable afterburner nozzle rejects confirmation", UnreadableAfterburnerNozzleRejectsConfirmation),
            ("mixed afterburner capability rejects confirmation", MixedAfterburnerCapabilityRejectsConfirmation),
            ("airframe preset lookup accepts runtime casing", AirframePresetLookupAcceptsRuntimeCasing),
            ("unknown and missing airframes bypass", UnknownAndMissingAirframesBypass),
            ("collective and runtime collective bypass", CollectiveAndRuntimeCollectiveBypass),
            ("unsupported expected feature bypasses", UnsupportedExpectedFeatureBypasses),
            ("live confirmation is required", LiveConfirmationIsRequired),
            ("airbrake ownership accepts exact aircraft", AirbrakeOwnershipAcceptsExactAircraft),
            ("airbrake ownership rejects another aircraft", AirbrakeOwnershipRejectsAnotherAircraft),
            ("aircraft ownership requires exact identity", AircraftOwnershipRequiresExactIdentity),
            ("pinned airbrake path confirms capability", PinnedAirbrakePathConfirmsCapability),
            ("pinned airbrake path requires its active gate", PinnedAirbrakePathRequiresActiveGate),
            ("runtime reconfigure preserves lower holding state", RuntimeReconfigurePreservesLowerHoldingState),
            ("runtime reconfigure preserves upper unlocked state", RuntimeReconfigurePreservesUpperUnlockedState),
            ("afterburner retarget preserves idle state", AfterburnerRetargetPreservesIdleState),
            ("disabled capability does not block readiness", DisabledCapabilityDoesNotBlockReadiness),
            ("low-frequency observer gap counts elapsed dwell", LowFrequencyObserverGapCountsElapsedDwell),
            ("explicit cancellation resets pending holds", ExplicitCancellationResetsPendingHolds),
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

    private static void ObserverAgePolicyRejectsStaleInput()
    {
        True(ComponentGatePolicy.AllowsBlock(
            controlsEnabled: true,
            paused: false,
            axisModifierHeld: false,
            command: ThrottleCommand.Decrease,
            direction: DetentDirection.Lower,
            observerAgeSeconds: ComponentGatePolicy.MaximumObserverAgeSeconds));

        False(ComponentGatePolicy.AllowsBlock(
            controlsEnabled: true,
            paused: false,
            axisModifierHeld: false,
            command: ThrottleCommand.Decrease,
            direction: DetentDirection.Lower,
            observerAgeSeconds: ComponentGatePolicy.MaximumObserverAgeSeconds + 0.001));

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
            0, -1, ThrottleCommand.Neutral,
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
            throttleUsesNegativeRange: true,
            idleApplies: false));
        Near(0.799998, result.SimulatedThrottle);
    }

    private static void NonnegativeAccumulatorFollowsBoundary()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.91, 0.91, ThrottleCommand.Increase,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.001,
            throttleUsesNegativeRange: false,
            idleApplies: false));
        Near(0.899999, result.SimulatedThrottle);
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
        True(result.IdleHeld);
    }

    private static void LargeEpsilonPreservesSafeDryThrustRequest()
    {
        var result = ThrottleBoundaryHold.Apply(new ThrottleBoundaryHoldInput(
            0.85, 0.7, ThrottleCommand.Neutral,
            EndpointDetentState.Locked, EndpointDetentState.Locked,
            0, 0.9, 0.05,
            idleApplies: false));
        Near(0.85, result.EffectiveThrottle);
        True(result.AfterburnerHeld);
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
            ("CAS1", "A-19 Brawler", false, AirbrakePath.Split, false, 0f, null, null, null),
            ("COIN", "CI-22 Cricket", false, AirbrakePath.None, false, null, null, null, null),
            ("Darkreach", "SFB-81 Darkreach", false, AirbrakePath.Split, false, 0f, null, null, null),
            ("EW1", "EW-25 Medusa", false, AirbrakePath.None, false, null, null, null, null),
            ("FastBomber1", "Alkyon AB-4", false, AirbrakePath.Split, true, 0f, 4, 0.9f, 1f),
            ("Multirole1", "KR-67 Ifrit", false, AirbrakePath.Split, true, 0f, 2, 0.9f, 1f),
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

    private static void AirbrakeOwnershipAcceptsExactAircraft()
    {
        var localAircraft = new object();

        True(ReferenceOwnership.AirbrakeMatches(
            localAircraft,
            serializedAircraft: localAircraft,
            attachedAircraft: null));
        True(ReferenceOwnership.AirbrakeMatches(
            localAircraft,
            serializedAircraft: null,
            attachedAircraft: localAircraft));
    }

    private static void AirbrakeOwnershipRejectsAnotherAircraft()
    {
        var localAircraft = new object();
        var otherAircraft = new object();

        False(ReferenceOwnership.AirbrakeMatches(
            localAircraft,
            serializedAircraft: otherAircraft,
            attachedAircraft: otherAircraft));
    }

    private static void AircraftOwnershipRequiresExactIdentity()
    {
        var localAircraft = new object();

        True(ReferenceOwnership.AircraftMatches(
            localAircraft,
            candidateAircraft: localAircraft));
        False(ReferenceOwnership.AircraftMatches(
            localAircraft,
            candidateAircraft: new object()));
        False(ReferenceOwnership.AircraftMatches(
            localAircraft,
            candidateAircraft: null));
    }

    private static void PinnedAirbrakePathConfirmsCapability()
    {
        True(AirbrakeCapabilityPaths.IsConfirmed(
            AirbrakePath.Component, componentConfirmed: true, splitSurfaceConfirmed: false));
        False(AirbrakeCapabilityPaths.IsConfirmed(
            AirbrakePath.Component, componentConfirmed: false, splitSurfaceConfirmed: true));
        True(AirbrakeCapabilityPaths.IsConfirmed(
            AirbrakePath.Split, componentConfirmed: false, splitSurfaceConfirmed: true));
        False(AirbrakeCapabilityPaths.IsConfirmed(
            AirbrakePath.None, componentConfirmed: true, splitSurfaceConfirmed: true));
    }

    private static void PinnedAirbrakePathRequiresActiveGate()
    {
        True(AirbrakeCapabilityPaths.HasActiveGate(
            AirbrakePath.Component,
            componentConfirmed: true,
            componentGateActive: true,
            splitSurfaceConfirmed: false,
            splitSurfaceGateActive: false));
        True(AirbrakeCapabilityPaths.HasActiveGate(
            AirbrakePath.Split,
            componentConfirmed: false,
            componentGateActive: false,
            splitSurfaceConfirmed: true,
            splitSurfaceGateActive: true));
        False(AirbrakeCapabilityPaths.HasActiveGate(
            AirbrakePath.Component,
            componentConfirmed: true,
            componentGateActive: false,
            splitSurfaceConfirmed: false,
            splitSurfaceGateActive: true));
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
