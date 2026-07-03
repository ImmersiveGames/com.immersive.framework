using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Passive canonical handle for one runtime-created content identity.
    /// The handle tracks ownership and release state only; it does not instantiate, destroy, pool, register roots or bind anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8C passive RuntimeContentHandle with release state; no materialization or release execution.")]
    public sealed class RuntimeContentHandle
    {
        private RuntimeContentState _state;
        private string _source;
        private string _reason;
        private string _message;

        public RuntimeContentHandle(
            RuntimeContentIdentity identity,
            RuntimeContentState initialState,
            string source,
            string reason,
            string message)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }

            ValidateInitialState(initialState);

            Identity = identity;
            _state = initialState;
            _source = Normalize(source);
            _reason = Normalize(reason);
            _message = Normalize(message);
        }

        public RuntimeContentHandle(RuntimeContentIdentity identity)
            : this(
                identity,
                RuntimeContentState.Declared,
                string.Empty,
                string.Empty,
                "Runtime content handle declared.")
        {
        }

        public RuntimeContentIdentity Identity { get; }

        public RuntimeContentOwner Owner => Identity.Owner;

        public RuntimeContentScope Scope => Identity.Scope;

        public RuntimeContentId ContentId => Identity.ContentId;

        public RuntimeContentState State => _state;

        public string Source => _source ?? string.Empty;

        public string Reason => _reason ?? string.Empty;

        public string Message => _message ?? string.Empty;

        public bool IsDeclared => _state == RuntimeContentState.Declared;

        public bool IsMaterialized => _state == RuntimeContentState.Materialized;

        public bool IsReleaseRequested => _state == RuntimeContentState.ReleaseRequested;

        public bool IsReleased => _state == RuntimeContentState.Released;

        public bool IsReleaseFailed => _state == RuntimeContentState.ReleaseFailed;

        public bool CanRequestRelease => _state is RuntimeContentState.Declared or RuntimeContentState.Materialized or RuntimeContentState.ReleaseFailed;

        public bool CanMarkMaterialized => _state == RuntimeContentState.Declared;

        public bool CanMarkReleased => _state == RuntimeContentState.ReleaseRequested;

        public bool CanMarkReleaseFailed => _state == RuntimeContentState.ReleaseRequested;

        public RuntimeContentHandleTransitionResult MarkMaterialized(string source, string reason)
        {
            if (_state == RuntimeContentState.Materialized)
            {
                return RuntimeContentHandleTransitionResult.IgnoredAlreadyInState(
                    Identity,
                    _state,
                    RuntimeContentState.Materialized,
                    source,
                    reason,
                    "Runtime content handle is already materialized.");
            }

            if (!CanMarkMaterialized)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    _state,
                    RuntimeContentState.Materialized,
                    source,
                    reason,
                    $"Runtime content handle cannot transition from '{_state}' to '{RuntimeContentState.Materialized}'.");
            }

            return ApplyState(
                RuntimeContentState.Materialized,
                source,
                reason,
                "Runtime content handle marked as materialized.");
        }

        public RuntimeContentHandleTransitionResult RequestRelease(string source, string reason)
        {
            if (_state == RuntimeContentState.ReleaseRequested)
            {
                return RuntimeContentHandleTransitionResult.IgnoredAlreadyInState(
                    Identity,
                    _state,
                    RuntimeContentState.ReleaseRequested,
                    source,
                    reason,
                    "Runtime content release was already requested.");
            }

            if (_state == RuntimeContentState.Released)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    _state,
                    RuntimeContentState.ReleaseRequested,
                    source,
                    reason,
                    "Runtime content release cannot be requested after the handle is already released.");
            }

            if (!CanRequestRelease)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    _state,
                    RuntimeContentState.ReleaseRequested,
                    source,
                    reason,
                    $"Runtime content handle cannot transition from '{_state}' to '{RuntimeContentState.ReleaseRequested}'.");
            }

            return ApplyState(
                RuntimeContentState.ReleaseRequested,
                source,
                reason,
                "Runtime content release requested.");
        }

        public RuntimeContentHandleTransitionResult MarkReleased(string source, string reason)
        {
            if (_state == RuntimeContentState.Released)
            {
                return RuntimeContentHandleTransitionResult.IgnoredAlreadyInState(
                    Identity,
                    _state,
                    RuntimeContentState.Released,
                    source,
                    reason,
                    "Runtime content is already released.");
            }

            if (!CanMarkReleased)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    _state,
                    RuntimeContentState.Released,
                    source,
                    reason,
                    $"Runtime content handle cannot transition from '{_state}' to '{RuntimeContentState.Released}' without a release request.");
            }

            return ApplyState(
                RuntimeContentState.Released,
                source,
                reason,
                "Runtime content marked as released.");
        }

        public RuntimeContentHandleTransitionResult MarkReleaseFailed(string source, string reason, string failureMessage)
        {
            string normalizedFailureMessage = Normalize(failureMessage);
            if (!CanMarkReleaseFailed)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    _state,
                    RuntimeContentState.ReleaseFailed,
                    source,
                    reason,
                    !string.IsNullOrWhiteSpace(normalizedFailureMessage)
                        ? normalizedFailureMessage
                        : $"Runtime content handle cannot transition from '{_state}' to '{RuntimeContentState.ReleaseFailed}' without a release request.");
            }

            return ApplyState(
                RuntimeContentState.ReleaseFailed,
                source,
                reason,
                !string.IsNullOrWhiteSpace(normalizedFailureMessage)
                    ? normalizedFailureMessage
                    : "Runtime content release failed.");
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"identity='{Identity.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' owner='{Owner.OwnerName}' state='{_state}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static RuntimeContentHandle Declared(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            return new RuntimeContentHandle(
                identity,
                RuntimeContentState.Declared,
                source,
                reason,
                "Runtime content handle declared.");
        }

        public static RuntimeContentHandle Materialized(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            return new RuntimeContentHandle(
                identity,
                RuntimeContentState.Materialized,
                source,
                reason,
                "Runtime content handle materialized.");
        }

        private RuntimeContentHandleTransitionResult ApplyState(
            RuntimeContentState newState,
            string source,
            string reason,
            string message)
        {
            var previousState = _state;
            _state = newState;
            _source = Normalize(source);
            _reason = Normalize(reason);
            _message = Normalize(message);

            return RuntimeContentHandleTransitionResult.AppliedResult(
                Identity,
                previousState,
                _state,
                _source,
                _reason,
                _message);
        }

        private static void ValidateInitialState(RuntimeContentState initialState)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentState), initialState) || initialState == RuntimeContentState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(initialState), initialState, "Runtime content handle initial state must be explicit.");
            }

            if (initialState is RuntimeContentState.ReleaseRequested or RuntimeContentState.Released or RuntimeContentState.ReleaseFailed)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialState),
                    initialState,
                    "Runtime content handle cannot start in a release terminal or release-requested state.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
