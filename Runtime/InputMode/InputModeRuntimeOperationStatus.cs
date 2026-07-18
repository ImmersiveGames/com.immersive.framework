using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Explicit operation status for the scoped InputMode runtime authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 scoped InputMode runtime operation status.")]
    public enum InputModeRuntimeOperationStatus
    {
        Unknown = 0,
        SucceededPrepared = 10,
        SucceededCommitted = 20,
        IgnoredAlreadyCurrent = 30,
        RolledBack = 40,
        RejectedInvalidRequest = 100,
        RejectedOperationInFlight = 110,
        RejectedForeignOrStaleTransaction = 120
    }
}
