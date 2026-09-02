using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority for the current Activity gameplay context. It projects
    /// the already prepared Session physical Player into Activity-owned input, camera
    /// and gameplay admission capabilities; it never stages or replaces physical state.
    /// </summary>
    internal sealed class PlayerGameplayCurrentContextRuntime
    {
        private sealed class ChainEvidence
        {
            internal PlayerGameplayOccupancySummary occupancy;
            internal PlayerGameplayInputBindingSummary input;
            internal PlayerGameplayCameraEligibilitySummary camera;
            internal PlayerGameplayAdmissionSummary admission;
            internal bool occupancyCreated;
            internal bool inputCreated;
            internal bool cameraCreated;
            internal bool admissionCreated;
            internal bool nestedRollbackAttempted;
            internal bool nestedRollbackSucceeded;
            internal string nestedRollbackMessage;
        }

        private readonly PlayerActorPreparationRuntimeHostModule _preparationModule;
        private readonly IPlayerGameplayCurrentContextEndpointSource _endpointSource;
        private readonly PlayerGameplayOccupancyRuntimeContext _occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext _inputContext;
        private readonly PlayerGameplayCameraEligibilityRuntimeContext _cameraContext;
        private readonly PlayerGameplayAdmissionRuntimeContext _admissionContext;
        private readonly string _sessionContextId;
        private readonly Dictionary<PlayerSlotId, PlayerGameplayInputReader>
            _gameplayInputConsumers =
                new Dictionary<PlayerSlotId, PlayerGameplayInputReader>();

        private PlayerGameplayCurrentContextRuntime(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            IPlayerGameplayCurrentContextEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            string sessionContextId)
        {
            this._preparationModule = preparationModule;
            this._endpointSource = endpointSource;
            this._occupancyContext = occupancyContext;
            this._inputContext = inputContext;
            this._cameraContext = cameraContext;
            this._admissionContext = admissionContext;
            this._sessionContextId = sessionContextId;
        }

        internal static bool TryCreate(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            IPlayerGameplayCurrentContextEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            out PlayerGameplayCurrentContextRuntime context,
            out string issue)
        {
            context = null;
            issue = string.Empty;
            if (preparationModule == null || !preparationModule.IsReady ||
                endpointSource == null || occupancyContext == null ||
                inputContext == null || cameraContext == null ||
                admissionContext == null ||
                !preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationHost) ||
                preparationHost == null || !preparationHost.IsInitialized ||
                preparationHost.Preparation == null ||
                !preparationHost.Preparation.IsInitialized)
            {
                issue = "Current gameplay context requires ready preparation and gameplay authorities.";
                return false;
            }

            string session = preparationHost.SessionContextId;
            if (string.IsNullOrEmpty(session) ||
                !string.Equals(session, occupancyContext.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, inputContext.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, cameraContext.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, admissionContext.SessionContextId, StringComparison.Ordinal))
            {
                issue = "Current gameplay authorities belong to different or uninitialized Session identities.";
                return false;
            }

            context = new PlayerGameplayCurrentContextRuntime(
                preparationModule, endpointSource, occupancyContext, inputContext,
                cameraContext, admissionContext, session);
            return true;
        }

        internal bool TryEnsureCurrentGameplay(
            PlayerActorPreparationSummary preparation,
            RuntimeContentOwner contextualOwner,
            string source,
            string reason,
            out PlayerGameplayAdmissionSummary admission,
            out bool rollbackAttempted,
            out bool rollbackSucceeded,
            out string rollbackMessage,
            out string issue)
        {
            admission = default;
            rollbackAttempted = false;
            rollbackSucceeded = false;
            rollbackMessage = string.Empty;
            issue = string.Empty;
            if (!contextualOwner.IsValid ||
                contextualOwner.Scope != RuntimeContentScope.Activity ||
                !preparation.IsValid || !preparation.IsPrepared ||
                !preparation.PlayerSlotId.IsValid ||
                !string.Equals(preparation.SessionContextId, _sessionContextId, StringComparison.Ordinal))
            {
                issue = "Current gameplay context requires current Activity ownership and exact prepared Session physical evidence.";
                return false;
            }

            PlayerGameplayAdmissionSnapshot snapshot = _admissionContext.CreateSnapshot();
            if (snapshot != null && snapshot.TryGetSummary(
                    preparation.PlayerSlotId, out PlayerGameplayAdmissionSummary previous) &&
                previous.IsAdmitted && previous.Owner != contextualOwner)
            {
                PlayerGameplayAdmissionResult released = TryReleaseCurrentGameplay(
                    preparation.PlayerSlotId, previous.Token, source,
                    "reproject-gameplay-context");
                if (!released.Succeeded)
                {
                    issue = released.ToDiagnosticString();
                    return false;
                }
            }

            var chain = new ChainEvidence();
            if (TryBuildChain(preparation, contextualOwner, chain, source, reason, out issue))
            {
                admission = chain.admission;
                rollbackSucceeded = true;
                return true;
            }

            string buildIssue = issue;
            rollbackAttempted = chain.nestedRollbackAttempted || chain.admissionCreated ||
                chain.cameraCreated || chain.inputCreated || chain.occupancyCreated;
            if (rollbackAttempted)
            {
                bool contextualRollback = TryReleaseContextualChain(
                    chain, source, "ensure-current-gameplay-rollback", out string rollbackIssue);
                rollbackSucceeded = (!chain.nestedRollbackAttempted ||
                    chain.nestedRollbackSucceeded) && contextualRollback;
                rollbackMessage = Join(chain.nestedRollbackMessage, rollbackIssue);
            }

            issue = Join(buildIssue, rollbackMessage);
            return false;
        }

        internal PlayerGameplayAdmissionResult TryReleaseCurrentGameplay(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken expectedAdmission,
            string source,
            string reason)
        {
            PlayerGameplayAdmissionSnapshot snapshot = _admissionContext.CreateSnapshot();
            if (snapshot == null || !snapshot.TryGetSummary(playerSlotId,
                    out PlayerGameplayAdmissionSummary current) ||
                current.Token != expectedAdmission)
            {
                return _admissionContext.TryRelease(playerSlotId, expectedAdmission, source, reason);
            }

            // Fail closed at the public Actor boundary before admission/input teardown can
            // restore the physical Action Map. Session PlayerInput may survive Activity exit,
            // but the retired Activity consumer must not retain gameplay authority.
            ReleaseInputConsumer(
                playerSlotId,
                "Activity gameplay input consumer authority released.");

            PlayerGameplayAdmissionResult released = _admissionContext.TryRelease(
                playerSlotId, expectedAdmission, source, reason);
            if (!released.Succeeded)
                return released;

            PlayerGameplayCameraEligibilityResult camera = _cameraContext.TryRelease(
                playerSlotId, current.CameraEligibilityToken, source, reason);
            if (!camera.Succeeded)
                return released;

            _inputContext.TryRelease(playerSlotId, current.InputBindingToken, source, reason);
            return released;
        }

        internal bool TryGetCurrentAdmission(
            PlayerSlotId playerSlotId,
            out PlayerGameplayAdmissionSummary admission)
        {
            admission = default;
            PlayerGameplayAdmissionSnapshot snapshot = _admissionContext.CreateSnapshot();
            return snapshot != null && snapshot.IsInitialized &&
                snapshot.TryGetSummary(playerSlotId, out admission);
        }

        private bool TryBuildChain(
            PlayerActorPreparationSummary preparation,
            RuntimeContentOwner owner,
            ChainEvidence chain,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            PlayerGameplayOccupancyResult occupancy = _occupancyContext.TryConfirmOccupancy(
                preparation, source, reason);
            if (!occupancy.Succeeded)
            {
                issue = occupancy.ToDiagnosticString();
                return false;
            }
            chain.occupancy = occupancy.CurrentSummary;
            chain.occupancyCreated = !occupancy.PreviousSummary.IsOccupied &&
                occupancy.CurrentSummary.IsOccupied;

            if (!_endpointSource.TryResolveGameplayEndpoints(preparation, out LocalPlayerHostAuthoring host,
                    out PlayerActorDeclaration actor, out UnityPlayerInputGateAdapter gate,
                    out PlayerGameplayInputReader gameplayInputReader,
                    out PlayerGameplayCameraAuthoring cameraAuthoring,
                    out PlayerGameplayCameraRequiredness cameraRequiredness,
                    out CameraOutputAuthoring outputSession, out issue))
                return false;

            PlayerGameplayInputBindingResult input = _inputContext.TryBind(
                preparation, chain.occupancy, owner, host, actor, gate, source, reason);
            if (!input.Succeeded)
            {
                chain.nestedRollbackAttempted = input.RollbackAttempted;
                chain.nestedRollbackSucceeded = input.RollbackSucceeded;
                chain.nestedRollbackMessage = input.RollbackMessage;
                issue = input.ToDiagnosticString();
                return false;
            }
            chain.input = input.CurrentSummary;
            chain.inputCreated = !input.PreviousSummary.IsBound && input.CurrentSummary.IsBound;

            PlayerGameplayCameraEligibilityResult camera = cameraAuthoring != null
                ? _cameraContext.TryConfirmEligibility(preparation, chain.occupancy, chain.input,
                    outputSession, actor, cameraAuthoring, source, reason)
                : cameraRequiredness == PlayerGameplayCameraRequiredness.Optional
                    ? _cameraContext.TrySkipOptional(preparation, chain.occupancy, chain.input,
                        outputSession, cameraRequiredness, source, reason)
                    : null;
            if (camera == null || !camera.Succeeded)
            {
                issue = camera == null
                    ? "Required Player camera has no explicit authoring endpoint during current gameplay projection."
                    : camera.ToDiagnosticString();
                return false;
            }
            chain.camera = camera.CurrentSummary;
            chain.cameraCreated = !camera.PreviousSummary.HasCurrentDecision &&
                camera.CurrentSummary.HasCurrentDecision;

            PlayerGameplayAdmissionResult admission = _admissionContext.TryAdmit(
                owner, chain.occupancy, chain.input, chain.camera, source, reason);
            if (!admission.Succeeded)
            {
                chain.nestedRollbackAttempted = admission.RollbackAttempted;
                chain.nestedRollbackSucceeded = admission.RollbackSucceeded;
                chain.nestedRollbackMessage = admission.RollbackIssue;
                if (admission.CurrentSummary.IsAdmitted)
                {
                    chain.admission = admission.CurrentSummary;
                    chain.admissionCreated = true;
                }
                issue = admission.ToDiagnosticString();
                return false;
            }
            chain.admission = admission.CurrentSummary;
            chain.admissionCreated = !admission.PreviousSummary.IsAdmitted &&
                admission.CurrentSummary.IsAdmitted;

            if (!TryBindInputConsumer(
                    actor,
                    gameplayInputReader,
                    chain.input,
                    chain.admission,
                    out issue))
                return false;

            return true;
        }

        private bool TryBindInputConsumer(
            PlayerActorDeclaration actor,
            PlayerGameplayInputReader consumer,
            PlayerGameplayInputBindingSummary input,
            PlayerGameplayAdmissionSummary admission,
            out string issue)
        {
            issue = string.Empty;
            PlayerSlotId playerSlotId = input.PlayerSlotId;

            if (consumer == null)
            {
                ReleaseInputConsumer(
                    playerSlotId,
                    "Current Player Actor composition has no authored player gameplay input reader.");
                return true;
            }

            if (_gameplayInputConsumers.TryGetValue(playerSlotId, out var previousConsumer) &&
                previousConsumer != null && !ReferenceEquals(previousConsumer, consumer))
            {
                previousConsumer.ReleaseRuntimeBinding(
                    "Gameplay input consumer replaced by a fresh Activity Actor occurrence.");
                _gameplayInputConsumers.Remove(playerSlotId);
            }

            if (actor.PlayerInput == null || actor.PlayerInput.actions == null)
            {
                issue =
                    "Gameplay input consumer requires current PlayerInput evidence on the Logical Actor declaration.";
                return false;
            }

            InputActionMap gameplayActionMap = actor.PlayerInput.currentActionMap;
            if (gameplayActionMap == null ||
                !string.Equals(gameplayActionMap.name, input.ActionMapName, StringComparison.Ordinal) ||
                !ReferenceEquals(gameplayActionMap.asset, actor.PlayerInput.actions))
            {
                issue =
                    $"Gameplay input consumer expected current runtime Action Map '{input.ActionMapName}' on PlayerInput.actions.";
                return false;
            }

            if (admission.InputBindingToken != input.Token)
            {
                issue =
                    "Gameplay input consumer requires admission and input evidence from the same Activity occurrence.";
                return false;
            }

            if (!consumer.TryBindRuntime(
                    actor,
                    actor.PlayerInput,
                    gameplayActionMap,
                    input.Token,
                    IsGameplayInputReady,
                    out issue))
                return false;

            _gameplayInputConsumers[playerSlotId] = consumer;
            return true;
        }

        private bool IsGameplayInputReady(PlayerGameplayInputBindingToken bindingToken)
        {
            if (!bindingToken.IsValid)
                return false;

            PlayerGameplayAdmissionSnapshot snapshot = _admissionContext.CreateSnapshot();
            return snapshot != null && snapshot.IsInitialized &&
                snapshot.TryGetSummary(
                    bindingToken.PlayerSlotId,
                    out PlayerGameplayAdmissionSummary admission) &&
                admission.GameplayReady &&
                admission.InputBindingToken == bindingToken;
        }

        private void ReleaseInputConsumer(PlayerSlotId playerSlotId, string reason)
        {
            if (!_gameplayInputConsumers.TryGetValue(playerSlotId, out var consumer))
                return;

            _gameplayInputConsumers.Remove(playerSlotId);
            if (consumer != null)
                consumer.ReleaseRuntimeBinding(reason);
        }

        private bool TryReleaseContextualChain(
            ChainEvidence chain, string source, string reason, out string issue)
        {
            issue = string.Empty;
            if (chain == null)
                return true;
            if (chain.admissionCreated && chain.admission.Token.IsValid)
            {
                ReleaseInputConsumer(
                    chain.admission.PlayerSlotId,
                    "Gameplay input consumer authority released during contextual rollback.");

                if (!_admissionContext.TryRelease(chain.admission.PlayerSlotId, chain.admission.Token,
                        source, reason).Succeeded)
                {
                    issue = "Could not release contextual gameplay admission after projection failure.";
                    return false;
                }
            }
            if (chain.cameraCreated && chain.camera.Token.IsValid &&
                !_cameraContext.TryRelease(chain.camera.PlayerSlotId, chain.camera.Token,
                    source, reason).Succeeded)
            {
                issue = "Could not release contextual camera evidence after projection failure.";
                return false;
            }
            if (chain.inputCreated && chain.input.Token.IsValid &&
                !_inputContext.TryRelease(chain.input.PlayerSlotId, chain.input.Token,
                    source, reason).Succeeded)
            {
                issue = "Could not release contextual input binding after projection failure.";
                return false;
            }
            // Occupancy is Session physical state and intentionally survives contextual rollback.
            return true;
        }

        private static string Join(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left)) return right ?? string.Empty;
            if (string.IsNullOrWhiteSpace(right)) return left;
            return left + " | " + right;
        }
    }
}
