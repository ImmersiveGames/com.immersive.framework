using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal delegate bool ValidatePlayerSessionBindingRecipient(out string issue);
    internal delegate IDisposable CreatePlayerSessionRecipientBinding(
        PlayerSessionJoinBindingLease lease);
    internal delegate bool TryBindPlayerSessionRecipientBinding(
        IDisposable binding,
        LocalPlayerProvisioningConsumerScope actualScope,
        out string issue);
    internal delegate bool ReleasePlayerSessionRecipientBinding(
        string reason,
        bool isStale,
        out string issue);

    /// <summary>
    /// Internal adapter between composition discovery and one binding destination.
    /// It lets the existing lifecycle binder own multiple recipient kinds without
    /// requiring new components to inherit the broad Experimental consumer API.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "I2 internal Player Session binding recipient seam.")]
    internal sealed class PlayerSessionScopedBindingRecipient
    {
        private readonly Component _component;
        private readonly LocalPlayerProvisioningConsumerScope _scope;
        private readonly Type _bindingKind;
        private readonly ValidatePlayerSessionBindingRecipient _validateScope;
        private readonly CreatePlayerSessionRecipientBinding _createBinding;
        private readonly TryBindPlayerSessionRecipientBinding _tryBind;
        private readonly ReleasePlayerSessionRecipientBinding _release;
        private bool _released;

        internal PlayerSessionScopedBindingRecipient(
            Component component,
            LocalPlayerProvisioningConsumerScope scope,
            Type bindingKind,
            ValidatePlayerSessionBindingRecipient validateScope,
            CreatePlayerSessionRecipientBinding createBinding,
            TryBindPlayerSessionRecipientBinding tryBind,
            ReleasePlayerSessionRecipientBinding release)
        {
            _component = component;
            _scope = scope;
            _bindingKind = bindingKind;
            _validateScope = validateScope;
            _createBinding = createBinding;
            _tryBind = tryBind;
            _release = release;
        }

        internal Component Component => _component;
        internal LocalPlayerProvisioningConsumerScope Scope => _scope;
        internal Type BindingKind => _bindingKind;

        internal static PlayerSessionScopedBindingRecipient ForLegacyConsumer(
            PlayerSessionScopedAccessConsumer consumer,
            CreatePlayerSessionRecipientBinding createBinding)
        {
            if (consumer == null)
                return null;

            LocalPlayerProvisioningConsumerScope authoredScope = consumer.Scope;

            return new PlayerSessionScopedBindingRecipient(
                consumer,
                authoredScope,
                typeof(PlayerSessionScopedAccessConsumer),
                (out string issue) =>
                {
                    if (!consumer.TryValidateScope(out issue))
                        return false;

                    if (consumer.Scope == authoredScope)
                        return true;

                    issue = "Player Session recipient authored scope changed after binding discovery.";
                    return false;
                },
                createBinding,
                (IDisposable binding,
                    LocalPlayerProvisioningConsumerScope actualScope,
                    out string issue) =>
                {
                    if (binding is not IPlayerSessionScopedAccess access)
                    {
                        issue = "Legacy Player Session recipient requires its broad scoped access binding.";
                        return false;
                    }

                    return consumer.TryBind(access, actualScope, out issue);
                },
                consumer.ReleaseScopedAccess);
        }

        internal static PlayerSessionScopedBindingRecipient ForJoinTrigger(
            PlayerSessionJoinTrigger trigger)
        {
            if (trigger == null)
                return null;

            LocalPlayerProvisioningConsumerScope authoredScope = trigger.Scope;
            return new PlayerSessionScopedBindingRecipient(
                trigger,
                authoredScope,
                typeof(PlayerSessionJoinTrigger),
                (out string issue) =>
                {
                    if (!trigger.TryValidateBindingScope(out issue))
                        return false;

                    if (trigger.Scope == authoredScope)
                        return true;

                    issue = "Player Session Join trigger scope changed after binding discovery.";
                    return false;
                },
                lease => new ManagerPlayerSessionJoinPort(lease),
                (IDisposable binding,
                    LocalPlayerProvisioningConsumerScope actualScope,
                    out string issue) =>
                {
                    if (binding is not IPlayerSessionJoinPort joinPort)
                    {
                        issue = "Player Session Join trigger requires its internal canonical Join port.";
                        return false;
                    }

                    return trigger.TryBindJoinPort(joinPort, actualScope, out issue);
                },
                (string reason, bool isStale, out string issue) =>
                {
                    trigger.ReleaseJoinPort(reason, isStale);
                    issue = string.Empty;
                    return true;
                });
        }

        internal bool TryValidateScope(out string issue)
        {
            if (_component == null)
            {
                issue = "Player Session binding recipient was destroyed.";
                return false;
            }

            if (!_scope.IsDefinedScope())
            {
                issue = "Player Session binding recipient requires an explicit Route or Activity scope.";
                return false;
            }

            if (_validateScope == null)
            {
                issue = "Player Session binding recipient has no authored-scope validator.";
                return false;
            }

            return _validateScope(out issue);
        }

        internal IDisposable CreateBinding(PlayerSessionJoinBindingLease lease) =>
            _createBinding != null ? _createBinding(lease) : null;

        internal bool TryBind(
            IDisposable binding,
            LocalPlayerProvisioningConsumerScope actualScope,
            out string issue)
        {
            if (binding == null)
            {
                issue = "Player Session binding recipient received no binding.";
                return false;
            }

            if (!TryValidateScope(out issue))
                return false;

            if (!actualScope.IsDefinedScope() || _scope != actualScope)
            {
                issue = $"Player Session recipient scope '{_scope}' does not match active '{actualScope}' scope.";
                return false;
            }

            if (_tryBind == null)
            {
                issue = "Player Session binding recipient has no binding receiver.";
                return false;
            }

            bool bound = _tryBind(binding, actualScope, out issue);
            if (bound)
                _released = false;
            return bound;
        }

        internal bool Release(string reason, bool isStale, out string issue)
        {
            if (_released)
            {
                issue = string.Empty;
                return true;
            }

            if (_component == null || _release == null)
            {
                _released = true;
                issue = string.Empty;
                return true;
            }

            if (!_release(reason, isStale, out issue))
                return false;

            _released = true;
            issue = string.Empty;
            return true;
        }

        internal bool HasSameDestination(PlayerSessionScopedBindingRecipient other)
        {
            return other != null &&
                ReferenceEquals(_component, other._component) &&
                _scope == other._scope &&
                _bindingKind == other._bindingKind;
        }
    }

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

        [NonSerialized]
        private bool _releasePending;

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public bool IsScopedAccessAvailable => _bindingState ==
            PlayerSessionScopedAccessState.Bound &&
            _access != null && _access.Snapshot.IsAvailable;

        public PlayerSessionScopedAccessState ScopedAccessState =>
            _access != null && _access.Snapshot.IsDisposed
                ? PlayerSessionScopedAccessState.Released
                : _bindingState;

        public string ScopedAccessDiagnostic => IsScopedAccessAvailable
            ? _access.Snapshot.Diagnostic
            : GetUnboundDiagnostic();

        public PlayerSessionScopedAccessSnapshot ScopedAccessSnapshot =>
            IsScopedAccessAvailable
                ? _access.Snapshot
                : PlayerSessionScopedAccessSnapshot.Unavailable(
                    scope,
                    _access != null ? _access.Snapshot.Owner : default,
                    GetUnboundDiagnostic());

        /// <summary>
        /// Advanced typed access for consumers already holding this concrete
        /// Player Session component. It does not resolve a global authority.
        /// </summary>
        protected bool TryGetAccess(
            out IPlayerSessionScopedAccess resolvedAccess,
            out string issue)
        {
            resolvedAccess = _access;
            if (_bindingState != PlayerSessionScopedAccessState.Bound ||
                resolvedAccess == null ||
                !resolvedAccess.Snapshot.IsAvailable)
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

        internal bool TryBind(
            IPlayerSessionScopedAccess scopedAccess,
            LocalPlayerProvisioningConsumerScope actualScope,
            out string issue)
        {
            if (_releasePending)
            {
                issue = _diagnostic;
                return false;
            }

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

        internal bool ReleaseScopedAccess(
            string reason,
            bool isStale,
            out string issue)
        {
            // A scene consumer may be destroyed before the persistent Runtime Host.
            // OnDestroy already releases the bound access on the consumer side, so a
            // later owner-side release must treat a Unity-destroyed wrapper as done.
            if (this == null)
            {
                issue = string.Empty;
                return true;
            }

            string resolvedReason = string.IsNullOrWhiteSpace(reason)
                ? UnboundDiagnostic
                : reason.Trim();
            PlayerSessionScopedAccessState nextState = isStale
                ? PlayerSessionScopedAccessState.Released
                : PlayerSessionScopedAccessState.Unavailable;
            bool wasPending = _releasePending;
            bool changed = _access != null || _bindingState != nextState ||
                !string.Equals(_diagnostic, resolvedReason, StringComparison.Ordinal);

            if (!ReleaseBoundScopedAccess(out string releaseIssue))
            {
                _diagnostic =
                    $"{resolvedReason} Scoped access release remains pending. {(string.IsNullOrWhiteSpace(releaseIssue) ? "No release diagnostic was supplied." : releaseIssue.Trim())}";
                _bindingState = PlayerSessionScopedAccessState.Released;
                issue = _diagnostic;
                if (!wasPending)
                {
                    LogWarning(
                        "Player Session scoped access release remains pending.",
                        "ReleasePending",
                        scope,
                        _diagnostic);
                }

                return false;
            }

            _diagnostic = resolvedReason;
            _bindingState = nextState;
            issue = string.Empty;
            if (changed || wasPending)
            {
                LogDebug("Player Session scoped access released.", isStale ? "StaleReleased" : "Released", scope, resolvedReason);
            }

            return true;
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
            if (!ReleaseBoundScopedAccess(out string releaseIssue))
            {
                _diagnostic =
                    $"Player Session consumer was destroyed while scoped access release remained pending. {(string.IsNullOrWhiteSpace(releaseIssue) ? "No release diagnostic was supplied." : releaseIssue.Trim())}";
                LogWarning(
                    "Player Session scoped access release was best-effort during consumer destruction.",
                    "DestroyedReleasePending",
                    scope,
                    _diagnostic);
            }

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

        protected virtual bool OnScopedAccessReleasing(
            IPlayerSessionScopedAccess scopedAccess,
            out string issue)
        {
            issue = string.Empty;
            return true;
        }

        private bool ReleaseBoundScopedAccess(out string issue)
        {
            issue = string.Empty;
            if (_access == null)
            {
                _releasePending = false;
                return true;
            }

            if (!OnScopedAccessReleasing(_access, out issue))
            {
                _releasePending = true;
                return false;
            }

            _access = null;
            _releasePending = false;
            issue = string.Empty;
            return true;
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
