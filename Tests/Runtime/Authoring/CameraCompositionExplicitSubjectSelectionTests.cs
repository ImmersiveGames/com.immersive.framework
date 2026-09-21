using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    public sealed class CameraCompositionSubjectSelectionContextTests
    {
        [Test]
        public void EmptySelectionIsStableAndClearIsIdempotent()
        {
            var context = new CameraCompositionSubjectSelectionContext(
                new CameraCompositionSubjectSelectionContextId("selection-empty"));
            int events = 0;
            context.SelectionChanged += _ => events++;

            Assert.That(context.Revision, Is.Zero);
            Assert.That(context.CurrentSnapshot.Count, Is.Zero);
            Assert.That(context.CurrentSnapshot.Revision, Is.Zero);
            Assert.That(context.CurrentSnapshot.ContextId, Is.EqualTo(context.ContextId));

            CameraCompositionSubjectSelectionResult cleared = context.Clear();
            Assert.That(cleared.Status, Is.EqualTo(
                CameraCompositionSubjectSelectionStatus.SucceededUnchanged));
            Assert.That(context.Revision, Is.Zero);
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void ReplaceSameSetIsIdempotentAcrossOrderAndClearResetsOnce()
        {
            var context = new CameraCompositionSubjectSelectionContext(
                new CameraCompositionSubjectSelectionContextId("selection-idempotent"));
            int events = 0;
            context.SelectionChanged += _ => events++;
            CameraSubjectId alpha = new CameraSubjectId("alpha");
            CameraSubjectId beta = new CameraSubjectId("beta");

            CameraCompositionSubjectSelectionResult first =
                context.Replace(new[] { beta, alpha });
            Assert.That(first.Status, Is.EqualTo(
                CameraCompositionSubjectSelectionStatus.SucceededChanged));
            Assert.That(context.Revision, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(context.CurrentSnapshot.SubjectIds.Select(id => id.Value),
                Is.EqualTo(new[] { "alpha", "beta" }));

            CameraCompositionSubjectSelectionResult same =
                context.Replace(new[] { alpha, beta });
            Assert.That(same.Status, Is.EqualTo(
                CameraCompositionSubjectSelectionStatus.SucceededUnchanged));
            Assert.That(context.Revision, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(1));

            CameraCompositionSubjectSelectionResult replaced =
                context.Replace(new[] { new CameraSubjectId("gamma") });
            Assert.That(replaced.Status, Is.EqualTo(
                CameraCompositionSubjectSelectionStatus.SucceededChanged));
            Assert.That(context.Revision, Is.EqualTo(2));
            Assert.That(context.CurrentSnapshot.SubjectIds.Select(id => id.Value),
                Is.EqualTo(new[] { "gamma" }));
            Assert.That(events, Is.EqualTo(2));

            Assert.That(context.Clear().Status, Is.EqualTo(
                CameraCompositionSubjectSelectionStatus.SucceededChanged));
            Assert.That(context.Revision, Is.EqualTo(3));
            Assert.That(context.CurrentSnapshot.Count, Is.Zero);
            Assert.That(context.Clear().Status, Is.EqualTo(
                CameraCompositionSubjectSelectionStatus.SucceededUnchanged));
            Assert.That(context.Revision, Is.EqualTo(3));
            Assert.That(events, Is.EqualTo(3));
        }

        [Test]
        public void InvalidOrDuplicateSubjectIdsDoNotMutateSelection()
        {
            var context = new CameraCompositionSubjectSelectionContext(
                new CameraCompositionSubjectSelectionContextId("selection-invalid"));
            context.Replace(new[] { new CameraSubjectId("kept") });
            int revision = context.Revision;
            int events = 0;
            context.SelectionChanged += _ => events++;

            Assert.That(context.Replace(new[] { default, new CameraSubjectId("other") }).Status,
                Is.EqualTo(CameraCompositionSubjectSelectionStatus.RejectedInvalidRequest));
            Assert.That(context.Replace(new[]
            {
                new CameraSubjectId("kept"),
                new CameraSubjectId("kept")
            }).Status, Is.EqualTo(CameraCompositionSubjectSelectionStatus.RejectedInvalidRequest));
            Assert.That(context.Revision, Is.EqualTo(revision));
            Assert.That(context.CurrentSnapshot.SubjectIds.Select(id => id.Value),
                Is.EqualTo(new[] { "kept" }));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void SelectionContractsDoNotExposePlayerConcepts()
        {
            Type[] types =
            {
                typeof(ICameraCompositionSubjectSelectionSource),
                typeof(CameraCompositionSubjectSelectionSnapshot),
                typeof(CameraCompositionSubjectSelectionContext),
                typeof(CameraSharedCompositionSubjectPolicyKind)
            };
            foreach (Type type in types)
            {
                MemberInfo[] members = type.GetMembers(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly);
                foreach (MemberInfo member in members)
                {
                    Assert.That(member.Name, Does.Not.Contain("Player").IgnoreCase, type.Name);
                    Assert.That(member.Name, Does.Not.Contain("Slot").IgnoreCase, type.Name);
                }
            }
        }
    }

    public sealed class CameraCompositionExplicitSubjectSelectionTests
    {
        [Test]
        public void AllAvailableSubjectsIgnoresExplicitSelection()
        {
            using var fixture = new Fixture(CameraRigPresentationIntent.Group);
            fixture.AddSubject("subject-b");
            fixture.AddSubject("subject-a");
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(2));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);

            var selection = new CameraCompositionSubjectSelectionContext(
                new CameraCompositionSubjectSelectionContextId("ignored-selection"));
            selection.Replace(new[] { new CameraSubjectId("subject-a") });
            fixture.Composition.AttachSubjectSelectionSource(selection);
            CameraSharedCompositionSnapshot reconciled = fixture.Composition.Reconcile(
                fixture.Availability.CreateSnapshot(),
                selection.CurrentSnapshot);

            Assert.That(reconciled.LastReconcileStatus, Is.EqualTo(
                CameraSharedCompositionReconcileStatus.SucceededNoChange));
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(2));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
        }

        [Test]
        public void ExplicitSelectionUsesOnlyTheSelectedSubject()
        {
            using var fixture = Fixture.StartExplicit();
            Transform selected = fixture.AddSubject("subject-a");
            Transform ignored = fixture.AddSubject("subject-b");
            fixture.Selection.Replace(new[] { new CameraSubjectId("subject-a") });

            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.SameAs(selected));
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.Not.SameAs(ignored));
            Assert.That(fixture.Context.Winner.RequestId, Is.EqualTo(fixture.Composition.RequestId));
        }

        [Test]
        public void MissingSelectedSubjectIsNotReplacedByAnotherAvailableSubject()
        {
            using var fixture = Fixture.StartExplicit();
            Transform other = fixture.AddSubject("subject-b");
            fixture.Selection.Replace(new[] { new CameraSubjectId("subject-a") });

            Assert.That(fixture.Composition.Snapshot.LastReconcileStatus, Is.EqualTo(
                CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects));
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.Composition.IsRequestPublished, Is.False);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.Not.SameAs(other));
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.Null);
        }

        [Test]
        public void SelectedSubjectDisappearanceReleasesRequestWithoutSubstitution()
        {
            using var fixture = Fixture.StartExplicit();
            CameraSubjectAvailabilityToken selected = fixture.AddSubjectToken("subject-a");
            fixture.Selection.Replace(new[] { new CameraSubjectId("subject-a") });
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);

            fixture.RemoveSubject(selected);
            Transform intruder = fixture.AddSubject("subject-b");

            Assert.That(fixture.Composition.Snapshot.LastReconcileStatus, Is.EqualTo(
                CameraSharedCompositionReconcileStatus.SucceededAwaitingSubjects));
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.Composition.IsRequestPublished, Is.False);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.Not.SameAs(intruder));
            Assert.That(fixture.Selection.CurrentSnapshot.SubjectIds.Select(id => id.Value),
                Is.EqualTo(new[] { "subject-a" }));
        }

        [Test]
        public void FreshOccurrenceReplacesStaleOccurrenceAndStaleSelectionIsRejected()
        {
            using var fixture = Fixture.StartExplicit();
            CameraSubjectAvailabilityToken first = fixture.AddSubjectToken("occ-1");
            fixture.Selection.Replace(new[] { new CameraSubjectId("occ-1") });
            CameraCompositionSubjectSelectionSnapshot stale =
                fixture.Selection.CurrentSnapshot;
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);

            fixture.RemoveSubject(first);
            Transform fresh = fixture.AddSubject("occ-2");
            fixture.Selection.Replace(new[] { new CameraSubjectId("occ-2") });
            int membershipRevision = fixture.Composition.Snapshot.MembershipRevision;

            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.SameAs(fresh));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);

            CameraSharedCompositionSnapshot rejected = fixture.Composition.Reconcile(
                fixture.Availability.CreateSnapshot(),
                stale);
            Assert.That(rejected.LastReconcileStatus, Is.EqualTo(
                CameraSharedCompositionReconcileStatus.RejectedStaleSubjectSelectionSnapshot));
            Assert.That(fixture.Composition.Snapshot.MembershipRevision, Is.EqualTo(membershipRevision));
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.SameAs(fresh));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
        }

        [Test]
        public void MissingSelectionSourceDoesNotFallBackToAllAvailableSubjects()
        {
            using var fixture = new Fixture(
                CameraRigPresentationIntent.ThirdPerson,
                CameraSharedCompositionSubjectPolicyKind.ExplicitSelection,
                activate: false);
            fixture.AddSubject("subject-a");
            fixture.AddSubject("subject-b");
            fixture.Activate();

            Assert.That(fixture.Composition.Snapshot.LastReconcileStatus, Is.EqualTo(
                CameraSharedCompositionReconcileStatus.BlockedMissingSubjectSelection));
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.Zero);
            Assert.That(fixture.Composition.IsRequestPublished, Is.False);
            Assert.That(fixture.Context.AdmittedRequestCount, Is.Zero);
            Assert.That(fixture.Applicator.HasAppliedDefault, Is.True);

            fixture.Selection.Replace(new[] { new CameraSubjectId("subject-a") });
            fixture.Composition.AttachSubjectSelectionSource(fixture.Selection);
            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow,
                Is.SameAs(fixture.SubjectRoot("subject-a").transform));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
        }

        [Test]
        public void ExplicitCompositionStartsWhenDependenciesArriveInAnyOrder()
        {
            using var fixture = new Fixture(
                CameraRigPresentationIntent.ThirdPerson,
                CameraSharedCompositionSubjectPolicyKind.ExplicitSelection,
                activate: false,
                attachDependencies: false);
            fixture.Selection.Replace(new[] { new CameraSubjectId("subject-a") });
            fixture.Composition.AttachSubjectSelectionSource(fixture.Selection);
            fixture.AttachOutput();
            fixture.AttachAvailability();
            fixture.AddSubject("subject-b");
            Transform selected = fixture.AddSubject("subject-a");
            fixture.Activate();

            Assert.That(fixture.Composition.Snapshot.SubjectCount, Is.EqualTo(1));
            Assert.That(fixture.CompositionRig.CinemachineCamera.Follow, Is.SameAs(selected));
            Assert.That(fixture.Composition.IsRequestPublished, Is.True);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
            private readonly Dictionary<string, GameObject> _subjects = new Dictionary<string, GameObject>();
            private readonly CameraSubjectAvailabilityOwnerId _subjectOwner;
            private readonly bool _attachDependencies;

            internal Fixture(
                CameraRigPresentationIntent intent,
                CameraSharedCompositionSubjectPolicyKind policy =
                    CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects,
                bool activate = true,
                bool attachDependencies = true)
            {
                _attachDependencies = attachDependencies;
                OutputDefinition = CreateDefinition<CameraOutputDefinition>();
                DefaultRig = CreateRig(CameraRigPresentationIntent.Fixed);
                CompositionRig = CreateRig(intent);
                Availability = new CameraSubjectAvailabilityContext(
                    new SubjectAvailabilityContextId("explicit-selection-availability"));
                Selection = new CameraCompositionSubjectSelectionContext(
                    new CameraCompositionSubjectSelectionContextId("explicit-selection"));
                _subjectOwner = new CameraSubjectAvailabilityOwnerId("explicit-selection-owner");

                GameObject outputRoot = CreateRoot("output");
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
                Assert.That(Session.Synchronize().Succeeded, Is.True, "Output session did not synchronize.");
                SetField(Output, "_context", Context);
                SetField(Output, "_applicator", Applicator);
                SetField(Output, "_session", Session);
                SetField(Output, "_initializedDefinition", OutputDefinition);

                GameObject compositionRoot = CreateRoot("composition");
                compositionRoot.SetActive(false);
                Composition = compositionRoot.AddComponent<CameraSharedComposition>();
                SetField(Composition, "compositionRig", CompositionRig);
                SetField(Composition, "requestPrecedence", 10);
                Composition.Configure(OutputDefinition, policy);
                if (_attachDependencies)
                {
                    AttachOutput();
                    AttachAvailability();
                }

                if (activate)
                {
                    Activate();
                }
            }

            internal CameraOutputDefinition OutputDefinition { get; }
            internal CameraRigComposer DefaultRig { get; }
            internal CameraRigComposer CompositionRig { get; }
            internal CameraOutputAuthoring Output { get; }
            internal CameraOutputContext Context { get; }
            internal CameraOutputRigApplicator Applicator { get; }
            internal CameraOutputSession Session { get; }
            internal CameraSubjectAvailabilityContext Availability { get; }
            internal CameraCompositionSubjectSelectionContext Selection { get; }
            internal CameraSharedComposition Composition { get; }

            internal static Fixture StartExplicit()
            {
                var fixture = new Fixture(
                    CameraRigPresentationIntent.ThirdPerson,
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection,
                    activate: false);
                fixture.Composition.AttachSubjectSelectionSource(fixture.Selection);
                fixture.Activate();
                return fixture;
            }

            internal void AttachOutput() => Composition.AttachOutputSession(Output);

            internal void AttachAvailability() =>
                Composition.AttachCameraSubjectAvailability(Availability);

            internal void Activate() => Composition.gameObject.SetActive(true);

            internal Transform AddSubject(string id) => SubjectRoot(id).transform;

            internal GameObject SubjectRoot(string id)
            {
                if (_subjects.TryGetValue(id, out GameObject existing))
                {
                    return existing;
                }

                GameObject subject = CreateRoot(id);
                _subjects.Add(id, subject);
                CameraSubjectAvailabilityResult result = Availability.TryMakeAvailable(
                    new CameraSubject(new CameraSubjectId(id), subject.transform, id),
                    _subjectOwner);
                Assert.That(result.Succeeded, Is.True, result.Message);
                return subject;
            }

            internal CameraSubjectAvailabilityToken AddSubjectToken(string id)
            {
                SubjectRoot(id);
                Assert.That(Availability.CreateSnapshot().TryGet(
                    new CameraSubjectId(id), out CameraSubjectAvailabilityEntry entry), Is.True);
                return entry.Token;
            }

            internal void RemoveSubject(CameraSubjectAvailabilityToken token)
            {
                CameraSubjectAvailabilityResult result = Availability.TryMakeUnavailable(token);
                Assert.That(result.Succeeded, Is.True, result.Message);
            }

            internal CameraRigComposer CreateRig(CameraRigPresentationIntent intent)
            {
                GameObject root = CreateRoot($"rig-{intent}");
                var composer = root.AddComponent<CameraRigComposer>();
                CameraRigBehaviorDefinition behavior = intent switch
                {
                    CameraRigPresentationIntent.Fixed =>
                        ScriptableObject.CreateInstance<FixedCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Follow =>
                        ScriptableObject.CreateInstance<FollowCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Mounted =>
                        ScriptableObject.CreateInstance<MountedCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.ThirdPerson =>
                        ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Group =>
                        ScriptableObject.CreateInstance<GroupCameraRigBehaviorDefinition>(),
                    _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, null)
                };
                _created.Add(behavior);
                SetField(composer, "behaviorDefinition", behavior);
                GameObject cameraRoot = CreateRoot($"camera-{intent}");
                cameraRoot.transform.SetParent(root.transform, false);
                CinemachineCamera camera = cameraRoot.AddComponent<CinemachineCamera>();
                SetField(composer, "cinemachineCamera", camera);
                if (intent == CameraRigPresentationIntent.Group)
                {
                    GameObject groupRoot = CreateRoot("group");
                    groupRoot.transform.SetParent(root.transform, false);
                    CinemachineTargetGroup group = groupRoot.AddComponent<CinemachineTargetGroup>();
                    CinemachineGroupFraming framing = cameraRoot.AddComponent<CinemachineGroupFraming>();
                    SetField(composer, "frameworkOwnedGroupTargetGroup", group);
                    SetField(composer, "frameworkOwnedGroupFraming", framing);
                }

                return composer;
            }

            private T CreateDefinition<T>() where T : ScriptableObject
            {
                T definition = ScriptableObject.CreateInstance<T>();
                _created.Add(definition);
                SetField(definition, "stableId", Guid.NewGuid().ToString("N"));
                return definition;
            }

            private GameObject CreateRoot(string name)
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
                    {
                        UnityEngine.Object.DestroyImmediate(_created[index]);
                    }
                }
            }

            private static void SetField(object target, string name, object value) =>
                target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(target, value);
        }
    }
}
