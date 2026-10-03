using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    public sealed class ResetTargetResolverTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private ResetRegistry _registry;
        private ResettableOwnerRegistrationRuntime _registration;
        private StableObjectBindingRegistry _bindings;
        private RuntimeContentOwner _activity;
        private RuntimeContentOwner _otherActivity;
        private RuntimeContentOwner _route;
        private RuntimeContentOwner _otherRoute;

        [SetUp]
        public void SetUp()
        {
            _registry = new ResetRegistry();
            _registration = new ResettableOwnerRegistrationRuntime(_registry);
            _bindings = new StableObjectBindingRegistry();
            _activity = RuntimeContentOwner.Activity("reset-035-e.activity", "Activity", RuntimeDefinitionToken.MintAnonymous());
            _otherActivity = RuntimeContentOwner.Activity("reset-035-e.other-activity", "Other Activity", RuntimeDefinitionToken.MintAnonymous());
            _route = RuntimeContentOwner.Route("reset-035-e.route", "Route", RuntimeDefinitionToken.MintAnonymous());
            _otherRoute = RuntimeContentOwner.Route("reset-035-e.other-route", "Other Route", RuntimeDefinitionToken.MintAnonymous());
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void ObjectTarget_ResolvesExactlyItsRegisteredResettable()
        {
            GameObject root = Create("Root");
            Resettable target = AddResettable(root.transform, "Target");
            Resettable other = AddResettable(root.transform, "Other");
            Register(_route, root);

            Assert.IsTrue(ResetTargetResolver.TryResolveObjectSubject(
                _registry, target, CurrentOwners(), out ResetSubject subject, out string diagnostic), diagnostic);
            Assert.AreEqual(target.RuntimeSubjectId, subject.SubjectId);
            Assert.AreNotEqual(other.RuntimeSubjectId, subject.SubjectId);
        }

        [Test]
        public void ObjectDirectTarget_IsValidAndCarriesOnlyDirectReference()
        {
            Resettable resettable = AddResettable(Create("DirectObject").transform, "Resettable");
            ResetObjectTarget objectTarget = ResetObjectTarget.Direct(resettable);
            ResetTarget target = ResetTarget.ForObject(objectTarget);

            Assert.IsTrue(target.IsValid);
            Assert.AreEqual(ResetTargetKind.Object, target.Kind);
            Assert.AreEqual(ResetReferenceMode.Direct, target.ObjectTarget.ReferenceMode);
            Assert.AreSame(resettable, target.ObjectTarget.DirectResettable);
        }

        [Test]
        public void ObjectStableTarget_IsValidAndCarriesStableReference()
        {
            StableObjectReference reference = StableObjectReference.ForEntry(
                Immersive.Framework.ObjectEntry.ObjectEntryId.From("qa.reset.object-stable"));
            ResetTarget target = ResetTarget.ForObject(ResetObjectTarget.Stable(reference));

            Assert.IsTrue(target.IsValid);
            Assert.AreEqual(ResetReferenceMode.Stable, target.ObjectTarget.ReferenceMode);
            Assert.AreEqual(reference, target.ObjectTarget.StableReference);
            Assert.IsNull(target.ObjectTarget.DirectResettable);
        }

        [Test]
        public void CompositionDirectTarget_IsValidAndCarriesOnlyDirectReference()
        {
            ResetComposition composition = Create("DirectComposition").AddComponent<ResetComposition>();
            ResetTarget target = ResetTarget.ForComposition(ResetCompositionTarget.Direct(composition));

            Assert.IsTrue(target.IsValid);
            Assert.AreEqual(ResetTargetKind.Composition, target.Kind);
            Assert.AreEqual(ResetReferenceMode.Direct, target.CompositionTarget.ReferenceMode);
            Assert.AreSame(composition, target.CompositionTarget.DirectComposition);
        }

        [Test]
        public void CompositionStableTarget_IsValidAndCarriesStableReference()
        {
            StableObjectReference reference = StableObjectReference.ForEntry(
                Immersive.Framework.ObjectEntry.ObjectEntryId.From("qa.reset.composition-stable"));
            ResetTarget target = ResetTarget.ForComposition(ResetCompositionTarget.Stable(reference));

            Assert.IsTrue(target.IsValid);
            Assert.AreEqual(ResetReferenceMode.Stable, target.CompositionTarget.ReferenceMode);
            Assert.AreEqual(reference, target.CompositionTarget.StableReference);
            Assert.IsNull(target.CompositionTarget.DirectComposition);
        }

        [Test]
        public void UnknownTarget_IsInvalidAndRejectedBeforeRuntimeLookup()
        {
            ResetTarget target = default;

            Assert.IsFalse(target.IsValid);
            Assert.IsFalse(System.Enum.IsDefined(typeof(ResetTargetKind), "StableReference"));
            ResetSelectionResolution result = ResetTargetResolver.Resolve(null, target, "test", "unknown");
            Assert.AreEqual(ResetSelectionResolutionStatus.RejectedInvalidRequest, result.Status);
            StringAssert.Contains("Unknown", result.Message);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ObjectTarget_NullOrUnregisteredFails(bool createUnregistered)
        {
            Resettable target = createUnregistered ? AddResettable(Create("Unregistered").transform, "Target") : null;
            Assert.IsFalse(ResetTargetResolver.TryResolveObjectSubject(
                _registry, target, CurrentOwners(), out _, out string diagnostic));
            StringAssert.Contains(createUnregistered ? "not registered" : "non-null", diagnostic);
            Assert.AreEqual(0, _registry.SubjectCount, "Resolving an Object target must never register it implicitly.");
        }

        [Test]
        public void ObjectTarget_RejectsRegistrationFromAnotherCurrentContext()
        {
            Resettable target = AddResettable(Create("Target").transform, "Target");
            Register(_otherActivity, target.gameObject);

            Assert.IsFalse(ResetTargetResolver.TryResolveObjectSubject(
                _registry, target, CurrentOwners(), out _, out string diagnostic));
            StringAssert.Contains("outside the current", diagnostic);
        }

        [Test]
        public void CompositionTarget_UsesTypedMembersDeduplicatesAndPreservesAuthoringOrder()
        {
            GameObject root = Create("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable first = AddResettable(root.transform, "First");
            Resettable second = AddResettable(root.transform, "Second");
            Resettable unlisted = AddResettable(root.transform, "Unlisted");
            SetExplicitMembers(composition, second, first, second);
            SetMemberMode(composition, ResetCompositionMemberMode.ExplicitMembers);
            Register(_route, root);

            Assert.IsTrue(ResetTargetResolver.TryResolveCompositionSubjects(
                _registry, composition, CurrentOwners(), out IReadOnlyList<ResetSubjectId> ids, out _, out string diagnostic), diagnostic);
            CollectionAssert.AreEqual(new[] { second.RuntimeSubjectId, first.RuntimeSubjectId }, ids);
            CollectionAssert.DoesNotContain(ids, unlisted.RuntimeSubjectId);
        }

        [Test]
        public void CompositionTarget_EmptyCompositionSucceedsWithDiagnostic()
        {
            ResetComposition composition = Create("Empty").AddComponent<ResetComposition>();
            Register(_route, composition.gameObject);

            Assert.IsTrue(ResetTargetResolver.TryResolveCompositionSubjects(
                _registry, composition, CurrentOwners(), out IReadOnlyList<ResetSubjectId> ids,
                out IReadOnlyList<ResetIssue> issues, out string diagnostic), diagnostic);
            Assert.IsEmpty(ids);
            Assert.IsTrue(issues.Any(issue => issue.Message.Contains("reset-composition-empty")));
            StringAssert.Contains("empty", diagnostic);
        }

        [Test]
        public void CompositionTarget_RejectsUnregisteredMemberWithoutRegisteringIt()
        {
            ResetComposition composition = Create("Composition").AddComponent<ResetComposition>();
            Resettable member = AddResettable(Create("ExternalRoot").transform, "UnregisteredMember");
            SetMemberMode(composition, ResetCompositionMemberMode.ExplicitMembers);
            SetExplicitMembers(composition, member);
            Register(_route, Create("RegisteredRoot"));

            Assert.IsFalse(ResetTargetResolver.TryResolveCompositionSubjects(
                _registry, composition, CurrentOwners(), out IReadOnlyList<ResetSubjectId> ids,
                out _, out string diagnostic));
            Assert.IsEmpty(ids);
            Assert.IsFalse(member.IsRegistered);
            StringAssert.Contains("not registered", diagnostic);
        }

        [Test]
        public void CurrentActivityAndCurrentRoute_ApplyParentAndChildMembershipScopes()
        {
            GameObject root = Create("Root");
            Resettable activityMember = AddResettable(root.transform, "Activity");
            Resettable routeActivityMember = AddResettable(root.transform, "RouteActivity");
            Resettable routeMember = AddResettable(root.transform, "Route");
            Resettable otherActivityMember = AddResettable(root.transform, "OtherActivity");
            Resettable otherRouteActivityMember = AddResettable(root.transform, "OtherRouteActivity");
            Resettable otherRouteMember = AddResettable(root.transform, "OtherRoute");
            SetMembership(routeActivityMember, ResetMembership.Activity);
            SetMembership(otherRouteActivityMember, ResetMembership.Activity);
            Register(_activity, activityMember.gameObject);
            Register(_route, routeActivityMember.gameObject, routeMember.gameObject);
            Register(_otherActivity, otherActivityMember.gameObject);
            Register(_otherRoute, otherRouteActivityMember.gameObject, otherRouteMember.gameObject);

            CollectionAssert.AreEquivalent(
                new[] { activityMember.RuntimeSubjectId, routeActivityMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activity, _route));
            CollectionAssert.AreEquivalent(
                new[] { activityMember.RuntimeSubjectId, routeActivityMember.RuntimeSubjectId, routeMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _route, _activity));
            CollectionAssert.DoesNotContain(
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _route, _activity),
                otherActivityMember.RuntimeSubjectId);
            CollectionAssert.DoesNotContain(
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _route, _activity),
                otherRouteActivityMember.RuntimeSubjectId);
            CollectionAssert.DoesNotContain(
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _route, _activity),
                otherRouteMember.RuntimeSubjectId);
        }

        [Test]
        public void ActivityRestart_CurrentActivityKeepsRouteActivityStateAndDropsRecreatedActivityState()
        {
            GameObject root = Create("Root");
            Resettable activityOwned = AddResettable(root.transform, "RecreatedActivityState");
            Resettable surviving = AddResettable(root.transform, "SurvivingRouteState");
            SetMembership(surviving, ResetMembership.Activity);
            Register(_activity, activityOwned.gameObject);
            Register(_route, surviving.gameObject);
            ResetSubjectId[] selected = ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activity, _route).ToArray();

            IReadOnlyList<ResetSubjectId> restartSubjects = ResetTargetResolver.FilterActivityRestartSurvivors(
                selected, _registry, _route, ResetTargetKind.CurrentActivity);

            CollectionAssert.AreEqual(new[] { surviving.RuntimeSubjectId }, restartSubjects);
        }

        [Test]
        public void ActivityRestart_BlocksClearAndReenterWhenResetFails()
        {
            ResetExecutionResult failed = ResetExecutionResult.RejectedInvalidRequest(
                ResetIssue.Error(ResetIssueKind.InvalidParticipant, "Required participant failed."),
                "test", "activity-restart");
            ResetExecutionResult succeeded = ResetExecutionResult.SucceededNoSubjects(
                ResetIssue.Info(ResetIssueKind.InvalidRequest, "No surviving subjects."),
                "test", "activity-restart");

            Assert.IsFalse(FrameworkRuntimeHost.ShouldContinueActivityRestartAfterReset(failed));
            Assert.IsTrue(FrameworkRuntimeHost.ShouldContinueActivityRestartAfterReset(succeeded));
        }

        [TestCase(ResetTargetKind.Object)]
        [TestCase(ResetTargetKind.Composition)]
        public async Task RequestTrigger_DispatchesTypedTargetToRuntimePort(ResetTargetKind targetKind)
        {
            var trigger = Create("Request").AddComponent<ResetRequestTrigger>();
            Resettable resettable = AddResettable(Create("Target").transform, "Resettable");
            ResetComposition composition = Create("Composition").AddComponent<ResetComposition>();
            trigger.Target = targetKind == ResetTargetKind.Object
                ? ResetTarget.ForObject(ResetObjectTarget.Direct(resettable))
                : ResetTarget.ForComposition(ResetCompositionTarget.Direct(composition));
            var runtime = new RecordingRuntime();
            Assert.IsTrue(trigger.TryBind(runtime, out string bindingIssue), bindingIssue);

            ResetExecutionResult execution = await trigger.RequestResetAsync();

            Assert.AreEqual(ResetExecutionStatus.SucceededNoSubjects, execution.Status);
            Assert.AreEqual(targetKind, runtime.Target.Kind);
            Assert.AreEqual(trigger.Target.ObjectTarget, runtime.Target.ObjectTarget);
            Assert.AreEqual(trigger.Target.CompositionTarget, runtime.Target.CompositionTarget);
            Assert.IsFalse(trigger.IsRequestInFlight);
        }

        [Test]
        public void RequestTrigger_ScopeBindingIsIdempotent_RejectsForeignAuthority_AndDetachesExplicitly()
        {
            GameObject root = Create("RequestRoot");
            ResetRequestTrigger trigger = root.AddComponent<ResetRequestTrigger>();
            var authority = new RecordingRuntime();
            var foreignAuthority = new RecordingRuntime();

            Assert.IsTrue(ResetRequestTriggerBinder.TryBind(new[] { root }, authority, out _, out string bindIssue), bindIssue);
            Assert.IsTrue(ResetRequestTriggerBinder.TryBind(new[] { root }, authority, out _, out string reentryIssue), reentryIssue);
            Assert.IsFalse(ResetRequestTriggerBinder.TryBind(new[] { root }, foreignAuthority, out _, out _));
            Assert.IsTrue(trigger.HasRuntimeBinding);
            Assert.IsFalse(trigger.TryUnbind(foreignAuthority, out _));
            Assert.IsTrue(ResetRequestTriggerBinder.TryRelease(new[] { root }, authority, out _, out string releaseIssue), releaseIssue);
            Assert.IsFalse(trigger.HasRuntimeBinding);
            Assert.IsTrue(ResetRequestTriggerBinder.TryRelease(new[] { root }, authority, out _, out _));
        }

        [Test]
        public void SubjectAdapter_ResetRegistrationPortDetachesExplicitlyAndIdempotently()
        {
            UnityResetSubjectAdapter adapter = Create("SubjectAdapter").AddComponent<UnityResetSubjectAdapter>();
            var runtime = new FakeResetRegistrationRuntime();
            Assert.IsTrue(adapter.TryBindResetRegistrationRuntime(runtime, out string bindIssue), bindIssue);
            Assert.IsTrue(adapter.HasResetRegistrationRuntimeBinding);
            Assert.IsTrue(adapter.TryUnbindResetRegistrationRuntime(runtime, out string releaseIssue), releaseIssue);
            Assert.IsFalse(adapter.HasResetRegistrationRuntimeBinding);
            Assert.IsTrue(adapter.TryUnbindResetRegistrationRuntime(runtime, out _));
        }

        private sealed class FakeResetRegistrationRuntime : IResetRegistrationRuntimePort
        {
            public bool TryResolveCurrentResetOwner(ResetSubjectScope scope, out RuntimeContentOwner owner, out string issue)
            {
                owner = default;
                issue = string.Empty;
                return false;
            }

            public ResetRegistryOperationResult RegisterResetSubject(ResetSubject subject, Object owner, string source, string reason) => default;

            public ResetRegistryOperationResult RegisterRuntimeResetSubject(string authoredPrefix, ResetSubjectScope scope,
                RuntimeContentOwner owner, Object ownerObject, string displayName, string diagnosticTag, string source, string reason) => default;

            public ResetRegistryOperationResult RegisterResetParticipant(ResetRegistrationHandle subjectHandle,
                IResetParticipant participant, Object owner, string source, string reason) => default;

            public ResetRegistryOperationResult UnregisterResetRegistration(ResetRegistrationHandle handle,
                Object owner, string source, string reason) => default;
        }

        private IReadOnlyList<RuntimeContentOwner> CurrentOwners() => new[] { _activity, _route };

        private void Register(RuntimeContentOwner owner, params GameObject[] roots)
        {
            Assert.IsTrue(_registration.TryRegisterOwnerContent(owner, roots, "test", "reset-035-e", out string diagnostic), diagnostic);
        }

        private GameObject Create(string objectName)
        {
            var gameObject = new GameObject(objectName);
            _created.Add(gameObject);
            return gameObject;
        }

        private static Resettable AddResettable(Transform parent, string objectName)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            return gameObject.AddComponent<Resettable>();
        }

        private static void SetMembership(Resettable resettable, ResetMembership membership)
        {
            var serializedObject = new SerializedObject(resettable);
            serializedObject.FindProperty("membership").enumValueIndex = (int)membership;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetMemberMode(ResetComposition composition, ResetCompositionMemberMode mode)
        {
            var serializedObject = new SerializedObject(composition);
            serializedObject.FindProperty("memberMode").enumValueIndex = (int)mode;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetExplicitMembers(ResetComposition composition, params Resettable[] members)
        {
            var serializedObject = new SerializedObject(composition);
            SerializedProperty list = serializedObject.FindProperty("explicitMembers");
            list.arraySize = members.Length;
            for (int index = 0; index < members.Length; index++)
                list.GetArrayElementAtIndex(index).objectReferenceValue = members[index];
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class RecordingRuntime : IResetTargetExecutionRuntimePort
        {
            internal ResetTarget Target { get; private set; }

            public Task<ResetSelectionExecutionRuntimeResult> ExecuteResetTargetAsync(ResetTarget target, string source, string reason)
            {
                Target = target;
                var resolution = ResetSelectionResolution.SucceededResult(
                    ResetSelectionMode.ExplicitSubjects, System.Array.Empty<ResetSubjectId>(), System.Array.Empty<ResetIssue>(),
                    source, reason, "Recorded target.");
                var execution = ResetExecutionResult.SucceededNoSubjects(
                    ResetIssue.Info(ResetIssueKind.InvalidRequest, "Test execution."), source, reason);
                return Task.FromResult(new ResetSelectionExecutionRuntimeResult(resolution, execution));
            }

        }
    }
}
