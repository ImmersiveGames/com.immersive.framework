using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal enum ActivityPlayerRelocationStatus { None = 0, Applied = 1, Failed = 2 }

    internal readonly struct ActivityPlayerRelocationEvidence
    {
        internal ActivityPlayerRelocationEvidence(
            RuntimeContentOwner owner, ActivityReadinessOccurrence occurrence,
            PlayerSlotId playerSlotId, ActorId actorId, string representationIdentity,
            Transform target, Transform anchor, Vector3 position, Quaternion rotation,
            ActivityPlayerRelocationStatus status, string diagnostic)
        {
            Owner = owner; Occurrence = occurrence; PlayerSlotId = playerSlotId;
            ActorId = actorId; RepresentationIdentity = representationIdentity ?? string.Empty;
            Target = target; Anchor = anchor; Position = position; Rotation = rotation;
            Status = status; Diagnostic = diagnostic ?? string.Empty;
        }

        internal RuntimeContentOwner Owner { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal PlayerSlotId PlayerSlotId { get; }
        internal ActorId ActorId { get; }
        internal string RepresentationIdentity { get; }
        internal Transform Target { get; }
        internal Transform Anchor { get; }
        internal Vector3 Position { get; }
        internal Quaternion Rotation { get; }
        internal ActivityPlayerRelocationStatus Status { get; }
        internal string Diagnostic { get; }
        internal bool IsApplied => Status == ActivityPlayerRelocationStatus.Applied;
    }
}
