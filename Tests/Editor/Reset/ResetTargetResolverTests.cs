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
        private RuntimeContentOwner _activity;
        private RuntimeContentOwner _otherActivity;
        private RuntimeContentOwner _route;

        [SetUp]
        public void SetUp()
        {
            _registry = new ResetRegistry();
            _registration = new ResettableOwnerRegistrationRuntime(_registry);
            _activity = RuntimeContentOwner.Activity("reset-035-e.activity", "Activity", RuntimeDefinitionToken.MintAnonymous());
            _otherActivity = RuntimeContentOwner.Activity("reset-035-e.other-activity", "Other Activity", RuntimeDefinitionToken.MintAnonymous());
            _route = RuntimeContentOwner.Route("reset-035-e.route", "Route", RuntimeDefinitionToken.MintAnonymous());
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
        public void CurrentActivityAndCurrentRoute_KeepMembershipAndOwnerFiltering()
        {
            GameObject root = Create("Root");
            Resettable activityMember = AddResettable(root.transform, "Activity");
            Resettable routeActivityMember = AddResettable(root.transform, "RouteActivity");
            Resettable routeMember = AddResettable(root.transform, "Route");
            SetMembership(routeActivityMember, ResetMembership.Activity);
            Register(_activity, activityMember.gameObject);
            Register(_route, routeActivityMember.gameObject, routeMember.gameObject);

            CollectionAssert.AreEquivalent(
                new[] { activityMember.RuntimeSubjectId, routeActivityMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentActivitySubjects(_registry, _activity, _route));
            CollectionAssert.AreEqual(new[] { routeMember.RuntimeSubjectId },
                ResetTargetResolver.ResolveCurrentRouteSubjects(_registry, _route));
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
                ? ResetTarget.ForObject(resettable)
                : ResetTarget.ForComposition(composition);
            var runtime = new RecordingRuntime();
            Assert.IsTrue(trigger.TryBind(runtime, out string bindingIssue), bindingIssue);

            ResetExecutionResult execution = await trigger.RequestResetAsync();

            Assert.AreEqual(ResetExecutionStatus.SucceededNoSubjects, execution.Status);
            Assert.AreEqual(targetKind, runtime.Target.Kind);
            Assert.AreEqual(trigger.Target.Object, runtime.Target.Object);
            Assert.AreEqual(trigger.Target.Composition, runtime.Target.Composition);
            Assert.IsFalse(trigger.IsRequestInFlight);
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

        private sealed class RecordingRuntime : IResetSelectionExecutionRuntimePort
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

            public Task<ResetSelectionExecutionRuntimeResult> ExecuteResetSelectionAsync(ResetSelectionConfig selection, string source, string reason)
            {
                throw new System.NotSupportedException();
            }
        }
    }
}
