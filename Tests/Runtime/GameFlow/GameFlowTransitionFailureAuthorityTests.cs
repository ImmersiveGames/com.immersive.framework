using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Gate;
using Immersive.Framework.Identity;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.GameFlow.Tests
{
    public sealed class GameFlowTransitionFailureAuthorityTests
    {
        [Test]
        public void Succeeded_IsAcceptedTransitionPhase()
        {
            TransitionResult result = TransitionResult.SucceededResult(
                TransitionOperationId.From("if-txn-01.succeeded"),
                TransitionKind.RouteSwitch,
                "test",
                "before",
                "ok",
                new[]
                {
                    TransitionStep.Succeeded(
                        0,
                        TransitionPhase.OperationOpened,
                        "before",
                        "ok")
                });

            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(result), Is.True);
            Assert.That(
                GameFlowRuntime.TryAcceptTransitionPhase(result, "Before", out string issue),
                Is.True);
            Assert.That(issue, Is.Empty);
        }

        [Test]
        public void CompletedWithWarnings_IsAcceptedAsCompleted()
        {
            TransitionResult result = TransitionResult.CompletedWithWarningsResult(
                TransitionOperationId.From("if-txn-01.warnings"),
                TransitionKind.RouteSwitch,
                "test",
                "after",
                "warnings",
                new[]
                {
                    TransitionStep.Succeeded(
                        0,
                        TransitionPhase.OperationClosed,
                        "after",
                        "ok")
                },
                new[] { "non-blocking-warning" });

            Assert.That(result.Completed, Is.True);
            Assert.That(result.CompletedWithWarnings, Is.True);
            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(result), Is.True);
            Assert.That(
                GameFlowRuntime.TryAcceptTransitionPhase(result, "After", out _),
                Is.True);
        }

        [Test]
        public void Skipped_IsAcceptedAsPolicyPhaseCompletion()
        {
            TransitionResult result = TransitionResult.SkippedResult(
                TransitionOperationId.From("if-txn-01.skipped"),
                TransitionKind.ActivitySwitch,
                "test",
                "before",
                "SkippedByActivityPolicy",
                new[]
                {
                    TransitionStep.Skipped(
                        0,
                        TransitionPhase.OperationOpened,
                        "activity-before-policy-skip",
                        "skipped")
                },
                TransitionEffectKind.Unknown,
                TransitionEffectStatus.Skipped,
                0,
                "None",
                0);

            Assert.That(result.Completed, Is.False);
            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(result), Is.True);
        }

        [Test]
        public void Failed_IsNotAcceptedTransitionPhase()
        {
            TransitionResult result = TransitionResult.FailedResult(
                TransitionOperationId.From("if-txn-01.failed"),
                TransitionKind.RouteSwitch,
                "test",
                "before",
                "required surface missing",
                new[]
                {
                    TransitionStep.Failed(
                        0,
                        TransitionPhase.OperationOpened,
                        "before",
                        "required surface missing")
                },
                new[] { "required surface missing" });

            Assert.That(result.Completed, Is.False);
            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(result), Is.False);
            Assert.That(
                GameFlowRuntime.TryAcceptTransitionPhase(result, "Before", out string issue),
                Is.False);
            Assert.That(issue, Does.Contain("Before"));
            Assert.That(issue, Does.Contain("Failed"));
        }

        [Test]
        public void RejectedAndCancelled_AreNotAccepted()
        {
            TransitionResult rejected = TransitionResult.RejectedResult(
                TransitionOperationId.From("if-txn-01.rejected"),
                TransitionKind.RouteSwitch,
                "test",
                "before",
                "rejected",
                new[] { "rejected" });

            TransitionResult cancelled = new TransitionResult(
                TransitionOperationId.From("if-txn-01.cancelled"),
                TransitionKind.RouteSwitch,
                TransitionStatus.Cancelled,
                "test",
                "after",
                "cancelled",
                new[]
                {
                    TransitionStep.Observed(
                        0,
                        TransitionPhase.OperationClosed,
                        "after",
                        "cancelled")
                },
                new[] { "cancelled" });

            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(rejected), Is.False);
            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(cancelled), Is.False);
        }

        [Test]
        public void InvalidDefault_IsNotAccepted()
        {
            Assert.That(GameFlowRuntime.IsAcceptedTransitionPhase(default), Is.False);
            Assert.That(
                GameFlowRuntime.TryAcceptTransitionPhase(default, "After", out string issue),
                Is.False);
            Assert.That(issue, Does.Contain("invalid"));
        }

        [Test]
        public void PreCommitRouteFailure_IsNotSucceededOrAuthoritative()
        {
            var result = FrameworkRouteRequestResult.FailedPreCommitTransition(
                "before failed",
                null,
                "test",
                "pre-commit");

            Assert.That(result.Kind, Is.EqualTo(FrameworkRouteRequestKind.FailedPreCommitTransition));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.False);
            Assert.That(result.Superseded, Is.False);
        }

        [Test]
        public void CommittedTargetRevealRouteFailure_PreservesDestinationAuthority()
        {
            var result = FrameworkRouteRequestResult.FailedCommittedTargetReveal(
                "after failed",
                null,
                "test",
                "reveal",
                default);

            Assert.That(result.Kind, Is.EqualTo(FrameworkRouteRequestKind.FailedCommittedTargetReveal));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.True);
        }

        [Test]
        public void PreCommitActivityFailure_IsNotSucceededOrAuthoritative()
        {
            var result = FrameworkActivityRequestResult.FailedPreCommitTransition(
                "before failed",
                null,
                "test",
                "pre-commit");

            Assert.That(result.Kind, Is.EqualTo(FrameworkActivityRequestKind.FailedPreCommitTransition));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.False);
            Assert.That(result.CommitBoundaryReached, Is.False);
        }

        [Test]
        public void CommittedTargetRevealActivityFailure_PreservesCommitBoundary()
        {
            var result = FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                "after failed",
                null,
                "test",
                "reveal",
                default);

            Assert.That(result.Kind, Is.EqualTo(FrameworkActivityRequestKind.FailedCommittedTargetReveal));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.CommitBoundaryReached, Is.True);
            Assert.That(result.DestinationAuthoritative, Is.True);
        }

        [Test]
        public void TransitionRevealKind_IsDistinctFromReadinessFailureKinds()
        {
            Assert.That(
                FrameworkRouteRequestKind.FailedCommittedTargetReveal,
                Is.Not.EqualTo(FrameworkRouteRequestKind.FailedCommittedTargetNotReady));
            Assert.That(
                FrameworkActivityRequestKind.FailedCommittedTargetReveal,
                Is.Not.EqualTo(FrameworkActivityRequestKind.FailedCommittedTargetNotReady));
            Assert.That(
                FrameworkRouteRequestKind.FailedPreCommitTransition,
                Is.Not.EqualTo(FrameworkRouteRequestKind.FailedCommittedTargetReveal));
        }

        [Test]
        public void ExistingReadinessFailureKinds_RemainAuthoritativeNonSuccess()
        {
            var notReady = FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                FrameworkRouteRequestKind.FailedCommittedTargetNotReady,
                "not ready",
                null,
                "test",
                "readiness",
                default);
            var cancelled = FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                FrameworkRouteRequestKind.FailedCommittedTargetReadinessCancelled,
                "cancelled",
                null,
                "test",
                "readiness",
                default);
            var invalidated = FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                FrameworkRouteRequestKind.FailedCommittedTargetReadinessInvalidated,
                "invalidated",
                null,
                "test",
                "readiness",
                default);

            Assert.That(notReady.Succeeded, Is.False);
            Assert.That(notReady.DestinationAuthoritative, Is.True);
            Assert.That(cancelled.DestinationAuthoritative, Is.True);
            Assert.That(invalidated.DestinationAuthoritative, Is.True);
        }

        [Test]
        public void Supersession_RemainsNonAuthoritative()
        {
            var result = new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.SupersededCommittedTargetByRouteReplacement,
                "superseded",
                null,
                "test",
                "RouteAuthorityReplaced",
                default);

            Assert.That(result.Superseded, Is.True);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.False);
        }

        [Test]
        public void StartupPreCommitAndRevealFlags_AreDistinct()
        {
            var preCommit = FrameworkGameFlowStartResult.FailedPreCommitTransition(
                "before failed",
                null);
            var reveal = FrameworkGameFlowStartResult.FailedCommittedTargetReveal(
                "after failed",
                null,
                default,
                ActivityEntryReadinessExecutionStatus.Ready);
            var readiness = FrameworkGameFlowStartResult.FailedCommittedDestination(
                "not ready",
                null,
                default,
                ActivityEntryReadinessExecutionStatus.Failed);

            Assert.That(preCommit.Started, Is.False);
            Assert.That(preCommit.PreCommitTransitionFailed, Is.True);
            Assert.That(preCommit.CommittedTargetRevealFailed, Is.False);
            Assert.That(preCommit.DestinationAuthoritative, Is.False);

            Assert.That(reveal.Started, Is.False);
            Assert.That(reveal.PreCommitTransitionFailed, Is.False);
            Assert.That(reveal.CommittedTargetRevealFailed, Is.True);
            Assert.That(reveal.DestinationAuthoritative, Is.True);

            Assert.That(readiness.Started, Is.False);
            Assert.That(readiness.PreCommitTransitionFailed, Is.False);
            Assert.That(readiness.CommittedTargetRevealFailed, Is.False);
            Assert.That(readiness.DestinationAuthoritative, Is.True);
        }

        [Test]
        public void RevealRecoveryGatePolicy_CreatesCapabilityBlockers()
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            try
            {
                // ActivityAsset needs a valid ActivityId for owner identity.
                typeof(ActivityAsset)
                    .GetField(
                        "activityId",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(activity, "activity.if-txn-01.reveal-recovery");

                if (!activity.HasValidActivityId)
                {
                    Assert.Inconclusive("Unable to assign ActivityId via reflection for recovery gate proof.");
                }

                var occurrence = new ActivityReadinessOccurrence(activity, 1);
                FrameworkIdentityKey owner = FrameworkIdentityKey.From(activity.ActivityId);
                GateSnapshot snapshot = CommittedTargetRevealRecoveryGatePolicy.Create(
                    occurrence,
                    owner,
                    "test",
                    "Transition After failed");

                Assert.That(snapshot.HasBlockers, Is.True);
                Assert.That(snapshot.BlockerCount, Is.EqualTo(3));
                Assert.That(
                    CommittedTargetRevealRecoveryGatePolicy.PolicySource,
                    Does.Contain("IF-TXN-01"));
                Assert.That(
                    CommittedTargetRevealRecoveryGatePolicy.PolicySource,
                    Does.Not.Contain("Readiness"));
            }
            finally
            {
                Object.DestroyImmediate(activity);
            }
        }

        [Test]
        public void ReadinessRecoveryPolicySource_RemainsDistinctFromReveal()
        {
            Assert.That(
                ActivityEntryReadinessRecoveryGatePolicy.PolicySource,
                Is.Not.EqualTo(CommittedTargetRevealRecoveryGatePolicy.PolicySource));
        }

        [Test]
        public void WaitStatusMapping_NonRegression()
        {
            Assert.That(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Ready),
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Ready));
            Assert.That(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Failed),
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Failed));
            Assert.That(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Invalidated),
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Invalidated));
            Assert.That(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Cancelled),
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Cancelled));
            Assert.That(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Superseded),
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Superseded));
        }
    }
}
