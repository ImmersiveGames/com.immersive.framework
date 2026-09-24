using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for the logical moment that caused a Progression Save request.
    /// This contract does not schedule autosave, observe Route/Activity events, or execute a backend by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save autosave/manual moment contract; passive only.")]
    public readonly struct ProgressionSaveMoment : IEquatable<ProgressionSaveMoment>
    {
        public ProgressionSaveMoment(
            ProgressionSaveMomentId momentId,
            ProgressionSaveMomentKind kind,
            string source,
            string reason)
        {
            if (!momentId.IsValid)
            {
                throw new ArgumentException("Progression Save moment requires a valid moment id.", nameof(momentId));
            }

            if (!Enum.IsDefined(typeof(ProgressionSaveMomentKind), kind) || kind == ProgressionSaveMomentKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Progression Save moment kind must be explicit.");
            }

            MomentId = momentId;
            Kind = kind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ProgressionSaveMomentId MomentId { get; }

        public ProgressionSaveMomentKind Kind { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => MomentId.IsValid && Kind != ProgressionSaveMomentKind.Unknown;

        public bool IsManual => Kind == ProgressionSaveMomentKind.Manual;

        public bool IsAutosave => Kind == ProgressionSaveMomentKind.Autosave;

        public bool IsCheckpoint => Kind == ProgressionSaveMomentKind.Checkpoint;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(ProgressionSaveMoment other)
        {
            return MomentId.Equals(other.MomentId)
                && Kind == other.Kind
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveMoment other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = MomentId.GetHashCode();
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
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"moment='{MomentId.StableText}' kind='{Kind}' source='{sourceText}' reason='{reasonText}'";
        }

        public static ProgressionSaveMoment Manual(string momentId, string source, string reason)
        {
            return new ProgressionSaveMoment(
                ProgressionSaveMomentId.From(momentId),
                ProgressionSaveMomentKind.Manual,
                source,
                reason);
        }

        public static ProgressionSaveMoment Autosave(string momentId, string source, string reason)
        {
            return new ProgressionSaveMoment(
                ProgressionSaveMomentId.From(momentId),
                ProgressionSaveMomentKind.Autosave,
                source,
                reason);
        }

        public static ProgressionSaveMoment Checkpoint(string momentId, string source, string reason)
        {
            return new ProgressionSaveMoment(
                ProgressionSaveMomentId.From(momentId),
                ProgressionSaveMomentKind.Checkpoint,
                source,
                reason);
        }

        public static bool operator ==(ProgressionSaveMoment left, ProgressionSaveMoment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveMoment left, ProgressionSaveMoment right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
