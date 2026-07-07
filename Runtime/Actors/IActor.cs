using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Minimal actor identity surface.
    /// It does not imply materialization, movement, input behavior, save ownership or lifecycle ownership.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A/F45A minimal actor identity contract.")]
    public interface IActor
    {
        ActorId ActorId { get; }

        ActorKind ActorKind { get; }

        ActorRole ActorRole { get; }

        string ActorDisplayName { get; }
    }
}
