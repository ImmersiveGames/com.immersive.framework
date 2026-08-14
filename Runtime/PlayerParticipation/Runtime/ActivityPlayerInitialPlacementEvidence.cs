using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal enum ActivityPlayerInitialPlacementStatus
    {
        None = 0,
        Applied = 1,
        Preserved = 2,
        Failed = 3
    }

    internal readonly struct ActivityPlayerInitialPlacementEvidence
    {
        internal ActivityPlayerInitialPlacementEvidence(
            RuntimeContentOwner owner,
            ActivityReadinessOccurrence occurrence,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            string representationIdentity,
            SceneProvidedPlayerInitialPlacementPolicy policy,
            Transform target,
            Transform anchor,
            Vector3 position,
            Quaternion rotation,
            ActivityPlayerInitialPlacementStatus status,
            string diagnostic)
        {
            Owner = owner;
            Occurrence = occurrence;
            PlayerSlotId = playerSlotId;
            ActorId = actorId;
            RepresentationIdentity = representationIdentity ?? string.Empty;
            Policy = policy;
            Target = target;
            Anchor = anchor;
            Position = position;
            Rotation = rotation;
            Status = status;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal RuntimeContentOwner Owner { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal PlayerSlotId PlayerSlotId { get; }
        internal ActorId ActorId { get; }
        internal string RepresentationIdentity { get; }
        internal SceneProvidedPlayerInitialPlacementPolicy Policy { get; }
        internal Transform Target { get; }
        internal Transform Anchor { get; }
        internal Vector3 Position { get; }
        internal Quaternion Rotation { get; }
        internal ActivityPlayerInitialPlacementStatus Status { get; }
        internal string Diagnostic { get; }

        internal bool IsSuccessful =>
            Status == ActivityPlayerInitialPlacementStatus.Applied ||
            Status == ActivityPlayerInitialPlacementStatus.Preserved;
    }
}
