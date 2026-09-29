using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal readonly struct PlayerPreparedActorOccurrence
    {
        internal PlayerPreparedActorOccurrence(
            PlayerActorPreparationToken preparationToken,
            PlayerActorDeclaration actorDeclaration,
            GameObject visualContent)
        {
            PreparationToken = preparationToken;
            ActorDeclaration = actorDeclaration;
            VisualContent = visualContent;
        }

        internal PlayerActorPreparationToken PreparationToken { get; }
        internal PlayerSlotId PlayerSlotId => PreparationToken.PlayerSlotId;
        internal PlayerActorDeclaration ActorDeclaration { get; }
        internal GameObject VisualContent { get; }

        internal bool IsValid =>
            PreparationToken.IsValid &&
            ActorDeclaration != null &&
            ActorDeclaration.transform != null;
    }
}
