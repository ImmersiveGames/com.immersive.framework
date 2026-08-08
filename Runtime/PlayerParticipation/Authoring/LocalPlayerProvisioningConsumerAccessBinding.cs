using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
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

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public bool IsBound => access != null && access.Snapshot.IsAvailable;

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
                return false;
            }

            if (scope != actualScope)
            {
                issue =
                    $"Local Player provisioning consumer binding scope '{scope}' does not match the active '{actualScope}' scope.";
                diagnostic = issue;
                return false;
            }

            if (scopedAccess == null || !scopedAccess.Snapshot.IsAvailable)
            {
                issue = scopedAccess != null
                    ? scopedAccess.Snapshot.Diagnostic
                    : "Local Player provisioning consumer binding received no scoped access.";
                diagnostic = issue;
                return false;
            }

            if (access != null && !ReferenceEquals(access, scopedAccess))
            {
                issue =
                    "Local Player provisioning consumer binding is already bound to a different live scope.";
                diagnostic = issue;
                return false;
            }

            access = scopedAccess;
            diagnostic = scopedAccess.Snapshot.Diagnostic;
            issue = string.Empty;
            return true;
        }

        internal void Release(string reason)
        {
            access = null;
            diagnostic = string.IsNullOrWhiteSpace(reason)
                ? UnboundDiagnostic
                : reason.Trim();
        }

        private string GetUnboundDiagnostic()
        {
            return !scope.IsDefinedScope()
                ? "Local Player provisioning consumer binding requires an explicit Route or Activity scope."
                : diagnostic;
        }

        private void OnDestroy()
        {
            access = null;
            diagnostic =
                "Local Player provisioning consumer binding was destroyed; any previous scoped access is invalid.";
        }
    }
}
