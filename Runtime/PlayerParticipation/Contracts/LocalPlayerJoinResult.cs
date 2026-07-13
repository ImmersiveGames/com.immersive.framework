
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete technical evidence for one local Player provisioning/admission operation.
    /// The PlayerActorDeclaration evidence is carried as its concrete Unity Component so the
    /// provisioning contract does not duplicate or redefine Actor declaration authority.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G.2 local Player join result contract.")]
    public sealed class LocalPlayerJoinResult
    {
        internal LocalPlayerJoinResult(
            LocalPlayerJoinStatus status,
            LocalPlayerJoinOperationId operationId,
            LocalPlayerJoinRequest request,
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult commitResult,
            PlayerParticipationOperationResult rollbackResult,
            PlayerSlotRuntimeSnapshot slot,
            PlayerInput playerInput,
            Component playerActorDeclaration,
            int unityPlayerIndex,
            LocalPlayerJoinCallbackConfirmation callbackConfirmation,
            string message)
        {
            Status = status;
            OperationId = operationId;
            Request = request;
            ReservationResult = reservationResult;
            CommitResult = commitResult;
            RollbackResult = rollbackResult;
            Slot = slot;
            PlayerInput = playerInput;
            PlayerActorDeclaration = playerActorDeclaration;
            UnityPlayerIndex = unityPlayerIndex;
            CallbackConfirmation = callbackConfirmation;
            Message = message ?? string.Empty;
        }

        public LocalPlayerJoinStatus Status { get; }

        public LocalPlayerJoinOperationId OperationId { get; }

        public LocalPlayerJoinRequest Request { get; }

        public PlayerParticipationOperationResult ReservationResult { get; }

        public PlayerParticipationOperationResult CommitResult { get; }

        public PlayerParticipationOperationResult RollbackResult { get; }

        public PlayerSlotRuntimeSnapshot Slot { get; }

        public PlayerInput PlayerInput { get; }

        public Component PlayerActorDeclaration { get; }

        public int UnityPlayerIndex { get; }

        public LocalPlayerJoinCallbackConfirmation CallbackConfirmation { get; }

        public string Message { get; }

        public bool Succeeded => Status == LocalPlayerJoinStatus.SucceededJoined;

        public bool Failed => Status is
            LocalPlayerJoinStatus.FailedAdmission or
            LocalPlayerJoinStatus.FailedRollback;

        public bool Rejected => !Succeeded && !Failed && Status != LocalPlayerJoinStatus.None;

        public bool Completed => Status != LocalPlayerJoinStatus.None;

        public bool HasReservationEvidence => ReservationResult != null;

        public bool HasRollbackEvidence => RollbackResult != null;

        public string ToDiagnosticString()
        {
            return $"operation='{OperationId.StableText}' status='{Status}' " +
                $"slot='{(Slot.PlayerSlotId.IsValid ? Slot.PlayerSlotId.StableText : string.Empty)}' " +
                $"unityPlayerIndex='{UnityPlayerIndex}' callback='{CallbackConfirmation}' " +
                $"playerInput='{(PlayerInput != null ? PlayerInput.name : string.Empty)}' message='{Message}'";
        }
    }
}
