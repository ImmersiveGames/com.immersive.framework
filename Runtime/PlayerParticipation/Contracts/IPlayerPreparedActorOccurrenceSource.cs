using System;
using System.Collections.Generic;
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

        bool TryGetCurrentPhysicalActorOccurrence(
            PlayerSlotId playerSlotId,
            out PlayerActorPhysicalOccurrence occurrence);

        void CollectCurrentPhysicalActorOccurrences(
            ICollection<PlayerActorPhysicalOccurrence> occurrences);
    }
}
