using System;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IPlayerActorSelectionRuntimePort
    {
        private LocalPlayerActorSelectionRequestAuthoringBindingResult
            BindLocalPlayerActorSelectionRequests()
        {
            if (_globalUiSceneRuntime == null)
            {
                return LocalPlayerActorSelectionRequestAuthoringBindingResult.Rejected(
                    "RejectedMissingGlobalUiRuntime",
                    "Local Player Actor Selection Request binding requires the initialized UIGlobal runtime.",
                    0,
                    0,
                    0,
                    0,
                    0);
            }

            IPlayerActorSelectionRuntimePort selectionRuntime = this;
            LocalPlayerActorSelectionRequestAuthoringBindingResult result =
                _globalUiSceneRuntime.TryBindLocalPlayerActorSelectionRequests(
                    selectionRuntime);
            if (result.Succeeded)
            {
                _logger?.Info(result.Message);
            }
            else
            {
                _logger?.Error(result.Message);
            }

            return result;
        }

        private LocalPlayerActorSelectionRequestAuthoringReleaseResult
            ReleaseLocalPlayerActorSelectionRequests(string reason)
        {
            if (_globalUiSceneRuntime == null)
            {
                return LocalPlayerActorSelectionRequestAuthoringReleaseResult
                    .OptionalAbsent(0);
            }

            IPlayerActorSelectionRuntimePort selectionRuntime = this;
            LocalPlayerActorSelectionRequestAuthoringReleaseResult result =
                _globalUiSceneRuntime.TryReleaseLocalPlayerActorSelectionRequests(
                    selectionRuntime);
            string diagnostic =
                $"{result.Message} reason='{(string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim())}'.";
            if (result.Succeeded)
            {
                _logger?.Info(diagnostic);
            }
            else
            {
                _logger?.Error(diagnostic);
            }

            return result;
        }

        bool IPlayerActorSelectionRuntimePort.TryValidatePlayerActorSelectionRuntime(
            out string issue)
        {
            return TryResolvePlayerActorSelectionRuntime(out _, out issue);
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
