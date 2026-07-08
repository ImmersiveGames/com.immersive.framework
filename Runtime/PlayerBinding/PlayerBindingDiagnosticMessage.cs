using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable passive diagnostic message for Player binding readiness.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49L passive Player binding diagnostic message.")]
    public readonly struct PlayerBindingDiagnosticMessage : IEquatable<PlayerBindingDiagnosticMessage>
    {
        public PlayerBindingDiagnosticMessage(
            PlayerBindingDiagnosticMessageKind kind,
            PlayerBindingDiagnosticSeverity severity,
            string text,
            PlayerBindingReadinessIssueKind readinessIssueKind = PlayerBindingReadinessIssueKind.None)
        {
            if (!Enum.IsDefined(typeof(PlayerBindingDiagnosticMessageKind), kind) || kind == PlayerBindingDiagnosticMessageKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Player binding diagnostic message kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(PlayerBindingDiagnosticSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Player binding diagnostic severity is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerBindingReadinessIssueKind), readinessIssueKind))
            {
                throw new ArgumentOutOfRangeException(nameof(readinessIssueKind), readinessIssueKind, "Player binding readiness issue kind is not defined.");
            }

            Kind = kind;
            Severity = severity;
            Text = text.NormalizeTextOrFallback(kind.ToString());
            ReadinessIssueKind = readinessIssueKind;
        }

        public PlayerBindingDiagnosticMessageKind Kind { get; }

        public PlayerBindingDiagnosticSeverity Severity { get; }

        public string Text { get; }

        public PlayerBindingReadinessIssueKind ReadinessIssueKind { get; }

        public bool IsError => Severity == PlayerBindingDiagnosticSeverity.Error;

        public bool IsWarning => Severity == PlayerBindingDiagnosticSeverity.Warning;

        public bool IsInfo => Severity == PlayerBindingDiagnosticSeverity.Info;

        public bool HasReadinessIssueKind => ReadinessIssueKind != PlayerBindingReadinessIssueKind.None;

        public bool Equals(PlayerBindingDiagnosticMessage other)
        {
            return Kind == other.Kind
                && Severity == other.Severity
                && ReadinessIssueKind == other.ReadinessIssueKind
                && string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerBindingDiagnosticMessage other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (int)Severity;
                hash = (hash * 397) ^ (int)ReadinessIssueKind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Text ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Severity}:{Kind}";
        }
    }
}
