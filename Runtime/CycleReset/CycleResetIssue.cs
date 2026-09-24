using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Structured diagnostic issue emitted by Cycle Reset planning or execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset structured issue.")]
    public readonly struct CycleResetIssue : IEquatable<CycleResetIssue>
    {
        public CycleResetIssue(
            CycleResetIssueKind kind,
            CycleResetParticipantId participantId,
            CycleResetScope participantScope,
            bool blocking,
            string message)
        {
            if (!Enum.IsDefined(typeof(CycleResetIssueKind), kind) || kind == CycleResetIssueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Cycle Reset issue kind must be explicit.");
            }

            Kind = kind;
            ParticipantId = participantId;
            ParticipantScope = participantScope;
            Blocking = blocking;
            Message = Normalize(message);
        }

        public CycleResetIssueKind Kind { get; }

        public CycleResetParticipantId ParticipantId { get; }

        public CycleResetScope ParticipantScope { get; }

        public bool Blocking { get; }

        public string Message { get; }

        public bool HasParticipantId => ParticipantId.IsValid;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(CycleResetIssue other)
        {
            return Kind == other.Kind
                && ParticipantId.Equals(other.ParticipantId)
                && ParticipantScope == other.ParticipantScope
                && Blocking == other.Blocking
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = hashCode * 397 ^ ParticipantId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)ParticipantScope;
                hashCode = hashCode * 397 ^ Blocking.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string participantText = HasParticipantId ? ParticipantId.StableText : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"kind='{Kind}' participantId='{participantText}' participantScope='{ParticipantScope}' blocking='{Blocking}' message='{messageText}'";
        }

        public static CycleResetIssue BlockingIssue(
            CycleResetIssueKind kind,
            CycleResetParticipantId participantId,
            CycleResetScope participantScope,
            string message)
        {
            return new CycleResetIssue(kind, participantId, participantScope, true, message);
        }

        public static CycleResetIssue NonBlockingIssue(
            CycleResetIssueKind kind,
            CycleResetParticipantId participantId,
            CycleResetScope participantScope,
            string message)
        {
            return new CycleResetIssue(kind, participantId, participantScope, false, message);
        }

        public static bool operator ==(CycleResetIssue left, CycleResetIssue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetIssue left, CycleResetIssue right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
