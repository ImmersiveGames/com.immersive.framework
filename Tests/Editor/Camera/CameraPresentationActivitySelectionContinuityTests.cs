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
    /// <summary>
    /// CAMERA-037-D/E: Activity persistent Camera Presentation selection
    /// continuity. Mirrors
    /// <c>CameraPresentationRouteSelectionContinuityTests</c> using the exact
    /// same shared <c>CameraPresentationLifecycleRuntime</c> pending-selection
    /// mechanism, now exercised with an <see cref="ActivityAsset"/> owner
    /// instead of a <see cref="RouteAsset"/> owner.
    /// </summary>
    public sealed class CameraPresentationActivitySelectionContinuityTests
    {
        [Test]
        public void ActivityEntersPersistentSelectionAndBecomesSessionOwnedWinner()
        {
            using var fixture = new Fixture();

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    fixture.ActivityA,
                    fixture.ActivityAContext,
                    nameof(ActivityEntersPersistentSelectionAndBecomesSessionOwnedWinner),
                    "activity-a-enter",
                    out string activityAIssue),
                Is.True,
                activityAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.ActivityA,
                    out string activityACommitIssue),
                Is.True,
                activityACommitIssue);

            CameraPresentationMaterializationHandle handle =
                fixture.GetSelectedHandle();
            Assert.That(handle.ScopeContext.Scope, Is.EqualTo(RuntimeContentScope.Session));
            Assert.That(handle.ScopeContext.Owner, Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(handle.PresentationRuntime.IsRequestPublished, Is.True);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(
                fixture.Output.Context.Winner.RequestId,
                Is.EqualTo(handle.PresentationRuntime.RequestId));
            Assert.That(
                fixture.Output.Context.Winner.Owner.Kind,
                Is.EqualTo(CameraRequestOwnerKind.Session));
            Assert.That(
                fixture.Output.Context.Winner.Lifetime.Kind,
                Is.EqualTo(CameraRequestLifetimeKind.Session));
        }

        [Test]
        public void EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence()
        {
            using var fixture = new Fixture();

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    fixture.ActivityA,
                    fixture.ActivityAContext,
                    nameof(EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence),
                    "activity-a-enter",
                    out string activityAIssue),
                Is.True,
                activityAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.ActivityA,
                    out string activityACommitIssue),
                Is.True,
                activityACommitIssue);

            CameraPresentationMaterializationHandle before =
                fixture.GetSelectedHandle();
            RuntimeContentIdentity identity = before.RuntimeContentIdentity;
            CameraRequestId requestId = before.PresentationRuntime.RequestId;
            CameraRequest winner = fixture.Output.Context.Winner;

            // Activity B declares zero selections: entering it must be a
            // pure no-op for the persistent selection (CAMERA-037-D contract).
            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    fixture.ActivityB,
                    fixture.ActivityBContext,
                    nameof(EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence),
                    "activity-b-enter",
                    out string activityBIssue),
                Is.True,
                activityBIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.ActivityB,
                    out string activityBCommitIssue),
                Is.True,
                activityBCommitIssue);

            // Activity A is released (its own scope actually ends) without
            // destroying the Session-owned persistent selection.
            Assert.That(
                fixture.Lifecycle.TryExitActivity(
                    fixture.ActivityA,
                    nameof(EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence),
                    "activity-a-exit",
                    out string activityAExitIssue),
                Is.True,
                activityAExitIssue);

            RuntimeRootRegistryOperationResult removed =
                fixture.RuntimeContent.RemoveScopeRoot(
                    fixture.ActivityAContext.Owner,
                    nameof(EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence),
                    "activity-a-scope-remove");
            Assert.That(removed.Status, Is.EqualTo(RuntimeRootRegistryOperationStatus.RootRemoved));

            CameraPresentationMaterializationHandle after =
                fixture.GetSelectedHandle();
            Assert.That(after, Is.SameAs(before));
            Assert.That(after.RuntimeContentIdentity, Is.EqualTo(identity));
            Assert.That(after.PresentationRuntime.RequestId, Is.EqualTo(requestId));
            Assert.That(after.ScopeContext.Owner, Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(after.ScopeContext.Owner, Is.Not.EqualTo(fixture.ActivityAContext.Owner));
            Assert.That(after.ScopeContext.Owner, Is.Not.EqualTo(fixture.ActivityBContext.Owner));
            Assert.That(after.IsReleased, Is.False);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(fixture.Output.Context.Winner.RequestId, Is.EqualTo(requestId));
            AssertEquivalentRequest(winner, fixture.Output.Context.Winner);
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
                    fixture.ActivityBContext,
                    fixture.ActivityBContext.CreateIdentity(contentId),
                    out _),
                Is.False,
                "The incoming Activity must not receive or own the persistent occurrence.");
            Assert.That(
                fixture.RuntimeContent.TryCreateScopeContext(
                    fixture.ActivityAContext.Owner,
                    nameof(EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence),
                    "activity-a-dead-scope-check",
                    out _),
                Is.False,
                "The outgoing Activity scope must be fully removable while the selection remains live.");
        }

        [Test]
        public void DifferentActivitySelectionReplacesCurrentOnlyAfterIncomingBecomesWinner()
        {
            using var fixture = new Fixture();
            const string test =
                nameof(DifferentActivitySelectionReplacesCurrentOnlyAfterIncomingBecomesWinner);

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    fixture.ActivityA,
                    fixture.ActivityAContext,
                    test,
                    "activity-a-enter",
                    out string activityAIssue),
                Is.True,
                activityAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.ActivityA,
                    out string activityACommitIssue),
                Is.True,
                activityACommitIssue);

            CameraPresentationMaterializationHandle before =
                fixture.GetSelectedHandle();
            RuntimeContentIdentity beforeIdentity = before.RuntimeContentIdentity;
            CameraRequestId beforeRequestId =
                before.PresentationRuntime.RequestId;
            ActivityAsset activityC = fixture.CreateActivity(
                "activity-c",
                fixture.CreatePresentation("Presentation C (Activity)"));
            RuntimeScopeContext activityCContext =
                fixture.CreateActivityContext(activityC, "activity-c");

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    activityC,
                    activityCContext,
                    test,
                    "activity-c-enter",
                    out string activityCIssue),
                Is.True,
                activityCIssue);

            CameraPresentationMaterializationHandle incoming =
                fixture.GetOnlyPendingHandle();
            RuntimeContentIdentity incomingIdentity =
                incoming.RuntimeContentIdentity;
            CameraRequestId incomingRequestId =
                incoming.PresentationRuntime.RequestId;
            Assert.That(incoming, Is.Not.SameAs(before));
            Assert.That(incomingIdentity, Is.Not.EqualTo(beforeIdentity));
            Assert.That(incomingRequestId, Is.Not.EqualTo(beforeRequestId));
            Assert.That(incoming.ScopeContext.Scope, Is.EqualTo(RuntimeContentScope.Session));
            Assert.That(incoming.ScopeContext.Owner, Is.EqualTo(fixture.SessionContext.Owner));
            Assert.That(fixture.GetSelectedHandle(), Is.SameAs(before));
            Assert.That(before.IsReleased, Is.False);
            Assert.That(before.PresentationRuntime.IsRequestPublished, Is.True);
            Assert.That(incoming.IsReleased, Is.False);
            Assert.That(incoming.PresentationRuntime.IsRequestPublished, Is.True);
            Assert.That(fixture.Output.Context.Contains(beforeRequestId), Is.True);
            Assert.That(fixture.Output.Context.Contains(incomingRequestId), Is.True);

            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    activityC,
                    out string activityCCommitIssue),
                Is.True,
                activityCCommitIssue);

            CameraPresentationMaterializationHandle after =
                fixture.GetSelectedHandle();
            Assert.That(after, Is.SameAs(incoming));
            Assert.That(before.IsReleased, Is.True);
            Assert.That(before.PresentationRuntime.IsRequestPublished, Is.False);
            Assert.That(after.IsReleased, Is.False);
            Assert.That(after.PresentationRuntime.IsRequestPublished, Is.True);
            Assert.That(fixture.Output.Context.HasWinner, Is.True);
            Assert.That(
                fixture.Output.Context.Winner.RequestId,
                Is.EqualTo(incomingRequestId));
            Assert.That(fixture.Output.Context.Contains(beforeRequestId), Is.False);
            Assert.That(fixture.Output.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(
                fixture.Output.Context.Winner.Owner.Kind,
                Is.EqualTo(CameraRequestOwnerKind.Session));
            Assert.That(
                fixture.Output.Context.Winner.Lifetime.Kind,
                Is.EqualTo(CameraRequestLifetimeKind.Session));
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    incomingIdentity,
                    out RuntimeContentHandle registered),
                Is.True);
            Assert.That(registered, Is.SameAs(after.RuntimeContentHandle));
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    fixture.SessionContext,
                    beforeIdentity,
                    out _),
                Is.False);
            Assert.That(
                fixture.RuntimeContent.TryGetHandle(
                    activityCContext,
                    incomingIdentity,
                    out _),
                Is.False);
        }

        [Test]
        public void FailedActivityReplacementRestoresTheSameCurrentSelection()
        {
            using var fixture = new Fixture();
            const string test =
                nameof(FailedActivityReplacementRestoresTheSameCurrentSelection);

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    fixture.ActivityA,
                    fixture.ActivityAContext,
                    test,
                    "activity-a-enter",
                    out string activityAIssue),
                Is.True,
                activityAIssue);
            Assert.That(
                fixture.Lifecycle.TryCommitSelection(
                    fixture.ActivityA,
                    out string activityACommitIssue),
                Is.True,
                activityACommitIssue);

            CameraPresentationMaterializationHandle before =
                fixture.GetSelectedHandle();
            RuntimeContentIdentity identity = before.RuntimeContentIdentity;
            CameraRequest request = fixture.Output.Context.Winner;

            ActivityAsset activityC = fixture.CreateActivity(
                "activity-c",
                fixture.CreatePresentation("Presentation C (Activity)"));
            RuntimeScopeContext activityCContext =
                fixture.CreateActivityContext(activityC, "activity-c");

            Assert.That(
                fixture.Lifecycle.TryEnterActivity(
                    activityC,
                    activityCContext,
                    test,
                    "activity-c-enter",
                    out string activityCIssue),
                Is.True,
                activityCIssue);

            CameraPresentationMaterializationHandle incoming =
                fixture.GetOnlyPendingHandle();
            RuntimeContentIdentity incomingIdentity =
                incoming.RuntimeContentIdentity;
            CameraRequestId incomingRequestId =
                incoming.PresentationRuntime.RequestId;
            Assert.That(fixture.Output.Context.Contains(incomingRequestId), Is.True);

            // Roll back before commit, exactly as an Activity transaction
            // failing before its own commit point must do.
            Assert.That(
                fixture.Lifecycle.TryRollbackSelection(
                    activityC,
                    test,
                    "activity-c-rollback",
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
            AssertEquivalentRequest(request, fixture.Output.Context.Winner);
            Assert.That(
                fixture.Output.Context.Contains(incomingRequestId),
                Is.False);
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
                        "Camera Activity Selection Test Session"));

                OutputDefinition = CreateOutputDefinition();
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

                PresentationA = CreatePresentation("Presentation A (Activity)");
                ActivityA = CreateActivity("activity-a", PresentationA);
                ActivityB = CreateActivity("activity-b");
                ActivityAContext = CreateActivityContext(ActivityA, "activity-a");
                ActivityBContext = CreateActivityContext(ActivityB, "activity-b");

                var availability =
                    new CameraSubjectAvailabilityContext(
                        new SubjectAvailabilityContextId(
                            $"camera-activity-selection-test:{Guid.NewGuid():N}"));
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
            internal RuntimeScopeContext ActivityAContext { get; }
            internal RuntimeScopeContext ActivityBContext { get; }
            internal CameraOutputDefinition OutputDefinition { get; }
            internal FixedCameraRigBehaviorDefinition FixedBehavior { get; }
            internal CameraOutputAuthoring Output { get; }
            internal CameraPresentationDefinition PresentationA { get; }
            internal ActivityAsset ActivityA { get; }
            internal ActivityAsset ActivityB { get; }
            internal CameraPresentationLifecycleRuntime Lifecycle { get; }

            internal CameraPresentationMaterializationHandle GetSelectedHandle()
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
                        OutputDefinition.OutputId,
                        out CameraPresentationMaterializationHandle handle),
                    Is.True);
                return handle;
            }

            internal CameraPresentationMaterializationHandle GetOnlyPendingHandle()
            {
                FieldInfo dictField =
                    typeof(CameraPresentationLifecycleRuntime).GetField(
                        "_pendingSelectionsByOwner",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(dictField, Is.Not.Null);
                var pendingByOwner =
                    (IDictionary)dictField.GetValue(Lifecycle);
                Assert.That(pendingByOwner, Is.Not.Null);
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
                    OutputDefinition);
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
                SetField(definition, "requestPrecedence", 100);
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
                        nameof(CameraPresentationActivitySelectionContinuityTests),
                        "test-root");
                Assert.That(root.Applied, Is.True, root.Message);
                Assert.That(
                    RuntimeContent.TryCreateScopeContext(
                        owner,
                        nameof(CameraPresentationActivitySelectionContinuityTests),
                        "test-context",
                        out RuntimeScopeContext context),
                    Is.True);
                return context;
            }

            private CameraSessionConfiguration CreateOutputConfiguration()
            {
                GameObject outputPrefab =
                    CreateRoot("Camera Output Prefab");
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
                    OutputDefinition);
                SetField(output, "unityCamera", unityCamera);
                SetField(output, "cinemachineBrain", brain);
                SetField(output, "fallbackCameraRig", fallbackRig);
                SetField(output, "initializeOnAwake", false);

                var configuration =
                    new CameraSessionConfiguration();
                SetField(
                    configuration,
                    "outputPrefabs",
                    new List<GameObject> { outputPrefab });
                return configuration;
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
