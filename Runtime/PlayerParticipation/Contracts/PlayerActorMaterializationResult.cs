using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete technical evidence for one attached Logical Player Actor materialization attempt.
    /// Session preparation and selection mutation remain outside this result.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.3 attached Logical Player Actor materialization result.")]
    public sealed class PlayerActorMaterializationResult
    {
        internal PlayerActorMaterializationResult(
            PlayerActorMaterializationStatus status,
            PlayerActorMaterializationStatus originalStatus,
            PlayerActorMaterializationRequest request,
            RuntimeMaterializationRequest runtimeContentRequest,
            RuntimeMaterializationResult runtimeContentResult,
            bool hasRuntimeContentResult,
            PlayerActorMaterializationSnapshot snapshot,
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerInput playerInput,
            PlayerActorDeclaration playerActorDeclaration,
            GameObject logicalActorHost,
            PlayerActorMaterializationHandle handle,
            string message)
        {
            Status = status;
            OriginalStatus = originalStatus == PlayerActorMaterializationStatus.None
                ? status
                : originalStatus;
            Request = request;
            RuntimeContentRequest = runtimeContentRequest;
            RuntimeContentResult = runtimeContentResult;
            HasRuntimeContentResult = hasRuntimeContentResult;
            Snapshot = snapshot;
            LocalPlayerHost = localPlayerHost;
            PlayerInput = playerInput;
            PlayerActorDeclaration = playerActorDeclaration;
            LogicalActorHost = logicalActorHost;
            Handle = handle;
            Message = message ?? string.Empty;
        }

        public PlayerActorMaterializationStatus Status { get; }
        public PlayerActorMaterializationStatus OriginalStatus { get; }
        public PlayerActorMaterializationRequest Request { get; }
        public RuntimeMaterializationRequest RuntimeContentRequest { get; }
        public RuntimeMaterializationResult RuntimeContentResult { get; }
        public bool HasRuntimeContentResult { get; }
        public PlayerActorMaterializationSnapshot Snapshot { get; }
        public LocalPlayerHostAuthoring LocalPlayerHost { get; }
        public PlayerInput PlayerInput { get; }
        public PlayerActorDeclaration PlayerActorDeclaration { get; }
        public GameObject LogicalActorHost { get; }
        public string Message { get; }

        internal PlayerActorMaterializationHandle Handle { get; }

        public bool Succeeded => Status == PlayerActorMaterializationStatus.SucceededStaged;
        public bool Failed => Status is
            PlayerActorMaterializationStatus.FailedInstantiate or
            PlayerActorMaterializationStatus.FailedMissingPlayerActorDeclaration or
            PlayerActorMaterializationStatus.FailedMultiplePlayerActorDeclarations or
            PlayerActorMaterializationStatus.FailedUnexpectedActorDeclaration or
            PlayerActorMaterializationStatus.FailedUnexpectedPlayerInput or
            PlayerActorMaterializationStatus.FailedActorIdentity or
            PlayerActorMaterializationStatus.FailedRuntimeContentRegistration or
            PlayerActorMaterializationStatus.FailedRollback;
        public bool Rejected => !Succeeded && !Failed && Status != PlayerActorMaterializationStatus.None;
        public bool Completed => Status != PlayerActorMaterializationStatus.None;
        public bool HasRuntimeContentRequest => RuntimeContentRequest.IsValid;
        public bool HasSnapshot => Snapshot.IsValid;
        public bool HasPhysicalEvidence =>
            LocalPlayerHost != null &&
            PlayerInput != null &&
            PlayerActorDeclaration != null &&
            LogicalActorHost != null;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' originalStatus='{OriginalStatus}' " +
                $"request=({Request.ToDiagnosticString()}) " +
                $"runtimeStatus='{(HasRuntimeContentResult ? RuntimeContentResult.Status.ToString() : string.Empty)}' " +
                $"host='{(LocalPlayerHost != null ? LocalPlayerHost.name : string.Empty)}' " +
                $"logicalActor='{(LogicalActorHost != null ? LogicalActorHost.name : string.Empty)}' " +
                $"declaration='{(PlayerActorDeclaration != null ? PlayerActorDeclaration.name : string.Empty)}' " +
                $"stagedInactive='{(LogicalActorHost != null && !LogicalActorHost.activeSelf)}' " +
                $"message='{Message}'";
        }

        internal static PlayerActorMaterializationResult Failure(
            PlayerActorMaterializationStatus status,
            PlayerActorMaterializationRequest request,
            RuntimeMaterializationRequest runtimeContentRequest,
            RuntimeMaterializationResult runtimeContentResult,
            bool hasRuntimeContentResult,
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerInput playerInput,
            PlayerActorDeclaration playerActorDeclaration,
            GameObject logicalActorHost,
            string message,
            PlayerActorMaterializationStatus originalStatus = PlayerActorMaterializationStatus.None)
        {
            return new PlayerActorMaterializationResult(
                status,
                originalStatus,
                request,
                runtimeContentRequest,
                runtimeContentResult,
                hasRuntimeContentResult,
                default,
                localPlayerHost,
                playerInput,
                playerActorDeclaration,
                logicalActorHost,
                null,
                message);
        }

        internal static PlayerActorMaterializationResult Success(
            PlayerActorMaterializationRequest request,
            RuntimeMaterializationRequest runtimeContentRequest,
            RuntimeMaterializationResult runtimeContentResult,
            PlayerActorMaterializationHandle handle,
            string message)
        {
            return new PlayerActorMaterializationResult(
                PlayerActorMaterializationStatus.SucceededStaged,
                PlayerActorMaterializationStatus.SucceededStaged,
                request,
                runtimeContentRequest,
                runtimeContentResult,
                true,
                handle.CreateSnapshot(),
                handle.LocalPlayerHost,
                handle.PlayerInput,
                handle.PlayerActorDeclaration,
                handle.LogicalActorHost,
                handle,
                message);
        }
    }
}
