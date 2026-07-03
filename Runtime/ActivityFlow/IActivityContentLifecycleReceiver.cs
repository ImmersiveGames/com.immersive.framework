using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Implement this on a component under an Activity Local Visibility Adapter root when scene-authored content
    /// needs to react to Activity enter/exit without owning Activity Flow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public interface IActivityContentLifecycleReceiver
    {
        void OnActivityContentEntered(ActivityContentLifecycleContext context);

        void OnActivityContentExited(ActivityContentLifecycleContext context);
    }
}
