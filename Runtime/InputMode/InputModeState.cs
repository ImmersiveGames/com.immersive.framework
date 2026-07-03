using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Passive snapshot of the current logical InputMode posture.
    /// It does not own Unity Input System state and does not imply action-map switching.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A passive InputMode state snapshot.")]
    public readonly struct InputModeState : IEquatable<InputModeState>
    {
        public InputModeState(InputModeDefinition currentMode, int revision, string owner, string reason)
        {
            if (!currentMode.IsValid)
            {
                throw new ArgumentException("InputMode state requires a valid current mode.", nameof(currentMode));
            }

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), revision, "InputMode state revision cannot be negative.");
            }

            CurrentMode = currentMode;
            Revision = revision;
            Owner = owner.NormalizeTextOrFallback(nameof(InputModeState));
            Reason = reason.NormalizeText();
        }

        public InputModeDefinition CurrentMode { get; }

        public InputModeKind CurrentKind => CurrentMode.Kind;

        public int Revision { get; }

        public string Owner { get; }

        public string Reason { get; }

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public bool IsValid => CurrentMode.IsValid;

        public InputModeState WithMode(InputModeDefinition nextMode, string owner, string reason)
        {
            return new InputModeState(nextMode, Revision + 1, owner, reason);
        }

        public bool Equals(InputModeState other)
        {
            return CurrentMode.Equals(other.CurrentMode)
                && Revision == other.Revision
                && string.Equals(Owner, other.Owner, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = CurrentMode.GetHashCode();
                hash = (hash * 397) ^ Revision;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Owner ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{CurrentKind}:revision={Revision}";
        }

        public static InputModeState InitialGameplay(string owner, string reason)
        {
            return new InputModeState(InputModeDefinitions.Gameplay(owner, reason), 0, owner, reason);
        }
    }
}
