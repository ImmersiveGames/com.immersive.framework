using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Lifetime state of the scene-local consumer binding. This reports
    /// transport availability only; it is never Player Session state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-06 explicit scoped consumer binding lifetime status.")]
    public enum LocalPlayerProvisioningConsumerBindingState
    {
        Unbound = 0,
        Bound = 10,
        Unavailable = 20,
        Released = 30
    }

    /// <summary>
    /// Scene-local receiving point for the Framework Core injected provisioning
    /// port. It contains no persistent authority reference and executes no
    /// provisioning operation by itself.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Local Player Provisioning Consumer Access")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-03 scene-local binding for scoped Local Player provisioning access.")]
    public sealed class LocalPlayerProvisioningConsumerAccessBinding : MonoBehaviour
    {
        private const string UnboundDiagnostic =
            "Local Player provisioning consumer access is not bound to a live framework scope.";

        [SerializeField]
        [Tooltip(
            "The Framework lifecycle scope that owns this consumer binding. This does not configure Player Session state.")]
        private LocalPlayerProvisioningConsumerScope scope =
            LocalPlayerProvisioningConsumerScope.Activity;

        [NonSerialized]
        private ILocalPlayerProvisioningConsumerAccess _access;

        [NonSerialized]
        private string _diagnostic = UnboundDiagnostic;

        [NonSerialized]
        private LocalPlayerProvisioningConsumerBindingState _bindingState =
            LocalPlayerProvisioningConsumerBindingState.Unbound;

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public bool IsBound => _bindingState ==
            LocalPlayerProvisioningConsumerBindingState.Bound &&
            _access != null && _access.Snapshot.IsAvailable;

        public LocalPlayerProvisioningConsumerBindingState BindingState =>
            _access != null && _access.Snapshot.IsDisposed
                ? LocalPlayerProvisioningConsumerBindingState.Released
                : _bindingState;

        public string Diagnostic => _access != null
            ? _access.Snapshot.Diagnostic
            : GetUnboundDiagnostic();

        public LocalPlayerProvisioningConsumerAccessSnapshot Snapshot =>
            _access != null
                ? _access.Snapshot
                : LocalPlayerProvisioningConsumerAccessSnapshot.Unavailable(
                    scope,
                    default,
                    GetUnboundDiagnostic());

        public bool TryGetAccess(
            out ILocalPlayerProvisioningConsumerAccess resolvedAccess,
            out string issue)
        {
            resolvedAccess = _access;
            if (resolvedAccess == null || !resolvedAccess.Snapshot.IsAvailable)
            {
                issue = Diagnostic;
                if (resolvedAccess != null && resolvedAccess.Snapshot.IsDisposed)
                {
                    _bindingState =
                        LocalPlayerProvisioningConsumerBindingState.Released;
                }

                resolvedAccess = null;
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal bool TryBind(
            ILocalPlayerProvisioningConsumerAccess scopedAccess,
            LocalPlayerProvisioningConsumerScope actualScope,
            out string issue)
        {
            if (!scope.IsDefinedScope())
            {
                issue =
                    "Local Player provisioning consumer binding requires an explicit Route or Activity scope.";
                _diagnostic = issue;
                _bindingState =
                    LocalPlayerProvisioningConsumerBindingState.Unavailable;
                LogBindingWarning("Player provisioning consumer access binding rejected.",
                    "InvalidAuthoredScope", actualScope, issue);
                return false;
            }

            if (scope != actualScope)
            {
                issue =
                    $"Local Player provisioning consumer binding scope '{scope}' does not match the active '{actualScope}' scope.";
                _diagnostic = issue;
                _bindingState =
                    LocalPlayerProvisioningConsumerBindingState.Unavailable;
                LogBindingDebug("Player provisioning consumer access binding skipped.",
                    "ScopeMismatch", actualScope, issue);
                return false;
            }

            if (scopedAccess == null || !scopedAccess.Snapshot.IsAvailable)
            {
                issue = scopedAccess != null
                    ? scopedAccess.Snapshot.Diagnostic
                    : "Local Player provisioning consumer binding received no scoped access.";
                _diagnostic = issue;
                _bindingState =
                    LocalPlayerProvisioningConsumerBindingState.Unavailable;
                LogBindingWarning("Player provisioning consumer access binding rejected.",
                    "AccessUnavailable", actualScope, issue);
                return false;
            }

            if (_access != null && !ReferenceEquals(_access, scopedAccess))
            {
                issue =
                    "Local Player provisioning consumer binding is already bound to a different live scope.";
                _diagnostic = issue;
                LogBindingWarning("Player provisioning consumer access binding rejected.",
                    "AlreadyBound", actualScope, issue);
                return false;
            }

            bool wasAlreadyBound = ReferenceEquals(_access, scopedAccess) &&
                _bindingState == LocalPlayerProvisioningConsumerBindingState.Bound;

            _access = scopedAccess;
            _diagnostic = scopedAccess.Snapshot.Diagnostic;
            _bindingState = LocalPlayerProvisioningConsumerBindingState.Bound;
            issue = string.Empty;

            if (wasAlreadyBound)
            {
                LogBindingTrace("Player provisioning consumer access binding is already current.",
                    "Idempotent", actualScope, _diagnostic);
            }
            else
            {
                LogBindingDebug("Player provisioning consumer access bound.",
                    "Bound", actualScope, _diagnostic);
            }

            return true;
        }

        internal void Release(string reason, bool isStale = false)
        {
            string resolvedReason = string.IsNullOrWhiteSpace(reason)
                ? UnboundDiagnostic
                : reason.Trim();
            LocalPlayerProvisioningConsumerBindingState nextState = isStale
                ? LocalPlayerProvisioningConsumerBindingState.Released
                : LocalPlayerProvisioningConsumerBindingState.Unavailable;
            bool changed = _access != null ||
                _bindingState != nextState ||
                !string.Equals(_diagnostic, resolvedReason, StringComparison.Ordinal);

            _access = null;
            _diagnostic = resolvedReason;
            _bindingState = nextState;

            if (!changed)
            {
                return;
            }

            LogBindingDebug(
                "Player provisioning consumer access released.",
                isStale ? "StaleReleased" : "Released",
                scope,
                resolvedReason);
        }

        private string GetUnboundDiagnostic()
        {
            return !scope.IsDefinedScope()
                ? "Local Player provisioning consumer binding requires an explicit Route or Activity scope."
                : _diagnostic;
        }

        private void OnDestroy()
        {
            bool hadLiveBinding = _access != null ||
                _bindingState == LocalPlayerProvisioningConsumerBindingState.Bound;

            _access = null;
            _diagnostic =
                "Local Player provisioning consumer binding was destroyed; any previous scoped access is invalid.";
            _bindingState =
                LocalPlayerProvisioningConsumerBindingState.Released;

            if (hadLiveBinding)
            {
                LogBindingDebug(
                    "Player provisioning consumer access released because its binding component was destroyed.",
                    "Destroyed",
                    scope,
                    _diagnostic);
            }
        }

        private void LogBindingTrace(
            string message,
            string status,
            LocalPlayerProvisioningConsumerScope runtimeScope,
            string reason)
        {
            FrameworkLogger.Create(typeof(LocalPlayerProvisioningConsumerAccessBinding))
                .Trace(message, BuildBindingFields(status, runtimeScope, reason));
        }

        private void LogBindingDebug(
            string message,
            string status,
            LocalPlayerProvisioningConsumerScope runtimeScope,
            string reason)
        {
            FrameworkLogger.Create(typeof(LocalPlayerProvisioningConsumerAccessBinding))
                .Debug(message, BuildBindingFields(status, runtimeScope, reason));
        }

        private void LogBindingWarning(
            string message,
            string status,
            LocalPlayerProvisioningConsumerScope runtimeScope,
            string reason)
        {
            FrameworkLogger.Create(typeof(LocalPlayerProvisioningConsumerAccessBinding))
                .Warning(message, BuildBindingFields(status, runtimeScope, reason));
        }

        private LogField[] BuildBindingFields(
            string status,
            LocalPlayerProvisioningConsumerScope runtimeScope,
            string reason)
        {
            return LogFields.Of(
                LogFields.Field("component", name),
                LogFields.Field("scene", gameObject.scene.name),
                LogFields.Field("status", status),
                LogFields.Field("authoredScope", scope),
                LogFields.Field("runtimeScope", runtimeScope),
                LogFields.Field("bindingState", _bindingState),
                LogFields.Field("message", reason ?? string.Empty));
        }
    }
}
