using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Scoped public binding that gives gameplay components access to keyed
    /// contributions for exact current Player Actor occurrences.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Actor Composition Access")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 public scoped binding for exact Player Actor composition contributions.")]
    public sealed class PlayerActorCompositionAccess : MonoBehaviour, IPlayerActorCompositionAccess
    {
        private const string UnboundDiagnostic =
            "Player Actor composition access is not bound to a live Route or Activity scope.";

        [SerializeField]
        [Tooltip("The Route or Activity occurrence that owns this Player Actor composition access.")]
        private LocalPlayerProvisioningConsumerScope scope =
            LocalPlayerProvisioningConsumerScope.Activity;

        [NonSerialized]
        private IPlayerSessionScopedAccess _scopedAccess;

        [NonSerialized]
        private string _diagnostic = UnboundDiagnostic;

        public LocalPlayerProvisioningConsumerScope Scope => scope;

        public bool IsScopedAccessAvailable => _scopedAccess != null &&
            _scopedAccess.Snapshot.IsAvailable;

        public string ScopedAccessDiagnostic => _scopedAccess != null
            ? _scopedAccess.Snapshot.Diagnostic
            : GetUnavailableDiagnostic();

        /// <summary>Opens an owner-scoped contribution for the exact current Player Actor occurrence in the request.</summary>
        public bool TryOpenContribution(
            PlayerActorCompositionContributionRequest request,
            out PlayerActorCompositionContribution contribution,
            out string issue)
        {
            contribution = null;
            if (!TryGetCompositionAccess(out IPlayerActorCompositionAccess access, out issue))
                return false;

            return access.TryOpenContribution(request, out contribution, out issue);
        }

        internal bool TryValidateScope(out string issue)
        {
            if (scope.IsDefinedScope())
            {
                issue = string.Empty;
                return true;
            }

            issue = "Player Actor composition access requires an explicit Route or Activity scope.";
            return false;
        }

        internal bool TryBind(
            IPlayerSessionScopedAccess scopedAccess,
            LocalPlayerProvisioningConsumerScope actualScope,
            out string issue)
        {
            if (!TryValidateScope(out issue))
                return false;

            if (!actualScope.IsDefinedScope() || scope != actualScope)
            {
                issue = $"Player Actor composition access scope '{scope}' does not match active '{actualScope}' scope.";
                _diagnostic = issue;
                return false;
            }

            if (scopedAccess == null || !scopedAccess.Snapshot.IsAvailable ||
                scopedAccess.Snapshot.Scope != actualScope)
            {
                issue = scopedAccess != null
                    ? scopedAccess.Snapshot.Diagnostic
                    : "Player Actor composition access received no scoped Player Session access.";
                _diagnostic = issue;
                return false;
            }

            if (scopedAccess is not IPlayerActorCompositionAccess)
            {
                issue = "Bound Player Session access does not provide the Player Actor composition contract.";
                _diagnostic = issue;
                return false;
            }

            if (_scopedAccess != null &&
                !ReferenceEquals(_scopedAccess, scopedAccess) &&
                _scopedAccess.Snapshot.IsAvailable)
            {
                issue = "Player Actor composition access is already bound to a different live scope.";
                _diagnostic = issue;
                return false;
            }

            _scopedAccess = scopedAccess;
            _diagnostic = scopedAccess.Snapshot.Diagnostic;
            issue = string.Empty;
            return true;
        }

        internal bool ReleaseScopedAccess(
            string reason,
            bool isStale,
            out string issue)
        {
            _scopedAccess = null;
            _diagnostic = string.IsNullOrWhiteSpace(reason)
                ? isStale
                    ? "Player Actor composition access was released with its owner scope."
                    : UnboundDiagnostic
                : reason.Trim();
            issue = string.Empty;
            return true;
        }

        private bool TryGetCompositionAccess(
            out IPlayerActorCompositionAccess access,
            out string issue)
        {
            access = null;
            IPlayerSessionScopedAccess scopedAccess = _scopedAccess;
            if (scopedAccess == null || !scopedAccess.Snapshot.IsAvailable)
            {
                issue = GetUnavailableDiagnostic();
                return false;
            }

            if (scopedAccess is not IPlayerActorCompositionAccess compositionAccess)
            {
                issue = "Bound Player Session access no longer provides the Player Actor composition contract.";
                return false;
            }

            access = compositionAccess;
            issue = string.Empty;
            return true;
        }

        private string GetUnavailableDiagnostic()
        {
            if (!scope.IsDefinedScope())
                return "Player Actor composition access requires an explicit Route or Activity scope.";

            if (_scopedAccess != null &&
                !string.IsNullOrWhiteSpace(_scopedAccess.Snapshot.Diagnostic))
            {
                return _scopedAccess.Snapshot.Diagnostic;
            }

            return _diagnostic;
        }

        private void OnDestroy()
        {
            _scopedAccess = null;
            _diagnostic = "Player Actor composition access component was destroyed.";
        }
    }
}
