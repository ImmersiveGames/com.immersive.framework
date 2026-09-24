using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Immutable diagnostic result for the passive Pause Activity binding declaration.
    /// It does not compose or mutate Pause runtime state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P2.1A immutable Pause Activity binding intent resolution.")]
    public readonly struct PauseActivityBindingIntentResolution :
        IEquatable<PauseActivityBindingIntentResolution>
    {
        private PauseActivityBindingIntentResolution(
            PauseActivityBindingIntentStatus status,
            PauseActivityBindingIntent intent,
            int declarationCount,
            string source,
            string diagnostic)
        {
            Status = status;
            Intent = intent;
            DeclarationCount = declarationCount < 0 ? 0 : declarationCount;
            Source = source.NormalizeTextOrFallback(nameof(PauseActivityBindingIntentResolution));
            Diagnostic = diagnostic.NormalizeText();
        }

        public PauseActivityBindingIntentStatus Status { get; }

        public PauseActivityBindingIntent Intent { get; }

        public int DeclarationCount { get; }

        public string Source { get; }

        public string Diagnostic { get; }

        public bool HasIntent => Status == PauseActivityBindingIntentStatus.IntentCreated && Intent.IsValid;

        public bool IsAbsent => Status == PauseActivityBindingIntentStatus.IntentAbsent;

        public bool HasBlockingIssue => Status is
            PauseActivityBindingIntentStatus.InvalidAuthoring or
            PauseActivityBindingIntentStatus.UnsupportedMultipleDeclarations;

        public bool Succeeded => HasIntent || IsAbsent;

        public bool Equals(PauseActivityBindingIntentResolution other)
        {
            return Status == other.Status &&
                Intent.Equals(other.Intent) &&
                DeclarationCount == other.DeclarationCount &&
                string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                string.Equals(Diagnostic, other.Diagnostic, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseActivityBindingIntentResolution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Status;
                hash = (hash * 397) ^ Intent.GetHashCode();
                hash = (hash * 397) ^ DeclarationCount;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Diagnostic ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return $"status='{Status}' declarations='{DeclarationCount}' source='{Source}' diagnostic='{Diagnostic}'";
        }

        internal static PauseActivityBindingIntentResolution Absent(string source)
        {
            return new PauseActivityBindingIntentResolution(
                PauseActivityBindingIntentStatus.IntentAbsent,
                default,
                0,
                source,
                "intent-absent: Activity does not declare product Pause binding.");
        }

        internal static PauseActivityBindingIntentResolution Created(
            PauseActivityBindingIntent intent,
            string source)
        {
            return new PauseActivityBindingIntentResolution(
                PauseActivityBindingIntentStatus.IntentCreated,
                intent,
                1,
                source,
                "intent-created: Activity requires product Pause binding for its admitted Local Player.");
        }

        internal static PauseActivityBindingIntentResolution Invalid(
            int declarationCount,
            string source,
            string diagnostic)
        {
            return new PauseActivityBindingIntentResolution(
                PauseActivityBindingIntentStatus.InvalidAuthoring,
                default,
                declarationCount,
                source,
                $"invalid-authoring: {diagnostic.NormalizeTextOrFallback("Pause Activity binding authoring is invalid.")}");
        }

        internal static PauseActivityBindingIntentResolution UnsupportedMultiple(
            int declarationCount,
            string source)
        {
            return new PauseActivityBindingIntentResolution(
                PauseActivityBindingIntentStatus.UnsupportedMultipleDeclarations,
                default,
                declarationCount,
                source,
                $"duplicate-authoring; unsupported-multiple-declarations: Activity has '{declarationCount}' Pause Activity Binding declarations. Exactly one declaration is supported.");
        }
    }
}
