using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing explicit request boundary for Session Player Actor selection.
    /// It delegates to one explicitly bound P3J selection authority and never stores
    /// current selection state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Local Player Actor Selection Requests")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "H2.2.12 public default Actor selection request surface with explicit runtime binding.")]
    public sealed class LocalPlayerActorSelectionRequestAuthoring : MonoBehaviour
    {
        private const string MissingRuntimeBindingDiagnostic =
            "Player Actor selection runtime port is not bound.";

        [SerializeField]
        [Tooltip(
            "Explicit local Player provisioning surface bound to the same Session runtime.")]
        private LocalPlayerProvisioningAuthoring provisioningAuthoring;

        [System.NonSerialized]
        private PlayerActorSelectionResult lastResult;

        [System.NonSerialized]
        private string lastDiagnostic =
            "No local Player Actor selection request has executed.";

        [System.NonSerialized]
        private int requestCount;

        [System.NonSerialized]
        private IPlayerActorSelectionRuntimePort playerActorSelectionRuntime;

        [System.NonSerialized]
        private string playerActorSelectionRuntimeBindingDiagnostic =
            MissingRuntimeBindingDiagnostic;

        public LocalPlayerProvisioningAuthoring ProvisioningAuthoring
        {
            get => provisioningAuthoring;
            set => provisioningAuthoring = value;
        }

        public bool HasProvisioningAuthoring =>
            provisioningAuthoring != null;

        public bool RuntimeReady =>
            TryResolveSelectionRuntime(
                out _,
                out _);

        public PlayerActorSelectionResult LastResult => lastResult;

        public string LastDiagnostic => lastDiagnostic;

        public int RequestCount => requestCount;

        public bool HasPlayerActorSelectionRuntimeBinding =>
            playerActorSelectionRuntime != null;

        public string PlayerActorSelectionRuntimeBindingStatus =>
            HasPlayerActorSelectionRuntimeBinding ? "Bound" : "Missing";

        public string PlayerActorSelectionRuntimeBindingDiagnostic =>
            playerActorSelectionRuntimeBindingDiagnostic;

        /// <summary>
        /// Explicitly applies the configured PlayerSlotProfile default Actor after
        /// the Slot has joined. Join and Actor selection remain separate transactions.
        /// </summary>
        public PlayerActorSelectionResult RequestDefaultActorSelection(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            requestCount++;

            var request = new PlayerActorSelectionRequest(
                playerSlotId,
                null,
                source,
                reason,
                expectedSelectionRevision);

            if (!TryResolveSelectionRuntime(
                    out IPlayerActorSelectionRuntimePort selectionRuntime,
                    out string issue))
            {
                return Complete(
                    PlayerActorSelectionResult.RuntimeUnavailable(
                        "SelectDefaultActor",
                        request,
                        issue));
            }

            return Complete(
                selectionRuntime.TrySelectDefaultActor(
                    playerSlotId,
                    expectedSelectionRevision,
                    source,
                    reason));
        }

        public bool TryValidateConfiguration(
            out string issue)
        {
            if (provisioningAuthoring == null)
            {
                issue =
                    "Local Player Actor selection requests require an explicit LocalPlayerProvisioningAuthoring.";
                return false;
            }

            if (!ReferenceEquals(
                    provisioningAuthoring.gameObject,
                    gameObject))
            {
                issue =
                    "Local Player Actor selection requests and LocalPlayerProvisioningAuthoring must share one product authoring GameObject.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal bool TryValidatePlayerActorSelectionRuntimeBinding(
            IPlayerActorSelectionRuntimePort selectionRuntime,
            out bool alreadyBound,
            out string issue)
        {
            alreadyBound = false;
            if (!TryValidateConfiguration(out issue))
            {
                return false;
            }

            if (selectionRuntime == null)
            {
                issue = MissingRuntimeBindingDiagnostic;
                return false;
            }

            if (playerActorSelectionRuntime == null)
            {
                issue = string.Empty;
                return true;
            }

            if (ReferenceEquals(
                    playerActorSelectionRuntime,
                    selectionRuntime))
            {
                alreadyBound = true;
                issue = string.Empty;
                return true;
            }

            issue =
                "Local Player Actor selection request authoring is already bound to a different runtime port for the current component lifetime.";
            return false;
        }

        internal void ApplyPlayerActorSelectionRuntimeBinding(
            IPlayerActorSelectionRuntimePort selectionRuntime)
        {
            if (!TryValidatePlayerActorSelectionRuntimeBinding(
                    selectionRuntime,
                    out bool alreadyBound,
                    out string issue))
            {
                playerActorSelectionRuntimeBindingDiagnostic = issue;
                throw new System.InvalidOperationException(issue);
            }

            playerActorSelectionRuntime = selectionRuntime;
            playerActorSelectionRuntimeBindingDiagnostic = alreadyBound
                ? "Player Actor selection runtime port binding is already applied."
                : "Player Actor selection runtime port is bound.";
        }

        internal bool TryValidatePlayerActorSelectionRuntimeRelease(
            IPlayerActorSelectionRuntimePort selectionRuntime,
            out bool alreadyReleased,
            out string issue)
        {
            alreadyReleased = playerActorSelectionRuntime == null;
            if (selectionRuntime == null)
            {
                issue =
                    "Player Actor selection runtime release requires the expected runtime port.";
                return false;
            }

            if (alreadyReleased || ReferenceEquals(
                    playerActorSelectionRuntime,
                    selectionRuntime))
            {
                issue = string.Empty;
                return true;
            }

            issue =
                "Local Player Actor selection request authoring is bound to a different runtime port and cannot be released by this Session.";
            return false;
        }

        internal void ReleasePlayerActorSelectionRuntime(
            IPlayerActorSelectionRuntimePort selectionRuntime)
        {
            if (!TryValidatePlayerActorSelectionRuntimeRelease(
                    selectionRuntime,
                    out bool alreadyReleased,
                    out string issue))
            {
                playerActorSelectionRuntimeBindingDiagnostic = issue;
                throw new System.InvalidOperationException(issue);
            }

            if (!alreadyReleased)
            {
                playerActorSelectionRuntime = null;
            }

            playerActorSelectionRuntimeBindingDiagnostic = alreadyReleased
                ? "Player Actor selection runtime port binding was already released."
                : "Player Actor selection runtime port binding was released.";
        }

        private bool TryResolveSelectionRuntime(
            out IPlayerActorSelectionRuntimePort selectionRuntime,
            out string issue)
        {
            selectionRuntime = null;
            issue = string.Empty;

            if (!TryValidateConfiguration(out issue))
            {
                return false;
            }

            if (!provisioningAuthoring.RuntimeReady)
            {
                issue =
                    "Local Player Actor selection runtime is unavailable because provisioning is not ready. " +
                    provisioningAuthoring.RuntimeDiagnostic;
                return false;
            }

            if (playerActorSelectionRuntime == null)
            {
                issue =
                    "Local Player Actor selection runtime is unavailable because the explicit runtime port is not bound.";
                return false;
            }

            if (!playerActorSelectionRuntime.TryValidatePlayerActorSelectionRuntime(
                    out issue))
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Local Player Actor selection runtime is unavailable because the P3J preparation authority is not ready."
                    : issue;
                return false;
            }

            selectionRuntime = playerActorSelectionRuntime;
            issue = string.Empty;
            return true;
        }

        private PlayerActorSelectionResult Complete(
            PlayerActorSelectionResult result)
        {
            lastResult = result;
            lastDiagnostic = result != null
                ? result.ToDiagnosticString()
                : "Local Player Actor selection returned no result.";
            return result;
        }

        private void OnDestroy()
        {
            playerActorSelectionRuntime = null;
            playerActorSelectionRuntimeBindingDiagnostic =
                "Player Actor selection request authoring was destroyed.";
        }

#if UNITY_EDITOR
        private void Reset()
        {
            provisioningAuthoring =
                GetComponent<LocalPlayerProvisioningAuthoring>();
        }

        private void OnValidate()
        {
            if (provisioningAuthoring == null)
            {
                provisioningAuthoring =
                    GetComponent<LocalPlayerProvisioningAuthoring>();
            }
        }
#endif
    }
}
