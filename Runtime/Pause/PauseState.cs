
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Logical framework pause state.
    /// This does not imply menu visibility, overlay visibility, input map state or Time.timeScale value.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum PauseState
    {
        /// <summary>Invalid default value. Do not use for canonical Pause snapshots or results.</summary>
        Unknown = 0,

        Running = 10,
        Paused = 20
    }
}
