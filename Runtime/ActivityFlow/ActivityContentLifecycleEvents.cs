using UnityEngine;
using UnityEngine.Events;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored UnityEvent bridge for Activity content lifecycle.
    /// Use this under an ActivityLocalVisibilityAdapter root when no custom gameplay script is needed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Content Lifecycle Events")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityContentLifecycleEvents : ActivityContentBehaviour
    {
        [Header("Activity Content Lifecycle")]
        [SerializeField] private UnityEvent entered = new UnityEvent();
        [SerializeField] private UnityEvent exited = new UnityEvent();

        public UnityEvent Entered => entered;

        public UnityEvent Exited => exited;

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            entered?.Invoke();
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            exited?.Invoke();
        }
    }
}
