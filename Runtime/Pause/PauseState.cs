using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Logical framework pause state.
    /// This does not imply menu visibility, overlay visibility, input map state or Time.timeScale value.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B Pause state primitive; logical state only.")]
    public enum PauseState
    {
        /// <summary>Invalid default value. Do not use for canonical Pause snapshots or results.</summary>
        Unknown = 0,

        Running = 10,
        Paused = 20
    }
}
