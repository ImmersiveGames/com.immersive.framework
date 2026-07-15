using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B exact staged Activity Player admission token.")]
    public readonly struct ActivityPlayerAdmissionStageToken :
        IEquatable<ActivityPlayerAdmissionStageToken>
    {
        internal ActivityPlayerAdmissionStageToken(
            string sessionContextId,
            RuntimeContentOwner owner,
            int stageSequence)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            StageSequence = stageSequence;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner Owner { get; }
        public int StageSequence { get; }

        public bool IsValid =>
            Owner.IsValid &&
            StageSequence > 0;

        public string StableText => IsValid
            ? $"activity-player-stage:{(string.IsNullOrEmpty(SessionContextId) ? "no-session" : SessionContextId)}:{Owner.Scope}:" +
              $"{Owner.OwnerIdentity.Value.Value}:{StageSequence}"
            : string.Empty;

        public bool Equals(ActivityPlayerAdmissionStageToken other)
        {
            return string.Equals(
                    SessionContextId,
                    other.SessionContextId,
                    StringComparison.Ordinal) &&
                Owner == other.Owner &&
                StageSequence == other.StageSequence;
        }

        public override bool Equals(object obj) =>
            obj is ActivityPlayerAdmissionStageToken other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(
                    SessionContextId ?? string.Empty);
                hash = hash * 397 ^ Owner.GetHashCode();
                hash = hash * 397 ^ StageSequence;
                return hash;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            ActivityPlayerAdmissionStageToken left,
            ActivityPlayerAdmissionStageToken right) => left.Equals(right);

        public static bool operator !=(
            ActivityPlayerAdmissionStageToken left,
            ActivityPlayerAdmissionStageToken right) => !left.Equals(right);
    }
}
