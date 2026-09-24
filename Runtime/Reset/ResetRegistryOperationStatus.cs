using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Result status for subject and participant registry operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset registry operation status.")]
    public enum ResetRegistryOperationStatus
    {
        Unknown = 0,
        Registered = 10,
        Unregistered = 20,
        AlreadyUnregistered = 30,
        RejectedInvalidRequest = 100,
        RejectedInvalidSubject = 110,
        RejectedInvalidParticipant = 120,
        RejectedSubjectNotFound = 130,
        RejectedDuplicateSubjectId = 140,
        RejectedDuplicateParticipantId = 150,
        RejectedInvalidHandle = 160,
        RejectedForeignOwner = 170
    }
}
