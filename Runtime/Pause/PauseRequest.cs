using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive logical Pause request.
    /// A request does not read input, show UI, change Time.timeScale or execute Gate blockers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B passive Pause request; no runtime execution.")]
    public readonly struct PauseRequest : IEquatable<PauseRequest>
    {
        public PauseRequest(
            PauseRequestId requestId,
            PauseRequestKind kind,
            string source,
            string reason)
        {
            if (!requestId.IsValid)
            {
                throw new ArgumentException("Pause request requires a valid request id.", nameof(requestId));
            }

            if (!Enum.IsDefined(typeof(PauseRequestKind), kind) || kind == PauseRequestKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Pause request kind must be explicit.");
            }

            RequestId = requestId;
            Kind = kind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PauseRequestId RequestId { get; }

        public PauseRequestKind Kind { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => RequestId.IsValid && Kind != PauseRequestKind.Unknown;

        public bool RequestsPause => Kind == PauseRequestKind.Pause;

        public bool RequestsResume => Kind == PauseRequestKind.Resume;

        public bool RequestsToggle => Kind == PauseRequestKind.Toggle;

        public bool Equals(PauseRequest other)
        {
            return RequestId.Equals(other.RequestId)
                && Kind == other.Kind
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RequestId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"request='{RequestId.StableText}' kind='{Kind}' source='{sourceText}' reason='{reasonText}'";
        }

        public static PauseRequest Pause(string requestId, string source, string reason)
        {
            return new PauseRequest(PauseRequestId.From(requestId), PauseRequestKind.Pause, source, reason);
        }

        public static PauseRequest Resume(string requestId, string source, string reason)
        {
            return new PauseRequest(PauseRequestId.From(requestId), PauseRequestKind.Resume, source, reason);
        }

        public static PauseRequest Toggle(string requestId, string source, string reason)
        {
            return new PauseRequest(PauseRequestId.From(requestId), PauseRequestKind.Toggle, source, reason);
        }

        public static PauseState ResolveTargetState(PauseRequestKind kind, PauseState currentState)
        {
            if (!Enum.IsDefined(typeof(PauseRequestKind), kind) || kind == PauseRequestKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Pause request kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(PauseState), currentState) || currentState == PauseState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, "Current Pause state must be explicit.");
            }

            if (kind == PauseRequestKind.Pause)
            {
                return PauseState.Paused;
            }

            if (kind == PauseRequestKind.Resume)
            {
                return PauseState.Running;
            }

            return currentState == PauseState.Paused ? PauseState.Running : PauseState.Paused;
        }

        public static bool operator ==(PauseRequest left, PauseRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseRequest left, PauseRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
