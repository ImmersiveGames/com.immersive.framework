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
    /// Stages one Actor occurrence and any explicitly configured subordinate visual content.
    /// It does not choose Actor Profiles or own gameplay, input, camera or Session authority.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "ADR-038 attached Player Actor occurrence materialization adapter.")]
    internal sealed class AttachedPlayerActorMaterializationAdapter
    {
        private const string ResourceType = "PlayerActorRuntimeHost";

        private readonly RuntimeContentRuntime _runtimeContentRuntime;
        private readonly string _sessionContextId;
        private int _materializationSequence;

        internal AttachedPlayerActorMaterializationAdapter(RuntimeContentRuntime runtimeContentRuntime, string sessionContextId)
        {
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _sessionContextId = sessionContextId.NormalizeText();
            if (string.IsNullOrEmpty(_sessionContextId))
            {
                throw new ArgumentException("Attached Player Actor materialization adapter requires a non-empty Session context identity.", nameof(sessionContextId));
            }
        }

        internal string SessionContextId => _sessionContextId;

        internal PlayerActorMaterializationResult TryMaterialize(
            RuntimeScopeContext scopeContext,
            PlayerSlotRuntimeSnapshot slot,
            ActorProfile actorProfile,
            LocalPlayerHostAuthoring localPlayerHost,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(AttachedPlayerActorMaterializationAdapter));
            string resolvedReason = reason.NormalizeTextOrFallback("player-actor-runtime-materialization");

            if (!scopeContext.IsValid || !slot.IsValid || !slot.IsJoined || localPlayerHost == null)
            {
                return Failure(PlayerActorMaterializationStatus.RejectedInvalidRequest, default, localPlayerHost, localPlayerHost != null ? localPlayerHost.PlayerInput : null, "Player Actor materialization requires a valid scope, Joined Slot and explicit Local Player Host.");
            }

            if (!TryValidateHost(localPlayerHost, slot, out PlayerActorMaterializationStatus hostStatus, out string hostIssue))
            {
                return Failure(hostStatus, default, localPlayerHost, localPlayerHost.PlayerInput, hostIssue);
            }

            if (!TryValidateProfile(actorProfile, out ActorProfileId actorProfileId, out PlayerActorMaterializationStatus profileStatus, out string profileIssue))
            {
                return Failure(profileStatus, default, localPlayerHost, localPlayerHost.PlayerInput, profileIssue);
            }

            _materializationSequence++;
            int materializationRevision = _materializationSequence;
            if (!PlayerActorMaterializationOperationId.TryCreate(
                    _sessionContextId,
                    scopeContext.Owner,
                    slot.PlayerSlotId,
                    materializationRevision,
                    out PlayerActorMaterializationOperationId operationId,
                    out string operationIssue))
            {
                return Failure(PlayerActorMaterializationStatus.FailedActorIdentity, default, localPlayerHost, localPlayerHost.PlayerInput, operationIssue);
            }

            if (!TryCreateRuntimeIdentities(
                    scopeContext,
                    slot,
                    materializationRevision,
                    out ActorId actorId,
                    out RuntimeContentId runtimeContentId,
                    out string identityIssue))
            {
                return Failure(PlayerActorMaterializationStatus.FailedActorIdentity, default, localPlayerHost, localPlayerHost.PlayerInput, identityIssue);
            }

            var request = new PlayerActorMaterializationRequest(operationId, _sessionContextId, scopeContext, slot, actorProfile, localPlayerHost, actorId, runtimeContentId, materializationRevision, resolvedSource, resolvedReason);
            var resource = new RuntimeMaterializationResource(ResourceType, actorProfileId.Value.Value, actorProfile.DisplayName, string.Empty);
            if (!_runtimeContentRuntime.TryCreateMaterializationRequest(scopeContext, runtimeContentId, resource, resolvedSource, resolvedReason, out RuntimeMaterializationRequest runtimeRequest, out RuntimeScopeTransitionGuardResult guardResult))
            {
                return PlayerActorMaterializationResult.Failure(MapGuardStatus(guardResult), request, default, default, false, localPlayerHost, localPlayerHost.PlayerInput, null, null, guardResult.Message);
            }

            GameObject stagingRoot = null;
            PlayerActorRuntimeHost runtimeHost = null;
            GameObject visualContent = null;
            try
            {
                stagingRoot = new GameObject($"[{operationId.StableText}] Player Actor Staging");
                stagingRoot.SetActive(false);
                stagingRoot.transform.SetParent(localPlayerHost.ActorMount, false);

                runtimeHost = UnityEngine.Object.Instantiate(localPlayerHost.PlayerActorRuntimeHostPrefab, stagingRoot.transform, false);
                if (runtimeHost == null)
                {
                    return MaterializerFailure(PlayerActorMaterializationStatus.FailedInstantiate, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, null, null, "Player Actor Runtime Host prefab instantiation returned null.");
                }

                runtimeHost.gameObject.SetActive(false);
                if (!runtimeHost.TryValidateConfiguration(out string runtimeHostIssue))
                {
                    return RollbackFailure(PlayerActorMaterializationStatus.RejectedInvalidRuntimeHostPrefab, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, null, runtimeHostIssue, resolvedSource, resolvedReason);
                }

                if (actorProfile.VisualContentPrefab != null)
                {
                    if (runtimeHost.VisualContentMount == null)
                    {
                        return RollbackFailure(PlayerActorMaterializationStatus.RejectedInvalidRuntimeHostPrefab, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, null, "Actor Profile has configured optional visual content, but the Actor Runtime Host has no explicit Visual Content Mount.", resolvedSource, resolvedReason);
                    }

                    visualContent = UnityEngine.Object.Instantiate(actorProfile.VisualContentPrefab, runtimeHost.VisualContentMount, false);
                    if (visualContent == null)
                    {
                        return RollbackFailure(PlayerActorMaterializationStatus.FailedInstantiate, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, null, "Configured Actor visual-content prefab instantiation returned null.", resolvedSource, resolvedReason);
                    }

                    visualContent.SetActive(false);
                    if (!TryValidateVisualContent(visualContent, runtimeHost.VisualContentMount, out PlayerActorMaterializationStatus visualContentStatus, out string visualContentIssue))
                    {
                        return RollbackFailure(visualContentStatus, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, visualContent, visualContentIssue, resolvedSource, resolvedReason);
                    }

                    visualContent.name = actorProfile.DisplayName;
                }

                runtimeHost.name = $"Player {slot.ConfiguredIndex + 1} [{slot.PlayerSlotId.StableText}] Runtime Host";
                runtimeHost.transform.SetParent(localPlayerHost.ActorMount, false);
                DestroyObject(stagingRoot);
                stagingRoot = null;

                PlayerActorDeclaration declaration = runtimeHost.PlayerActorDeclaration;
                declaration.EstablishRuntimeOccurrenceIdentity(actorId, actorProfile.DisplayName, localPlayerHost.PlayerInput, $"{resolvedReason}; profile='{actorProfileId.StableText}'; slot='{slot.PlayerSlotId.StableText}'; owner='{scopeContext.Owner.StableText}'.");
                if (!declaration.HasPlayerInputEvidence || !ReferenceEquals(declaration.PlayerInput, localPlayerHost.PlayerInput) || declaration.ActorId != actorId)
                {
                    return RollbackFailure(PlayerActorMaterializationStatus.FailedActorIdentity, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, visualContent, "Player Actor Runtime Host identity or PlayerInput evidence did not match the generated materialization request.", resolvedSource, resolvedReason);
                }

                RuntimeContentHandle runtimeHandle = RuntimeContentHandle.Materialized(runtimeRequest.Identity, resolvedSource, resolvedReason);
                RuntimeMaterializationResult appliedResult = _runtimeContentRuntime.ApplyMaterializationResult(RuntimeMaterializationResult.Success(runtimeRequest, runtimeHandle, resolvedSource, resolvedReason, "Player Actor occurrence and configured visual content staged inactive."), resolvedSource, resolvedReason);
                if (!appliedResult.Succeeded)
                {
                    return RollbackFailure(PlayerActorMaterializationStatus.FailedRuntimeContentRegistration, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, visualContent, appliedResult.Message, resolvedSource, resolvedReason, appliedResult, true);
                }

                var handle = new PlayerActorMaterializationHandle(request, runtimeRequest, runtimeHandle, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, visualContent, null, false, resolvedSource, resolvedReason);
                return PlayerActorMaterializationResult.Success(request, runtimeRequest, appliedResult, handle, "Player Actor occurrence materialized under the explicit Actor Mount and staged inactive.");
            }
            catch (Exception exception)
            {
                return RollbackFailure(PlayerActorMaterializationStatus.FailedInstantiate, request, runtimeRequest, localPlayerHost, localPlayerHost.PlayerInput, runtimeHost, visualContent, $"Player Actor materialization threw '{exception.GetType().Name}'. {exception.Message}", resolvedSource, resolvedReason);
            }
            finally
            {
                if (stagingRoot != null) DestroyObject(stagingRoot);
            }
        }

        internal bool TryRollbackMaterialization(PlayerActorMaterializationHandle handle, string source, string reason, out string issue) => TryReleaseMaterialization(handle, source, reason, out issue);

        internal bool TryReleaseMaterialization(PlayerActorMaterializationHandle handle, string source, string reason, out string issue)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(AttachedPlayerActorMaterializationAdapter));
            string resolvedReason = reason.NormalizeTextOrFallback("player-actor-runtime-release");
            if (handle == null || !string.Equals(handle.Request.SessionContextId, _sessionContextId, StringComparison.Ordinal))
            {
                issue = "Player Actor release requires a typed materialization handle from this Session.";
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
                if (handle.PlayerActorRuntimeHost != null) handle.PlayerActorRuntimeHost.gameObject.SetActive(false);
                handle.PlayerActorDeclaration?.ClearPlayerInputEvidence(handle.PlayerInput);

                if (handle.DestroyLocalPlayerHostOnRelease)
                {
                    if (handle.ReleaseProxy != null) DestroyObject(handle.ReleaseProxy);
                    if (handle.LocalPlayerHost != null) DestroyObject(handle.LocalPlayerHost.gameObject);
                }
                else
                {
                    if (handle.VisualContent != null)
                    {
                        handle.VisualContent.transform.SetParent(null, true);
                        DestroyObject(handle.VisualContent);
                    }
                    if (handle.PlayerActorRuntimeHost != null)
                    {
                        handle.PlayerActorRuntimeHost.transform.SetParent(null, true);
                        DestroyObject(handle.PlayerActorRuntimeHost.gameObject);
                    }
                }

                RuntimeReleaseResult releaseResult = _runtimeContentRuntime.ReleaseHandleLogically(handle.RuntimeContentRequest.Context, handle.RuntimeContentRequest.Identity, RuntimeReleasePolicy.MarkReleasedAndUnregister, resolvedSource, resolvedReason);
                if (!releaseResult.Succeeded)
                {
                    handle.MarkReleaseFailed(resolvedSource, resolvedReason, releaseResult.Message);
                    issue = releaseResult.Message;
                    return false;
                }
                handle.MarkReleased(resolvedSource, resolvedReason);
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                handle.MarkReleaseFailed(resolvedSource, resolvedReason, exception.Message);
                issue = $"Player Actor Runtime Host release failed. {exception.Message}";
                return false;
            }
        }

        private static bool TryValidateHost(LocalPlayerHostAuthoring host, PlayerSlotRuntimeSnapshot slot, out PlayerActorMaterializationStatus status, out string issue)
        {
            if (!host.IsJoined || !host.HasJoinedSlot)
            {
                status = PlayerActorMaterializationStatus.RejectedHostNotJoined;
                issue = "Player Actor materialization requires a joined Local Player Host.";
                return false;
            }
            if (host.JoinedPlayerSlotId != slot.PlayerSlotId || host.JoinedConfiguredIndex != slot.ConfiguredIndex)
            {
                status = PlayerActorMaterializationStatus.RejectedSlotMismatch;
                issue = "Local Player Host joined Slot identity does not match the requested Session Slot snapshot.";
                return false;
            }
            if (!host.HasPlayerActorRuntimeHostPrefab)
            {
                status = PlayerActorMaterializationStatus.RejectedMissingRuntimeHostPrefab;
                issue = "Local Player Host requires an explicit Player Actor Runtime Host prefab. No Actor Profile fallback is available.";
                return false;
            }
            if (host.PlayerInput == null || host.ActorMount == null)
            {
                status = PlayerActorMaterializationStatus.RejectedHostUnavailable;
                issue = "Local Player Host requires explicit PlayerInput and Actor Mount evidence.";
                return false;
            }
            if (!host.PlayerActorRuntimeHostPrefab.TryValidateConfiguration(out issue))
            {
                status = PlayerActorMaterializationStatus.RejectedInvalidRuntimeHostPrefab;
                return false;
            }
            PlayerInput[] playerInputs = host.GetComponentsInChildren<PlayerInput>(true);
            if (playerInputs.Length != 1 || !ReferenceEquals(playerInputs[0], host.PlayerInput) || host.ActorMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                status = PlayerActorMaterializationStatus.RejectedHostUnavailable;
                issue = "Local Player Host must retain exactly one PlayerInput outside Actor Mount.";
                return false;
            }
            status = PlayerActorMaterializationStatus.SucceededStaged;
            issue = string.Empty;
            return true;
        }

        internal static bool TryValidateProfile(ActorProfile actorProfile, out ActorProfileId actorProfileId, out PlayerActorMaterializationStatus status, out string issue)
        {
            actorProfileId = default;
            if (actorProfile == null)
            {
                status = PlayerActorMaterializationStatus.RejectedProfileUnavailable;
                issue = "Player Actor materialization requires an explicit Actor Profile.";
                return false;
            }
            if (!actorProfile.TryGetActorProfileId(out actorProfileId, out issue) || actorProfile.ActorKind != ActorKind.Player || actorProfile.ActorRole != ActorRole.Protagonist)
            {
                status = PlayerActorMaterializationStatus.RejectedInvalidProfile;
                return false;
            }
            if (actorProfile.VisualContentPrefab != null)
            {
                return TryValidateVisualContent(actorProfile.VisualContentPrefab, null, out status, out issue);
            }

            status = PlayerActorMaterializationStatus.SucceededStaged;
            issue = string.Empty;
            return true;
        }

        private static bool TryValidateVisualContent(GameObject visualContent, Transform expectedVisualContentMount, out PlayerActorMaterializationStatus status, out string issue)
        {
            if (visualContent == null)
            {
                status = PlayerActorMaterializationStatus.RejectedInvalidVisualContentPrefab;
                issue = "Configured Actor visual content is missing.";
                return false;
            }
            if (expectedVisualContentMount != null && visualContent.transform.parent != expectedVisualContentMount)
            {
                status = PlayerActorMaterializationStatus.FailedInstantiate;
                issue = "Configured Actor visual content must be materialized directly under the exact Actor Runtime Host Visual Content Mount.";
                return false;
            }
            if (visualContent.GetComponentInChildren<PlayerInput>(true) != null || visualContent.GetComponentInChildren<ActorDeclaration>(true) != null || visualContent.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null)
            {
                status = PlayerActorMaterializationStatus.FailedUnexpectedActorDeclaration;
                issue = "Optional Actor visual content must not contain PlayerInput, Framework Actor declarations or Player Actor Runtime Host infrastructure.";
                return false;
            }
            status = PlayerActorMaterializationStatus.SucceededStaged;
            issue = string.Empty;
            return true;
        }

        private bool TryCreateRuntimeIdentities(RuntimeScopeContext scopeContext, PlayerSlotRuntimeSnapshot slot, int sequence, out ActorId actorId, out RuntimeContentId runtimeContentId, out string issue)
        {
            try
            {
                string suffix = $"{_sessionContextId}:{scopeContext.Owner.Scope}:{scopeContext.Owner.OwnerIdentity.Value.Value}:{slot.PlayerSlotId.Value.Value}:{sequence}";
                actorId = ActorId.From($"player-actor:{suffix}");
                runtimeContentId = RuntimeContentId.From($"player-actor-content:{suffix}");
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                actorId = default;
                runtimeContentId = default;
                issue = $"Framework-generated Player Actor identity failed. {exception.Message}";
                return false;
            }
        }

        private PlayerActorMaterializationResult RollbackFailure(PlayerActorMaterializationStatus status, PlayerActorMaterializationRequest request, RuntimeMaterializationRequest runtimeRequest, LocalPlayerHostAuthoring host, PlayerInput playerInput, PlayerActorRuntimeHost runtimeHost, GameObject visualContent, string message, string source, string reason, RuntimeMaterializationResult runtimeResult = default, bool hasRuntimeResult = false)
        {
            if (runtimeHost != null)
            {
                runtimeHost.PlayerActorDeclaration?.ClearPlayerInputEvidence(playerInput);
                DestroyObject(runtimeHost.gameObject);
            }
            if (runtimeRequest.IsValid && _runtimeContentRuntime.TryGetHandle(runtimeRequest.Context, runtimeRequest.Identity, out _))
            {
                _runtimeContentRuntime.ReleaseHandleLogically(runtimeRequest.Context, runtimeRequest.Identity, RuntimeReleasePolicy.MarkReleasedAndUnregister, source, reason);
            }
            return PlayerActorMaterializationResult.Failure(status, request, runtimeRequest, runtimeResult, hasRuntimeResult, host, playerInput, runtimeHost, visualContent, message);
        }

        private static PlayerActorMaterializationResult MaterializerFailure(PlayerActorMaterializationStatus status, PlayerActorMaterializationRequest request, RuntimeMaterializationRequest runtimeRequest, LocalPlayerHostAuthoring host, PlayerInput playerInput, PlayerActorRuntimeHost runtimeHost, GameObject visualContent, string message)
        {
            return PlayerActorMaterializationResult.Failure(status, request, runtimeRequest, RuntimeMaterializationResult.Failure(runtimeRequest, RuntimeMaterializationStatus.FailedMaterializer, request.Source, request.Reason, message), true, host, playerInput, runtimeHost, visualContent, message);
        }

        private static PlayerActorMaterializationResult Failure(PlayerActorMaterializationStatus status, PlayerActorMaterializationRequest request, LocalPlayerHostAuthoring host, PlayerInput playerInput, string message)
        {
            return PlayerActorMaterializationResult.Failure(status, request, default, default, false, host, playerInput, null, null, message);
        }

        private static PlayerActorMaterializationStatus MapGuardStatus(RuntimeScopeTransitionGuardResult guardResult)
        {
            return guardResult.Status switch
            {
                RuntimeScopeTransitionGuardStatus.RejectedScopeCancelling => PlayerActorMaterializationStatus.RejectedScopeCancellation,
                RuntimeScopeTransitionGuardStatus.RejectedStaleToken => PlayerActorMaterializationStatus.RejectedStaleScope,
                RuntimeScopeTransitionGuardStatus.RejectedMissingScope or RuntimeScopeTransitionGuardStatus.RejectedScopeRemoved or RuntimeScopeTransitionGuardStatus.RejectedMismatchedOwner => PlayerActorMaterializationStatus.RejectedScopeTransition,
                _ => PlayerActorMaterializationStatus.RejectedInvalidRequest
            };
        }

        private static void DestroyObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
