
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Unity-facing Pause surface adapter boundary.
    /// A Pause surface adapter presents the current logical Pause snapshot, but it does not own Pause state,
    /// input binding, Gate evaluation, Route/Activity lifecycle or Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public interface IPauseSurfaceAdapter
    {
        string AdapterName { get; }

        bool Supports(PauseSnapshot snapshot);

        void Apply(PauseSnapshot snapshot);
    }
}
