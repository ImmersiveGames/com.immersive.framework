using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Status for resolving a ResetSelectionConfig into explicit ResetSubject ids.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12D Reset selection resolution status.")]
    public enum ResetSelectionResolutionStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededNoSubjects = 20,
        Failed = 30,
        RejectedInvalidRequest = 40,
        RejectedRuntimeUnavailable = 50,
        RejectedRuntimeContextUnavailable = 60
    }
}
