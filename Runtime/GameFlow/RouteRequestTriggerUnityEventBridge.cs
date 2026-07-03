using Immersive.Foundation.Events;
using UnityEngine;
using UnityEngine.Events;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Optional Inspector bridge for RouteRequestTrigger Foundation events.
    /// The trigger remains the typed event publisher; this component only adapts those events to UnityEvent callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RouteRequestTrigger))]
    [AddComponentMenu("Immersive Framework/Route Request Trigger Unity Event Bridge")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class RouteRequestTriggerUnityEventBridge : MonoBehaviour
    {
        private RouteRequestTrigger _trigger;
        private IEventBinding _binding;

        [Header("Events")]
        [SerializeField] private UnityEvent requestSubmitted;
        [SerializeField] private UnityEvent requestSucceeded;
        [SerializeField] private UnityEvent requestIgnored;
        [SerializeField] private UnityEvent requestFailed;
        [SerializeField] private UnityEvent requestCompleted;

        public UnityEvent RequestSubmitted => requestSubmitted;

        public UnityEvent RequestSucceeded => requestSucceeded;

        public UnityEvent RequestIgnored => requestIgnored;

        public UnityEvent RequestFailed => requestFailed;

        public UnityEvent RequestCompleted => requestCompleted;

        private void OnEnable()
        {
            _trigger = GetComponent<RouteRequestTrigger>();
            _binding = _trigger.SubscribeRequestEvents(OnRouteRequestEvent);
        }

        private void OnDisable()
        {
            _binding?.Dispose();
            _binding = null;
            _trigger = null;
        }

        private void OnRouteRequestEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null)
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                requestSubmitted?.Invoke();
                return;
            }

            if (!requestEvent.IsCompleted)
            {
                return;
            }

            if (requestEvent.Succeeded)
            {
                requestSucceeded?.Invoke();
            }
            else if (requestEvent.Ignored)
            {
                requestIgnored?.Invoke();
            }
            else if (requestEvent.Failed)
            {
                requestFailed?.Invoke();
            }

            requestCompleted?.Invoke();
        }
    }
}
