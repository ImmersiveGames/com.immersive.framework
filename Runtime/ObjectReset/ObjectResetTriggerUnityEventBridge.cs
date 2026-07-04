using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.Events;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// Optional Inspector bridge for ObjectResetTrigger Foundation events.
    /// The trigger remains the typed event publisher; this component only adapts those events to UnityEvent callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ObjectResetTrigger))]
    [AddComponentMenu("Immersive Framework/Object Reset/Object Reset Trigger Unity Event Bridge")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14G optional UnityEvent bridge for Object Reset authored triggers.")]
    public sealed class ObjectResetTriggerUnityEventBridge : MonoBehaviour
    {
        private ObjectResetTrigger _trigger;
        private IEventBinding _binding;

        [Header("Events")]
        [SerializeField] private UnityEvent requestSubmitted = new UnityEvent();
        [SerializeField] private UnityEvent requestSucceeded = new UnityEvent();
        [SerializeField] private UnityEvent requestSucceededWithParticipants = new UnityEvent();
        [SerializeField] private UnityEvent requestSucceededNoParticipants = new UnityEvent();
        [SerializeField] private UnityEvent requestCompletedWithWarnings = new UnityEvent();
        [SerializeField] private UnityEvent requestIgnored = new UnityEvent();
        [SerializeField] private UnityEvent requestFailed = new UnityEvent();
        [SerializeField] private UnityEvent requestCompleted = new UnityEvent();

        public UnityEvent RequestSubmitted => requestSubmitted;

        public UnityEvent RequestSucceeded => requestSucceeded;

        public UnityEvent RequestSucceededWithParticipants => requestSucceededWithParticipants;

        public UnityEvent RequestSucceededNoParticipants => requestSucceededNoParticipants;

        public UnityEvent RequestCompletedWithWarnings => requestCompletedWithWarnings;

        public UnityEvent RequestIgnored => requestIgnored;

        public UnityEvent RequestFailed => requestFailed;

        public UnityEvent RequestCompleted => requestCompleted;

        private void OnEnable()
        {
            _trigger = GetComponent<ObjectResetTrigger>();
            _binding = _trigger.SubscribeRequestEvents(OnObjectResetTriggerEvent);
        }

        private void OnDisable()
        {
            _binding?.Dispose();
            _binding = null;
            _trigger = null;
        }

        private void OnObjectResetTriggerEvent(ObjectResetTriggerEvent requestEvent)
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

                if (requestEvent.SucceededNoParticipants)
                {
                    requestSucceededNoParticipants?.Invoke();
                }
                else if (requestEvent.CompletedWithWarnings)
                {
                    requestCompletedWithWarnings?.Invoke();
                }
                else if (requestEvent.SucceededWithParticipants)
                {
                    requestSucceededWithParticipants?.Invoke();
                }
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
