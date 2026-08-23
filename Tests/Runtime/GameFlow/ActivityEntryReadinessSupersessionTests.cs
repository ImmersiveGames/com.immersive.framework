using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
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

        [Test]
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

        [Test]
        public void SourcesConfiguredBeforeGameFlow_ApplyBothFacetsOnce()
        {
            var bindings = new ActivityParticipantSourceBindings();
            var playerSource = new TestPlayerSource();
            var applications = new List<string>();

            bindings.SetSources(playerSource, playerSource);
            bindings.ApplyTo(
                source =>
                {
                    applications.Add("Content");
                    Assert.That(source, Is.SameAs(playerSource));
                },
                source =>
                {
                    applications.Add("Readiness");
                    Assert.That(source, Is.SameAs(playerSource));
                });

            Assert.That(applications, Is.EqualTo(new[] { "Content", "Readiness" }));
        }

        [Test]
        public void ContentReplacement_PreservesCanonicalReadiness()
        {
            var bindings = new ActivityParticipantSourceBindings();
            var playerSource = new TestPlayerSource();
            var sceneLocalContent = new TestContentSource();

            bindings.SetSources(playerSource, playerSource);
            bindings.SetContentSource(sceneLocalContent);

            Assert.That(bindings.ContentSource, Is.SameAs(sceneLocalContent));
            Assert.That(bindings.ReadinessSource, Is.SameAs(playerSource));
        }

        [Test]
        public void ClearingSources_RemovesBothFacets()
        {
            var bindings = new ActivityParticipantSourceBindings();
            var playerSource = new TestPlayerSource();
            int applyCount = 0;

            bindings.SetSources(playerSource, playerSource);
            bindings.SetSources(null, null);
            bindings.ApplyTo(
                source =>
                {
                    applyCount++;
                    Assert.That(source, Is.Null);
                },
                source =>
                {
                    applyCount++;
                    Assert.That(source, Is.Null);
                });

            Assert.That(applyCount, Is.EqualTo(2));
        }

        [Test]
        public void SourcesConfiguredAfterGameFlow_CanBeReappliedWithoutDuplication()
        {
            var bindings = new ActivityParticipantSourceBindings();
            var playerSource = new TestPlayerSource();
            int contentApplications = 0;
            int readinessApplications = 0;

            bindings.ApplyTo(
                _ => contentApplications++,
                _ => readinessApplications++);
            bindings.SetSources(playerSource, playerSource);
            bindings.ApplyTo(
                source =>
                {
                    contentApplications++;
                    Assert.That(source, Is.SameAs(playerSource));
                },
                source =>
                {
                    readinessApplications++;
                    Assert.That(source, Is.SameAs(playerSource));
                });

            Assert.That(contentApplications, Is.EqualTo(2));
            Assert.That(readinessApplications, Is.EqualTo(2));
        }

        private class TestContentSource : IActivityContentExecutionParticipantSource
        {
            public ActivityContentExecutionParticipantSourceResult
                ResolveActivityContentExecutionParticipants(
                    ActivityContentExecutionParticipantSourceRequest request)
            {
                return null;
            }
        }

        private sealed class TestPlayerSource :
            TestContentSource,
            IActivityReadinessParticipantSource
        {
            public IReadOnlyList<ActivityReadinessParticipant>
                ResolveActivityReadinessParticipants(ActivityAsset activity)
            {
                return Array.Empty<ActivityReadinessParticipant>();
            }
        }
    }
}
