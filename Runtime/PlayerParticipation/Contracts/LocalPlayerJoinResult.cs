using Immersive.Framework.ApiStatus;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete technical evidence for one local Player provisioning/admission operation.
    /// PlayerInput and LocalPlayerHostAuthoring are stable host evidence. Logical Actor identity,
    /// ActorProfile selection and Actor materialization are intentionally absent.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3G/P3J local Player join result with stable Local Player Host evidence.")]
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
            LocalPlayerHostAuthoring localPlayerHost,
            int unityPlayerIndex,
            LocalPlayerJoinCallbackConfirmation callbackConfirmation,
            string message,
            LocalPlayerJoinStatus originalStatus = LocalPlayerJoinStatus.None)
        {
            Status = status;
            OriginalStatus = originalStatus == LocalPlayerJoinStatus.None
                ? status
                : originalStatus;
            OperationId = operationId;
            Request = request;
            ReservationResult = reservationResult;
            CommitResult = commitResult;
            RollbackResult = rollbackResult;
            Slot = slot;
            PlayerInput = playerInput;
            LocalPlayerHost = localPlayerHost;
            UnityPlayerIndex = unityPlayerIndex;
            CallbackConfirmation = callbackConfirmation;
            Message = message ?? string.Empty;
        }

        public LocalPlayerJoinStatus Status { get; }
        public LocalPlayerJoinStatus OriginalStatus { get; }
        public LocalPlayerJoinOperationId OperationId { get; }
        public LocalPlayerJoinRequest Request { get; }
        public PlayerParticipationOperationResult ReservationResult { get; }
        public PlayerParticipationOperationResult CommitResult { get; }
        public PlayerParticipationOperationResult RollbackResult { get; }
        public PlayerSlotRuntimeSnapshot Slot { get; }
        public PlayerInput PlayerInput { get; }
        public LocalPlayerHostAuthoring LocalPlayerHost { get; }
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
        public bool HasCommitEvidence => CommitResult != null;
        public bool HasRollbackEvidence => RollbackResult != null;
        public bool HasLocalPlayerHostEvidence => LocalPlayerHost != null;

        internal static LocalPlayerJoinResult RuntimeUnavailable(
            LocalPlayerJoinRequest request,
            string message)
        {
            return new LocalPlayerJoinResult(
                LocalPlayerJoinStatus.RejectedRuntimeUnavailable,
                default,
                request,
                null,
                null,
                null,
                default,
                null,
                null,
                -1,
                LocalPlayerJoinCallbackConfirmation.None,
                string.IsNullOrWhiteSpace(message)
                    ? "Local Player provisioning runtime is unavailable."
                    : message.Trim());
        }

        public string ToDiagnosticString()
        {
            return $"operation='{OperationId.StableText}' status='{Status}' originalStatus='{OriginalStatus}' " +
                $"slot='{(Slot.PlayerSlotId.IsValid ? Slot.PlayerSlotId.StableText : string.Empty)}' " +
                $"unityPlayerIndex='{UnityPlayerIndex}' callback='{CallbackConfirmation}' " +
                $"playerInput='{(PlayerInput != null ? PlayerInput.name : string.Empty)}' " +
                $"localPlayerHost='{(LocalPlayerHost != null ? LocalPlayerHost.name : string.Empty)}' " +
                $"actorMount='{(LocalPlayerHost != null && LocalPlayerHost.ActorMount != null ? LocalPlayerHost.ActorMount.name : string.Empty)}' " +
                $"logicalActorPrepared='{(LocalPlayerHost != null && LocalPlayerHost.HasLogicalActor)}' " +
                $"message='{Message}'";
        }
    }
}
