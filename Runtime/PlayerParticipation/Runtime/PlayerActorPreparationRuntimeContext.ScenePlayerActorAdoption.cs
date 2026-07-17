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
        private const string SceneAdoptionResourceType = "ScenePlayerLogicalActorHost";

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

        private readonly Dictionary<PlayerSlotId, SceneAdoptionRecord> sceneAdoptions =
            new Dictionary<PlayerSlotId, SceneAdoptionRecord>();
        private int sceneAdoptionSequence;

        internal int SceneAdoptionCount => sceneAdoptions.Count;

        internal ScenePlayerActorAdoptionResult TryAdoptScenePlayerActor(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeScopeContext scopeContext,
            SceneLocalPlayerAdmissionAuthoring authoring,
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
                !scopeContext.IsValid ||
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
                        ? "Scene Player Actor adoption requires runtime content, a valid Activity scope and complete authoring."
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

            if (!participationContext.TryGetActorSelection(
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
            PlayerActorDeclaration sceneActor = authoring.SceneLogicalPlayerActor;
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

            if (sceneActor == null ||
                host.ActorMount == null ||
                (!ReferenceEquals(sceneActor.transform, host.ActorMount) &&
                 !sceneActor.transform.IsChildOf(host.ActorMount)))
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
                    "Scene Logical Player Actor must remain under the exact Local Player Host Actor Mount.");
            }

            if (sceneAdoptions.TryGetValue(playerSlotId, out SceneAdoptionRecord existingAdoption))
            {
                if (IsCurrentSceneAdoption(
                        existingAdoption,
                        scopeContext.Owner,
                        host,
                        sceneActor) &&
                    records.TryGetValue(playerSlotId, out PreparationRecord existingPreparation) &&
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

            if (records.ContainsKey(playerSlotId))
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

            sceneAdoptionSequence++;
            int adoptionRevision = sceneAdoptionSequence;
            if (!PlayerActorMaterializationOperationId.TryCreate(
                    sessionContextId,
                    scopeContext.Owner,
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
                    $"{sessionContextId}:{scopeContext.Owner.Scope}:" +
                    $"{scopeContext.Owner.OwnerIdentity.Value.Value}:" +
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
                sessionContextId,
                scopeContext,
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
                "ExternalSceneOwned");

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    scopeContext,
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
                    $"{resolvedReason}; ownership='ExternalSceneOwned'; profile='{actorProfileId.StableText}'; " +
                    $"slot='{playerSlotId.StableText}'; owner='{scopeContext.Owner.StableText}'.");

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
                    sceneActor,
                    releaseProxy,
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

                PlayerActorPreparationSummary prepared = CreatePreparedSummary(
                    slot,
                    handle,
                    PlayerActorPreparationState.Prepared,
                    resolvedSource,
                    resolvedReason,
                    "External Scene Logical Player Actor adopted and prepared without physical ownership transfer.");
                records.Add(playerSlotId, new PreparationRecord(handle, prepared));
                revision++;

                var token = new ScenePlayerActorAdoptionToken(
                    sessionContextId,
                    playerSlotId,
                    actorId,
                    runtimeRequest.Identity,
                    prepared.Token,
                    adoptionRevision);
                sceneAdoptions.Add(
                    playerSlotId,
                    new SceneAdoptionRecord(
                        token,
                        scopeContext.Owner,
                        host,
                        sceneActor,
                        releaseProxy,
                        previousActorId,
                        previousDisplayName,
                        previousReason,
                        previousPlayerInput));
                lastOperationStatus = PlayerActorPreparationStatus.SucceededPrepared;
                lastOperationMessage =
                    "External Scene Logical Player Actor adopted by the preparation authority.";

                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.SucceededAdopted,
                    operation,
                    playerSlotId,
                    authoring,
                    token,
                    true,
                    resolvedSource,
                    resolvedReason,
                    lastOperationMessage);
            }
            catch (Exception exception)
            {
                bool preparationRemoved = records.Remove(playerSlotId);
                bool adoptionRemoved = sceneAdoptions.Remove(playerSlotId);
                if (preparationRemoved || adoptionRemoved)
                {
                    revision++;
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
            SceneLocalPlayerAdmissionAuthoring authoring,
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
                expectedToken.SessionContextId != sessionContextId ||
                !sceneAdoptions.TryGetValue(playerSlotId, out SceneAdoptionRecord adoption) ||
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

            if (!records.ContainsKey(playerSlotId))
            {
                if (adoption.SceneActor == null)
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
                        "Canonical preparation was released, but the external Scene Actor is missing.");
                }

                RestoreSceneActorDeclaration(adoption);
                sceneAdoptions.Remove(playerSlotId);
                return SceneAdoptionResult(
                    ScenePlayerActorAdoptionStatus.SucceededReleased,
                    operation,
                    playerSlotId,
                    authoring,
                    expectedToken,
                    true,
                    resolvedSource,
                    resolvedReason,
                    "Scene Actor adoption bookkeeping finalized after canonical preparation release; external Actor preserved.");
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

            if (adoption.SceneActor == null)
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
                    "Preparation released, but the external Scene Actor is missing.");
            }

            RestoreSceneActorDeclaration(adoption);
            sceneAdoptions.Remove(playerSlotId);
            return SceneAdoptionResult(
                ScenePlayerActorAdoptionStatus.SucceededReleased,
                operation,
                playerSlotId,
                authoring,
                expectedToken,
                true,
                resolvedSource,
                resolvedReason,
                "Scene Player Actor adoption released. Framework proxy and runtime evidence were removed; external Actor was preserved.");
        }

        internal bool TryGetScenePlayerActorAdoption(
            PlayerSlotId playerSlotId,
            out ScenePlayerActorAdoptionToken token)
        {
            if (playerSlotId.IsValid &&
                sceneAdoptions.TryGetValue(playerSlotId, out SceneAdoptionRecord record) &&
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
            SceneLocalPlayerAdmissionAuthoring authoring,
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
