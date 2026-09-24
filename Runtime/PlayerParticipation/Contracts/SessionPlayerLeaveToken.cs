using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-local correlation evidence for one staged Session Player Leave operation.
    /// The token is valid only for the exact participation context, Slot occurrence revision
    /// and Leave operation that issued it.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Session Player Leave occurrence correlation evidence.")]
    internal readonly struct SessionPlayerLeaveToken : IEquatable<SessionPlayerLeaveToken>
    {
        internal SessionPlayerLeaveToken(
            string contextId,
            int operationSequence,
            PlayerSlotId playerSlotId,
            int expectedOccurrenceRevision,
            int leavingSlotRevision)
        {
            ContextId = contextId.NormalizeText();
            OperationSequence = operationSequence;
            PlayerSlotId = playerSlotId;
            ExpectedOccurrenceRevision = expectedOccurrenceRevision;
            LeavingSlotRevision = leavingSlotRevision;
        }

        internal string ContextId { get; }
        internal int OperationSequence { get; }
        internal PlayerSlotId PlayerSlotId { get; }
        internal int ExpectedOccurrenceRevision { get; }
        internal int LeavingSlotRevision { get; }

        internal bool IsValid =>
            !string.IsNullOrEmpty(ContextId) &&
            OperationSequence > 0 &&
            PlayerSlotId.IsValid &&
            ExpectedOccurrenceRevision >= 0 &&
            LeavingSlotRevision > ExpectedOccurrenceRevision;

        internal string StableText => IsValid
            ? $"session-player-leave:{ContextId}:{OperationSequence}:{PlayerSlotId.StableText}:{ExpectedOccurrenceRevision}:{LeavingSlotRevision}"
            : string.Empty;

        public bool Equals(SessionPlayerLeaveToken other)
        {
            return string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
                OperationSequence == other.OperationSequence &&
                PlayerSlotId == other.PlayerSlotId &&
                ExpectedOccurrenceRevision == other.ExpectedOccurrenceRevision &&
                LeavingSlotRevision == other.LeavingSlotRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionPlayerLeaveToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ OperationSequence;
                hashCode = hashCode * 397 ^ PlayerSlotId.GetHashCode();
                hashCode = hashCode * 397 ^ ExpectedOccurrenceRevision;
                hashCode = hashCode * 397 ^ LeavingSlotRevision;
                return hashCode;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            SessionPlayerLeaveToken left,
            SessionPlayerLeaveToken right) => left.Equals(right);

        public static bool operator !=(
            SessionPlayerLeaveToken left,
            SessionPlayerLeaveToken right) => !left.Equals(right);
    }
}
