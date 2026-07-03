using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Optional base class for MonoBehaviours under an Activity Local Visibility Adapter root.
    /// Prefer this for scene-authored Activity content that needs lifecycle callbacks
    /// without manually implementing IActivityContentLifecycleReceiver.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public abstract class ActivityContentBehaviour : MonoBehaviour, IActivityContentLifecycleReceiver
    {

        void IActivityContentLifecycleReceiver.OnActivityContentEntered(ActivityContentLifecycleContext context)
        {

            OnActivityContentEntered(context);
        }

        void IActivityContentLifecycleReceiver.OnActivityContentExited(ActivityContentLifecycleContext context)
        {

            OnActivityContentExited(context);
        }

        /// <summary>
        /// Called after this Activity content root has entered the active Activity scope.
        /// </summary>
        protected virtual void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
        }

        /// <summary>
        /// Called before this Activity content root leaves the active Activity scope.
        /// </summary>
        protected virtual void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
        }
    }
}
