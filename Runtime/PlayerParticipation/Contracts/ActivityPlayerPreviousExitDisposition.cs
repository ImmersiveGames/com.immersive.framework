using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Describes how the previous Activity Player lifecycle exit was satisfied.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Explicit previous Activity exit disposition during Player handoff.")]
    public enum ActivityPlayerPreviousExitDisposition
    {
        None = 0,
        SupersededAwaitingCommit = 10,
        SupersededByCommittedHandoff = 20
    }
}
