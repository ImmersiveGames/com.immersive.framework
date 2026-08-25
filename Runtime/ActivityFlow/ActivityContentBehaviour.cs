using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Optional base class for MonoBehaviours under an ActivityContentContribution root.
    /// Prefer this for scene-authored Activity content that needs lifecycle callbacks
    /// without manually implementing IActivityContentLifecycleReceiver.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public abstract class ActivityContentBehaviour : MonoBehaviour, IActivityContentLifecycleReceiver
    {
        public bool IsActivityContentActive { get; private set; }

        public bool HasActivityContentContext { get; private set; }

        public ActivityContentLifecycleContext LastActivityContentContext { get; private set; }

        public ActivityAsset ActiveActivity => IsActivityContentActive
            ? LastActivityContentContext.Activity
            : null;

        public ActivityContentContribution ActivityContentContribution =>
            LastActivityContentContext.Contribution;

        public string LifecycleSource => HasActivityContentContext
            ? LastActivityContentContext.Source
            : string.Empty;

        public string LifecycleReason => HasActivityContentContext
            ? LastActivityContentContext.Reason
            : string.Empty;

        void IActivityContentLifecycleReceiver.OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            HasActivityContentContext = true;
            LastActivityContentContext = context;
            IsActivityContentActive = true;

            OnActivityContentEntered(context);
        }

        void IActivityContentLifecycleReceiver.OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            HasActivityContentContext = true;
            LastActivityContentContext = context;
            IsActivityContentActive = false;

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
