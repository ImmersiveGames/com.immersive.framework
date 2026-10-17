using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Evidência física interna e validada da autoridade de Player da Session.</summary>
    internal readonly struct PlayerActorPhysicalOccurrence
    {
        internal PlayerActorPhysicalOccurrence(
            ActorOccurrenceId occurrenceId,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken preparation,
            PlayerActorDeclaration declaration,
            GameObject actorRoot,
            RuntimeContentOwner owner)
        {
            OccurrenceId = occurrenceId;
            PlayerSlotId = playerSlotId;
            Preparation = preparation;
            Declaration = declaration;
            ActorRoot = actorRoot;
            Owner = owner;
        }

        internal ActorOccurrenceId OccurrenceId { get; }
        internal PlayerSlotId PlayerSlotId { get; }
        internal PlayerActorPreparationToken Preparation { get; }
        internal PlayerActorDeclaration Declaration { get; }
        internal GameObject ActorRoot { get; }
        internal RuntimeContentOwner Owner { get; }
        internal ActorId ActorId
        {
            get
            {
                try { return Declaration != null ? Declaration.ActorId : default; }
                catch { return default; }
            }
        }
        internal bool IsValid => OccurrenceId.IsValid && PlayerSlotId.IsValid && Preparation.IsValid &&
            Declaration != null && ActorRoot != null && Owner.IsValid && ActorId.IsValid;
    }
}
