using System;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal interface IPlayerPreparedActorOccurrenceSource
    {
        event Action<PlayerSlotId> CurrentActorInvalidated;

        string SessionContextId { get; }

        bool TryGetCurrentActorOccurrence(
            PlayerSlotId playerSlotId,
            out PlayerPreparedActorOccurrence occurrence);
    }
}
