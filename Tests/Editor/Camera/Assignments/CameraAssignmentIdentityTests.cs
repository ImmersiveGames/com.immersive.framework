using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraAssignmentIdentityTests
    {
        [Test]
        public void StableIds_UseValueEquality()
        {
            Assert.That(new CameraDefinitionId("definition.a"), Is.EqualTo(new CameraDefinitionId("definition.a")));
            Assert.That(new SessionCameraAssignmentId("assignment.a"), Is.EqualTo(new SessionCameraAssignmentId("assignment.a")));
            Assert.That(new CameraOutputId("output.a"), Is.EqualTo(new CameraOutputId("output.a")));
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
        public void ReusedDefinition_DoesNotShareOccurrenceIdentityOrStateAcrossAssignments()
        {
            var definition = new CameraDefinitionId("definition.shared");
            var firstAssignment = CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.a"), new CameraOutputId("output.a"));
            var secondAssignment = CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.b"), new CameraOutputId("output.a"));
            Assert.That(definition, Is.EqualTo(new CameraDefinitionId("definition.shared")));
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
                new SessionCameraAssignmentId("assignment.a"), new CameraDefinitionId("definition.a"),
                CameraOccurrenceMode.IndividualPerPlayer, CameraMembershipPolicy.None,
                CameraTargetPolicy.NoSubject, new[] { output });
            Assert.That(individualWithoutMembership.TryValidate(out _), Is.False);

            var duplicateOutputs = new SessionCameraAssignment(
                new SessionCameraAssignmentId("assignment.b"), new CameraDefinitionId("definition.a"),
                CameraOccurrenceMode.SharedGroup, CameraMembershipPolicy.ExplicitPlayerSlots,
                CameraTargetPolicy.MemberActorTargets, new[] { output, output }, new[] { PlayerSlotId.Player1 });
            Assert.That(duplicateOutputs.TryValidate(out _), Is.False);
        }

        [Test]
        public void ModeDoesNotImplyMembershipOrTargetPolicy()
        {
            var assignment = new SessionCameraAssignment(
                new SessionCameraAssignmentId("assignment.a"), new CameraDefinitionId("definition.a"),
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
    }
}
