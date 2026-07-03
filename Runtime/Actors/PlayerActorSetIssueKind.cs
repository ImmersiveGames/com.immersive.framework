using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds for PlayerActor declaration validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A PlayerActor validation issue kinds.")]
    public enum PlayerActorSetIssueKind
    {
        None = 0,
        InvalidActorId = 10,
        InvalidDeclaration = 20,
        MissingRequiredPlayerInputEvidence = 30,
        DuplicatePlayerActorId = 40
    }
}
