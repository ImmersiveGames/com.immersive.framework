using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Passive definition for one canonical InputMode posture.
    /// It carries diagnostics only; it does not switch action maps or read input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A passive InputMode definition.")]
    public readonly struct InputModeDefinition : IEquatable<InputModeDefinition>
    {
        public InputModeDefinition(InputModeKind kind, InputModeId modeId, string displayName, string source, string reason)
        {
            if (!InputModeRules.IsValidKind(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "InputMode definition kind must be explicit.");
            }

            if (!modeId.IsValid)
            {
                throw new ArgumentException("InputMode definition requires a valid mode id.", nameof(modeId));
            }

            Kind = kind;
            ModeId = modeId;
            DisplayName = displayName.NormalizeTextOrFallback(kind.ToString());
            Source = source.NormalizeTextOrFallback(nameof(InputModeDefinition));
            Reason = reason.NormalizeText();
        }

        public InputModeKind Kind { get; }

        public InputModeId ModeId { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => InputModeRules.IsValidKind(Kind) && ModeId.IsValid;

        public bool Equals(InputModeDefinition other)
        {
            return Kind == other.Kind
                && ModeId.Equals(other.ModeId)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ ModeId.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{ModeId.StableText}";
        }
    }
}
