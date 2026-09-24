using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IPlayerActorSelectionRuntimePort
    {
        bool IPlayerActorSelectionRuntimePort.TryValidatePlayerActorSelectionRuntime(
            out string issue)
        {
            return TryResolvePlayerActorSelectionRuntime(out _, out issue);
        }

        PlayerActorSelectionResult IPlayerActorSelectionRuntimePort.TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            if (!TryResolvePlayerActorSelectionRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparationRuntime,
                    out string issue))
            {
                return PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    issue);
            }

            return preparationRuntime.TrySelectActorProfile(request);
        }

        PlayerActorSelectionResult IPlayerActorSelectionRuntimePort.TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            if (!TryResolvePlayerActorSelectionRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparationRuntime,
                    out string issue))
            {
                var request = new PlayerActorSelectionRequest(
                    playerSlotId,
                    null,
                    source,
                    reason,
                    expectedSelectionRevision);
                return PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor",
                    request,
                    issue);
            }

            return preparationRuntime.TrySelectDefaultActor(
                playerSlotId,
                expectedSelectionRevision,
                source,
                reason);
        }

        PlayerActorSelectionResult IPlayerActorSelectionRuntimePort.TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            if (!TryResolvePlayerActorSelectionRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparationRuntime,
                    out string issue))
            {
                return PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection",
                    request,
                    issue);
            }

            return preparationRuntime.TryReplaceActorSelection(request);
        }

        PlayerActorSelectionResult IPlayerActorSelectionRuntimePort.TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            if (!TryResolvePlayerActorSelectionRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparationRuntime,
                    out string issue))
            {
                return PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    issue);
            }

            return preparationRuntime.TryClearActorSelection(request);
        }

        private bool TryResolvePlayerActorSelectionRuntime(
            out PlayerActorPreparationRuntimeHostModule preparationRuntime,
            out string issue)
        {
            preparationRuntime = GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (preparationRuntime == null)
            {
                issue =
                    "Player Actor selection runtime is unavailable because the P3J preparation authority is not attached.";
                return false;
            }

            if (!preparationRuntime.IsReady)
            {
                issue = string.IsNullOrWhiteSpace(preparationRuntime.Diagnostic)
                    ? "Player Actor selection runtime is unavailable because the P3J preparation authority is not ready."
                    : preparationRuntime.Diagnostic;
                preparationRuntime = null;
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
