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
            GameObject presentation)
        {
            PreparationToken = preparationToken;
            ActorDeclaration = actorDeclaration;
            Presentation = presentation;
        }

        internal PlayerActorPreparationToken PreparationToken { get; }
        internal PlayerSlotId PlayerSlotId => PreparationToken.PlayerSlotId;
        internal PlayerActorDeclaration ActorDeclaration { get; }
        internal GameObject Presentation { get; }

        internal bool IsValid =>
            PreparationToken.IsValid &&
            ActorDeclaration != null &&
            ActorDeclaration.transform != null &&
            Presentation != null &&
            Presentation.transform != null;
    }
}
