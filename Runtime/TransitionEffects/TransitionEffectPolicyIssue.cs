using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. One explicit issue produced by Transition Effect policy evaluation.
    /// Issues are passive diagnostics; they do not register blockers, discover adapters or execute effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19E Transition Effect policy issue; passive diagnostics only.")]
    public readonly struct TransitionEffectPolicyIssue : IEquatable<TransitionEffectPolicyIssue>
    {
        public TransitionEffectPolicyIssue(
            string code,
            TransitionEffectPolicyIssueSeverity severity,
            TransitionEffectId effectId,
            TransitionEffectKind effectKind,
            TransitionEffectRequiredness requiredness,
            string message)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Transition Effect policy issue requires a code.", nameof(code));
            }

            if (!Enum.IsDefined(typeof(TransitionEffectPolicyIssueSeverity), severity) || severity == TransitionEffectPolicyIssueSeverity.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Transition Effect policy issue severity must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionEffectKind), effectKind) || effectKind == TransitionEffectKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind), effectKind, "Transition Effect policy issue kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionEffectRequiredness), requiredness) || requiredness == TransitionEffectRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Transition Effect policy issue requiredness must be explicit.");
            }

            Code = Normalize(code);
            Severity = severity;
            EffectId = effectId;
            EffectKind = effectKind;
            Requiredness = requiredness;
            Message = Normalize(message);
        }

        public string Code { get; }

        public TransitionEffectPolicyIssueSeverity Severity { get; }

        public TransitionEffectId EffectId { get; }

        public TransitionEffectKind EffectKind { get; }

        public TransitionEffectRequiredness Requiredness { get; }

        public string Message { get; }

        public bool BlocksTransition => Severity == TransitionEffectPolicyIssueSeverity.Blocking;

        public bool IsValid => !string.IsNullOrWhiteSpace(Code)
            && Severity != TransitionEffectPolicyIssueSeverity.Unknown
            && EffectKind != TransitionEffectKind.Unknown
            && Requiredness != TransitionEffectRequiredness.Unknown;

        public bool Equals(TransitionEffectPolicyIssue other)
        {
            return string.Equals(Code, other.Code, StringComparison.Ordinal)
                && Severity == other.Severity
                && EffectId.Equals(other.EffectId)
                && EffectKind == other.EffectKind
                && Requiredness == other.Requiredness
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectPolicyIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(Code ?? string.Empty);
                hashCode = hashCode * 397 ^ (int)Severity;
                hashCode = hashCode * 397 ^ EffectId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)EffectKind;
                hashCode = hashCode * 397 ^ (int)Requiredness;
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
            string messageText = Message.ToDiagnosticText();
            string effectText = EffectId.IsValid ? EffectId.StableText : "<none>";
            return $"code='{Code}' severity='{Severity}' blocksTransition='{BlocksTransition}' effect='{effectText}' effectKind='{EffectKind}' requiredness='{Requiredness}' message='{messageText}'";
        }

        public static TransitionEffectPolicyIssue RequiredAdapterMissing(TransitionEffectRequest request)
        {
            return new TransitionEffectPolicyIssue(
                TransitionEffectAuthoringPolicy.RequiredAdapterMissingIssue,
                TransitionEffectPolicyIssueSeverity.Blocking,
                request.EffectId,
                request.EffectKind,
                request.Requiredness,
                $"Required Transition Effect adapter missing for kind '{request.EffectKind}'.");
        }

        public static TransitionEffectPolicyIssue OptionalAdapterMissing(TransitionEffectRequest request)
        {
            return new TransitionEffectPolicyIssue(
                TransitionEffectAuthoringPolicy.OptionalAdapterMissingIssue,
                TransitionEffectPolicyIssueSeverity.Warning,
                request.EffectId,
                request.EffectKind,
                request.Requiredness,
                $"Optional Transition Effect adapter missing for kind '{request.EffectKind}'.");
        }

        public static TransitionEffectPolicyIssue DuplicateEffectId(TransitionEffectRequest request)
        {
            return new TransitionEffectPolicyIssue(
                TransitionEffectAuthoringPolicy.DuplicateEffectIdIssue,
                TransitionEffectPolicyIssueSeverity.Blocking,
                request.EffectId,
                request.EffectKind,
                request.Requiredness,
                $"Duplicate Transition Effect id '{request.EffectId.StableText}' in one effect plan.");
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
