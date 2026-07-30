using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.Events;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored readiness contribution. The ActivityFlow runtime discovers and starts it;
    /// gameplay completes or fails it through the typed public methods below.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Readiness Participant")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "M03 authorable Activity readiness participant.")]
    public sealed class ActivityReadinessParticipant : MonoBehaviour, IActivityContentExecutionParticipant
    {
        [Header("Identity")]
        [SerializeField] private string participantId;
        [SerializeField] private ActivityContentExecutionRequiredness requiredness = ActivityContentExecutionRequiredness.Required;
        [SerializeField] private int order;
        [Header("Callbacks")]
        [SerializeField] private UnityEvent preparationStarted = new UnityEvent();
        [SerializeField] private UnityEvent preparationReleased = new UnityEvent();
        [Header("Runtime Diagnostics")]
        [SerializeField] private ActivityReadinessParticipantState state;
        [SerializeField] private string lastReason;
        [SerializeField] private int occurrence;

        internal event Action<ActivityReadinessParticipant> StateChanged;

        public string ParticipantId => participantId;
        public ActivityContentExecutionRequiredness Requiredness => requiredness;
        public ActivityReadinessParticipantState State => state;
        public string LastReason => lastReason;
        public int Occurrence => occurrence;
        public UnityEvent PreparationStarted => preparationStarted;
        public UnityEvent PreparationReleased => preparationReleased;

        public void CompletePreparation()
        {
            TrySetTerminalState(ActivityReadinessParticipantState.Completed, "Completed");
        }

        public void FailPreparation(string reason)
        {
            lastReason = string.IsNullOrWhiteSpace(reason) ? "Failed" : reason;
            TrySetTerminalState(ActivityReadinessParticipantState.Failed, lastReason);
        }

        ActivityContentExecutionParticipantDescriptor IActivityContentExecutionParticipant.GetActivityContentExecutionDescriptor()
        {
            RuntimeContentId id = RuntimeContentId.From(participantId);
            return requiredness == ActivityContentExecutionRequiredness.Required
                ? ActivityContentExecutionParticipantDescriptor.Required(id, true, true, order, name, nameof(ActivityReadinessParticipant), "Authorable readiness participant")
                : ActivityContentExecutionParticipantDescriptor.Optional(id, true, true, order, name, nameof(ActivityReadinessParticipant), "Authorable readiness participant");
        }

        ActivityContentExecutionResult IActivityContentExecutionParticipant.ExecuteActivityContent(ActivityContentExecutionRequest request)
        {
            if (request.Phase == ActivityContentExecutionPhase.Exit)
            {
                Release("ActivityExit");
                return ActivityContentExecutionResult.Success(request, nameof(ActivityReadinessParticipant), "Released", "Activity readiness participant released on Activity exit.");
            }

            occurrence++;
            state = ActivityReadinessParticipantState.Preparing;
            lastReason = "Preparing";
            preparationStarted?.Invoke();
            StateChanged?.Invoke(this);

            // Existing execution contracts are synchronous. The initial result preserves
            // their Required/Optional failure semantics while the official adapter publishes
            // subsequent semantic completion to presentation.
            return request.IsRequired
                ? ActivityContentExecutionResult.BlockingFailure(request, 1, nameof(ActivityReadinessParticipant), "Preparing", "Required readiness participant is awaiting semantic completion.")
                : ActivityContentExecutionResult.NonBlockingFailure(request, 1, nameof(ActivityReadinessParticipant), "Preparing", "Optional readiness participant is awaiting semantic completion.");
        }

        internal bool IsValidForDiscovery(out string issue)
        {
            if (string.IsNullOrWhiteSpace(participantId))
            {
                issue = "Participant Id is required.";
                return false;
            }

            if (requiredness == ActivityContentExecutionRequiredness.Unknown)
            {
                issue = "Requiredness must be Required or Optional.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal void Release(string reason)
        {
            state = ActivityReadinessParticipantState.Released;
            lastReason = reason;
            preparationReleased?.Invoke();
            StateChanged?.Invoke(this);
        }

        private void TrySetTerminalState(ActivityReadinessParticipantState terminalState, string reason)
        {
            if (state != ActivityReadinessParticipantState.Preparing)
            {
                lastReason = "LateCompletionRejected";
                StateChanged?.Invoke(this);
                return;
            }

            state = terminalState;
            lastReason = reason;
            StateChanged?.Invoke(this);
        }
    }
}
