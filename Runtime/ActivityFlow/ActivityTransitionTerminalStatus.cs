using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Terminal result of one Activity transition transaction.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ARCH-A2 Activity transition terminal result vocabulary.")]
    internal enum ActivityTransitionTerminalStatus
    {
        Unknown = 0,
        None = 10,
        FailedBeforeCommit = 20,
        CommittedReady = 30,
        CommittedNotReady = 40,
        CommittedFinalizationFailed = 50
    }
}
