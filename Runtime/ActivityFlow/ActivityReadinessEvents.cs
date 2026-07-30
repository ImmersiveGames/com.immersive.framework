using UnityEngine;
using UnityEngine.Events;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>Explicit scene observer for Activity readiness presentation. It never decides readiness.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Readiness Events")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "M03 authorable Activity readiness UI observer.")]
    public sealed class ActivityReadinessEvents : MonoBehaviour
    {
        [SerializeField] private UnityEvent preparing = new UnityEvent();
        [SerializeField] private UnityEvent ready = new UnityEvent();
        [SerializeField] private UnityEvent notReady = new UnityEvent();
        [SerializeField] private string lastReason;
        [SerializeField] private int lastRevision;
        private ActivityReadinessSnapshot _lastSnapshot;

        public UnityEvent Preparing => preparing;
        public UnityEvent Ready => ready;
        public UnityEvent NotReady => notReady;
        public string LastReason => lastReason;
        public int LastRevision => lastRevision;
        public ActivityReadinessSnapshot LastSnapshot => _lastSnapshot;

        internal void Apply(ActivityReadinessSnapshot snapshot)
        {
            if (snapshot.Revision == lastRevision)
            {
                return;
            }

            lastRevision = snapshot.Revision;
            lastReason = snapshot.Reason;
            _lastSnapshot = snapshot;
            if (snapshot.IsReady)
            {
                ready?.Invoke();
            }
            else if (snapshot.IsPreparing)
            {
                preparing?.Invoke();
            }
            else
            {
                notReady?.Invoke();
            }
        }
    }
}
