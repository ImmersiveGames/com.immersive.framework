using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds for generic actor declaration validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A generic actor validation issue kinds.")]
    public enum ActorSetIssueKind
    {
        None = 0,
        InvalidActorId = 10,
        InvalidDeclaration = 20,
        DuplicateActorId = 30,
        UnknownActorKind = 40,
        UnknownActorRole = 50
    }
}
