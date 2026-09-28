using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraPresentationRouteSelectionContinuityTests
    {
        [Test]
        public void EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence()
        {
            using var fixture = new Fixture();

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    fixture.RouteA,
                    fixture.RouteAContext,
                    nameof(EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence),
                    "route-a-enter",
                    out string routeAIssue),
                Is.True,
                routeAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.RouteA,
                    out string routeACommitIssue),
                Is.True,
                routeACommitIssue);

            CameraPresentationMaterializationHandle before =
                fixture.GetSelectedHandle();
            RuntimeContentIdentity identity = before.RuntimeContentIdentity;
            CameraRequestId requestId = before.PresentationRuntime.RequestId;

            Assert.That(before.ScopeContext.Scope, Is.EqualTo(RuntimeContentScope.Session));
            Assert.That(before.ScopeContext.Owner, Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(before.PresentationRuntime.LifecycleScope, Is.EqualTo(RuntimeContentScope.Session));
            Assert.That(before.PresentationRuntime.IsRequestPublished, Is.True);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            CameraRequest request = fixture.Output.Context.Winner;
            Assert.That(fixture.Output.Context.Winner.RequestId, Is.EqualTo(requestId));
            Assert.That(
                request.Owner.Kind,
                Is.EqualTo(CameraRequestOwnerKind.Session));
            Assert.That(
                request.Lifetime.Kind,
                Is.EqualTo(CameraRequestLifetimeKind.Session));
            AssertEquivalentRequest(
                request,
                fixture.Output.Context.Winner);

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    fixture.RouteB,
                    fixture.RouteBContext,
                    nameof(EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence),
                    "route-b-enter",
                    out string routeBIssue),
                Is.True,
                routeBIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.RouteB,
                    out string routeBCommitIssue),
                Is.True,
                routeBCommitIssue);
            Assert.That(
                fixture.Lifecycle.TryExitRoute(
                    fixture.RouteA,
                    nameof(EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence),
                    "route-a-exit",
                    out string routeAExitIssue),
                Is.True,
                routeAExitIssue);

            RuntimeRootRegistryOperationResult removed =
                fixture.RuntimeContent.RemoveScopeRoot(
                    fixture.RouteAContext.Owner,
                    nameof(EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence),
                    "route-a-scope-remove");
            Assert.That(removed.Status, Is.EqualTo(RuntimeRootRegistryOperationStatus.RootRemoved));

            CameraPresentationMaterializationHandle after =
                fixture.GetSelectedHandle();
            Assert.That(after, Is.SameAs(before));
            Assert.That(after.RuntimeContentIdentity, Is.EqualTo(identity));
            Assert.That(after.PresentationRuntime.RequestId, Is.EqualTo(requestId));
            Assert.That(after.ScopeContext.Owner, Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(after.ScopeContext.Owner, Is.Not.EqualTo(fixture.RouteAContext.Owner));
            Assert.That(after.ScopeContext.Owner, Is.Not.EqualTo(fixture.RouteBContext.Owner));
            Assert.That(after.IsReleased, Is.False);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(fixture.Output.Context.Winner.RequestId, Is.EqualTo(requestId));
            AssertEquivalentRequest(
                request,
                fixture.Output.Context.Winner);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    identity,
                    out RuntimeContentHandle registered),
                Is.True);
            Assert.That(registered, Is.SameAs(after.RuntimeContentHandle));

            RuntimeContentId contentId =
                CameraPresentationMaterializationRuntime.CreateContentId(
                    fixture.PresentationA);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.RouteBContext,
                    fixture.RouteBContext.CreateIdentity(contentId),
                    out _),
                Is.False,
                "The incoming Route must not receive or own the persistent occurrence.");
            Assert.That(
                fixture.RuntimeContent.TryCreateScopeContext(
                    fixture.RouteAContext.Owner,
                    nameof(EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence),
                    "route-a-dead-scope-check",
                    out _),
                Is.False,
                "The outgoing Route scope must be fully removable while the selection remains live.");
        }

        [Test]
        public void DifferentSelectionReplacesCurrentOnlyAfterIncomingBecomesWinner()
        {
            using var fixture = new Fixture();
            const string test =
                nameof(DifferentSelectionReplacesCurrentOnlyAfterIncomingBecomesWinner);

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    fixture.RouteA,
                    fixture.RouteAContext,
                    test,
                    "route-a-enter",
                    out string routeAIssue),
                Is.True,
                routeAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.RouteA,
                    out string routeACommitIssue),
                Is.True,
                routeACommitIssue);

            CameraPresentationMaterializationHandle before =
                fixture.GetSelectedHandle();
            RuntimeContentIdentity identity = before.RuntimeContentIdentity;
            CameraRequestId requestId = before.PresentationRuntime.RequestId;
            CameraRequest request = fixture.Output.Context.Winner;
            RouteAsset routeC = fixture.CreateRoute(
                "route-c",
                fixture.CreatePresentation("Presentation C"));
            RuntimeScopeContext routeCContext =
                fixture.CreateRouteContext(routeC, "route-c");

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    routeC,
                    routeCContext,
                    test,
                    "route-c-enter",
                    out string routeCIssue),
                Is.True,
                routeCIssue);

            CameraPresentationMaterializationHandle incoming =
                fixture.GetOnlyPendingHandle();
            Assert.That(fixture.GetSelectedHandle(), Is.SameAs(before));
            Assert.That(before.IsReleased, Is.False);
            Assert.That(before.PresentationRuntime.IsRequestPublished, Is.True);
            Assert.That(fixture.Output.Context.Contains(requestId), Is.True);
            Assert.That(incoming.IsReleased, Is.False);
            Assert.That(
                incoming.ScopeContext.Scope,
                Is.EqualTo(RuntimeContentScope.Session));
            Assert.That(
                incoming.ScopeContext.Owner,
                Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(
                incoming.PresentationRuntime.IsRequestPublished,
                Is.True);
            Assert.That(
                fixture.Output.Context.Contains(
                    incoming.PresentationRuntime.RequestId),
                Is.True);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.EqualTo(2));
            CameraRequestId winnerBeforeCommit =
                fixture.Output.Context.Winner.RequestId;
            Assert.That(
                winnerBeforeCommit == requestId ||
                winnerBeforeCommit == incoming.PresentationRuntime.RequestId,
                Is.True);

            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    routeC,
                    out string routeCCommitIssue),
                Is.True,
                routeCCommitIssue);

            CameraPresentationMaterializationHandle after =
                fixture.GetSelectedHandle();
            Assert.That(after, Is.SameAs(incoming));
            Assert.That(before.IsReleased, Is.True);
            Assert.That(after.IsReleased, Is.False);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(
                fixture.Output.Context.Winner.RequestId,
                Is.EqualTo(after.PresentationRuntime.RequestId));
            Assert.That(fixture.Output.Context.Contains(requestId), Is.False);
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(
                fixture.Output.Context.Winner.Owner.Kind,
                Is.EqualTo(CameraRequestOwnerKind.Session));
            Assert.That(
                fixture.Output.Context.Winner.Lifetime.Kind,
                Is.EqualTo(CameraRequestLifetimeKind.Session));
            Assert.That(after.ScopeContext.Owner, Is.Not.EqualTo(routeCContext.Owner));
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    after.RuntimeContentIdentity,
                    out _),
                Is.True);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    identity,
                    out _),
                Is.False);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    routeCContext,
                    after.RuntimeContentIdentity,
                    out _),
                Is.False);

            Assert.That(
                fixture.Lifecycle.TryExitRoute(
                    fixture.RouteA,
                    test,
                    "route-a-exit",
                    out string exitIssue),
                Is.True,
                exitIssue);
            RuntimeRootRegistryOperationResult removed =
                fixture.RuntimeContent.RemoveScopeRoot(
                    fixture.RouteAContext.Owner,
                    test,
                    "route-a-scope-remove");
            Assert.That(
                removed.Status,
                Is.EqualTo(RuntimeRootRegistryOperationStatus.RootRemoved));
            Assert.That(fixture.GetSelectedHandle(), Is.SameAs(after));
            Assert.That(after.IsReleased, Is.False);
            Assert.That(request.RequestId, Is.EqualTo(requestId));
        }

        [Test]
        public void FailedReplacementRestoresTheSameCurrentSelection()
        {
            using var fixture = new Fixture();
            const string test =
                nameof(FailedReplacementRestoresTheSameCurrentSelection);

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    fixture.RouteA,
                    fixture.RouteAContext,
                    test,
                    "route-a-enter",
                    out string routeAIssue),
                Is.True,
                routeAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.RouteA,
                    out string routeACommitIssue),
                Is.True,
                routeACommitIssue);

            CameraPresentationMaterializationHandle before =
                fixture.GetSelectedHandle();
            RuntimeContentIdentity identity = before.RuntimeContentIdentity;
            CameraRequest request = fixture.Output.Context.Winner;
            RouteAsset routeC = fixture.CreateRoute(
                "route-c",
                fixture.CreatePresentation("Presentation C"));
            RuntimeScopeContext routeCContext =
                fixture.CreateRouteContext(routeC, "route-c");

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    routeC,
                    routeCContext,
                    test,
                    "route-c-enter",
                    out string routeCIssue),
                Is.True,
                routeCIssue);

            CameraPresentationMaterializationHandle incoming =
                fixture.GetOnlyPendingHandle();
            RuntimeContentIdentity incomingIdentity =
                incoming.RuntimeContentIdentity;
            CameraRequestId incomingRequestId =
                incoming.PresentationRuntime.RequestId;
            Assert.That(
                incoming.ScopeContext.Scope,
                Is.EqualTo(RuntimeContentScope.Session));
            Assert.That(fixture.Output.Context.Contains(incomingRequestId), Is.True);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);

            Assert.That(
                fixture.Lifecycle.TryRollbackSelection(
                    routeC,
                    test,
                    "route-c-rollback",
                    out string rollbackIssue),
                Is.True,
                rollbackIssue);

            Assert.That(fixture.GetSelectedHandle(), Is.SameAs(before));
            Assert.That(before.IsReleased, Is.False);
            Assert.That(before.RuntimeContentIdentity, Is.EqualTo(identity));
            Assert.That(
                before.PresentationRuntime.RequestId,
                Is.EqualTo(request.RequestId));
            Assert.That(incoming.IsReleased, Is.True);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(
                fixture.Output.Context.Winner.RequestId,
                Is.EqualTo(request.RequestId));
            AssertEquivalentRequest(
                request,
                fixture.Output.Context.Winner);
            Assert.That(
                fixture.Output.Context.Contains(incomingRequestId),
                Is.False);
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    identity,
                    out _),
                Is.True);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    incomingIdentity,
                    out _),
                Is.False);
        }

        [Test]
        public void StartupActivitySelectionForSamePendingRouteOutputIsRejectedWithoutResidualCandidate()
        {
            using var fixture = new Fixture();
            const string test =
                nameof(StartupActivitySelectionForSamePendingRouteOutputIsRejectedWithoutResidualCandidate);

            CameraPresentationDefinition activityPresentation =
                fixture.CreatePresentation(
                    "Startup Activity Presentation",
                    fixture.OutputDefinition,
                    1000);
            ActivityAsset startupActivity = fixture.CreateActivity(
                "startup-activity-s",
                activityPresentation);
            RuntimeScopeContext activityContext =
                fixture.CreateActivityContext(
                    startupActivity,
                    "startup-activity-s");

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    fixture.RouteA,
                    fixture.RouteAContext,
                    test,
                    "route-r-enter",
                    out string routeIssue),
                Is.True,
                routeIssue);

            CameraPresentationMaterializationHandle routeCandidate =
                fixture.GetPendingHandle(fixture.RouteA);
            RuntimeContentIdentity routeIdentity =
                routeCandidate.RuntimeContentIdentity;
            CameraRequestId routeRequestId =
                routeCandidate.PresentationRuntime.RequestId;
            RuntimeContentIdentity rejectedActivityIdentity =
                fixture.SessionContext.CreateIdentity(
                    CameraPresentationMaterializationRuntime.CreateContentId(
                        activityPresentation));

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    startupActivity,
                    activityContext,
                    test,
                    "startup-activity-s-enter",
                    out string activityIssue),
                Is.False);
            Assert.That(
                activityIssue,
                Does.Contain(fixture.OutputDefinition.OutputId.ToString()));
            Assert.That(activityIssue, Does.Contain("Route 'route-a'"));
            Assert.That(
                activityIssue,
                Does.Contain("Activity 'startup-activity-s'"));

            Assert.That(fixture.PendingOwnerCount, Is.EqualTo(1));
            Assert.That(
                fixture.GetPendingHandle(fixture.RouteA),
                Is.SameAs(routeCandidate));
            Assert.That(
                fixture.GetSelectedHandle(),
                Is.SameAs(routeCandidate));
            Assert.That(routeCandidate.IsReleased, Is.False);
            Assert.That(
                routeCandidate.ScopeContext.Owner,
                Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(
                fixture.Output.Context.Winner.RequestId,
                Is.EqualTo(routeRequestId));
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    rejectedActivityIdentity,
                    out _),
                Is.False);

            Assert.That(
                fixture.Lifecycle.TryRollbackSelection(
                    fixture.RouteA,
                    test,
                    "route-r-rollback",
                    out string rollbackIssue),
                Is.True,
                rollbackIssue);
            Assert.That(routeCandidate.IsReleased, Is.True);
            Assert.That(fixture.PendingOwnerCount, Is.Zero);
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(fixture.Output.Context.HasWinner, Is.False);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    routeIdentity,
                    out _),
                Is.False);
        }

        [Test]
        public void StartupActivitySelectionForDifferentPendingRouteOutputIsAllowedAndRollsBackIndependently()
        {
            using var fixture = new Fixture();
            const string test =
                nameof(StartupActivitySelectionForDifferentPendingRouteOutputIsAllowedAndRollsBackIndependently);

            CameraPresentationDefinition activityPresentation =
                fixture.CreatePresentation(
                    "Startup Activity Presentation Output B",
                    fixture.OutputDefinitionB);
            ActivityAsset startupActivity = fixture.CreateActivity(
                "startup-activity-output-b",
                activityPresentation);
            RuntimeScopeContext activityContext =
                fixture.CreateActivityContext(
                    startupActivity,
                    "startup-activity-output-b");

            Assert.That(
                fixture.Lifecycle.TryEnterRoute(
                    fixture.RouteA,
                    fixture.RouteAContext,
                    test,
                    "route-output-a-enter",
                    out string routeIssue),
                Is.True,
                routeIssue);
            CameraPresentationMaterializationHandle routeCandidate =
                fixture.GetPendingHandle(fixture.RouteA);

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    startupActivity,
                    activityContext,
                    test,
                    "activity-output-b-enter",
                    out string activityIssue),
                Is.True,
                activityIssue);
            CameraPresentationMaterializationHandle activityCandidate =
                fixture.GetPendingHandle(startupActivity);

            Assert.That(fixture.PendingOwnerCount, Is.EqualTo(2));
            Assert.That(
                fixture.GetSelectedHandle(fixture.OutputDefinition),
                Is.SameAs(routeCandidate));
            Assert.That(
                fixture.GetSelectedHandle(fixture.OutputDefinitionB),
                Is.SameAs(activityCandidate));
            Assert.That(
                routeCandidate.ScopeContext.Owner,
                Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(
                activityCandidate.ScopeContext.Owner,
                Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(fixture.OutputB.Context.AdmittedRequestCount, Is.EqualTo(1));

            Assert.That(
                fixture.Lifecycle.TryRollbackSelection(
                    startupActivity,
                    test,
                    "activity-output-b-rollback",
                    out string activityRollbackIssue),
                Is.True,
                activityRollbackIssue);
            Assert.That(activityCandidate.IsReleased, Is.True);
            Assert.That(routeCandidate.IsReleased, Is.False);
            Assert.That(fixture.PendingOwnerCount, Is.EqualTo(1));
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(fixture.OutputB.Context.HasWinner, Is.False);
            Assert.That(fixture.OutputB.Context.AdmittedRequestCount, Is.Zero);

            Assert.That(
                fixture.Lifecycle.TryRollbackSelection(
                    fixture.RouteA,
                    test,
                    "route-output-a-rollback",
                    out string routeRollbackIssue),
                Is.True,
                routeRollbackIssue);
            Assert.That(routeCandidate.IsReleased, Is.True);
            Assert.That(fixture.PendingOwnerCount, Is.Zero);
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.Zero);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created =
                new List<UnityEngine.Object>();
            private readonly CameraSessionOutputMaterializationRuntime
                _outputMaterialization;

            internal Fixture()
            {
                RuntimeContent = new RuntimeContentRuntime();
                SessionContext = CreateContext(
                    RuntimeContentOwner.Session(
                        Guid.NewGuid().ToString("N"),
                        "Camera Selection Test Session"));

                OutputDefinition = CreateOutputDefinition();
                OutputDefinitionB = CreateOutputDefinition();
                FixedBehavior =
                    ScriptableObject.CreateInstance<
                        FixedCameraRigBehaviorDefinition>();
                _created.Add(FixedBehavior);

                CameraSessionConfiguration configuration =
                    CreateOutputConfiguration();
                GameObject outputParent = CreateRoot("Output Parent");
                Assert.That(
                    CameraSessionOutputMaterializationRuntime.TryCreate(
                        configuration,
                        outputParent.transform,
                        out _outputMaterialization,
                        out string outputIssue),
                    Is.True,
                    outputIssue);
                Assert.That(
                    _outputMaterialization.Topology.TryGetOutput(
                        OutputDefinition.OutputId,
                        out CameraOutputAuthoring output,
                        out string lookupIssue),
                    Is.True,
                    lookupIssue);
                Output = output;
                Assert.That(
                    _outputMaterialization.Topology.TryGetOutput(
                        OutputDefinitionB.OutputId,
                        out CameraOutputAuthoring outputB,
                        out string lookupIssueB),
                    Is.True,
                    lookupIssueB);
                OutputB = outputB;

                PresentationA = CreatePresentation("Presentation A");
                RouteA = CreateRoute("route-a", PresentationA);
                RouteB = CreateRoute("route-b");
                RouteAContext = CreateRouteContext(RouteA, "route-a");
                RouteBContext = CreateRouteContext(RouteB, "route-b");

                var availability =
                    new CameraSubjectAvailabilityContext(
                        new SubjectAvailabilityContextId(
                            $"camera-selection-test:{Guid.NewGuid():N}"));
                var materializer =
                    new CameraPresentationMaterializationRuntime(
                        RuntimeContent);
                Lifecycle =
                    new CameraPresentationLifecycleRuntime(
                        materializer,
                        _outputMaterialization.Topology,
                        availability,
                        null,
                        null,
                        SessionContext);
            }

            internal RuntimeContentRuntime RuntimeContent { get; }
            internal RuntimeScopeContext SessionContext { get; }
            internal RuntimeScopeContext RouteAContext { get; }
            internal RuntimeScopeContext RouteBContext { get; }
            internal CameraOutputDefinition OutputDefinition { get; }
            internal CameraOutputDefinition OutputDefinitionB { get; }
            internal FixedCameraRigBehaviorDefinition FixedBehavior { get; }
            internal CameraOutputAuthoring Output { get; }
            internal CameraOutputAuthoring OutputB { get; }
            internal CameraPresentationDefinition PresentationA { get; }
            internal RouteAsset RouteA { get; }
            internal RouteAsset RouteB { get; }
            internal CameraPresentationLifecycleRuntime Lifecycle { get; }

            internal CameraPresentationMaterializationHandle GetSelectedHandle()
            {
                return GetSelectedHandle(OutputDefinition);
            }

            internal CameraPresentationMaterializationHandle GetSelectedHandle(
                CameraOutputDefinition outputDefinition)
            {
                FieldInfo field =
                    typeof(CameraPresentationLifecycleRuntime).GetField(
                        "_selectedByOutput",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                var selected =
                    (Dictionary<CameraOutputId,
                        CameraPresentationMaterializationHandle>)
                    field.GetValue(Lifecycle);
                Assert.That(
                    selected.TryGetValue(
                        outputDefinition.OutputId,
                        out CameraPresentationMaterializationHandle handle),
                    Is.True);
                return handle;
            }

            internal int PendingOwnerCount
            {
                get
                {
                    return GetPendingSelectionsByOwner().Count;
                }
            }

            internal CameraPresentationMaterializationHandle GetPendingHandle(
                UnityEngine.Object owner)
            {
                IDictionary pendingByOwner = GetPendingSelectionsByOwner();
                object entry = pendingByOwner[owner];
                Assert.That(entry, Is.Not.Null);
                FieldInfo handlesField = entry.GetType().GetField(
                    "Handles",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(handlesField, Is.Not.Null);
                var handles =
                    (List<CameraPresentationMaterializationHandle>)
                    handlesField.GetValue(entry);
                Assert.That(handles, Is.Not.Null);
                Assert.That(handles.Count, Is.EqualTo(1));
                return handles[0];
            }

            internal CameraPresentationMaterializationHandle GetOnlyPendingHandle()
            {
                IDictionary pendingByOwner = GetPendingSelectionsByOwner();
                Assert.That(pendingByOwner.Count, Is.EqualTo(1));

                object entry = null;
                foreach (DictionaryEntry kv in pendingByOwner)
                {
                    entry = kv.Value;
                }

                Assert.That(entry, Is.Not.Null);
                FieldInfo handlesField = entry.GetType().GetField(
                    "Handles",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(handlesField, Is.Not.Null);
                var handles =
                    (List<CameraPresentationMaterializationHandle>)
                    handlesField.GetValue(entry);
                Assert.That(handles, Is.Not.Null);
                Assert.That(handles.Count, Is.EqualTo(1));
                return handles[0];
            }

            private IDictionary GetPendingSelectionsByOwner()
            {
                FieldInfo dictField =
                    typeof(CameraPresentationLifecycleRuntime).GetField(
                        "_pendingSelectionsByOwner",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(dictField, Is.Not.Null);
                var pendingByOwner =
                    (IDictionary)dictField.GetValue(Lifecycle);
                Assert.That(pendingByOwner, Is.Not.Null);
                return pendingByOwner;
            }

            internal RouteAsset CreateRoute(
                string routeId,
                params CameraPresentationDefinition[] selections)
            {
                RouteAsset route =
                    ScriptableObject.CreateInstance<RouteAsset>();
                _created.Add(route);
                route.name = routeId;
                SetField(route, "routeId", routeId);
                SetField(route, "routeName", routeId);
                SetField(
                    route,
                    "cameraPresentationSelections",
                    selections ??
                    Array.Empty<CameraPresentationDefinition>());
                return route;
            }

            internal RuntimeScopeContext CreateRouteContext(
                RouteAsset route,
                string ownerId)
            {
                return CreateContext(
                    RuntimeContentOwner.Route(
                        ownerId,
                        route.name,
                        RuntimeDefinitionToken.FromUnityObject(route)));
            }

            internal ActivityAsset CreateActivity(
                string activityId,
                params CameraPresentationDefinition[] selections)
            {
                ActivityAsset activity =
                    ScriptableObject.CreateInstance<ActivityAsset>();
                _created.Add(activity);
                activity.name = activityId;
                SetField(activity, "activityId", activityId);
                SetField(activity, "activityName", activityId);
                SetField(
                    activity,
                    "cameraPresentationSelections",
                    selections ??
                    Array.Empty<CameraPresentationDefinition>());
                return activity;
            }

            internal RuntimeScopeContext CreateActivityContext(
                ActivityAsset activity,
                string ownerId)
            {
                return CreateContext(
                    RuntimeContentOwner.Activity(
                        ownerId,
                        activity.name,
                        RuntimeDefinitionToken.FromUnityObject(activity)));
            }

            internal CameraPresentationDefinition CreatePresentation(
                string presentationName)
            {
                return CreatePresentation(
                    presentationName,
                    OutputDefinition);
            }

            internal CameraPresentationDefinition CreatePresentation(
                string presentationName,
                CameraOutputDefinition outputDefinition,
                int requestPrecedence = 100)
            {
                GameObject rigRoot =
                    CreateRoot($"{presentationName} Rig");
                CameraRigComposer composer =
                    rigRoot.AddComponent<CameraRigComposer>();
                SetField(
                    composer,
                    "behaviorDefinition",
                    FixedBehavior);
                SetField(
                    composer,
                    "cinemachineCamera",
                    rigRoot.AddComponent<CinemachineCamera>());

                CameraPresentationDefinition definition =
                    ScriptableObject.CreateInstance<
                        CameraPresentationDefinition>();
                definition.name = presentationName;
                _created.Add(definition);
                SetField(
                    definition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
                SetField(
                    definition,
                    "outputDefinition",
                    outputDefinition);
                SetField(definition, "rigPrefab", rigRoot);
                SetField(
                    definition,
                    "subjectPolicy",
                    CameraSharedCompositionSubjectPolicyKind
                        .AllAvailableSubjects);
                SetField(
                    definition,
                    "transitionMode",
                    CameraPresentationTransitionMode.Cut);
                SetField(
                    definition,
                    "requestPrecedence",
                    requestPrecedence);
                return definition;
            }

            public void Dispose()
            {
                Lifecycle?.Dispose();
                _outputMaterialization?.Dispose();
                for (int index = _created.Count - 1;
                     index >= 0;
                     index--)
                {
                    UnityEngine.Object value = _created[index];
                    if (value != null)
                    {
                        UnityEngine.Object.DestroyImmediate(value);
                    }
                }
            }

            private RuntimeScopeContext CreateContext(
                RuntimeContentOwner owner)
            {
                RuntimeRootRegistryOperationResult root =
                    RuntimeContent.CreateScopeRoot(
                        owner,
                        nameof(CameraPresentationRouteSelectionContinuityTests),
                        "test-root");
                Assert.That(root.Applied, Is.True, root.Message);
                Assert.That(
                    RuntimeContent.TryCreateScopeContext(
                        owner,
                        nameof(CameraPresentationRouteSelectionContinuityTests),
                        "test-context",
                        out RuntimeScopeContext context),
                    Is.True);
                return context;
            }

            private CameraSessionConfiguration CreateOutputConfiguration()
            {
                GameObject outputPrefab = CreateOutputPrefab(
                    "Camera Output Prefab A",
                    OutputDefinition);
                GameObject outputPrefabB = CreateOutputPrefab(
                    "Camera Output Prefab B",
                    OutputDefinitionB);

                var configuration =
                    new CameraSessionConfiguration();
                SetField(
                    configuration,
                    "outputPrefabs",
                    new List<GameObject> { outputPrefab, outputPrefabB });
                return configuration;
            }

            private GameObject CreateOutputPrefab(
                string prefabName,
                CameraOutputDefinition outputDefinition)
            {
                GameObject outputPrefab = CreateRoot(prefabName);
                outputPrefab.SetActive(false);
                UnityEngine.Camera unityCamera =
                    outputPrefab.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain =
                    outputPrefab.AddComponent<CinemachineBrain>();

                GameObject fallbackRigRoot =
                    CreateRoot("Fallback Camera Rig");
                fallbackRigRoot.transform.SetParent(
                    outputPrefab.transform,
                    false);
                CameraRigComposer fallbackRig =
                    fallbackRigRoot.AddComponent<CameraRigComposer>();
                SetField(
                    fallbackRig,
                    "behaviorDefinition",
                    FixedBehavior);
                SetField(
                    fallbackRig,
                    "cinemachineCamera",
                    fallbackRigRoot.AddComponent<CinemachineCamera>());

                CameraOutputAuthoring output =
                    outputPrefab.AddComponent<CameraOutputAuthoring>();
                SetField(
                    output,
                    "outputDefinition",
                    outputDefinition);
                SetField(output, "unityCamera", unityCamera);
                SetField(output, "cinemachineBrain", brain);
                SetField(output, "fallbackCameraRig", fallbackRig);
                SetField(output, "initializeOnAwake", false);

                return outputPrefab;
            }

            private CameraOutputDefinition CreateOutputDefinition()
            {
                CameraOutputDefinition definition =
                    ScriptableObject.CreateInstance<
                        CameraOutputDefinition>();
                _created.Add(definition);
                SetField(
                    definition,
                    "stableId",
                    Guid.NewGuid().ToString("N"));
                return definition;
            }

            private GameObject CreateRoot(string rootName)
            {
                var root = new GameObject(rootName);
                _created.Add(root);
                return root;
            }
        }

        private static void SetField(
            object target,
            string name,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void AssertEquivalentRequest(
            CameraRequest expected,
            CameraRequest actual)
        {
            Assert.That(actual.RequestId, Is.EqualTo(expected.RequestId));
            Assert.That(actual.OutputId, Is.EqualTo(expected.OutputId));
            Assert.That(actual.Owner, Is.EqualTo(expected.Owner));
            Assert.That(actual.Lifetime, Is.EqualTo(expected.Lifetime));
            Assert.That(actual.Rig.Composer, Is.SameAs(expected.Rig.Composer));
            Assert.That(actual.Policy, Is.EqualTo(expected.Policy));
            Assert.That(
                actual.PresentationTransitionMode,
                Is.EqualTo(expected.PresentationTransitionMode));
            Assert.That(
                actual.ReleaseCondition,
                Is.EqualTo(expected.ReleaseCondition));
            Assert.That(
                actual.DiagnosticSource,
                Is.EqualTo(expected.DiagnosticSource));
            Assert.That(
                actual.DiagnosticReason,
                Is.EqualTo(expected.DiagnosticReason));
        }
    }
}
