using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerEntry
{
    /// <summary>
    /// API status: Experimental. Passive PlayerEntry contract that connects stable PlayerSlot identity,
    /// stable Actor identity and Actor readiness evidence without owning join, spawn, view binding or control binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49D passive PlayerEntry contract.")]
    public interface IPlayerEntry
    {
        PlayerSlotId PlayerSlotId { get; }

        ActorId ActorId { get; }

        PlayerEntryState State { get; }

        ActorReadinessSnapshot ActorReadiness { get; }

        bool IsActorReadyForView { get; }

        bool IsActorReadyForControl { get; }

        PlayerEntrySnapshot CreateSnapshot();
    }
}
