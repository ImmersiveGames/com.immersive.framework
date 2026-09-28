using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.Camera
{
    /// <summary>Mode-specific runtime identity. Mutable occurrence state is deliberately held elsewhere.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-038-B Camera Occurrence identity.")]
    public readonly struct CameraOccurrenceIdentity : IEquatable<CameraOccurrenceIdentity>
    {
        private CameraOccurrenceIdentity(SessionCameraAssignmentId assignmentId, CameraOutputId outputId, PlayerOccurrenceId playerOccurrenceId)
        { AssignmentId = assignmentId; OutputId = outputId; PlayerOccurrenceId = playerOccurrenceId; }

        public SessionCameraAssignmentId AssignmentId { get; }
        public CameraOutputId OutputId { get; }
        public PlayerOccurrenceId PlayerOccurrenceId { get; }
        public bool IsIndividual => PlayerOccurrenceId.IsValid;
        public bool IsValid => AssignmentId.IsValid && OutputId.IsValid;

        public static CameraOccurrenceIdentity ForSessionOrShared(SessionCameraAssignmentId assignmentId, CameraOutputId outputId)
        {
            if (!assignmentId.IsValid || !outputId.IsValid) throw new ArgumentException("Assignment and Output identities must be valid.");
            return new CameraOccurrenceIdentity(assignmentId, outputId, default);
        }

        public static CameraOccurrenceIdentity ForIndividual(SessionCameraAssignmentId assignmentId, PlayerOccurrenceId playerOccurrenceId, CameraOutputId outputId)
        {
            if (!assignmentId.IsValid || !playerOccurrenceId.IsValid || !outputId.IsValid) throw new ArgumentException("Assignment, exact Player occurrence and Output identities must be valid.");
            return new CameraOccurrenceIdentity(assignmentId, outputId, playerOccurrenceId);
        }

        public bool Equals(CameraOccurrenceIdentity other) => AssignmentId == other.AssignmentId && OutputId == other.OutputId && PlayerOccurrenceId == other.PlayerOccurrenceId;
        public override bool Equals(object obj) => obj is CameraOccurrenceIdentity other && Equals(other);
        public override int GetHashCode() { unchecked { return ((AssignmentId.GetHashCode() * 397) ^ OutputId.GetHashCode()) * 397 ^ PlayerOccurrenceId.GetHashCode(); } }
        public override string ToString() => IsValid ? (IsIndividual ? $"{AssignmentId}/{PlayerOccurrenceId}/{OutputId}" : $"{AssignmentId}/{OutputId}") : string.Empty;
        public static bool operator ==(CameraOccurrenceIdentity left, CameraOccurrenceIdentity right) => left.Equals(right);
        public static bool operator !=(CameraOccurrenceIdentity left, CameraOccurrenceIdentity right) => !left.Equals(right);
    }
}
