using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Transition;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.GameFlow.Tests
{
    public sealed class ActivityEntryReadinessSupersessionTests
    {
        [Test]
        public void SupersededWait_MapsToSupersededExecution()
        {
            ActivityEntryReadinessExecutionStatus status =
                GameFlowRuntime.MapWaitStatus(
                    ActivityEntryReadinessWaitStatus.Superseded);

            Assert.That(
                status,
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Superseded));
        }

        public void GenericCancellation_RemainsCancelled()
        {
            ActivityEntryReadinessExecutionStatus status =
                GameFlowRuntime.MapWaitStatus(
                    ActivityEntryReadinessWaitStatus.Cancelled);

            Assert.That(
                status,
                Is.EqualTo(ActivityEntryReadinessExecutionStatus.Cancelled));
        }

        [TestCase(ActivityEntryReadinessWaitStatus.Ready,
            ActivityEntryReadinessExecutionStatus.Ready)]
        [TestCase(ActivityEntryReadinessWaitStatus.Failed,
            ActivityEntryReadinessExecutionStatus.Failed)]
        [TestCase(ActivityEntryReadinessWaitStatus.Invalidated,
            ActivityEntryReadinessExecutionStatus.Invalidated)]
        public void TerminalReadinessOutcomes_PreserveTheirExistingClassification(
            ActivityEntryReadinessWaitStatus waitStatus,
            ActivityEntryReadinessExecutionStatus expectedStatus)
        {
            ActivityEntryReadinessExecutionStatus status =
                GameFlowRuntime.MapWaitStatus(
                    waitStatus);

            Assert.That(status, Is.EqualTo(expectedStatus));
        }

        [Test]
        public void SupersededRouteResult_IsNotSuccessfulOrAuthoritative()
        {
            var result = new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind
                    .SupersededCommittedTargetByRouteReplacement,
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
        public void RouteReplacementInterruption_IsTypedAndIdempotent()
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            try
            {
                var scope = new ActivityEntryReadinessWaitScope(
                    TransitionOperationId.From("test.route-replacement"),
                    new ActivityReadinessOccurrence(activity, 1));

                scope.Cancel(
                    ActivityEntryReadinessInterruptionReason.RouteAuthorityReplaced,
                    "Menu");
                scope.Cancel(
                    ActivityEntryReadinessInterruptionReason.RuntimeDisposed);

                Assert.That(scope.Token.IsCancellationRequested, Is.True);
                Assert.That(
                    scope.InterruptionReason,
                    Is.EqualTo(
                        ActivityEntryReadinessInterruptionReason
                            .RouteAuthorityReplaced));
                Assert.That(
                    scope.CancellationDiagnostic,
                    Is.EqualTo("RouteAuthorityReplaced replacementRoute='Menu'"));

                scope.Dispose();
                scope.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(activity);
            }
        }
    }
}
