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
        private PlayerActorMaterializationState state;
        private string source;
        private string reason;
        private string message;

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
            state = PlayerActorMaterializationState.StagedInactive;
            this.source = source.NormalizeText();
            this.reason = reason.NormalizeText();
            message = "Logical Player Actor is staged inactive.";
        }

        internal PlayerActorMaterializationRequest Request { get; }
        internal RuntimeMaterializationRequest RuntimeContentRequest { get; }
        internal RuntimeContentHandle RuntimeContentHandle { get; }
        internal LocalPlayerHostAuthoring LocalPlayerHost { get; }
        internal PlayerInput PlayerInput { get; }
        internal PlayerActorDeclaration PlayerActorDeclaration { get; }
        internal GameObject LogicalActorHost { get; }
        internal PlayerActorMaterializationState State => state;
        internal string Source => source ?? string.Empty;
        internal string Reason => reason ?? string.Empty;
        internal string Message => message ?? string.Empty;

        internal bool TryActivate(string operationSource, string operationReason, out string issue)
        {
            if (state == PlayerActorMaterializationState.Active)
            {
                issue = string.Empty;
                return true;
            }

            if (state != PlayerActorMaterializationState.StagedInactive)
            {
                issue = $"Logical Player Actor cannot activate from state '{state}'.";
                return false;
            }

            if (LogicalActorHost == null)
            {
                issue = "Logical Player Actor instance is missing before activation.";
                return false;
            }

            LogicalActorHost.SetActive(true);
            state = PlayerActorMaterializationState.Active;
            source = operationSource.NormalizeTextOrFallback(Source);
            reason = operationReason.NormalizeTextOrFallback(Reason);
            message = "Logical Player Actor activated.";
            issue = string.Empty;
            return true;
        }

        internal bool TryDeactivate(string operationSource, string operationReason, out string issue)
        {
            if (state == PlayerActorMaterializationState.StagedInactive)
            {
                issue = string.Empty;
                return true;
            }

            if (state != PlayerActorMaterializationState.Active)
            {
                issue = $"Logical Player Actor cannot deactivate from state '{state}'.";
                return false;
            }

            if (LogicalActorHost == null)
            {
                issue = "Logical Player Actor instance is missing before deactivation.";
                return false;
            }

            LogicalActorHost.SetActive(false);
            state = PlayerActorMaterializationState.StagedInactive;
            source = operationSource.NormalizeTextOrFallback(Source);
            reason = operationReason.NormalizeTextOrFallback(Reason);
            message = "Logical Player Actor deactivated.";
            issue = string.Empty;
            return true;
        }

        internal void MarkReleaseRequested(string operationSource, string operationReason)
        {
            state = PlayerActorMaterializationState.ReleaseRequested;
            source = operationSource.NormalizeTextOrFallback(Source);
            reason = operationReason.NormalizeTextOrFallback(Reason);
            message = "Logical Player Actor physical release requested.";
        }

        internal void MarkReleased(string operationSource, string operationReason)
        {
            state = PlayerActorMaterializationState.Released;
            source = operationSource.NormalizeTextOrFallback(Source);
            reason = operationReason.NormalizeTextOrFallback(Reason);
            message = "Logical Player Actor physical instance released.";
        }

        internal void MarkReleaseFailed(
            string operationSource,
            string operationReason,
            string failureMessage)
        {
            state = PlayerActorMaterializationState.ReleaseFailed;
            source = operationSource.NormalizeTextOrFallback(Source);
            reason = operationReason.NormalizeTextOrFallback(Reason);
            message = failureMessage.NormalizeTextOrFallback(
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
                state,
                Source,
                Reason);
        }
    }
}
