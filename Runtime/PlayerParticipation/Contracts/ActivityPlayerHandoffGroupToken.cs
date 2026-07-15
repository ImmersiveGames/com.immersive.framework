using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E exact multi-Slot Activity Player handoff group token.")]
    public readonly struct ActivityPlayerHandoffGroupToken :
        IEquatable<ActivityPlayerHandoffGroupToken>
    {
        internal ActivityPlayerHandoffGroupToken(
            string sessionContextId,
            RuntimeContentOwner targetOwner,
            int slotCount,
            int groupRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            TargetOwner = targetOwner;
            SlotCount = slotCount;
            GroupRevision = groupRevision;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner TargetOwner { get; }
        public int SlotCount { get; }
        public int GroupRevision { get; }
        public bool IsValid =>
            TargetOwner.IsValid &&
            TargetOwner.Scope == RuntimeContentScope.Activity &&
            SlotCount >= 0 &&
            GroupRevision > 0 &&
            (SlotCount == 0 || !string.IsNullOrEmpty(SessionContextId));
        public string StableText => IsValid
            ? $"activity-player-handoff-group:{(string.IsNullOrEmpty(SessionContextId) ? "no-session" : SessionContextId)}:" +
              $"{TargetOwner.OwnerId}:{GroupRevision}:{SlotCount}"
            : string.Empty;

        public bool Equals(ActivityPlayerHandoffGroupToken other) =>
            string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
            TargetOwner == other.TargetOwner && SlotCount == other.SlotCount &&
            GroupRevision == other.GroupRevision;
        public override bool Equals(object obj) =>
            obj is ActivityPlayerHandoffGroupToken other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(
                    SessionContextId ?? string.Empty);
                hash = hash * 397 ^ TargetOwner.GetHashCode();
                hash = hash * 397 ^ SlotCount;
                hash = hash * 397 ^ GroupRevision;
                return hash;
            }
        }
        public override string ToString() => StableText;
        public static bool operator ==(ActivityPlayerHandoffGroupToken left,
            ActivityPlayerHandoffGroupToken right) => left.Equals(right);
        public static bool operator !=(ActivityPlayerHandoffGroupToken left,
            ActivityPlayerHandoffGroupToken right) => !left.Equals(right);
    }
}
