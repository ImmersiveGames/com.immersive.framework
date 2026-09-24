using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Outcome/status of a transition operation or transition step.
    /// This is logical diagnostics data and does not imply visual progress or loading UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B Transition status primitive; passive result status only.")]
    public enum TransitionStatus
    {
        /// <summary>Invalid default value. Do not use for canonical transition results.</summary>
        Unknown = 0,

        Planned = 10,
        Running = 20,
        Observed = 30,
        Skipped = 40,
        Succeeded = 50,
        CompletedWithWarnings = 60,
        Failed = 70,
        Rejected = 80,
        Cancelled = 90
    }
}
