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
    /// Typed physical handle for one attached Logical Player Actor instance.
    /// The generic RuntimeContentHandle remains the ownership/registry evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.3 typed physical handle for attached Logical Player Actor materialization.")]
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
            PlayerActorDeclaration playerActorDeclaration,
            GameObject logicalActorHost,
            string source,
            string reason)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException(
                    "Player Actor materialization handle requires a valid typed request.",
                    nameof(request));
            }

            if (!runtimeContentRequest.IsValid ||
                runtimeContentRequest.Identity != request.RuntimeContentIdentity)
            {
                throw new ArgumentException(
                    "Player Actor materialization handle Runtime Content request must match the typed request identity.",
                    nameof(runtimeContentRequest));
            }

            if (runtimeContentHandle == null ||
                runtimeContentHandle.Identity != runtimeContentRequest.Identity ||
                !runtimeContentHandle.IsMaterialized)
            {
                throw new ArgumentException(
                    "Player Actor materialization handle requires matching materialized Runtime Content evidence.",
                    nameof(runtimeContentHandle));
            }

            Request = request;
            RuntimeContentRequest = runtimeContentRequest;
            RuntimeContentHandle = runtimeContentHandle;
            LocalPlayerHost = localPlayerHost != null
                ? localPlayerHost
                : throw new ArgumentNullException(nameof(localPlayerHost));
            PlayerInput = playerInput != null
                ? playerInput
                : throw new ArgumentNullException(nameof(playerInput));
            PlayerActorDeclaration = playerActorDeclaration != null
                ? playerActorDeclaration
                : throw new ArgumentNullException(nameof(playerActorDeclaration));
            LogicalActorHost = logicalActorHost != null
                ? logicalActorHost
                : throw new ArgumentNullException(nameof(logicalActorHost));
            _state = PlayerActorMaterializationState.StagedInactive;
            this._source = source.NormalizeText();
            this._reason = reason.NormalizeText();
            _message = "Logical Player Actor is staged inactive.";
        }

        internal PlayerActorMaterializationRequest Request { get; }
        internal RuntimeMaterializationRequest RuntimeContentRequest { get; }
        internal RuntimeContentHandle RuntimeContentHandle { get; }
        internal LocalPlayerHostAuthoring LocalPlayerHost { get; }
        internal PlayerInput PlayerInput { get; }
        internal PlayerActorDeclaration PlayerActorDeclaration { get; }
        internal GameObject LogicalActorHost { get; }
        internal PlayerActorMaterializationState State => _state;
        internal string Source => _source ?? string.Empty;
        internal string Reason => _reason ?? string.Empty;
        internal string Message => _message ?? string.Empty;

        internal bool TryActivate(string operationSource, string operationReason, out string issue)
        {
            if (_state == PlayerActorMaterializationState.Active)
            {
                issue = string.Empty;
                return true;
            }

            if (_state != PlayerActorMaterializationState.StagedInactive)
            {
                issue = $"Logical Player Actor cannot activate from state '{_state}'.";
                return false;
            }

            if (LogicalActorHost == null)
            {
                issue = "Logical Player Actor instance is missing before activation.";
                return false;
            }

            RoutePlayerSpatialEntryRuntimeBinding spatialEntryBinding =
                LocalPlayerHost != null
                    ? LocalPlayerHost.GetComponent<
                        RoutePlayerSpatialEntryRuntimeBinding>()
                    : null;

            bool declarationBelongsToLogicalActor =
                PlayerActorDeclaration != null &&
                (ReferenceEquals(
                     PlayerActorDeclaration.transform,
                     LogicalActorHost.transform) ||
                 PlayerActorDeclaration.transform
                     .IsChildOf(LogicalActorHost.transform));
            bool requiresRouteSpatialEntry =
                declarationBelongsToLogicalActor && !_hasEverActivated;
            if (requiresRouteSpatialEntry)
            {
                if (spatialEntryBinding != null)
                {
                    if (!spatialEntryBinding.TryApplyBeforeActivation(
                            this,
                            out issue))
                    {
                        return false;
                    }
                }
                else if (!_hasEverActivated)
                {
                    issue =
                        "Session-owned framework Logical Player Actor cannot perform its first activation without the current Route spatial-entry occurrence gate.";
                    return false;
                }
            }

            LogicalActorHost.SetActive(true);
            _hasEverActivated = true;
            _state = PlayerActorMaterializationState.Active;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Logical Player Actor activated.";
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

            if (_state != PlayerActorMaterializationState.Active)
            {
                issue = $"Logical Player Actor cannot deactivate from state '{_state}'.";
                return false;
            }

            if (LogicalActorHost == null)
            {
                issue = "Logical Player Actor instance is missing before deactivation.";
                return false;
            }

            LogicalActorHost.SetActive(false);
            _state = PlayerActorMaterializationState.StagedInactive;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Logical Player Actor deactivated.";
            issue = string.Empty;
            return true;
        }

        internal void MarkReleaseRequested(string operationSource, string operationReason)
        {
            _state = PlayerActorMaterializationState.ReleaseRequested;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Logical Player Actor physical release requested.";
        }

        internal void MarkReleased(string operationSource, string operationReason)
        {
            _state = PlayerActorMaterializationState.Released;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = "Logical Player Actor physical instance released.";
        }

        internal void MarkReleaseFailed(
            string operationSource,
            string operationReason,
            string failureMessage)
        {
            _state = PlayerActorMaterializationState.ReleaseFailed;
            _source = operationSource.NormalizeTextOrFallback(Source);
            _reason = operationReason.NormalizeTextOrFallback(Reason);
            _message = failureMessage.NormalizeTextOrFallback(
                "Logical Player Actor physical release failed.");
        }

        internal PlayerActorMaterializationSnapshot CreateSnapshot()
        {
            return new PlayerActorMaterializationSnapshot(
                Request.OperationId,
                RuntimeContentRequest.Identity,
                Request.Slot.PlayerSlotId,
                Request.ActorProfileId,
                Request.ActorId,
                Request.MaterializationRevision,
                _state,
                Source,
                Reason);
        }
    }
}
