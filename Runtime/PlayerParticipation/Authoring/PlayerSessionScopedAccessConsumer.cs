using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Runtime lifetime state for a Player Session product consumer. It reports
    /// only the Framework-injected scoped access transport, never Player state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-06 direct scoped consumer access lifetime status.")]
    public enum PlayerSessionScopedAccessState
    {
        Unbound = 0,
        Bound = 10,
        Unavailable = 20,
        Released = 30
    }

    /// <summary>
    /// Implementation detail shared by Player Session product components.
    /// It is abstract and omitted from Add Component; only a concrete Command
    /// or Status component may receive a Framework-owned scoped access port.
    /// </summary>
    [AddComponentMenu("")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "Internal direct lifecycle receiver for Player Session product consumers.")]
    public abstract class PlayerSessionScopedAccessConsumer : MonoBehaviour
    {
        private const string UnboundDiagnostic =
            "Player Session scoped access is not bound to a live framework scope.";

        [SerializeField]
        [Tooltip("The Framework lifecycle scope that owns this Player Session component. This does not configure Player Session state.")]
        private LocalPlayerProvisioningConsumerScope scope =
            LocalPlayerProvisioningConsumerScope.Activity;

        [NonSerialized]
        private IPlayerSessionScopedAccess _access;

        [NonSerialized]
        private string _diagnostic = UnboundDiagnostic;

        [NonSerialized]
        private PlayerSessionScopedAccessState _bindingState =
            PlayerSessionScopedAccessState.Unbound;

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public bool IsScopedAccessAvailable => _bindingState ==
            PlayerSessionScopedAccessState.Bound &&
            _access != null && _access.Snapshot.IsAvailable;

        public bool IsBound => IsScopedAccessAvailable;

        public PlayerSessionScopedAccessState ScopedAccessState =>
            _access != null && _access.Snapshot.IsDisposed
                ? PlayerSessionScopedAccessState.Released
                : _bindingState;

        public PlayerSessionScopedAccessState BindingState => ScopedAccessState;

        public string ScopedAccessDiagnostic => _access != null
            ? _access.Snapshot.Diagnostic
            : GetUnboundDiagnostic();

        public string Diagnostic => ScopedAccessDiagnostic;

        public PlayerSessionScopedAccessSnapshot ScopedAccessSnapshot =>
            _access != null
                ? _access.Snapshot
                : PlayerSessionScopedAccessSnapshot.Unavailable(
                    scope,
                    default,
                    GetUnboundDiagnostic());

        public PlayerSessionScopedAccessSnapshot Snapshot =>
            ScopedAccessSnapshot;

        /// <summary>
        /// Advanced typed access for consumers already holding this concrete
        /// Player Session component. It does not resolve a global authority.
        /// </summary>
        public bool TryGetAccess(
            out IPlayerSessionScopedAccess resolvedAccess,
            out string issue)
        {
            resolvedAccess = _access;
            if (resolvedAccess == null || !resolvedAccess.Snapshot.IsAvailable)
            {
                issue = ScopedAccessDiagnostic;
                if (resolvedAccess != null && resolvedAccess.Snapshot.IsDisposed)
                {
                    _bindingState = PlayerSessionScopedAccessState.Released;
                }

                resolvedAccess = null;
                return false;
            }

            issue = string.Empty;
            return true;
        }

        /// <summary>
        /// Obsolete Manager-Provisioned endpoint retained only so existing
        /// consumers can migrate independently to IPlayerSessionScopedAccess
        /// and the optional ILocalPlayerJoinAccess capability.
        /// </summary>
        [Obsolete(
            "Use TryGetAccess(out IPlayerSessionScopedAccess, out issue) and " +
            "TryGetJoinAccess only when a Manager-Provisioned join is required.")]
        public bool TryGetAccess(
            out ILocalPlayerProvisioningConsumerAccess resolvedAccess,
            out string issue)
        {
            resolvedAccess = null;
            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out issue))
            {
                return false;
            }

            if (access is ManagerPlayerSessionScopedAccess managerAccess)
            {
                resolvedAccess = new LegacyManagerProvisioningConsumerAccess(
                    managerAccess);
                issue = string.Empty;
                return true;
            }

            issue = "Manager-Provisioned Player access is unavailable for this Player Session scope.";
            return false;
        }

        /// <summary>
        /// Returns the explicit Manager-Provisioned join capability when the
        /// current scoped provider can create a Local Player Host.
        /// </summary>
        public bool TryGetJoinAccess(
            out ILocalPlayerJoinAccess joinAccess,
            out string issue)
        {
            joinAccess = null;
            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out issue))
            {
                return false;
            }

            if (access is ILocalPlayerJoinAccess resolvedJoinAccess)
            {
                joinAccess = resolvedJoinAccess;
                issue = string.Empty;
                return true;
            }

            issue = "Manager-Provisioned Player join capability is unavailable for this Player Session scope.";
            return false;
        }

        internal bool TryBind(
            IPlayerSessionScopedAccess scopedAccess,
            LocalPlayerProvisioningConsumerScope actualScope,
            out string issue)
        {
            if (!scope.IsDefinedScope())
            {
                issue = "Player Session component requires an explicit Route or Activity scope.";
                SetUnavailable(issue, actualScope, "InvalidAuthoredScope", true);
                return false;
            }

            if (scope != actualScope)
            {
                issue = $"Player Session component scope '{scope}' does not match the active '{actualScope}' scope.";
                SetUnavailable(issue, actualScope, "ScopeMismatch", false);
                return false;
            }

            if (scopedAccess == null || !scopedAccess.Snapshot.IsAvailable)
            {
                issue = scopedAccess != null
                    ? scopedAccess.Snapshot.Diagnostic
                    : "Player Session component received no scoped access.";
                SetUnavailable(issue, actualScope, "AccessUnavailable", true);
                return false;
            }

            if (_access != null && !ReferenceEquals(_access, scopedAccess))
            {
                issue = "Player Session component is already bound to a different live scope.";
                _diagnostic = issue;
                LogWarning("Player Session scoped access rejected.", "AlreadyBound", actualScope, issue);
                return false;
            }

            bool wasAlreadyBound = ReferenceEquals(_access, scopedAccess) &&
                _bindingState == PlayerSessionScopedAccessState.Bound;
            _access = scopedAccess;
            _diagnostic = scopedAccess.Snapshot.Diagnostic;
            _bindingState = PlayerSessionScopedAccessState.Bound;
            issue = string.Empty;

            if (wasAlreadyBound)
            {
                LogTrace("Player Session scoped access is already current.", "Idempotent", actualScope, _diagnostic);
            }
            else
            {
                OnScopedAccessBound(scopedAccess);
                LogDebug("Player Session scoped access bound.", "Bound", actualScope, _diagnostic);
            }

            return true;
        }

        internal void ReleaseScopedAccess(string reason, bool isStale = false)
        {
            // A scene consumer may be destroyed before the persistent Runtime Host.
            // OnDestroy already releases the bound access on the consumer side, so a
            // later owner-side release must treat a Unity-destroyed wrapper as done.
            if (this == null)
            {
                return;
            }

            string resolvedReason = string.IsNullOrWhiteSpace(reason)
                ? UnboundDiagnostic
                : reason.Trim();
            PlayerSessionScopedAccessState nextState = isStale
                ? PlayerSessionScopedAccessState.Released
                : PlayerSessionScopedAccessState.Unavailable;
            bool changed = _access != null || _bindingState != nextState ||
                !string.Equals(_diagnostic, resolvedReason, StringComparison.Ordinal);

            ReleaseBoundScopedAccess();
            _diagnostic = resolvedReason;
            _bindingState = nextState;
            if (changed)
            {
                LogDebug("Player Session scoped access released.", isStale ? "StaleReleased" : "Released", scope, resolvedReason);
            }
        }

        internal bool TryValidateScope(out string issue)
        {
            if (scope.IsDefinedScope())
            {
                issue = string.Empty;
                return true;
            }

            issue = "Player Session component requires an explicit Route or Activity scope.";
            return false;
        }

        private string GetUnboundDiagnostic()
        {
            return !scope.IsDefinedScope()
                ? "Player Session component requires an explicit Route or Activity scope."
                : _diagnostic;
        }

        private void OnDestroy()
        {
            bool hadLiveBinding = _access != null ||
                _bindingState == PlayerSessionScopedAccessState.Bound;
            ReleaseBoundScopedAccess();
            _diagnostic = "Player Session component was destroyed; any previous scoped access is invalid.";
            _bindingState = PlayerSessionScopedAccessState.Released;
            if (hadLiveBinding)
            {
                LogDebug("Player Session scoped access released because its consumer component was destroyed.", "Destroyed", scope, _diagnostic);
            }
        }

        private void SetUnavailable(string issue, LocalPlayerProvisioningConsumerScope runtimeScope, string status, bool warning)
        {
            _diagnostic = issue;
            _bindingState = PlayerSessionScopedAccessState.Unavailable;
            if (warning)
            {
                LogWarning("Player Session scoped access rejected.", status, runtimeScope, issue);
            }
            else
            {
                LogDebug("Player Session scoped access skipped.", status, runtimeScope, issue);
            }
        }

        protected virtual void OnScopedAccessBound(
            IPlayerSessionScopedAccess scopedAccess)
        {
        }

        protected virtual void OnScopedAccessReleasing(
            IPlayerSessionScopedAccess scopedAccess)
        {
        }

        private void ReleaseBoundScopedAccess()
        {
            if (_access != null)
            {
                OnScopedAccessReleasing(_access);
                _access = null;
            }
        }

        private void LogTrace(string message, string status, LocalPlayerProvisioningConsumerScope runtimeScope, string reason)
        {
            FrameworkLogger.Create(GetType()).Trace(message, BuildFields(status, runtimeScope, reason));
        }

        private void LogDebug(string message, string status, LocalPlayerProvisioningConsumerScope runtimeScope, string reason)
        {
            FrameworkLogger.Create(GetType()).Debug(message, BuildFields(status, runtimeScope, reason));
        }

        private void LogWarning(string message, string status, LocalPlayerProvisioningConsumerScope runtimeScope, string reason)
        {
            FrameworkLogger.Create(GetType()).Warning(message, BuildFields(status, runtimeScope, reason));
        }

        private LogField[] BuildFields(string status, LocalPlayerProvisioningConsumerScope runtimeScope, string reason)
        {
            // Diagnostics must remain side-effect free during Unity teardown. A
            // managed wrapper can outlive its native Unity object, so never read
            // Unity-backed properties after the object has become fake-null.
            string componentName = this != null
                ? name
                : GetType().Name;
            string sceneName = this != null && gameObject != null
                ? gameObject.scene.name
                : string.Empty;

            return LogFields.Of(
                LogFields.Field("component", componentName),
                LogFields.Field("scene", sceneName),
                LogFields.Field("status", status),
                LogFields.Field("authoredScope", scope),
                LogFields.Field("runtimeScope", runtimeScope),
                LogFields.Field("bindingState", _bindingState),
                LogFields.Field("message", reason ?? string.Empty));
        }
    }
}
