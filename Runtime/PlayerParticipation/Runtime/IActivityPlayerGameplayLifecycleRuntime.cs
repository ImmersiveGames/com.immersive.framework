using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal interface IActivityPlayerGameplayLifecycleRuntime
    {
        bool TryHandleCommittedPreviousExit(
            ActivityContentExecutionRequest request,
            out bool handled,
            out string issue);

        bool TryAdoptCommittedTarget(
            ActivityContentExecutionRequest request,
            IReadOnlyList<PlayerSlotRuntimeSnapshot> projectedSlots,
            out IReadOnlyList<ActivityPlayerGameplayAdoptedSlot> adoptedSlots,
            out string issue);

        bool TryReleaseGameplayBeforeActor(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason,
            out bool released,
            out string issue);
    }
}
