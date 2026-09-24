using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive request describing a desired Transition Effect.
    /// The request does not discover adapters, play visuals, load scenes, block Gate or mutate lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B passive Transition Effect request; no adapter execution.")]
    public readonly struct TransitionEffectRequest : IEquatable<TransitionEffectRequest>
    {
        public TransitionEffectRequest(
            TransitionEffectId effectId,
            TransitionEffectKind effectKind,
            TransitionEffectRequiredness requiredness,
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            TransitionPhase phase,
            string source,
            string reason)
        {
            if (!effectId.IsValid)
            {
                throw new ArgumentException("Transition Effect request requires a valid effect id.", nameof(effectId));
            }

            if (!Enum.IsDefined(typeof(TransitionEffectKind), effectKind) || effectKind == TransitionEffectKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind), effectKind, "Transition Effect request kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionEffectRequiredness), requiredness) || requiredness == TransitionEffectRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Transition Effect request requiredness must be explicit.");
            }

            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Effect request requires a valid Transition operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), transitionKind) || transitionKind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionKind), transitionKind, "Transition Effect request transition kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionPhase), phase) || phase == TransitionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Transition Effect request phase must be explicit.");
            }

            EffectId = effectId;
            EffectKind = effectKind;
            Requiredness = requiredness;
            OperationId = operationId;
            TransitionKind = transitionKind;
            Phase = phase;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public TransitionEffectId EffectId { get; }

        public TransitionEffectKind EffectKind { get; }

        public TransitionEffectRequiredness Requiredness { get; }

        public TransitionOperationId OperationId { get; }

        public TransitionKind TransitionKind { get; }

        public TransitionPhase Phase { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == TransitionEffectRequiredness.Required;

        public bool IsOptional => Requiredness == TransitionEffectRequiredness.Optional;

        public bool IsValid => EffectId.IsValid
            && EffectKind != TransitionEffectKind.Unknown
            && Requiredness != TransitionEffectRequiredness.Unknown
            && OperationId.IsValid
            && TransitionKind != TransitionKind.Unknown
            && Phase != TransitionPhase.Unknown;

        public bool Equals(TransitionEffectRequest other)
        {
            return EffectId.Equals(other.EffectId)
                && EffectKind == other.EffectKind
                && Requiredness == other.Requiredness
                && OperationId.Equals(other.OperationId)
                && TransitionKind == other.TransitionKind
                && Phase == other.Phase
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = EffectId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)EffectKind;
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)TransitionKind;
                hashCode = hashCode * 397 ^ (int)Phase;
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
            return $"effect='{EffectId.StableText}' effectKind='{EffectKind}' requiredness='{Requiredness}' operation='{OperationId.StableText}' transitionKind='{TransitionKind}' phase='{Phase}' source='{sourceText}' reason='{reasonText}'";
        }

        public static TransitionEffectRequest Required(
            string effectId,
            TransitionEffectKind effectKind,
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            TransitionPhase phase,
            string source,
            string reason)
        {
            return new TransitionEffectRequest(
                TransitionEffectId.From(effectId),
                effectKind,
                TransitionEffectRequiredness.Required,
                operationId,
                transitionKind,
                phase,
                source,
                reason);
        }

        public static TransitionEffectRequest Optional(
            string effectId,
            TransitionEffectKind effectKind,
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            TransitionPhase phase,
            string source,
            string reason)
        {
            return new TransitionEffectRequest(
                TransitionEffectId.From(effectId),
                effectKind,
                TransitionEffectRequiredness.Optional,
                operationId,
                transitionKind,
                phase,
                source,
                reason);
        }

        public static bool operator ==(TransitionEffectRequest left, TransitionEffectRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionEffectRequest left, TransitionEffectRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
