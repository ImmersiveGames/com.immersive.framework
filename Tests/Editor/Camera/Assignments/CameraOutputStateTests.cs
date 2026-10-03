using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using NUnit.Framework;
using System.Reflection;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class CameraOutputStateTests
    {
        private static readonly CameraOutputId Output = new CameraOutputId("output.main");
        private static readonly SessionCameraAssignmentId AssignmentId = new SessionCameraAssignmentId("assignment.main");
        private static readonly CameraOccurrenceIdentity Occurrence = CameraOccurrenceIdentity.ForSessionOrShared(AssignmentId, Output);

        [Test]
        public void OutputAuthoringUsesFallbackFieldWithoutDefaultCompatibilityAlias()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Assert.That(typeof(CameraOutputAuthoring).GetField("fallbackCameraRig", flags), Is.Not.Null);
            Assert.That(typeof(CameraOutputAuthoring).GetField("defaultCameraRig", flags), Is.Null);
        }

        [Test]
        public void FallbackMustBeAvailableBeforeCoverageOrNormalPresentation()
        {
            var state = new CameraOutputState(Output);
            Assert.That(state.TryCoverWithFallback(out _), Is.False);
            Assert.That(state.TryPresentNormalOccurrence(Occurrence, out _), Is.False);
            Assert.That(state.TryMakeFallbackAvailable(out _), Is.True);
            Assert.That(state.IsFallbackCovering, Is.True);
            Assert.That(state.HasPresentedNormalOccurrence, Is.False);
        }

        [Test]
        public void AssignmentOccurrenceAndFallbackAreIndependentStates()
        {
            var state = ReadyState();
            Assert.That(state.TrySetActiveAssignment(Assignment(), out _), Is.True);
            Assert.That(state.ActiveAssignmentId, Is.EqualTo(AssignmentId));
            Assert.That(state.HasPresentedNormalOccurrence, Is.False);
            Assert.That(state.IsFallbackCovering, Is.True);

            Assert.That(state.TryPresentNormalOccurrence(Occurrence, out _), Is.True);
            Assert.That(state.ActiveAssignmentId, Is.EqualTo(AssignmentId));
            Assert.That(state.PresentedNormalOccurrence, Is.EqualTo(Occurrence));
            Assert.That(state.IsFallbackCovering, Is.False);
        }

        [Test]
        public void FallbackCoveragePreservesActiveAssignmentAndOccurrence()
        {
            var state = ReadyPresentedState();
            Assert.That(state.TryCoverWithFallback(out _), Is.True);
            Assert.That(state.ActiveAssignmentId, Is.EqualTo(AssignmentId));
            Assert.That(state.HasPresentedNormalOccurrence, Is.False);
            Assert.That(state.RetainedNormalOccurrence, Is.EqualTo(Occurrence));
            Assert.That(state.IsFallbackCovering, Is.True);
        }

        [Test]
        public void ReleasingFallbackCoverageRestoresSameOccurrence()
        {
            var state = ReadyPresentedState();
            state.TryCoverWithFallback(out _);
            Assert.That(state.TryRestoreNormalOccurrence(out _), Is.True);
            Assert.That(state.HasPresentedNormalOccurrence, Is.True);
            Assert.That(state.PresentedNormalOccurrence, Is.EqualTo(Occurrence));
            Assert.That(state.IsFallbackCovering, Is.False);
        }

        [Test]
        public void InvalidStateTransitionsFailExplicitly()
        {
            var state = ReadyPresentedState();
            Assert.That(state.TrySetActiveAssignment(Assignment("output.other"), out string mismatch), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(mismatch), Is.False);
            Assert.That(state.TryPresentNormalOccurrence(
                CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.other"), Output), out _), Is.False);

            Assert.That(state.TrySetActiveAssignment(Assignment(), out _), Is.True);
            Assert.That(state.TryPresentNormalOccurrence(
                CameraOccurrenceIdentity.ForSessionOrShared(new SessionCameraAssignmentId("assignment.other"), Output), out _), Is.False);
            Assert.That(state.TryRestoreNormalOccurrence(out _), Is.False);

            Assert.That(state.TryPresentNormalOccurrence(Occurrence, out _), Is.True);
            Assert.That(state.TryRestoreNormalOccurrence(out _), Is.False,
                "Normal occurrence cannot be restored while Fallback does not cover the Output.");
        }

        [Test]
        public void AssignmentCannotBeReplacedUntilNormalOccurrenceIsCovered()
        {
            var state = ReadyPresentedState();
            var replacement = Assignment("output.main", "assignment.next");
            Assert.That(state.TrySetActiveAssignment(replacement, out _), Is.False);
            Assert.That(state.ActiveAssignmentId, Is.EqualTo(AssignmentId));
            Assert.That(state.PresentedNormalOccurrence, Is.EqualTo(Occurrence));

            Assert.That(state.TryCoverWithFallback(out _), Is.True);
            Assert.That(state.TrySetActiveAssignment(replacement, out _), Is.True);
            Assert.That(state.ActiveAssignmentId, Is.EqualTo(replacement.Id));
            Assert.That(state.IsFallbackCovering, Is.True);
            Assert.That(state.HasRetainedNormalOccurrence, Is.False);
        }

        private static CameraOutputState ReadyState()
        {
            var state = new CameraOutputState(Output);
            Assert.That(state.TryMakeFallbackAvailable(out _), Is.True);
            return state;
        }

        private static CameraOutputState ReadyPresentedState()
        {
            var state = ReadyState();
            Assert.That(state.TrySetActiveAssignment(Assignment(), out _), Is.True);
            Assert.That(state.TryPresentNormalOccurrence(Occurrence, out _), Is.True);
            return state;
        }

        private static SessionCameraAssignment Assignment(
            string output = "output.main",
            string assignmentId = "assignment.main") =>
            new SessionCameraAssignment(
                new SessionCameraAssignmentId(assignmentId),
                CameraOccurrenceMode.SessionScoped,
                CameraMembershipPolicy.None,
                CameraTargetPolicy.NoSubject,
                new[] { new CameraOutputMapping(new CameraOutputId(output)) });
    }
}
