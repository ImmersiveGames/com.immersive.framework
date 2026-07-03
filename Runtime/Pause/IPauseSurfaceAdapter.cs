using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Unity-facing Pause surface adapter boundary.
    /// A Pause surface adapter presents the current logical Pause snapshot, but it does not own Pause state,
    /// input binding, Gate evaluation, Route/Activity lifecycle or Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F27A Pause UIGlobal surface adapter boundary.")]
    public interface IPauseSurfaceAdapter
    {
        string AdapterName { get; }

        bool Supports(PauseSnapshot snapshot);

        void Apply(PauseSnapshot snapshot);
    }
}
