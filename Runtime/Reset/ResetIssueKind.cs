using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Issue kinds emitted by the Reset subject registry and future reset execution layer.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset issue kinds.")]
    public enum ResetIssueKind
    {
        Unknown = 0,
        InvalidRequest = 10,
        InvalidSubject = 20,
        InvalidParticipant = 30,
        SubjectNotFound = 40,
        DuplicateSubject = 50,
        DuplicateParticipant = 60,
        InvalidHandle = 70,
        ForeignOwner = 80,
        StaleOwner = 90,
        NoSubjects = 100,
        NoParticipants = 110,
        Exception = 900
    }
}
