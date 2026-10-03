using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraAssignmentIdentityTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) UnityEngine.Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void StableIds_UseValueEquality()
        {
            Assert.That(new SessionCameraAssignmentId("assignment.a"), Is.EqualTo(new SessionCameraAssignmentId("assignment.a")));
            Assert.That(new CameraOutputId("output.a"), Is.EqualTo(new CameraOutputId("output.a")));
        }

        [Test]
        public void NewAssignmentAssets_GenerateUniqueStableIdAndKeepItWhenReordered()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var first = ScriptableObject.CreateInstance<SessionCameraAssignmentAsset>();
            var second = ScriptableObject.CreateInstance<SessionCameraAssignmentAsset>();
            _created.Add(first);
            _created.Add(second);
            string firstId = (string)typeof(SessionCameraAssignmentAsset)
                .GetField("assignmentId", flags).GetValue(first);
            string secondId = (string)typeof(SessionCameraAssignmentAsset)
                .GetField("assignmentId", flags).GetValue(second);

            Assert.That(Guid.TryParseExact(firstId, "N", out _), Is.True);
            Assert.That(Guid.TryParseExact(secondId, "N", out _), Is.True);
            Assert.That(firstId, Is.Not.EqualTo(secondId));

            typeof(SessionCameraAssignmentAsset)
                .GetField("targetPolicy", flags)
                .SetValue(first, CameraTargetPolicy.MemberActorTargets);
            Assert.That(typeof(SessionCameraAssignmentAsset)
                .GetField("assignmentId", flags).GetValue(first),
                Is.EqualTo(firstId));

            var assignments = new List<SessionCameraAssignmentAsset>
            {
                first,
                second
            };
            assignments.Reverse();

            Assert.That(assignments[1], Is.SameAs(first));
            Assert.That(typeof(SessionCameraAssignmentAsset)
                .GetField("assignmentId", flags).GetValue(assignments[1]),
                Is.EqualTo(firstId));
            Assert.That(typeof(SessionCameraAssignmentAsset)
                .GetField("assignmentId", flags).GetValue(assignments[0]),
                Is.EqualTo(secondId));
        }

        [Test]
        public void IndividualIdentity_DiffersForExactPlayerOccurrences()
        {
            var assignment = new SessionCameraAssignmentId("assignment.a");
            var output = new CameraOutputId("output.a");
            var first = CameraOccurrenceIdentity.ForIndividual(assignment, new PlayerOccurrenceId("join.1"), output);
            var rejoined = CameraOccurrenceIdentity.ForIndividual(assignment, new PlayerOccurrenceId("join.2"), output);
            Assert.That(first, Is.Not.EqualTo(rejoined));
            Assert.That(first, Is.EqualTo(CameraOccurrenceIdentity.ForIndividual(assignment, new PlayerOccurrenceId("join.1"), output)));
        }

        [Test]
        public void SessionAndSharedIdentity_IsIndependentOfPlayer()
        {
            var occurrence = CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.a"), new CameraOutputId("output.a"));
            Assert.That(occurrence.IsIndividual, Is.False);
            Assert.That(occurrence.PlayerOccurrenceId.IsValid, Is.False);
        }

        [Test]
        public void ReusedRigPrefab_DoesNotShareOccurrenceIdentityOrStateAcrossAssignments()
        {
            var firstAssignment = CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.a"), new CameraOutputId("output.a"));
            var secondAssignment = CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.b"), new CameraOutputId("output.a"));
            Assert.That(firstAssignment, Is.Not.EqualTo(secondAssignment));

            var occurrenceState = new Dictionary<CameraOccurrenceIdentity, string>
            {
                [firstAssignment] = "first-state"
            };
            Assert.That(occurrenceState.ContainsKey(secondAssignment), Is.False);
            occurrenceState[secondAssignment] = "second-state";
            Assert.That(occurrenceState[firstAssignment], Is.EqualTo("first-state"));
            Assert.That(occurrenceState[secondAssignment], Is.EqualTo("second-state"));
        }

        [Test]
        public void Assignment_RejectsInvalidPolicyCombinationsAndDuplicateOutputs()
        {
            var output = new CameraOutputMapping(new CameraOutputId("output.a"));
            var individualWithoutMembership = new SessionCameraAssignment(
                new SessionCameraAssignmentId("assignment.a"),
                CameraOccurrenceMode.IndividualPerPlayer, CameraMembershipPolicy.None,
                CameraTargetPolicy.NoSubject, new[] { output });
            Assert.That(individualWithoutMembership.TryValidate(out _), Is.False);

            var duplicateOutputs = new SessionCameraAssignment(
                new SessionCameraAssignmentId("assignment.b"),
                CameraOccurrenceMode.SharedGroup, CameraMembershipPolicy.ExplicitPlayerSlots,
                CameraTargetPolicy.MemberActorTargets, new[] { output, output }, new[] { PlayerSlotId.Player1 });
            Assert.That(duplicateOutputs.TryValidate(out _), Is.False);
        }

        [Test]
        public void ModeDoesNotImplyMembershipOrTargetPolicy()
        {
            var assignment = new SessionCameraAssignment(
                new SessionCameraAssignmentId("assignment.a"),
                CameraOccurrenceMode.SessionScoped, CameraMembershipPolicy.None,
                CameraTargetPolicy.NoSubject, new[] { new CameraOutputMapping(new CameraOutputId("output.a")) });
            Assert.That(assignment.TryValidate(out _), Is.True);
            Assert.That(assignment.MembershipPolicy, Is.EqualTo(CameraMembershipPolicy.None));
            Assert.That(assignment.TargetPolicy, Is.EqualTo(CameraTargetPolicy.NoSubject));
        }

        [Test]
        public void OccurrenceFactories_RejectMissingRequiredIdentity()
        {
            Assert.Throws<ArgumentException>(() => CameraOccurrenceIdentity.ForIndividual(
                new SessionCameraAssignmentId("assignment.a"), default, new CameraOutputId("output.a")));
            Assert.Throws<ArgumentException>(() => CameraOccurrenceIdentity.ForSessionOrShared(
                default, new CameraOutputId("output.a")));
        }

        [Test]
        public void RouteAndActivityHaveNoCameraSelectionAuthority()
        {
            Assert.That(DeclaresCameraSelection(typeof(RouteAsset)), Is.False);
            Assert.That(DeclaresCameraSelection(typeof(ActivityAsset)), Is.False);
        }

        [Test]
        public void AssignmentOutputHasNoPresentationOrRequestSelectionSeam()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Assert.That(typeof(CameraOutputAuthoring).GetProperty("Context", flags), Is.Null);
            Assert.That(typeof(CameraOutputSession).GetMethod("Admit", flags), Is.Null);
            Assert.That(typeof(CameraOutputSession).GetMethod("Release", flags), Is.Null);

            string[] legacySelectionTypes = typeof(CameraOutputSession).Assembly
                .GetTypes()
                .Select(type => type.Name)
                .Where(name => name.StartsWith("CameraPresentation", StringComparison.Ordinal) ||
                               name.StartsWith("CameraRequest", StringComparison.Ordinal) ||
                               name == "CameraOutputContext")
                .ToArray();
            Assert.That(legacySelectionTypes, Is.Empty);
        }

        private static bool DeclaresCameraSelection(Type owner)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return owner.GetFields(flags).Any(field =>
                       field.Name.IndexOf("camera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       field.FieldType.Name.StartsWith("CameraPresentation", StringComparison.Ordinal)) ||
                   owner.GetProperties(flags).Any(property =>
                       property.Name.IndexOf("camera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       property.PropertyType.Name.StartsWith("CameraPresentation", StringComparison.Ordinal));
        }
    }
}
