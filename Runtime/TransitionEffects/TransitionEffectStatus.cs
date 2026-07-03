using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive status for one Transition Effect request/result.
    /// This is not visual progress and does not imply an effect adapter is running.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B Transition Effect status primitive; passive diagnostics only.")]
    public enum TransitionEffectStatus
    {
        /// <summary>Invalid default value.</summary>
        Unknown = 0,

        Planned = 10,
        Running = 20,
        Skipped = 30,
        Succeeded = 40,
        CompletedWithWarnings = 50,
        Failed = 60,
        Rejected = 70,
        MissingAdapter = 80
    }
}
