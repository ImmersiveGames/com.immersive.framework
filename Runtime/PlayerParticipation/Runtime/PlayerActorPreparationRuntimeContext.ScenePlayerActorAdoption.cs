using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeContext
    {
        private const string SceneAdoptionResourceType = "ScenePlayerActorRuntimeHost";

        private sealed class SceneAdoptionRecord
        {
            internal SceneAdoptionRecord(
                ScenePlayerActorAdoptionToken token,
                RuntimeContentOwner owner,
                LocalPlayerHostAuthoring host,
                PlayerActorDeclaration sceneActor,
                GameObject releaseProxy,
                string previousActorId,
                string previousDisplayName,
                string previousReason,
                PlayerInput previousPlayerInput)
            {
                Token = token;
                Owner = owner;
                Host = host;
                SceneActor = sceneActor;
                ReleaseProxy = releaseProxy;
                PreviousActorId = previousActorId.NormalizeText();
                PreviousDisplayName = previousDisplayName.NormalizeText();
                PreviousReason = previousReason.NormalizeText();
                PreviousPlayerInput = previousPlayerInput;
            }

            internal ScenePlayerActorAdoptionToken Token { get; }
            internal RuntimeContentOwner Owner { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration SceneActor { get; }
            internal GameObject ReleaseProxy { get; }
            internal string PreviousActorId { get; }
            internal string PreviousDisplayName { get; }
            internal string PreviousReason { get; }
            internal PlayerInput PreviousPlayerInput { get; }
        }

        private readonly Dictionary<PlayerSlotId, SceneAdoptionRecord> _sceneAdoptions =
            new Dictionary<PlayerSlotId, SceneAdoptionRecord>();
        private int _sceneAdoptionSequence;

        internal int SceneAdoptionCount => _sceneAdoptions.Count;

        internal ScenePlayerActorAdoptionResult TryAdoptScenePlayerActor(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeScopeContext activityScopeContext,
            RuntimeScopeContext physicalScopeContext,
            SceneProvidedLocalPlayerAuthoring authoring,
            string source,
            string reason)
        {
            const string operation = "AdoptScenePlayerActor";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "adopt-scene-player-actor");

            string issue = string.Empty;
            PlayerSlotId playerSlotId = default;
            if (runtimeContentRuntime == null ||
                !activityScopeContext.IsValid ||
                !physicalScopeContext.IsValid ||
                physicalScopeContext.Scope != RuntimeContentScope.Session ||
                authoring == null ||
                !authoring.TryGetPlayerSlotId(out playerSlotId, out issue))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    string.IsNullOrWhiteSpace(issue)
                        ? "Scene Player Actor adoption requires runtime content, valid Activity contextual and Session physical scopes and complete authoring."
                        : issue);
            }

            if (!authoring.TryValidateRuntimeEvidence(out issue))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedActorMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            if (!_participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsValid ||
                !slot.IsJoined)
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedSlotNotJoined,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    $"Scene Player Actor adoption requires Joined Slot '{playerSlotId.StableText}'.");
            }

            if (!slot.HasSelectedActor ||
                slot.SelectedActorProfile == null ||
                !ReferenceEquals(slot.SelectedActorProfile, authoring.ActorProfile))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedSelectionMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene Player Actor adoption requires the authored Actor Profile to be the current Session selection.");
            }

            LocalPlayerHostAuthoring host = authoring.LocalPlayerHost;
            PlayerActorRuntimeHost sceneRuntimeHost = authoring.ScenePlayerActorRuntimeHost;
            PlayerActorDeclaration sceneActor = authoring.ScenePlayerActorDeclaration;
            GameObject scenePresentation = authoring.ScenePresentation;
            if (host == null ||
                !host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId ||
                host.JoinedConfiguredIndex != slot.ConfiguredIndex)
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedHostMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene Player Actor adoption requires matching committed Host and Slot evidence.");
            }

            if (host.transform.parent != null)
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedHostMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene Player Actor adoption requires the declared Local Player Host to be a composition root so its whole physical Player hierarchy can be promoted atomically.");
            }

            if (!TryResolveCurrentActorCorrelation(
                    activityScopeContext,
                    playerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    host,
                    out PlayerSlotAssignmentSnapshot assignment,
                    out PlayerHostEvidenceSnapshot hostEvidence,
                    out issue))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedHostMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            if (sceneRuntimeHost == null ||
                sceneActor == null ||
                scenePresentation == null ||
                host.ActorMount == null ||
                !ReferenceEquals(sceneRuntimeHost.transform.parent, host.ActorMount) ||
                !ReferenceEquals(scenePresentation.transform.parent, sceneRuntimeHost.PresentationMount))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedActorMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene Player Actor Runtime Host and Presentation must remain under their exact authored mounts.");
            }

            if (_sceneAdoptions.TryGetValue(playerSlotId, out SceneAdoptionRecord existingAdoption))
            {
                if (IsCurrentSceneAdoption(
                        existingAdoption,
                        physicalScopeContext.Owner,
                        host,
                        sceneActor) &&
                    _records.TryGetValue(playerSlotId, out PreparationRecord existingPreparation) &&
                    existingPreparation.Summary.IsPrepared &&
                    existingPreparation.Summary.Token == existingAdoption.Token.PreparationToken)
                {
                    return SceneAdoptionResult(
                        ScenePlayerActorAdoptionStatus.SucceededAlreadyAdopted,
                        operation,
                        playerSlotId,
                        authoring,
                        existingAdoption.Token,
                        false,
                        resolvedSource,
                        resolvedReason,
                        "Scene Logical Player Actor is already adopted by the same Activity owner and Host.");
                }

                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedForeignOrStaleAdoption,
                    operation,
                    playerSlotId,
                    authoring,
                    existingAdoption.Token,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Player Slot retains a different or stale Scene Actor adoption. Release it explicitly before re-adoption.");
            }

            if (_records.ContainsKey(playerSlotId))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedPreparationConflict,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Player Slot already has a prepared Logical Actor that is not owned by this Scene adoption.");
            }

            if (!authoring.ActorProfile.TryGetActorProfileId(
                    out ActorProfileId actorProfileId,
                    out issue))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedActorMismatch,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            _sceneAdoptionSequence++;
            int adoptionRevision = _sceneAdoptionSequence;
            if (!PlayerActorMaterializationOperationId.TryCreate(
                    _sessionContextId,
                    physicalScopeContext.Owner,
                    playerSlotId,
                    adoptionRevision,
                    out PlayerActorMaterializationOperationId operationId,
                    out issue))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            ActorId actorId;
            RuntimeContentId runtimeContentId;
            try
            {
                string suffix =
                    $"{_sessionContextId}:{physicalScopeContext.Owner.Scope}:" +
                    $"{physicalScopeContext.Owner.OwnerIdentity.Value.Value}:" +
                    $"{playerSlotId.Value.Value}:{adoptionRevision}";
                actorId = ActorId.From($"scene-player-actor:{suffix}");
                runtimeContentId = RuntimeContentId.From(
                    $"scene-player-actor-content:{suffix}");
            }
            catch (Exception exception)
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    $"Scene Player Actor identity generation failed. {exception.Message}");
            }

            var request = new PlayerActorMaterializationRequest(
                operationId,
                _sessionContextId,
                physicalScopeContext,
                slot,
                authoring.ActorProfile,
                host,
                actorId,
                runtimeContentId,
                adoptionRevision,
                resolvedSource,
                resolvedReason);
            var resource = new RuntimeMaterializationResource(
                SceneAdoptionResourceType,
                actorProfileId.Value.Value,
                authoring.ActorProfile.DisplayName,
                "SceneProvidedOriginalPromotedToSession");

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    physicalScopeContext,
                    runtimeContentId,
                    resource,
                    resolvedSource,
                    resolvedReason,
                    out RuntimeMaterializationRequest runtimeRequest,
                    out RuntimeScopeTransitionGuardResult guardResult))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.FailedRuntimeContentRegistration,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    guardResult.Message);
            }

            GameObject releaseProxy = null;
            RuntimeContentHandle runtimeHandle = null;
            string previousActorId = sceneActor.ActorId.Value.Value;
            string previousDisplayName = sceneActor.ActorDisplayName;
            string previousReason = sceneActor.Reason;
            PlayerInput previousPlayerInput = sceneActor.PlayerInput;
            try
            {
                sceneActor.ConfigureForDiagnostics(
                    actorId.Value.Value,
                    authoring.ActorProfile.DisplayName,
                    host.PlayerInput,
                    $"{resolvedReason}; ownership='SessionOwned'; origin='SceneProvided'; profile='{actorProfileId.StableText}'; " +
                    $"slot='{playerSlotId.StableText}'; owner='{physicalScopeContext.Owner.StableText}'.");

                if (!sceneActor.HasPlayerInputEvidence ||
                    !ReferenceEquals(sceneActor.PlayerInput, host.PlayerInput) ||
                    sceneActor.ActorId != actorId)
                {
                    RestoreSceneActorDeclaration(
                        sceneActor,
                        previousActorId,
                        previousDisplayName,
                        previousReason,
                        previousPlayerInput);
                    return SceneAdoptionResult(
                        ScenePlayerActorAdoptionStatus.RejectedActorMismatch,
                        operation,
                        playerSlotId,
                        authoring,
                        default,
                        false,
                        resolvedSource,
                        resolvedReason,
                        "Scene Logical Player Actor identity or PlayerInput evidence did not match the adoption request.");
                }

                runtimeHandle = RuntimeContentHandle.Materialized(
                    runtimeRequest.Identity,
                    resolvedSource,
                    resolvedReason);
                RuntimeMaterializationResult physicalResult =
                    RuntimeMaterializationResult.Success(
                        runtimeRequest,
                        runtimeHandle,
                        resolvedSource,
                        resolvedReason,
                        "External Scene Logical Player Actor adopted without physical instantiation.");
                RuntimeMaterializationResult appliedResult =
                    runtimeContentRuntime.ApplyMaterializationResult(
                        physicalResult,
                        resolvedSource,
                        resolvedReason);
                if (!appliedResult.Succeeded)
                {
                    bool rollbackSucceeded = TryRollbackSceneAdoptionRegistration(
                        runtimeContentRuntime,
                        runtimeRequest,
                        sceneActor,
                        host.PlayerInput,
                        null,
                        previousActorId,
                        previousDisplayName,
                        previousReason,
                        previousPlayerInput,
                        resolvedSource,
                        "scene-adoption-registration-rollback",
                        out string rollbackIssue);
                    return SceneAdoptionResult(
                        rollbackSucceeded
                            ? ScenePlayerActorAdoptionStatus.FailedRuntimeContentRegistration
                            : ScenePlayerActorAdoptionStatus.FailedRollback,
                        operation,
                        playerSlotId,
                        authoring,
                        default,
                        false,
                        resolvedSource,
                        resolvedReason,
                        rollbackSucceeded
                            ? appliedResult.Message
                            : $"{appliedResult.Message} Rollback failed. {rollbackIssue}");
                }

                releaseProxy = new GameObject(
                    $"[_Framework Scene Actor Adoption] {playerSlotId.StableText}");
                releaseProxy.SetActive(false);
                releaseProxy.transform.SetParent(host.ActorMount, false);

                var handle = new PlayerActorMaterializationHandle(
                    request,
                    runtimeRequest,
                    runtimeHandle,
                    host,
                    host.PlayerInput,
                    sceneRuntimeHost,
                    scenePresentation,
                    releaseProxy,
                    true,
                    resolvedSource,
                    resolvedReason);
                if (!handle.TryActivate(
                        resolvedSource,
                        resolvedReason,
                        out string activationIssue))
                {
                    bool rollbackSucceeded = TryRollbackSceneAdoptionRegistration(
                        runtimeContentRuntime,
                        runtimeRequest,
                        sceneActor,
                        host.PlayerInput,
                        releaseProxy,
                        previousActorId,
                        previousDisplayName,
                        previousReason,
                        previousPlayerInput,
                        resolvedSource,
                        "scene-adoption-activation-rollback",
                        out string rollbackIssue);
                    return SceneAdoptionResult(
                        rollbackSucceeded
                            ? ScenePlayerActorAdoptionStatus.FailedActivation
                            : ScenePlayerActorAdoptionStatus.FailedRollback,
                        operation,
                        playerSlotId,
                        authoring,
                        default,
                        false,
                        resolvedSource,
                        resolvedReason,
                        rollbackSucceeded
                            ? activationIssue
                            : $"{activationIssue} Rollback failed. {rollbackIssue}");
                }

                // The Session record below remains the authority. This Unity scene
                // migration only prevents the adopted original object from being
                // unloaded with the supplying Activity scene.
                UnityEngine.Object.DontDestroyOnLoad(host.gameObject);

                PlayerActorPreparationSummary prepared = CreatePreparedSummary(
                    slot,
                    handle,
                    assignment,
                    hostEvidence,
                    PlayerActorPhysicalOwnership.FrameworkOwned,
                    PlayerActorPreparationState.Prepared,
                    resolvedSource,
                    resolvedReason,
                    "Original Scene Logical Player Actor adopted into Session physical lifetime.");
                _records.Add(
                    playerSlotId,
                    new PreparationRecord(handle, host, prepared));
                _revision++;

                var token = new ScenePlayerActorAdoptionToken(
                    _sessionContextId,
                    playerSlotId,
                    actorId,
                    runtimeRequest.Identity,
                    prepared.Token,
                    adoptionRevision);
                _sceneAdoptions.Add(
                    playerSlotId,
                    new SceneAdoptionRecord(
                        token,
                        physicalScopeContext.Owner,
                        host,
                        sceneActor,
                        releaseProxy,
                        previousActorId,
                        previousDisplayName,
                        previousReason,
                        previousPlayerInput));
                _lastOperationStatus = PlayerActorPreparationStatus.SucceededPrepared;
                _lastOperationMessage =
                    "Original Scene Logical Player Actor adopted by the Session physical representation authority.";

                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.SucceededAdopted,
                    operation,
                    playerSlotId,
                    authoring,
                    token,
                    true,
                    resolvedSource,
                    resolvedReason,
                    _lastOperationMessage);
            }
            catch (Exception exception)
            {
                bool preparationRemoved = _records.Remove(playerSlotId);
                bool adoptionRemoved = _sceneAdoptions.Remove(playerSlotId);
                if (preparationRemoved || adoptionRemoved)
                {
                    _revision++;
                }

                bool rollbackSucceeded = TryRollbackSceneAdoptionRegistration(
                    runtimeContentRuntime,
                    runtimeRequest,
                    sceneActor,
                    host.PlayerInput,
                    releaseProxy,
                    previousActorId,
                    previousDisplayName,
                    previousReason,
                    previousPlayerInput,
                    resolvedSource,
                    "scene-adoption-exception-rollback",
                    out string rollbackIssue);
                return SceneAdoptionResult(
                    rollbackSucceeded
                        ? ScenePlayerActorAdoptionStatus.FailedRuntimeContentRegistration
                        : ScenePlayerActorAdoptionStatus.FailedRollback,
                    operation,
                    playerSlotId,
                    authoring,
                    default,
                    false,
                    resolvedSource,
                    resolvedReason,
                    rollbackSucceeded
                        ? $"Scene Player Actor adoption threw '{exception.GetType().Name}'. {exception.Message}"
                        : $"Scene Player Actor adoption threw '{exception.GetType().Name}'. {exception.Message} Rollback failed. {rollbackIssue}");
            }
        }

        internal ScenePlayerActorAdoptionResult TryReleaseScenePlayerActorAdoption(
            SceneProvidedLocalPlayerAuthoring authoring,
            ScenePlayerActorAdoptionToken expectedToken,
            string source,
            string reason)
        {
            const string operation = "ReleaseScenePlayerActorAdoption";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-scene-player-actor-adoption");
            PlayerSlotId playerSlotId = expectedToken.PlayerSlotId;

            if (authoring == null ||
                !expectedToken.IsValid ||
                expectedToken.SessionContextId != _sessionContextId ||
                !_sceneAdoptions.TryGetValue(playerSlotId, out SceneAdoptionRecord adoption) ||
                adoption.Token != expectedToken ||
                !ReferenceEquals(adoption.Host, authoring.LocalPlayerHost) ||
                !ReferenceEquals(adoption.SceneActor, authoring.SceneLogicalPlayerActor))
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.RejectedForeignOrStaleAdoption,
                    operation,
                    playerSlotId,
                    authoring,
                    expectedToken,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene Player Actor release rejected a foreign or stale adoption token.");
            }

            if (!_records.ContainsKey(playerSlotId))
            {
                _sceneAdoptions.Remove(playerSlotId);
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.SucceededReleased,
                    operation,
                    playerSlotId,
                    authoring,
                    expectedToken,
                    true,
                    resolvedSource,
                    resolvedReason,
                    "Scene Actor adoption bookkeeping finalized after terminal canonical preparation release; the adopted physical Host/Actor may already have been destroyed by its terminal adapter.");
            }

            PlayerActorPreparationResult release = TryReleasePreparedActor(
                playerSlotId,
                expectedToken.PreparationToken,
                resolvedSource,
                resolvedReason);
            if (release == null || !release.Succeeded)
            {
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.FailedRelease,
                    operation,
                    playerSlotId,
                    authoring,
                    expectedToken,
                    false,
                    resolvedSource,
                    resolvedReason,
                    release != null
                        ? release.ToDiagnosticString()
                        : "Scene Player Actor preparation release returned no result.");
            }

            _sceneAdoptions.Remove(playerSlotId);
            return SceneAdoptionResult(
                ScenePlayerActorAdoptionStatus.SucceededReleased,
                operation,
                playerSlotId,
                authoring,
                expectedToken,
                true,
                resolvedSource,
                resolvedReason,
                "Scene Player Actor adoption released through terminal physical cleanup; Framework proxy and runtime evidence were removed and the adopted physical composition is not preserved as external Scene ownership.");
        }

        internal bool TryGetScenePlayerActorAdoption(
            PlayerSlotId playerSlotId,
            out ScenePlayerActorAdoptionToken token)
        {
            if (playerSlotId.IsValid &&
                _sceneAdoptions.TryGetValue(playerSlotId, out SceneAdoptionRecord record) &&
                record.Token.IsValid)
            {
                token = record.Token;
                return true;
            }

            token = default;
            return false;
        }

        private bool IsCurrentSceneAdoption(
            SceneAdoptionRecord record,
            RuntimeContentOwner owner,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration sceneActor)
        {
            return record != null &&
                record.Token.IsValid &&
                record.Owner == owner &&
                ReferenceEquals(record.Host, host) &&
                ReferenceEquals(record.SceneActor, sceneActor) &&
                record.SceneActor != null &&
                record.ReleaseProxy != null;
        }

        private static bool TryRollbackSceneAdoptionRegistration(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeMaterializationRequest runtimeRequest,
            PlayerActorDeclaration sceneActor,
            PlayerInput playerInput,
            GameObject releaseProxy,
            string previousActorId,
            string previousDisplayName,
            string previousReason,
            PlayerInput previousPlayerInput,
            string source,
            string reason,
            out string issue)
        {
            var failures = new List<string>();
            if (sceneActor != null)
            {
                RestoreSceneActorDeclaration(
                    sceneActor,
                    previousActorId,
                    previousDisplayName,
                    previousReason,
                    previousPlayerInput);
            }

            if (releaseProxy != null)
            {
                DestroySceneAdoptionObject(releaseProxy);
            }

            if (runtimeContentRuntime != null && runtimeRequest.IsValid)
            {
                try
                {
                    if (runtimeContentRuntime.TryGetHandle(
                            runtimeRequest.Context,
                            runtimeRequest.Identity,
                            out _))
                    {
                        RuntimeReleaseResult release =
                            runtimeContentRuntime.ReleaseHandleLogically(
                                runtimeRequest.Context,
                                runtimeRequest.Identity,
                                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                                source,
                                reason);
                        if (!release.Succeeded)
                        {
                            failures.Add(release.Message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(exception.Message);
                }
            }

            issue = string.Join(" | ", failures);
            return failures.Count == 0;
        }

        private static void RestoreSceneActorDeclaration(
            SceneAdoptionRecord record)
        {
            if (record == null)
            {
                return;
            }

            RestoreSceneActorDeclaration(
                record.SceneActor,
                record.PreviousActorId,
                record.PreviousDisplayName,
                record.PreviousReason,
                record.PreviousPlayerInput);
        }

        private static void RestoreSceneActorDeclaration(
            PlayerActorDeclaration sceneActor,
            string previousActorId,
            string previousDisplayName,
            string previousReason,
            PlayerInput previousPlayerInput)
        {
            if (sceneActor == null)
            {
                return;
            }

            sceneActor.ConfigureForDiagnostics(
                previousActorId,
                previousDisplayName,
                previousPlayerInput,
                previousReason);
        }

        private ScenePlayerActorAdoptionResult SceneAdoptionResult(
            ScenePlayerActorAdoptionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            SceneProvidedLocalPlayerAuthoring authoring,
            ScenePlayerActorAdoptionToken token,
            bool stateChanged,
            string source,
            string reason,
            string message)
        {
            return new ScenePlayerActorAdoptionResult(
                status,
                operation,
                playerSlotId,
                authoring != null ? authoring.ActorProfile : null,
                authoring != null ? authoring.SceneLogicalPlayerActor : null,
                token,
                stateChanged,
                source,
                reason,
                message);
        }

        private static void DestroySceneAdoptionObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
