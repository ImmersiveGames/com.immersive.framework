using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-local correlation evidence for one committed Scene Local Player admission.
    /// It authorizes release only for the exact Session context, admission operation, Slot and
    /// joined revision. The runtime record additionally verifies the exact authoring surface.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B1 typed Scene Local Player admission correlation and foreign/stale rejection evidence.")]
    public readonly struct SceneLocalPlayerAdmissionToken :
        IEquatable<SceneLocalPlayerAdmissionToken>
    {
        internal SceneLocalPlayerAdmissionToken(
            string contextId,
            int operationSequence,
            PlayerSlotId playerSlotId,
            int joinedSlotRevision)
        {
            ContextId = contextId.NormalizeText();
            OperationSequence = operationSequence;
            PlayerSlotId = playerSlotId;
            JoinedSlotRevision = joinedSlotRevision;
        }

        public string ContextId { get; }
        public int OperationSequence { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public int JoinedSlotRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(ContextId) &&
            OperationSequence > 0 &&
            PlayerSlotId.IsValid &&
            JoinedSlotRevision >= 0;

        public string StableText => IsValid
            ? $"{ContextId}:{OperationSequence}:{PlayerSlotId.StableText}:{JoinedSlotRevision}"
            : string.Empty;

        public bool Equals(SceneLocalPlayerAdmissionToken other)
        {
            return string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
                OperationSequence == other.OperationSequence &&
                PlayerSlotId == other.PlayerSlotId &&
                JoinedSlotRevision == other.JoinedSlotRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneLocalPlayerAdmissionToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ OperationSequence;
                hashCode = hashCode * 397 ^ PlayerSlotId.GetHashCode();
                hashCode = hashCode * 397 ^ JoinedSlotRevision;
                return hashCode;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            SceneLocalPlayerAdmissionToken left,
            SceneLocalPlayerAdmissionToken right) => left.Equals(right);

        public static bool operator !=(
            SceneLocalPlayerAdmissionToken left,
            SceneLocalPlayerAdmissionToken right) => !left.Equals(right);
    }
}
