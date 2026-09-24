using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// ADR-020 Activity-context release status for one exact Session Player Leave occurrence.
    /// This stage never commits Slot vacancy or clears Session-scoped Actor selection.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Session Player Activity representation release status.")]
    internal enum SessionPlayerActivityRepresentationReleaseStatus
    {
        None = 0,
        SucceededReleased = 10,
        SucceededAlreadyReleased = 20,
        SucceededNoCurrentRepresentation = 30,
        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedLeaveCorrelation = 120,
        RejectedTransitionInFlight = 130,
        RejectedRepresentationCorrelation = 140,
        FailedGameplayRelease = 200,
        FailedActorRelease = 210,
        FailedInvariant = 220
    }
}
