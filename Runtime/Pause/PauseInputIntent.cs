using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Framework-facing Pause input intent.
    /// It carries normalized command intent only; it does not resolve Input System actions, poll devices or dispatch PauseRequest.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23E Pause input intent; no input adapter, resolver or dispatch path.")]
    public readonly struct PauseInputIntent : IEquatable<PauseInputIntent>
    {
        public PauseInputIntent(PauseInputSignal signal, string source, string reason)
        {
            if (!signal.IsValid)
            {
                throw new ArgumentException("Pause input intent requires a valid Pause input signal.", nameof(signal));
            }

            Signal = signal;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PauseInputSignal Signal { get; }

        public string Source { get; }

        public string Reason { get; }

        public PauseInputActionId ActionId => Signal.ActionId;

        public PauseInputCommandKind CommandKind => Signal.CommandKind;

        public PauseInputSourceKind SourceKind => Signal.SourceKind;

        public bool IsValid => Signal.IsValid;

        public bool IsPauseStateIntent => Signal.IsPauseStateCommand;

        public bool IsMenuIntent => Signal.IsMenuCommand;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(PauseInputIntent other)
        {
            return Signal.Equals(other.Signal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseInputIntent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Signal.GetHashCode();
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
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"action='{ActionId.StableText}' command='{CommandKind}' sourceKind='{SourceKind}' pauseStateIntent='{IsPauseStateIntent}' menuIntent='{IsMenuIntent}' source='{sourceText}' reason='{reasonText}'";
        }

        public static PauseInputIntent FromSignal(PauseInputSignal signal, string source, string reason)
        {
            return new PauseInputIntent(signal, source, reason);
        }

        public static bool operator ==(PauseInputIntent left, PauseInputIntent right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseInputIntent left, PauseInputIntent right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
