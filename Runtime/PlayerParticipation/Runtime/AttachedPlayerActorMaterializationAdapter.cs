using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Unity adapter that stages one ActorProfile Logical Actor Host below an explicit
    /// LocalPlayerHostAuthoring.ActorMount. It does not choose the Profile, own Session
    /// selection, apply occupancy, enable gameplay input or publish camera requests.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.3/P3J.4 attached Unity Logical Player Actor materialization and release adapter.")]
    internal sealed class AttachedPlayerActorMaterializationAdapter
    {
        private const string ResourceType = "PlayerLogicalActorHost";

        private readonly RuntimeContentRuntime runtimeContentRuntime;
        private readonly string sessionContextId;
        private int materializationSequence;

        internal AttachedPlayerActorMaterializationAdapter(
            RuntimeContentRuntime runtimeContentRuntime,
            string sessionContextId)
        {
            this.runtimeContentRuntime = runtimeContentRuntime ??
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
            this.sessionContextId = sessionContextId.NormalizeText();
            if (string.IsNullOrEmpty(this.sessionContextId))
            {
                throw new ArgumentException(
                    "Attached Player Actor materialization adapter requires a non-empty Session context identity.",
                    nameof(sessionContextId));
            }
        }

        internal string SessionContextId => sessionContextId;

        internal PlayerActorMaterializationResult TryMaterialize(
            RuntimeScopeContext scopeContext,
            PlayerSlotRuntimeSnapshot slot,
            ActorProfile actorProfile,
            LocalPlayerHostAuthoring localPlayerHost,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(AttachedPlayerActorMaterializationAdapter));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "logical-player-actor-materialization");

            if (!scopeContext.IsValid)
            {
                return Failure(
                    PlayerActorMaterializationStatus.RejectedInvalidRequest,
                    default,
                    localPlayerHost,
                    null,
                    "Logical Player Actor materialization requires a valid Runtime Scope Context.");
            }

            if (!slot.IsValid || !slot.IsJoined)
            {
                return Failure(
                    PlayerActorMaterializationStatus.RejectedInvalidRequest,
                    default,
                    localPlayerHost,
                    localPlayerHost != null ? localPlayerHost.PlayerInput : null,
                    "Logical Player Actor materialization requires a valid Joined Player Slot snapshot.");
            }

            if (localPlayerHost == null)
            {
                return Failure(
                    PlayerActorMaterializationStatus.RejectedHostUnavailable,
                    default,
                    null,
                    null,
                    "Logical Player Actor materialization requires an explicit Local Player Host.");
            }

            if (!TryValidateHost(localPlayerHost, slot, out PlayerActorMaterializationStatus hostStatus, out string hostIssue))
            {
                return Failure(
                    hostStatus,
                    default,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    hostIssue);
            }

            if (!TryValidateProfile(
                    actorProfile,
                    out ActorProfileId actorProfileId,
                    out PlayerActorMaterializationStatus profileStatus,
                    out string profileIssue))
            {
                return Failure(
                    profileStatus,
                    default,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    profileIssue);
            }

            materializationSequence++;
            int materializationRevision = materializationSequence;
            if (!PlayerActorMaterializationOperationId.TryCreate(
                    sessionContextId,
                    scopeContext.Owner,
                    slot.PlayerSlotId,
                    materializationRevision,
                    out PlayerActorMaterializationOperationId operationId,
                    out string operationIssue))
            {
                return Failure(
                    PlayerActorMaterializationStatus.FailedActorIdentity,
                    default,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    operationIssue);
            }

            if (!TryCreateRuntimeIdentities(
                    scopeContext,
                    slot,
                    materializationRevision,
                    out ActorId actorId,
                    out RuntimeContentId runtimeContentId,
                    out string identityIssue))
            {
                return Failure(
                    PlayerActorMaterializationStatus.FailedActorIdentity,
                    default,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    identityIssue);
            }

            var request = new PlayerActorMaterializationRequest(
                operationId,
                sessionContextId,
                scopeContext,
                slot,
                actorProfile,
                localPlayerHost,
                actorId,
                runtimeContentId,
                materializationRevision,
                resolvedSource,
                resolvedReason);

            var resource = new RuntimeMaterializationResource(
                ResourceType,
                actorProfileId.Value.Value,
                actorProfile.DisplayName,
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    scopeContext,
                    runtimeContentId,
                    resource,
                    resolvedSource,
                    resolvedReason,
                    out RuntimeMaterializationRequest runtimeRequest,
                    out RuntimeScopeTransitionGuardResult guardResult))
            {
                return PlayerActorMaterializationResult.Failure(
                    MapGuardStatus(guardResult),
                    request,
                    default,
                    default,
                    false,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    null,
                    null,
                    guardResult.Message);
            }

            GameObject stagingRoot = null;
            GameObject logicalActorHost = null;
            PlayerActorDeclaration declaration = null;
            try
            {
                stagingRoot = new GameObject(
                    $"[{operationId.StableText}] Staging");
                stagingRoot.SetActive(false);
                stagingRoot.transform.SetParent(localPlayerHost.ActorMount, false);

                logicalActorHost = UnityEngine.Object.Instantiate(
                    actorProfile.LogicalActorHostPrefab,
                    stagingRoot.transform,
                    false);
                if (logicalActorHost == null)
                {
                    return MaterializerFailure(
                        PlayerActorMaterializationStatus.FailedInstantiate,
                        request,
                        runtimeRequest,
                        localPlayerHost,
                        localPlayerHost.PlayerInput,
                        null,
                        null,
                        "Logical Player Actor prefab instantiation returned null.");
                }

                logicalActorHost.name = actorProfile.LogicalActorHostPrefab.name;
                logicalActorHost.SetActive(false);
                logicalActorHost.transform.SetParent(localPlayerHost.ActorMount, false);
                DestroyObject(stagingRoot);
                stagingRoot = null;

                if (!TryValidateInstance(
                        logicalActorHost,
                        out declaration,
                        out PlayerActorMaterializationStatus instanceStatus,
                        out string instanceIssue))
                {
                    return RollbackFailureOrOriginal(
                        instanceStatus,
                        request,
                        runtimeRequest,
                        localPlayerHost,
                        localPlayerHost.PlayerInput,
                        declaration,
                        logicalActorHost,
                        instanceIssue,
                        resolvedSource,
                        resolvedReason);
                }

                declaration.ConfigureForDiagnostics(
                    actorId.Value.Value,
                    actorProfile.DisplayName,
                    localPlayerHost.PlayerInput,
                    $"{resolvedReason}; profile='{actorProfileId.StableText}'; " +
                    $"slot='{slot.PlayerSlotId.StableText}'; owner='{scopeContext.Owner.StableText}'.");

                if (!declaration.HasPlayerInputEvidence ||
                    !ReferenceEquals(declaration.PlayerInput, localPlayerHost.PlayerInput) ||
                    declaration.ActorId != actorId)
                {
                    return RollbackFailureOrOriginal(
                        PlayerActorMaterializationStatus.FailedActorIdentity,
                        request,
                        runtimeRequest,
                        localPlayerHost,
                        localPlayerHost.PlayerInput,
                        declaration,
                        logicalActorHost,
                        "Logical Player Actor identity or PlayerInput evidence did not match the generated materialization request.",
                        resolvedSource,
                        resolvedReason);
                }

                RuntimeContentHandle runtimeHandle = RuntimeContentHandle.Materialized(
                    runtimeRequest.Identity,
                    resolvedSource,
                    resolvedReason);
                RuntimeMaterializationResult physicalResult = RuntimeMaterializationResult.Success(
                    runtimeRequest,
                    runtimeHandle,
                    resolvedSource,
                    resolvedReason,
                    "Attached Logical Player Actor instance staged inactive.");
                RuntimeMaterializationResult appliedResult =
                    runtimeContentRuntime.ApplyMaterializationResult(
                        physicalResult,
                        resolvedSource,
                        resolvedReason);

                if (!appliedResult.Succeeded)
                {
                    return RollbackFailureOrOriginal(
                        PlayerActorMaterializationStatus.FailedRuntimeContentRegistration,
                        request,
                        runtimeRequest,
                        localPlayerHost,
                        localPlayerHost.PlayerInput,
                        declaration,
                        logicalActorHost,
                        appliedResult.Message,
                        resolvedSource,
                        resolvedReason,
                        appliedResult,
                        true);
                }

                var typedHandle = new PlayerActorMaterializationHandle(
                    request,
                    runtimeRequest,
                    runtimeHandle,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    declaration,
                    logicalActorHost,
                    resolvedSource,
                    resolvedReason);

                return PlayerActorMaterializationResult.Success(
                    request,
                    runtimeRequest,
                    appliedResult,
                    typedHandle,
                    "Logical Player Actor materialized under the explicit Actor Mount and staged inactive.");
            }
            catch (Exception exception)
            {
                return RollbackFailureOrOriginal(
                    PlayerActorMaterializationStatus.FailedInstantiate,
                    request,
                    runtimeRequest,
                    localPlayerHost,
                    localPlayerHost.PlayerInput,
                    declaration,
                    logicalActorHost,
                    $"Logical Player Actor materialization threw '{exception.GetType().Name}'. {exception.Message}",
                    resolvedSource,
                    resolvedReason);
            }
            finally
            {
                if (stagingRoot != null)
                {
                    DestroyObject(stagingRoot);
                }
            }
        }

        internal bool TryRollbackMaterialization(
            PlayerActorMaterializationHandle handle,
            string source,
            string reason,
            out string issue)
        {
            return TryReleaseMaterialization(handle, source, reason, out issue);
        }

        internal bool TryReleaseMaterialization(
            PlayerActorMaterializationHandle handle,
            string source,
            string reason,
            out string issue)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(AttachedPlayerActorMaterializationAdapter));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "logical-player-actor-rollback");

            if (handle == null)
            {
                issue = "Logical Player Actor rollback requires a typed materialization handle.";
                return false;
            }

            if (!string.Equals(
                    handle.Request.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                issue = "Logical Player Actor rollback rejected a foreign Session materialization handle.";
                return false;
            }

            if (handle.State == PlayerActorMaterializationState.Released)
            {
                issue = string.Empty;
                return true;
            }

            handle.MarkReleaseRequested(resolvedSource, resolvedReason);
            try
            {
                if (handle.LogicalActorHost != null)
                {
                    handle.LogicalActorHost.SetActive(false);
                }

                if (handle.PlayerActorDeclaration != null)
                {
                    handle.PlayerActorDeclaration.ClearPlayerInputEvidence(handle.PlayerInput);
                }

                if (handle.LogicalActorHost != null)
                {
                    DestroyObject(handle.LogicalActorHost);
                }

                RuntimeReleaseResult releaseResult =
                    runtimeContentRuntime.ReleaseHandleLogically(
                        handle.RuntimeContentRequest.Context,
                        handle.RuntimeContentRequest.Identity,
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        resolvedSource,
                        resolvedReason);
                if (!releaseResult.Succeeded)
                {
                    handle.MarkReleaseFailed(
                        resolvedSource,
                        resolvedReason,
                        releaseResult.Message);
                    issue = releaseResult.Message;
                    return false;
                }

                handle.MarkReleased(resolvedSource, resolvedReason);
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                handle.MarkReleaseFailed(
                    resolvedSource,
                    resolvedReason,
                    exception.Message);
                issue = $"Logical Player Actor rollback failed. {exception.Message}";
                return false;
            }
        }

        private static bool TryValidateHost(
            LocalPlayerHostAuthoring host,
            PlayerSlotRuntimeSnapshot slot,
            out PlayerActorMaterializationStatus status,
            out string issue)
        {
            if (!host.IsJoined || !host.HasJoinedSlot)
            {
                status = PlayerActorMaterializationStatus.RejectedHostNotJoined;
                issue = "Logical Player Actor materialization requires a joined Local Player Host.";
                return false;
            }

            if (host.JoinedPlayerSlotId != slot.PlayerSlotId ||
                host.JoinedConfiguredIndex != slot.ConfiguredIndex)
            {
                status = PlayerActorMaterializationStatus.RejectedSlotMismatch;
                issue = "Local Player Host joined Slot identity does not match the requested Session Slot snapshot.";
                return false;
            }

            if (host.PlayerInput == null || host.ActorMount == null)
            {
                status = PlayerActorMaterializationStatus.RejectedHostUnavailable;
                issue = "Local Player Host requires explicit PlayerInput and Actor Mount evidence.";
                return false;
            }

            if (!ReferenceEquals(host.PlayerInput.gameObject, host.gameObject) ||
                ReferenceEquals(host.ActorMount, host.transform) ||
                !host.ActorMount.IsChildOf(host.transform))
            {
                status = PlayerActorMaterializationStatus.RejectedHostUnavailable;
                issue = "Local Player Host PlayerInput or Actor Mount evidence is not attached to the declared technical host.";
                return false;
            }

            PlayerInput[] playerInputs = host.GetComponentsInChildren<PlayerInput>(true);
            if (playerInputs.Length != 1 || !ReferenceEquals(playerInputs[0], host.PlayerInput))
            {
                status = PlayerActorMaterializationStatus.RejectedHostUnavailable;
                issue = $"Local Player Host requires exactly one PlayerInput authority. Found '{playerInputs.Length}'.";
                return false;
            }

            if (host.ActorMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                status = PlayerActorMaterializationStatus.RejectedHostUnavailable;
                issue = "Local Player Host Actor Mount must not contain a second PlayerInput.";
                return false;
            }

            if (host.PlayerSlotDeclaration == null)
            {
                status = PlayerActorMaterializationStatus.RejectedHostNotJoined;
                issue = "Joined Local Player Host has no runtime PlayerSlotDeclaration evidence.";
                return false;
            }

            status = PlayerActorMaterializationStatus.SucceededStaged;
            issue = string.Empty;
            return true;
        }

        private static bool TryValidateProfile(
            ActorProfile actorProfile,
            out ActorProfileId actorProfileId,
            out PlayerActorMaterializationStatus status,
            out string issue)
        {
            actorProfileId = default;
            if (actorProfile == null)
            {
                status = PlayerActorMaterializationStatus.RejectedProfileUnavailable;
                issue = "Logical Player Actor materialization requires an explicit Actor Profile.";
                return false;
            }

            if (!actorProfile.TryGetActorProfileId(out actorProfileId, out issue))
            {
                status = PlayerActorMaterializationStatus.RejectedInvalidProfile;
                return false;
            }

            if (actorProfile.ActorKind != ActorKind.Player ||
                actorProfile.ActorRole != ActorRole.Protagonist)
            {
                status = PlayerActorMaterializationStatus.RejectedInvalidProfile;
                issue = $"Actor Profile '{actorProfile.name}' must classify a Player Protagonist Logical Actor.";
                return false;
            }

            if (actorProfile.LogicalActorHostPrefab == null)
            {
                status = PlayerActorMaterializationStatus.RejectedMissingLogicalActorPrefab;
                issue = $"Actor Profile '{actorProfile.name}' has no Logical Actor Host prefab.";
                return false;
            }

            return TryValidateActorObject(
                actorProfile.LogicalActorHostPrefab,
                out _,
                out status,
                out issue,
                "Actor Profile Logical Actor Host prefab");
        }

        private static bool TryValidateInstance(
            GameObject logicalActorHost,
            out PlayerActorDeclaration declaration,
            out PlayerActorMaterializationStatus status,
            out string issue)
        {
            return TryValidateActorObject(
                logicalActorHost,
                out declaration,
                out status,
                out issue,
                "Materialized Logical Actor Host instance");
        }

        private static bool TryValidateActorObject(
            GameObject actorObject,
            out PlayerActorDeclaration declaration,
            out PlayerActorMaterializationStatus status,
            out string issue,
            string label)
        {
            declaration = null;
            if (actorObject == null)
            {
                status = PlayerActorMaterializationStatus.RejectedInvalidLogicalActorPrefab;
                issue = $"{label} is missing.";
                return false;
            }

            if (actorObject.GetComponentInChildren<PlayerInput>(true) != null)
            {
                status = PlayerActorMaterializationStatus.FailedUnexpectedPlayerInput;
                issue = $"{label} must not contain PlayerInput. PlayerInput belongs to the stable Local Player Host.";
                return false;
            }

            PlayerActorDeclaration[] playerDeclarations =
                actorObject.GetComponentsInChildren<PlayerActorDeclaration>(true);
            if (playerDeclarations.Length == 0)
            {
                status = PlayerActorMaterializationStatus.FailedMissingPlayerActorDeclaration;
                issue = $"{label} requires exactly one PlayerActorDeclaration.";
                return false;
            }

            if (playerDeclarations.Length > 1)
            {
                status = PlayerActorMaterializationStatus.FailedMultiplePlayerActorDeclarations;
                issue = $"{label} requires exactly one PlayerActorDeclaration. Found '{playerDeclarations.Length}'.";
                return false;
            }

            ActorDeclaration[] actorDeclarations =
                actorObject.GetComponentsInChildren<ActorDeclaration>(true);
            if (actorDeclarations.Length != 1 ||
                !ReferenceEquals(actorDeclarations[0], playerDeclarations[0]))
            {
                status = PlayerActorMaterializationStatus.FailedUnexpectedActorDeclaration;
                issue = $"{label} requires one canonical PlayerActorDeclaration and no additional ActorDeclaration. Found '{actorDeclarations.Length}'.";
                return false;
            }

            declaration = playerDeclarations[0];
            status = PlayerActorMaterializationStatus.SucceededStaged;
            issue = string.Empty;
            return true;
        }

        private bool TryCreateRuntimeIdentities(
            RuntimeScopeContext scopeContext,
            PlayerSlotRuntimeSnapshot slot,
            int sequence,
            out ActorId actorId,
            out RuntimeContentId runtimeContentId,
            out string issue)
        {
            try
            {
                string identitySuffix =
                    $"{sessionContextId}:{scopeContext.Owner.Scope}:" +
                    $"{scopeContext.Owner.OwnerIdentity.Value.Value}:" +
                    $"{slot.PlayerSlotId.Value.Value}:{sequence}";
                actorId = ActorId.From($"player-actor:{identitySuffix}");
                runtimeContentId = RuntimeContentId.From(
                    $"player-actor-content:{identitySuffix}");
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                actorId = default;
                runtimeContentId = default;
                issue = $"Framework-generated Logical Player Actor identity failed. {exception.Message}";
                return false;
            }
        }

        private PlayerActorMaterializationResult RollbackFailureOrOriginal(
            PlayerActorMaterializationStatus originalStatus,
            PlayerActorMaterializationRequest request,
            RuntimeMaterializationRequest runtimeRequest,
            LocalPlayerHostAuthoring host,
            PlayerInput playerInput,
            PlayerActorDeclaration declaration,
            GameObject logicalActorHost,
            string message,
            string source,
            string reason,
            RuntimeMaterializationResult runtimeResult = default,
            bool hasRuntimeResult = false)
        {
            bool physicalRollbackSucceeded = TryRollbackUncommitted(
                declaration,
                playerInput,
                logicalActorHost,
                out string physicalRollbackIssue);
            bool runtimeRollbackSucceeded = TryRollbackRegisteredRuntimeContent(
                runtimeRequest,
                source,
                reason,
                out string runtimeRollbackIssue);
            if (!physicalRollbackSucceeded || !runtimeRollbackSucceeded)
            {
                string rollbackIssue = string.Join(
                    " ",
                    new[] { physicalRollbackIssue, runtimeRollbackIssue })
                        .NormalizeText();
                return PlayerActorMaterializationResult.Failure(
                    PlayerActorMaterializationStatus.FailedRollback,
                    request,
                    runtimeRequest,
                    runtimeResult,
                    hasRuntimeResult,
                    host,
                    playerInput,
                    declaration,
                    logicalActorHost,
                    $"{message} Rollback failed. {rollbackIssue}",
                    originalStatus);
            }

            RuntimeMaterializationResult effectiveRuntimeResult = runtimeResult;
            bool effectiveHasRuntimeResult = hasRuntimeResult;
            if (!effectiveHasRuntimeResult && runtimeRequest.IsValid)
            {
                effectiveRuntimeResult = RuntimeMaterializationResult.Failure(
                    runtimeRequest,
                    RuntimeMaterializationStatus.FailedMaterializer,
                    source,
                    reason,
                    message);
                effectiveHasRuntimeResult = true;
            }

            return PlayerActorMaterializationResult.Failure(
                originalStatus,
                request,
                runtimeRequest,
                effectiveRuntimeResult,
                effectiveHasRuntimeResult,
                host,
                playerInput,
                null,
                null,
                message);
        }


        private bool TryRollbackRegisteredRuntimeContent(
            RuntimeMaterializationRequest runtimeRequest,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!runtimeRequest.IsValid)
            {
                return true;
            }

            if (!runtimeContentRuntime.TryGetHandle(
                    runtimeRequest.Context,
                    runtimeRequest.Identity,
                    out RuntimeContentHandle registeredHandle))
            {
                return true;
            }

            RuntimeReleaseResult releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                runtimeRequest.Context,
                runtimeRequest.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                source,
                reason);
            if (releaseResult.Succeeded)
            {
                return true;
            }

            issue = $"RuntimeContent rollback failed for '{registeredHandle.Identity.StableText}'. {releaseResult.Message}";
            return false;
        }

        private static bool TryRollbackUncommitted(
            PlayerActorDeclaration declaration,
            PlayerInput playerInput,
            GameObject logicalActorHost,
            out string issue)
        {
            try
            {
                if (logicalActorHost != null)
                {
                    logicalActorHost.SetActive(false);
                }

                if (declaration != null)
                {
                    declaration.ClearPlayerInputEvidence(playerInput);
                }

                if (logicalActorHost != null)
                {
                    DestroyObject(logicalActorHost);
                }

                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }
        }

        private static PlayerActorMaterializationResult MaterializerFailure(
            PlayerActorMaterializationStatus status,
            PlayerActorMaterializationRequest request,
            RuntimeMaterializationRequest runtimeRequest,
            LocalPlayerHostAuthoring host,
            PlayerInput playerInput,
            PlayerActorDeclaration declaration,
            GameObject logicalActorHost,
            string message)
        {
            RuntimeMaterializationResult runtimeResult = RuntimeMaterializationResult.Failure(
                runtimeRequest,
                RuntimeMaterializationStatus.FailedMaterializer,
                request.Source,
                request.Reason,
                message);
            return PlayerActorMaterializationResult.Failure(
                status,
                request,
                runtimeRequest,
                runtimeResult,
                true,
                host,
                playerInput,
                declaration,
                logicalActorHost,
                message);
        }

        private static PlayerActorMaterializationResult Failure(
            PlayerActorMaterializationStatus status,
            PlayerActorMaterializationRequest request,
            LocalPlayerHostAuthoring host,
            PlayerInput playerInput,
            string message)
        {
            return PlayerActorMaterializationResult.Failure(
                status,
                request,
                default,
                default,
                false,
                host,
                playerInput,
                null,
                null,
                message);
        }

        private static PlayerActorMaterializationStatus MapGuardStatus(
            RuntimeScopeTransitionGuardResult guardResult)
        {
            switch (guardResult.Status)
            {
                case RuntimeScopeTransitionGuardStatus.RejectedScopeCancelling:
                    return PlayerActorMaterializationStatus.RejectedScopeCancellation;
                case RuntimeScopeTransitionGuardStatus.RejectedStaleToken:
                    return PlayerActorMaterializationStatus.RejectedStaleScope;
                case RuntimeScopeTransitionGuardStatus.RejectedMissingScope:
                case RuntimeScopeTransitionGuardStatus.RejectedScopeRemoved:
                case RuntimeScopeTransitionGuardStatus.RejectedMismatchedOwner:
                    return PlayerActorMaterializationStatus.RejectedScopeTransition;
                default:
                    return PlayerActorMaterializationStatus.RejectedInvalidRequest;
            }
        }

        private static void DestroyObject(UnityEngine.Object value)
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
