using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Passive request to change the logical InputMode posture.
    /// It is not a Unity action-map command, does not mutate PlayerInput and is not a framework input manager.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A passive InputMode request.")]
    public readonly struct InputModeRequest : IEquatable<InputModeRequest>
    {
        public InputModeRequest(InputModeKind targetMode, string requester, string reason)
        {
            TargetMode = targetMode;
            Requester = requester.NormalizeTextOrFallback(nameof(InputModeRequest));
            Reason = reason.NormalizeText();
        }

        public InputModeKind TargetMode { get; }

        public string Requester { get; }

        public string Reason { get; }

        public bool IsValid => InputModeRules.IsValidKind(TargetMode);

        public bool Equals(InputModeRequest other)
        {
            return TargetMode == other.TargetMode
                && string.Equals(Requester, other.Requester, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)TargetMode;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Requester ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{TargetMode}:{Requester}";
        }

        public static InputModeRequest To(InputModeKind targetMode, string requester, string reason)
        {
            return new InputModeRequest(targetMode, requester, reason);
        }
    }
}
