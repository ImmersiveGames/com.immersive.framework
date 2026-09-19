using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    public sealed class CameraCompositionRequestParticipationTests
    {
        [Test]
        public void FollowLifecycleMovesDefaultToCompositionAndBackWithNewOccurrence()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Follow);
            AssertDefault(fixture);

            CameraSubjectAvailabilityToken first = fixture.AddSubject("subject-a");
            CameraRequestId requestId = fixture.Composition.RequestId;
            AssertComposition(fixture, 1);

            fixture.RemoveSubject(first);
            AssertDefault(fixture);

            CameraSubjectAvailabilityToken second = fixture.AddSubject("subject-a");
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(fixture.Composition.RequestId, Is.EqualTo(requestId));
            AssertComposition(fixture, 1);
        }

        [Test]
        public void GroupTransitionsOneTwoOneKeepSingleRequestAndZeroReleasesIt()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Group);
            CameraSubjectAvailabilityToken first = fixture.AddSubject("subject-a");
            CameraRequestId requestId = fixture.Composition.RequestId;
            AssertComposition(fixture, 1);

            CameraSubjectAvailabilityToken second = fixture.AddSubject("subject-b");
            AssertComposition(fixture, 2);
            Assert.That(fixture.Composition.RequestId, Is.EqualTo(requestId));

            fixture.RemoveSubject(first);
            AssertComposition(fixture, 1);
            Assert.That(fixture.Composition.RequestId, Is.EqualTo(requestId));

            fixture.RemoveSubject(second);
            AssertDefault(fixture);
        }

        [Test]
        public void HigherPrecedenceRequestPreemptsAndReleaseRestoresComposition()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Follow);
            fixture.AddSubject("subject-a");
            CameraRequest higher = fixture.CreateRequest("higher", fixture.CreateRig(CameraRigPresentationIntent.Fixed), 100);

            Assert.That(fixture.Session.Admit(higher).Succeeded, Is.True);
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(higher.RequestId));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.EqualTo(2));

            Assert.That(fixture.Session.Release(higher.RequestId).Succeeded, Is.True);
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(fixture.Composition.RequestId));
            Assert.That(fixture.Applicator.AppliedCamera, Is.SameAs(fixture.CompositionRig.CinemachineCamera));
        }

        [Test]
        public void ForceDefaultDoesNotRemoveCompositionAndReleaseRestoresWinner()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Follow);
            fixture.AddSubject("subject-a");
            var owner = new CameraOutputForceDefaultOwnerId("test-force-default");

            Assert.That(fixture.Session.ForceDefault(owner).Succeeded, Is.True);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);

            Assert.That(fixture.Session.ReleaseForceDefault(owner).Succeeded, Is.True);
            Assert.That(fixture.Applicator.AppliedCamera, Is.SameAs(fixture.CompositionRig.CinemachineCamera));
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(fixture.Composition.RequestId));
        }

        [Test]
        public void TwoCompositionsOnSameOutputUseNormalArbitration()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Fixed);
            CameraRigComposer secondRig = fixture.CreateRig(CameraRigPresentationIntent.Fixed);
            CameraSharedComposition second = fixture.CreateComposition("second", secondRig, 20);

            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
            Assert.That(second.IsRequestPublished, Is.True);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.EqualTo(2));
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(second.RequestId));
            second.gameObject.SetActive(false);
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(fixture.Composition.RequestId));
        }

        [Test]
        public void CompositionRequestsRemainIsolatedPerOutput()
        {
            using var first = new Fixture(CameraRigPresentationIntent.Fixed, "first");
            using var second = new Fixture(CameraRigPresentationIntent.Fixed, "second");
            Assert.That(first.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(second.Context.AdmittedRequestCount, Is.EqualTo(1));
            Assert.That(first.Context.Winner.OutputId, Is.EqualTo(first.Context.OutputId));
            Assert.That(second.Context.Winner.OutputId, Is.EqualTo(second.Context.OutputId));
            Assert.That(first.Context.OutputId, Is.Not.EqualTo(second.Context.OutputId));
        }

        [Test]
        public void CompositionRigEqualToDefaultRigBlocksBeforePublication()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Fixed, configureComposition: false);
            fixture.ConfigureComposition(fixture.DefaultRig);
            Assert.That(fixture.Composition.Snapshot.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.BlockedCompositionRigIsDefault));
            Assert.That(fixture.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);
        }

        [Test]
        public void TeardownReleasesOnlyCompositionRequestAndClearsItsPresentation()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Follow);
            fixture.AddSubject("subject-a");
            fixture.Composition.gameObject.SetActive(false);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.Null);
            Assert.That(fixture.Composition.IsRequestPublished, Is.False);
        }

        [Test]
        public void StaleMembershipEvidenceCannotReactivateReleasedRequest()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Follow);
            CameraSubjectAvailabilityToken token = fixture.AddSubject("subject-a");
            CameraSubjectAvailabilitySnapshot stale = fixture.Availability.CreateSnapshot();
            fixture.RemoveSubject(token);
            AssertDefault(fixture);

            CameraSharedCompositionSnapshot result = fixture.Composition.Reconcile(stale);
            Assert.That(result.LastReconcileStatus,
                Is.EqualTo(CameraSharedCompositionReconcileStatus.RejectedStaleAvailabilitySnapshot));
            Assert.That(fixture.Composition.IsRequestPublished, Is.False);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.Zero);
        }

        [Test]
        public void FixedWithZeroSubjectsPublishesWithoutFakeTarget()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Fixed);
            AssertComposition(fixture, 1);
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.Null);
            Assert.That(fixture.CompositionRig.CinemachineCamera.LookAt, Is.Null);
            Assert.That(fixture.Context.Winner.TargetSource.Kind,
                Is.EqualTo(CameraTargetSourceKind.Composition));
            Assert.That(fixture.Context.Winner.TargetSource.HasSourceObject, Is.False);
        }

        [TestCase(CameraRigPresentationIntent.Follow)]
        [TestCase(CameraRigPresentationIntent.Mounted)]
        [TestCase(CameraRigPresentationIntent.ThirdPerson)]
        public void SingleTargetIntentsUseExistingPresentabilityContract(
            CameraRigPresentationIntent intent)
        {
            using var fixture = new Fixture(intent);
            AssertDefault(fixture);

            fixture.AddSubject("subject-a");

            AssertComposition(fixture, 1);
        }

        [Test]
        public void CompositionRequestUsesDedicatedOwnerLifetimeAndDiagnosticTargetSource()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Fixed);
            CameraRequest request = fixture.Context.Winner;
            Assert.That(request.Owner.Kind, Is.EqualTo(CameraRequestOwnerKind.Composition));
            Assert.That(request.Lifetime.Kind, Is.EqualTo(CameraRequestLifetimeKind.Composition));
            Assert.That(request.TargetSource.Kind, Is.EqualTo(CameraTargetSourceKind.Composition));
            Assert.That(request.Rig.Composer, Is.SameAs(fixture.CompositionRig));
            Assert.That(request.OutputId, Is.EqualTo(fixture.Context.OutputId));
            Assert.That(request.Owner.OwnerScopeId.Value,
                Is.EqualTo(fixture.Composition.MembershipContextIdText));
            Assert.That(request.Lifetime.ScopeId.Value,
                Is.EqualTo(fixture.Composition.MembershipContextIdText));
            Assert.That(request.TargetSource.LogicalSourceId,
                Is.EqualTo(fixture.Composition.MembershipContextIdText));
            Assert.That(request.TargetSource.LogicalSourceId,
                Is.Not.EqualTo(fixture.Composition.ViewIdText));
        }

        private static void AssertDefault(Fixture fixture)
        {
            Assert.That(fixture.Composition.IsRequestPublished, Is.False);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);
            Assert.That(fixture.Applicator.AppliedCamera, Is.SameAs(fixture.DefaultRig.CinemachineCamera));
        }

        private static void AssertComposition(Fixture fixture, int admittedCount)
        {
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.EqualTo(admittedCount));
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(fixture.Composition.RequestId));
            Assert.That(fixture.Applicator.AppliedCamera, Is.SameAs(fixture.CompositionRig.CinemachineCamera));
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
            private readonly CameraSubjectAvailabilityOwnerId _subjectOwner;
            private readonly string _suffix;

            internal Fixture(
                CameraRigPresentationIntent intent,
                string suffix = "fixture",
                bool configureComposition = true)
            {
                _suffix = suffix;
                OutputDefinition = Definition<CameraOutputDefinition>();
                ViewDefinition = Definition<CameraViewDefinition>();
                DefaultRig = CreateRig(CameraRigPresentationIntent.Fixed);
                CompositionRig = CreateRig(intent);

                var outputRoot = Root($"output-{suffix}");
                UnityEngine.Camera unityCamera = outputRoot.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = outputRoot.AddComponent<CinemachineBrain>();
                Output = outputRoot.AddComponent<CameraOutputAuthoring>();
                SetField(Output, "outputDefinition", OutputDefinition);
                SetField(Output, "unityCamera", unityCamera);
                SetField(Output, "cinemachineBrain", brain);
                SetField(Output, "defaultCameraRig", DefaultRig);
                SetField(Output, "initializeOnAwake", false);

                Context = new CameraOutputContext(OutputDefinition.OutputId);
                Applicator = new CameraOutputRigApplicator(
                    new CameraOutputBinding(OutputDefinition.OutputId, unityCamera, brain));
                Session = new CameraOutputSession(
                    Context, Applicator, CameraRigReference.FromComposer(DefaultRig));
                Assert.That(Session.Synchronize().Succeeded, Is.True);
                SetField(Output, "_context", Context);
                SetField(Output, "_applicator", Applicator);
                SetField(Output, "_session", Session);
                SetField(Output, "_initializedDefinition", OutputDefinition);

                Availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId($"availability-{suffix}"));
                _subjectOwner = new CameraSubjectAvailabilityOwnerId($"subject-owner-{suffix}");

                GameObject compositionRoot = Root($"composition-{suffix}");
                compositionRoot.SetActive(false);
                Composition = compositionRoot.AddComponent<CameraSharedComposition>();
                SetField(Composition, "compositionRig", CompositionRig);
                SetField(Composition, "requestPrecedence", 10);
                Composition.Configure(ViewDefinition, OutputDefinition,
                    CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
                Composition.AttachOutputSession(Output);
                Composition.AttachCameraSubjectAvailability(Availability);
                if (configureComposition) compositionRoot.SetActive(true);
            }

            internal CameraOutputDefinition OutputDefinition { get; }
            internal CameraViewDefinition ViewDefinition { get; }
            internal CameraRigComposer DefaultRig { get; }
            internal CameraRigComposer CompositionRig { get; }
            internal CameraOutputAuthoring Output { get; }
            internal CameraOutputContext Context { get; }
            internal CameraOutputRigApplicator Applicator { get; }
            internal CameraOutputSession Session { get; }
            internal CameraSubjectAvailabilityContext Availability { get; }
            internal CameraSharedComposition Composition { get; }

            internal void ConfigureComposition(CameraRigComposer rig)
            {
                SetField(Composition, "compositionRig", rig);
                Composition.gameObject.SetActive(true);
            }

            internal CameraSubjectAvailabilityToken AddSubject(string id)
            {
                GameObject subject = Root($"{_suffix}-{id}-{Guid.NewGuid():N}");
                CameraSubjectAvailabilityResult result = Availability.TryMakeAvailable(
                    new CameraSubject(new CameraSubjectId(id), subject.transform, id), _subjectOwner);
                Assert.That(result.Succeeded, Is.True, result.Message);
                return result.Token;
            }

            internal void RemoveSubject(CameraSubjectAvailabilityToken token)
            {
                CameraSubjectAvailabilityResult result = Availability.TryMakeUnavailable(token);
                Assert.That(result.Succeeded, Is.True, result.Message);
            }

            internal CameraRigComposer CreateRig(CameraRigPresentationIntent intent)
            {
                GameObject root = Root($"rig-{intent}-{Guid.NewGuid():N}");
                var composer = root.AddComponent<CameraRigComposer>();
                CameraRigBehaviorDefinition behavior = intent switch
                {
                    CameraRigPresentationIntent.Fixed => ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Follow => ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Mounted => ScriptableObject.CreateInstance<MountedCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.ThirdPerson => ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Group => ScriptableObject.CreateInstance<GroupCameraRigBehaviorDefinition>(),
                    _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, null)
                };
                _created.Add(behavior);
                SetField(composer, "behaviorDefinition", behavior);
                GameObject cameraRoot = Root($"camera-{intent}-{Guid.NewGuid():N}");
                cameraRoot.transform.SetParent(root.transform, false);
                CinemachineCamera camera = cameraRoot.AddComponent<CinemachineCamera>();
                SetField(composer, "cinemachineCamera", camera);
                if (intent == CameraRigPresentationIntent.Group)
                {
                    GameObject groupRoot = Root($"group-{Guid.NewGuid():N}");
                    groupRoot.transform.SetParent(root.transform, false);
                    CinemachineTargetGroup group = groupRoot.AddComponent<CinemachineTargetGroup>();
                    CinemachineGroupFraming framing = cameraRoot.AddComponent<CinemachineGroupFraming>();
                    SetField(composer, "frameworkOwnedGroupTargetGroup", group);
                    SetField(composer, "frameworkOwnedGroupFraming", framing);
                }
                return composer;
            }

            internal CameraRequest CreateRequest(string id, CameraRigComposer rig, int precedence)
            {
                CameraRequestCreateResult result = CameraRequestCreateResult.Create(
                    new CameraRequestId($"{_suffix}-{id}"), Context.OutputId,
                    new CameraRequestOwner(CameraRequestOwnerKind.Activity,
                        new CameraRequestOwnerScopeId($"{_suffix}-{id}-owner")),
                    new CameraRequestLifetime(CameraRequestLifetimeKind.Activity,
                        new CameraRequestLifetimeScopeId($"{_suffix}-{id}-lifetime")),
                    CameraRigReference.FromComposer(rig),
                    CameraTargetSourceDescriptor.Logical(CameraTargetSourceKind.Activity,
                        $"{_suffix}-{id}-target"),
                    new CameraRequestPolicy(precedence, $"{_suffix}-{id}-tie"),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(CameraCompositionRequestParticipationTests), id);
                Assert.That(result.IsSucceeded, Is.True, result.BlockingIssue);
                return result.Request;
            }

            internal CameraSharedComposition CreateComposition(
                string id, CameraRigComposer rig, int precedence)
            {
                CameraViewDefinition view = Definition<CameraViewDefinition>();
                var availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId($"{_suffix}-{id}-availability"));
                GameObject root = Root($"{_suffix}-{id}-composition");
                root.SetActive(false);
                var composition = root.AddComponent<CameraSharedComposition>();
                SetField(composition, "compositionRig", rig);
                SetField(composition, "requestPrecedence", precedence);
                composition.Configure(view, OutputDefinition,
                    CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
                composition.AttachOutputSession(Output);
                composition.AttachCameraSubjectAvailability(availability);
                root.SetActive(true);
                return composition;
            }

            private T Definition<T>() where T : ScriptableObject
            {
                T definition = ScriptableObject.CreateInstance<T>();
                _created.Add(definition);
                SetField(definition, "stableId", Guid.NewGuid().ToString("N"));
                return definition;
            }

            private GameObject Root(string name)
            {
                var root = new GameObject(name);
                _created.Add(root);
                return root;
            }

            public void Dispose()
            {
                for (int index = _created.Count - 1; index >= 0; index--)
                {
                    if (_created[index] != null)
                        UnityEngine.Object.DestroyImmediate(_created[index]);
                }
            }
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{name}' on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }
}
