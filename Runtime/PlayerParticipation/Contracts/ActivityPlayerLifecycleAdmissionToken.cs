using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7G exact same-Route Activity Player lifecycle admission token.")]
    public readonly struct ActivityPlayerLifecycleAdmissionToken :
        IEquatable<ActivityPlayerLifecycleAdmissionToken>
    {
        internal ActivityPlayerLifecycleAdmissionToken(
            string sessionContextId,
            RuntimeContentOwner previousOwner,
            RuntimeContentOwner targetOwner,
            int sequence)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PreviousOwner = previousOwner;
            TargetOwner = targetOwner;
            Sequence = sequence;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner PreviousOwner { get; }
        public RuntimeContentOwner TargetOwner { get; }
        public int Sequence { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PreviousOwner.IsValid &&
            PreviousOwner.Scope == RuntimeContentScope.Activity &&
            TargetOwner.IsValid &&
            TargetOwner.Scope == RuntimeContentScope.Activity &&
            PreviousOwner != TargetOwner &&
            Sequence > 0;

        public string StableText =>
            IsValid
                ? $"{SessionContextId}:{PreviousOwner.StableText}->{TargetOwner.StableText}:{Sequence}"
                : string.Empty;

        public bool Equals(ActivityPlayerLifecycleAdmissionToken other) =>
            string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
            PreviousOwner == other.PreviousOwner &&
            TargetOwner == other.TargetOwner &&
            Sequence == other.Sequence;

        public override bool Equals(object obj) =>
            obj is ActivityPlayerLifecycleAdmissionToken other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(
                    SessionContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ PreviousOwner.GetHashCode();
                hashCode = hashCode * 397 ^ TargetOwner.GetHashCode();
                hashCode = hashCode * 397 ^ Sequence;
                return hashCode;
            }
        }

        public static bool operator ==(
            ActivityPlayerLifecycleAdmissionToken left,
            ActivityPlayerLifecycleAdmissionToken right) => left.Equals(right);

        public static bool operator !=(
            ActivityPlayerLifecycleAdmissionToken left,
            ActivityPlayerLifecycleAdmissionToken right) => !left.Equals(right);

        public override string ToString() => StableText;
    }
}
