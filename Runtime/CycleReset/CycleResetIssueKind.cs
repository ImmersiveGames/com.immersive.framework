using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Structured issue kind emitted while building or executing Cycle Reset.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset issue kind primitive.")]
    public enum CycleResetIssueKind
    {
        Unknown = 0,
        InvalidRequest = 5,
        NullParticipant = 10,
        InvalidDescriptor = 20,
        UnsupportedScope = 30,
        DuplicateParticipantId = 40,
        InvalidParticipantResult = 50,
        ParticipantException = 60,
        RequiredParticipantFailed = 70,
        OptionalParticipantFailed = 80,
        RequestAlreadyInFlight = 90,
        ParticipantSourceException = 100
    }
}
