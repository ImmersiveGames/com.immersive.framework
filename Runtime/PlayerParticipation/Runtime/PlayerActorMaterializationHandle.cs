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
    /// Typed physical handle for one Player Actor Runtime Host and its selected Presentation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "ADR-023 Player Actor Runtime Host and Presentation materialization handle.")]
    internal sealed class PlayerActorMaterializationHandle
    {
        private PlayerActorMaterializationState _state;
        private bool _hasEverActivated;
        private string _source;
        private string _reason;
        private string _message;

        internal PlayerActorMaterializationHandle(
            PlayerActorMaterializationRequest request,
            RuntimeMaterializationRequest runtimeContentRequest,
            RuntimeContentHandle runtimeContentHandle,
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerInput playerInput,
            PlayerActorRuntimeHost playerActorRuntimeHost,
            GameObject presentation,
            GameObject releaseProxy,
            bool destroyLocalPlayerHostOnRelease,
            string source,
            string reason)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Player Actor materialization handle requires a valid typed request.", nameof(request));
            }

            if (!runtimeContentRequest.IsValid || runtimeContentRequest.Identity != request.RuntimeContentIdentity)
            {
                throw new ArgumentException("Player Actor materialization handle Runtime Content request must match the typed request identity.", nameof(runtimeContentRequest));
            }

            if (runtimeContentHandle == null || runtimeContentHandle.Identity != runtimeContentRequest.Identity || !runtimeContentHandle.IsMaterialized)
            {
                throw new ArgumentException("Player Actor materialization handle requires matching materialized Runtime Content evidence.", nameof(runtimeContentHandle));
            }

            Request = request;
            RuntimeContentRequest = runtimeContentRequest;
            RuntimeContentHandle = runtimeContentHandle;
            LocalPlayerHost = localPlayerHost ?? throw new ArgumentNullException(nameof(localPlayerHost));
            PlayerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
            PlayerActorRuntimeHost = playerActorRuntimeHost ?? throw new ArgumentNullException(nameof(playerActorRuntimeHost));
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            ReleaseProxy = releaseProxy;
            DestroyLocalPlayerHostOnRelease = destroyLocalPlayerHostOnRelease;
            _state = PlayerActorMaterializationState.StagedInactive;
            _source = source.NormalizeText();
            _reason = reason.NormalizeText();
            _message = "Player Actor Runtime Host and Presentation are staged inactive.";
        }

        internal PlayerActorMaterializationRequest Request { get; }
        internal RuntimeMaterializationRequest RuntimeContentRequest { get; }
        internal RuntimeContentHandle RuntimeContentHandle { get; }
        internal LocalPlayerHostAuthoring LocalPlayerHost { get; }
        internal PlayerInput PlayerInput { get; }
        internal PlayerActorRuntimeHost PlayerActorRuntimeHost { get; }
        internal PlayerActorDeclaration PlayerActorDeclaration => PlayerActorRuntimeHost != null ? PlayerActorRuntimeHost.PlayerActorDeclaration : null;
        internal GameObject Presentation { get; }
        internal GameObject ReleaseProxy { get; }
        internal bool DestroyLocalPlayerHostOnRelease { get; }
        internal bool RequiresRouteSpatialEntryOnFirstActivation => !DestroyLocalPlayerHostOnRelease;
        internal PlayerActorMaterializationState State => _state;
        internal string Source => _source ?? string.Empty;
        internal string Reason => _reason ?? string.Empty;
        internal string Message => _message ?? string.Empty;

        internal bool TryActivate(string operationSource, string operationReason, out string issue)
        {
            issue = string.Empty;
            if (_state == PlayerActorMaterializationState.Active)
            {
                issue = string.Empty;
                return true;
            }

            if (_state != PlayerActorMaterializationState.StagedInactive)
            {
                issue = $"Player Actor Runtime Host cannot activate from state '{_state}'.";
                return false;
            }

            if (PlayerActorRuntimeHost == null || PlayerActorDeclaration == null || Presentation == null)
            {
                issue = "Player Actor Runtime Host or selected Presentation is missing before activation.";
                return false;
            }

            RoutePlayerSpatialEntryRuntimeBinding spatialEntryBinding = LocalPlayerHost != null
                ? LocalPlayerHost.GetComponent<RoutePlayerSpatialEntryRuntimeBinding>()
                : null;
            if (RequiresRouteSpatialEntryOnFirstActivation && !_hasEverActivated &&
                (spatialEntryBinding == null || !spatialEntryBinding.TryApplyBeforeActivation(this, out issue)))
            {
                issue = string.IsNullOrEmpty(issue)
                    ? "Session-owned Player Actor Runtime Host cannot perform its first activation without the current Route spatial-entry occurrence gate."
                    : issue;
                return false;
            }

            PlayerActorRuntimeHost.gameObject.SetActive(true);
            _hasEverActivated = true;
            _state = PlayerActorMaterializationState.Active;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Player Actor Runtime Host and Presentation activated.";
            issue = string.Empty;
            return true;
        }

        internal bool TryDeactivate(string operationSource, string operationReason, out string issue)
        {
            if (_state == PlayerActorMaterializationState.StagedInactive)
            {
                issue = string.Empty;
                return true;
            }

            if (_state != PlayerActorMaterializationState.Active || PlayerActorRuntimeHost == null)
            {
                issue = "Player Actor Runtime Host cannot deactivate from its current state.";
                return false;
            }

            PlayerActorRuntimeHost.gameObject.SetActive(false);
            _state = PlayerActorMaterializationState.StagedInactive;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Player Actor Runtime Host and Presentation deactivated.";
            issue = string.Empty;
            return true;
        }

        internal void MarkReleaseRequested(string operationSource, string operationReason)
        {
            _state = PlayerActorMaterializationState.ReleaseRequested;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Player Actor Runtime Host physical release requested.";
        }

        internal void MarkReleased(string operationSource, string operationReason)
        {
            _state = PlayerActorMaterializationState.Released;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Player Actor Runtime Host physical instances released.";
        }

        internal void MarkReleaseFailed(string operationSource, string operationReason, string failureMessage)
        {
            _state = PlayerActorMaterializationState.ReleaseFailed;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = failureMessage.NormalizeTextOrFallback("Player Actor Runtime Host physical release failed.");
        }

        internal PlayerActorMaterializationSnapshot CreateSnapshot()
        {
            return new PlayerActorMaterializationSnapshot(Request.OperationId, RuntimeContentRequest.Identity, Request.Slot.PlayerSlotId, Request.ActorProfileId, Request.ActorId, Request.MaterializationRevision, _state, Source, Reason);
        }
    }
}
