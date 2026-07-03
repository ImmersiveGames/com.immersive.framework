using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Deterministic status for a passive InputMode request preview/result.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A InputMode request status vocabulary.")]
    public enum InputModeRequestStatus
    {
        Unknown = 0,
        Succeeded = 10,
        IgnoredAlreadyInMode = 20,
        FailedInvalidCurrentState = 30,
        FailedInvalidTargetMode = 40
    }
}
