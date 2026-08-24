using System;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private SceneLocalPlayerAdmissionCompositeLifecycleParticipant
            _sceneLocalPlayerCompositeLifecycleParticipant;
        private SceneLocalPlayerAdmissionRuntimeHostModule
            _composedSceneLocalPlayerAdmissionModule;
        internal bool TryComposeSceneLocalPlayerAdmissionLifecycle(
            SceneLocalPlayerAdmissionRuntimeHostModule sceneModule,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady)
            {
                issue = _diagnostic;
                return false;
            }

            if (sceneModule == null || !sceneModule.IsReady)
            {
                issue = "Scene Local Player admission lifecycle composition requires a ready Scene admission module.";
                return false;
            }

            if (_sceneLocalPlayerCompositeLifecycleParticipant == null)
            {
                _composedSceneLocalPlayerAdmissionModule = sceneModule;
                _sceneLocalPlayerCompositeLifecycleParticipant =
                    new SceneLocalPlayerAdmissionCompositeLifecycleParticipant(
                        _activityLifecycleParticipant,
                        sceneModule,
                        this);
            }
            else if (!ReferenceEquals(
                         _composedSceneLocalPlayerAdmissionModule,
                         sceneModule))
            {
                issue =
                    "Scene Local Player admission lifecycle is already composed with another host-scoped Scene admission module.";
                return false;
            }

            // The base preparation module may re-register its canonical participant after
            // provisioning or host registration. Scene Local Player composition is the more
            // complete source and must remain authoritative for every later Activity transition.
            _runtimeHost.SetActivityContentExecutionParticipantSource(
                _sceneLocalPlayerCompositeLifecycleParticipant);
            return true;
        }

        internal bool TryRetireSceneLocalPlayerContextForSessionPlayerLeave(
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason,
            out SceneLocalPlayerAdmissionActivityLifecycleResult result,
            out string issue)
        {
            result = null;
            issue = string.Empty;
            if (!IsReady)
            {
                issue = _diagnostic;
                return false;
            }

            if (_sceneLocalPlayerCompositeLifecycleParticipant == null)
            {
                return true;
            }

            result = _sceneLocalPlayerCompositeLifecycleParticipant
                .TryRetireContextForSessionPlayerLeave(
                    leaveToken,
                    source,
                    reason);
            if (result == null || !result.Succeeded)
            {
                issue = result != null
                    ? result.ToDiagnosticString()
                    : "Scene Local Player contextual lifecycle retirement returned no result.";
                return false;
            }

            return true;
        }

        private void RetireSceneLocalPlayerContextForSessionTermination()
        {
            if (_sceneLocalPlayerCompositeLifecycleParticipant == null)
            {
                return;
            }

            if (!_sceneLocalPlayerCompositeLifecycleParticipant
                    .TryRetireAllContextForSessionTermination(
                        nameof(PlayerActorPreparationRuntimeHostModule),
                        "runtime-host-shutdown",
                        out string issue))
            {
                _diagnostic = "Scene Local Player contextual termination retirement failed. " + issue;
            }
        }

        internal ScenePlayerActorAdoptionResult TryAdoptSceneLocalPlayerActor(
            RuntimeScopeContext scopeContext,
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason)
        {
            if (!IsReady || _preparationContext == null || _runtimeHost == null)
            {
                return ScenePlayerActorAdoptionResult.RuntimeUnavailable(
                    "AdoptScenePlayerActor",
                    authoring,
                    source,
                    reason,
                    _diagnostic);
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

            if (!TryGetRegisteredHost(
                    playerSlotId,
                    out LocalPlayerHostAuthoring registeredHost,
                    out issue) ||
                !ReferenceEquals(
                    registeredHost,
                    authoring.LocalPlayerHost))
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

            if (!TryApplySceneProvidedRouteSpatialEntry(
                    authoring,
                    out issue))
            {
                var placementFailure =
                    new ScenePlayerActorAdoptionResult(
                        ScenePlayerActorAdoptionStatus.RejectedInvalidRequest,
                        "AdoptScenePlayerActor",
                        playerSlotId,
                        authoring.ActorProfile,
                        authoring.SceneLogicalPlayerActor,
                        default,
                        false,
                        source,
                        reason,
                        "Scene Player Actor Route spatial-entry gate failed. " +
                        issue);
                authoring.SetActorAdoptionResult(placementFailure);
                _diagnostic = placementFailure.ToDiagnosticString();
                return placementFailure;
            }

            ScenePlayerActorAdoptionResult result =
                _preparationContext.TryAdoptScenePlayerActor(
                    _runtimeHost.RuntimeContentRuntime,
                    scopeContext,
                    _sessionPhysicalScopeContext,
                    authoring,
                    source,
                    reason);
            authoring.SetActorAdoptionResult(result);

            _diagnostic = result != null
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
            if (!IsReady || _preparationContext == null)
            {
                return ScenePlayerActorAdoptionResult.RuntimeUnavailable(
                    "ReleaseScenePlayerActorAdoption",
                    authoring,
                    source,
                    reason,
                    _diagnostic);
            }

            ScenePlayerActorAdoptionResult result =
                _preparationContext.TryReleaseScenePlayerActorAdoption(
                    authoring,
                    expectedToken,
                    source,
                    reason);
            if (authoring != null)
            {
                authoring.SetActorAdoptionResult(result);
            }

            _diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Scene Player Actor adoption release returned no result.";
            return result;
        }

        internal bool TryGetScenePlayerActorAdoption(
            PlayerSlotId playerSlotId,
            out ScenePlayerActorAdoptionToken token)
        {
            token = default;
            return _preparationContext != null &&
                _preparationContext.TryGetScenePlayerActorAdoption(
                    playerSlotId,
                    out token);
        }

        internal bool TryGetScenePlayerActorPreparationSummary(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary)
        {
            summary = default;
            return _preparationContext != null &&
                _preparationContext.TryGetPreparationSummary(
                    playerSlotId,
                    out summary);
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
