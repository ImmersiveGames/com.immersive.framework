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
        private ILocalPlayerProvisioningConsumerAccess access;

        [NonSerialized]
        private string diagnostic = UnboundDiagnostic;

        [NonSerialized]
        private LocalPlayerProvisioningConsumerBindingState bindingState =
            LocalPlayerProvisioningConsumerBindingState.Unbound;

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public bool IsBound => bindingState ==
            LocalPlayerProvisioningConsumerBindingState.Bound &&
            access != null && access.Snapshot.IsAvailable;

        public LocalPlayerProvisioningConsumerBindingState BindingState =>
            access != null && access.Snapshot.IsDisposed
                ? LocalPlayerProvisioningConsumerBindingState.Released
                : bindingState;

        public string Diagnostic => access != null
            ? access.Snapshot.Diagnostic
            : GetUnboundDiagnostic();

        public LocalPlayerProvisioningConsumerAccessSnapshot Snapshot =>
            access != null
                ? access.Snapshot
                : LocalPlayerProvisioningConsumerAccessSnapshot.Unavailable(
                    scope,
                    default,
                    GetUnboundDiagnostic());

        public bool TryGetAccess(
            out ILocalPlayerProvisioningConsumerAccess resolvedAccess,
            out string issue)
        {
            resolvedAccess = access;
            if (resolvedAccess == null || !resolvedAccess.Snapshot.IsAvailable)
            {
                issue = Diagnostic;
                if (resolvedAccess != null && resolvedAccess.Snapshot.IsDisposed)
                {
                    bindingState =
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
                diagnostic = issue;
                bindingState =
                    LocalPlayerProvisioningConsumerBindingState.Unavailable;
                LogBindingWarning("Player provisioning consumer access binding rejected.",
                    "InvalidAuthoredScope", actualScope, issue);
                return false;
            }

            if (scope != actualScope)
            {
                issue =
                    $"Local Player provisioning consumer binding scope '{scope}' does not match the active '{actualScope}' scope.";
                diagnostic = issue;
                bindingState =
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
                diagnostic = issue;
                bindingState =
                    LocalPlayerProvisioningConsumerBindingState.Unavailable;
                LogBindingWarning("Player provisioning consumer access binding rejected.",
                    "AccessUnavailable", actualScope, issue);
                return false;
            }

            if (access != null && !ReferenceEquals(access, scopedAccess))
            {
                issue =
                    "Local Player provisioning consumer binding is already bound to a different live scope.";
                diagnostic = issue;
                LogBindingWarning("Player provisioning consumer access binding rejected.",
                    "AlreadyBound", actualScope, issue);
                return false;
            }

            bool wasAlreadyBound = ReferenceEquals(access, scopedAccess) &&
                bindingState == LocalPlayerProvisioningConsumerBindingState.Bound;

            access = scopedAccess;
            diagnostic = scopedAccess.Snapshot.Diagnostic;
            bindingState = LocalPlayerProvisioningConsumerBindingState.Bound;
            issue = string.Empty;

            if (wasAlreadyBound)
            {
                LogBindingTrace("Player provisioning consumer access binding is already current.",
                    "Idempotent", actualScope, diagnostic);
            }
            else
            {
                LogBindingDebug("Player provisioning consumer access bound.",
                    "Bound", actualScope, diagnostic);
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
            bool changed = access != null ||
                bindingState != nextState ||
                !string.Equals(diagnostic, resolvedReason, StringComparison.Ordinal);

            access = null;
            diagnostic = resolvedReason;
            bindingState = nextState;

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
                : diagnostic;
        }

        private void OnDestroy()
        {
            bool hadLiveBinding = access != null ||
                bindingState == LocalPlayerProvisioningConsumerBindingState.Bound;

            access = null;
            diagnostic =
                "Local Player provisioning consumer binding was destroyed; any previous scoped access is invalid.";
            bindingState =
                LocalPlayerProvisioningConsumerBindingState.Released;

            if (hadLiveBinding)
            {
                LogBindingDebug(
                    "Player provisioning consumer access released because its binding component was destroyed.",
                    "Destroyed",
                    scope,
                    diagnostic);
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
                LogFields.Field("bindingState", bindingState),
                LogFields.Field("message", reason ?? string.Empty));
        }
    }
}
