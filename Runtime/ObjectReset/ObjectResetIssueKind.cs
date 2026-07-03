using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Issue kinds emitted while validating or resolving Object Reset requests.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset issue kinds; no Unity object reset side effects.")]
    public enum ObjectResetIssueKind
    {
        Unknown = 0,
        InvalidRequest = 10,
        RequestAlreadyInFlight = 25,
        RuntimeContextUnavailable = 30,
        TargetNotFound = 40,
        TargetScopeMismatch = 50,
        TargetOwnerMissing = 60,
        ForeignOrStaleTarget = 80,
        ParticipantSourceException = 90,
        InvalidParticipant = 100,
        DuplicateParticipant = 110,
        NoParticipants = 120,
        ForeignOrStaleParticipant = 130,
        RequiredParticipantFailed = 140,
        OptionalParticipantFailed = 150,
        // Reserved for a future uncategorized adapter/participant execution failure.
        Exception = 900
    }
}
