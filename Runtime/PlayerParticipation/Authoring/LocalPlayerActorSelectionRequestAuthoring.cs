using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing explicit request boundary for Session Player Actor selection.
    /// It delegates to the one FrameworkRuntimeHost-scoped P3J authority and never
    /// stores current selection state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Local Player Actor Selection Requests")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7I public default Actor selection request surface for joined local Player Slots.")]
    public sealed class LocalPlayerActorSelectionRequestAuthoring : MonoBehaviour
    {
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

        public LocalPlayerProvisioningAuthoring ProvisioningAuthoring
        {
            get => provisioningAuthoring;
            set => provisioningAuthoring = value;
        }

        public bool HasProvisioningAuthoring =>
            provisioningAuthoring != null;

        public bool RuntimeReady =>
            TryResolvePreparationRuntime(
                out _,
                out _);

        public PlayerActorSelectionResult LastResult => lastResult;

        public string LastDiagnostic => lastDiagnostic;

        public int RequestCount => requestCount;

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

            if (!TryResolvePreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparationRuntime,
                    out string issue))
            {
                return Complete(
                    PlayerActorSelectionResult.RuntimeUnavailable(
                        "SelectDefaultActor",
                        request,
                        issue));
            }

            return Complete(
                preparationRuntime.TrySelectDefaultActor(
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

        private bool TryResolvePreparationRuntime(
            out PlayerActorPreparationRuntimeHostModule preparationRuntime,
            out string issue)
        {
            preparationRuntime = null;
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

            if (!FrameworkRuntimeHost.TryGetCurrent(
                    out FrameworkRuntimeHost runtimeHost) ||
                runtimeHost == null)
            {
                issue =
                    "Local Player Actor selection runtime is unavailable because FrameworkRuntimeHost is missing.";
                return false;
            }

            preparationRuntime =
                runtimeHost.GetComponent<
                    PlayerActorPreparationRuntimeHostModule>();
            if (preparationRuntime == null ||
                !preparationRuntime.IsReady)
            {
                preparationRuntime = null;
                issue =
                    "Local Player Actor selection runtime is unavailable because the P3J preparation authority is not ready.";
                return false;
            }

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
