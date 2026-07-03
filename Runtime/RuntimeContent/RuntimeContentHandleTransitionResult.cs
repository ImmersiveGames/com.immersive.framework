using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic result for one passive runtime content handle state transition.
    /// It records the state change decision and request context without executing release behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8C runtime content handle transition result; no release execution.")]
    public readonly struct RuntimeContentHandleTransitionResult
    {
        public RuntimeContentHandleTransitionResult(
            RuntimeContentIdentity identity,
            RuntimeContentState previousState,
            RuntimeContentState currentState,
            RuntimeContentState requestedState,
            RuntimeContentHandleTransitionStatus status,
            string source,
            string reason,
            string message)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }

            if (!Enum.IsDefined(typeof(RuntimeContentState), previousState) || previousState == RuntimeContentState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(previousState), previousState, "Previous runtime content state must be explicit.");
            }

            if (!Enum.IsDefined(typeof(RuntimeContentState), currentState) || currentState == RuntimeContentState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, "Current runtime content state must be explicit.");
            }

            if (!Enum.IsDefined(typeof(RuntimeContentState), requestedState) || requestedState == RuntimeContentState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedState), requestedState, "Requested runtime content state must be explicit.");
            }

            if (!Enum.IsDefined(typeof(RuntimeContentHandleTransitionStatus), status) || status == RuntimeContentHandleTransitionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Runtime content handle transition status must be explicit.");
            }

            Identity = identity;
            PreviousState = previousState;
            CurrentState = currentState;
            RequestedState = requestedState;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public RuntimeContentIdentity Identity { get; }

        public RuntimeContentOwner Owner => Identity.Owner;

        public RuntimeContentScope Scope => Identity.Scope;

        public RuntimeContentId ContentId => Identity.ContentId;

        public RuntimeContentState PreviousState { get; }

        public RuntimeContentState CurrentState { get; }

        public RuntimeContentState RequestedState { get; }

        public RuntimeContentHandleTransitionStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Applied => Status == RuntimeContentHandleTransitionStatus.Applied;

        public bool Ignored => Status == RuntimeContentHandleTransitionStatus.IgnoredAlreadyInState;

        public bool Rejected => Status == RuntimeContentHandleTransitionStatus.RejectedInvalidTransition;

        public string ToDiagnosticString()
        {
            string source = Source.ToDiagnosticText();
            string reason = Reason.ToDiagnosticText();
            string message = Message.ToDiagnosticText();
            return $"identity='{Identity.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' previousState='{PreviousState}' currentState='{CurrentState}' requestedState='{RequestedState}' status='{Status}' source='{source}' reason='{reason}' message='{message}'";
        }

        public static RuntimeContentHandleTransitionResult AppliedResult(
            RuntimeContentIdentity identity,
            RuntimeContentState previousState,
            RuntimeContentState currentState,
            string source,
            string reason,
            string message)
        {
            return new RuntimeContentHandleTransitionResult(
                identity,
                previousState,
                currentState,
                currentState,
                RuntimeContentHandleTransitionStatus.Applied,
                source,
                reason,
                message);
        }

        public static RuntimeContentHandleTransitionResult IgnoredAlreadyInState(
            RuntimeContentIdentity identity,
            RuntimeContentState state,
            RuntimeContentState requestedState,
            string source,
            string reason,
            string message)
        {
            return new RuntimeContentHandleTransitionResult(
                identity,
                state,
                state,
                requestedState,
                RuntimeContentHandleTransitionStatus.IgnoredAlreadyInState,
                source,
                reason,
                message);
        }

        public static RuntimeContentHandleTransitionResult RejectedInvalidTransition(
            RuntimeContentIdentity identity,
            RuntimeContentState currentState,
            RuntimeContentState requestedState,
            string source,
            string reason,
            string message)
        {
            return new RuntimeContentHandleTransitionResult(
                identity,
                currentState,
                currentState,
                requestedState,
                RuntimeContentHandleTransitionStatus.RejectedInvalidTransition,
                source,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
