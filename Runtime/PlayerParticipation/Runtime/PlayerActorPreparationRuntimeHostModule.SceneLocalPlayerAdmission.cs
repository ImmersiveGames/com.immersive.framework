using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private SceneLocalPlayerAdmissionCompositeLifecycleParticipant
            sceneLocalPlayerCompositeLifecycleParticipant;
        private SceneLocalPlayerAdmissionRuntimeHostModule
            composedSceneLocalPlayerAdmissionModule;
        private readonly HashSet<PlayerSlotId> sceneOwnedHostRegistrations =
            new HashSet<PlayerSlotId>();

        internal bool TryComposeSceneLocalPlayerAdmissionLifecycle(
            SceneLocalPlayerAdmissionRuntimeHostModule sceneModule,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady)
            {
                issue = diagnostic;
                return false;
            }

            if (sceneModule == null || !sceneModule.IsReady)
            {
                issue = "Scene Local Player admission lifecycle composition requires a ready Scene admission module.";
                return false;
            }

            if (sceneLocalPlayerCompositeLifecycleParticipant == null)
            {
                composedSceneLocalPlayerAdmissionModule = sceneModule;
                sceneLocalPlayerCompositeLifecycleParticipant =
                    new SceneLocalPlayerAdmissionCompositeLifecycleParticipant(
                        activityLifecycleParticipant,
                        sceneModule,
                        this);
            }
            else if (!ReferenceEquals(
                         composedSceneLocalPlayerAdmissionModule,
                         sceneModule))
            {
                issue =
                    "Scene Local Player admission lifecycle is already composed with another host-scoped Scene admission module.";
                return false;
            }

            // The base preparation module may re-register its canonical participant after
            // provisioning or host registration. Scene Local Player composition is the more
            // complete source and must remain authoritative for every later Activity transition.
            runtimeHost.SetActivityContentExecutionParticipantSource(
                sceneLocalPlayerCompositeLifecycleParticipant);
            return true;
        }

        internal ScenePlayerActorAdoptionResult TryAdoptSceneLocalPlayerActor(
            RuntimeScopeContext scopeContext,
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason)
        {
            if (!IsReady || preparationContext == null || runtimeHost == null)
            {
                return ScenePlayerActorAdoptionResult.RuntimeUnavailable(
                    "AdoptScenePlayerActor",
                    authoring,
                    source,
                    reason,
                    diagnostic);
            }

            PlayerSlotId playerSlotId = default;
            string issue = string.Empty;
            if (authoring == null ||
                !authoring.TryGetPlayerSlotId(
                    out playerSlotId,
                    out issue))
            {
                var invalid = new ScenePlayerActorAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedInvalidRequest,
                    "AdoptScenePlayerActor",
                    playerSlotId,
                    authoring != null ? authoring.ActorProfile : null,
                    authoring != null ? authoring.SceneLogicalPlayerActor : null,
                    default,
                    false,
                    source,
                    reason,
                    string.IsNullOrWhiteSpace(issue)
                        ? "Scene Player Actor adoption requires complete authoring."
                        : issue);
                if (authoring != null)
                {
                    authoring.SetActorAdoptionResult(invalid);
                }

                return invalid;
            }

            if (!TryRegisterSceneLocalPlayerHost(
                    playerSlotId,
                    authoring.LocalPlayerHost,
                    out bool registeredNow,
                    out issue))
            {
                return new ScenePlayerActorAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedHostMismatch,
                    "AdoptScenePlayerActor",
                    playerSlotId,
                    authoring.ActorProfile,
                    authoring.SceneLogicalPlayerActor,
                    default,
                    false,
                    source,
                    reason,
                    issue);
            }

            ScenePlayerActorAdoptionResult result =
                preparationContext.TryAdoptScenePlayerActor(
                    runtimeHost.RuntimeContentRuntime,
                    scopeContext,
                    authoring,
                    source,
                    reason);
            authoring.SetActorAdoptionResult(result);
            if (result != null && result.Succeeded)
            {
                if (registeredNow)
                {
                    sceneOwnedHostRegistrations.Add(playerSlotId);
                }
            }
            else if (registeredNow)
            {
                joinedHosts.Remove(playerSlotId);
            }

            diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Scene Player Actor adoption returned no result.";
            return result;
        }

        internal ScenePlayerActorAdoptionResult TryReleaseSceneLocalPlayerActor(
            SceneLocalPlayerAdmissionAuthoring authoring,
            ScenePlayerActorAdoptionToken expectedToken,
            string source,
            string reason)
        {
            if (!IsReady || preparationContext == null)
            {
                return ScenePlayerActorAdoptionResult.RuntimeUnavailable(
                    "ReleaseScenePlayerActorAdoption",
                    authoring,
                    source,
                    reason,
                    diagnostic);
            }

            ScenePlayerActorAdoptionResult result =
                preparationContext.TryReleaseScenePlayerActorAdoption(
                    authoring,
                    expectedToken,
                    source,
                    reason);
            if (authoring != null)
            {
                authoring.SetActorAdoptionResult(result);
            }

            if (result != null && result.Succeeded &&
                sceneOwnedHostRegistrations.Remove(expectedToken.PlayerSlotId))
            {
                TryUnregisterSceneLocalPlayerHost(
                    expectedToken.PlayerSlotId,
                    authoring != null ? authoring.LocalPlayerHost : null);
            }

            diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Scene Player Actor adoption release returned no result.";
            return result;
        }

        internal bool TryGetScenePlayerActorAdoption(
            PlayerSlotId playerSlotId,
            out ScenePlayerActorAdoptionToken token)
        {
            token = default;
            return preparationContext != null &&
                preparationContext.TryGetScenePlayerActorAdoption(
                    playerSlotId,
                    out token);
        }

        internal bool TryGetScenePlayerActorPreparationSummary(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary)
        {
            summary = default;
            return preparationContext != null &&
                preparationContext.TryGetPreparationSummary(
                    playerSlotId,
                    out summary);
        }

        private bool TryRegisterSceneLocalPlayerHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring host,
            out bool registeredNow,
            out string issue)
        {
            registeredNow = false;
            issue = string.Empty;
            if (!playerSlotId.IsValid ||
                host == null ||
                !host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId)
            {
                issue = "Scene Local Player Host registration requires matching Joined Host and Slot evidence.";
                return false;
            }

            if (joinedHosts.TryGetValue(
                    playerSlotId,
                    out LocalPlayerHostAuthoring existing))
            {
                if (ReferenceEquals(existing, host))
                {
                    if (sceneOwnedHostRegistrations.Contains(playerSlotId))
                    {
                        return true;
                    }

                    issue =
                        $"Player Slot '{playerSlotId.StableText}' is already registered by another Local Player physical source.";
                    return false;
                }

                if (existing == null ||
                    !existing.IsJoined ||
                    !existing.HasJoinedSlot ||
                    existing.JoinedPlayerSlotId != playerSlotId)
                {
                    joinedHosts.Remove(playerSlotId);
                }
                else
                {
                    issue =
                        $"Player Slot '{playerSlotId.StableText}' is already registered to another Local Player Host.";
                    return false;
                }
            }

            joinedHosts.Add(playerSlotId, host);
            registeredNow = true;
            return true;
        }

        private void TryUnregisterSceneLocalPlayerHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring expectedHost)
        {
            if (!playerSlotId.IsValid ||
                !joinedHosts.TryGetValue(
                    playerSlotId,
                    out LocalPlayerHostAuthoring registered) ||
                !ReferenceEquals(registered, expectedHost))
            {
                return;
            }

            joinedHosts.Remove(playerSlotId);
        }
    }

    internal static class LocalPlayerProvisioningSceneAdmissionLifecycleExtensions
    {
        internal static void RegisterSceneLocalPlayerAdmissionLifecycleSourceIfAvailable(
            this LocalPlayerProvisioningRuntimeHostModule provisioning)
        {
            if (provisioning == null)
            {
                throw new ArgumentNullException(nameof(provisioning));
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                provisioning.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            SceneLocalPlayerAdmissionRuntimeHostModule sceneAdmission =
                provisioning.GetComponent<SceneLocalPlayerAdmissionRuntimeHostModule>();
            if (sceneAdmission == null || !sceneAdmission.IsReady)
            {
                return;
            }

            string issue = string.Empty;
            if (preparation == null ||
                !preparation.TryComposeSceneLocalPlayerAdmissionLifecycle(
                    sceneAdmission,
                    out issue))
            {
                throw new InvalidOperationException(
                    "Local Player provisioning could not restore the composed Scene Local Player Activity lifecycle source. " +
                    issue);
            }
        }
    }
}
