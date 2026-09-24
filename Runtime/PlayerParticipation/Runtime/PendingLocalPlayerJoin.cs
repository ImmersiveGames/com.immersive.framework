using Immersive.Framework.ApiStatus;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-local correlation state created after Slot reservation and before JoinPlayer.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G.3 synchronous Pending Local Player Join correlation state.")]
    internal sealed class PendingLocalPlayerJoin
    {
        internal PendingLocalPlayerJoin(
            LocalPlayerJoinOperationId operationId,
            LocalPlayerJoinRequest request,
            PlayerParticipationOperationResult reservationResult)
        {
            OperationId = operationId;
            Request = request;
            ReservationResult = reservationResult;
            CallbackConfirmation = LocalPlayerJoinCallbackConfirmation.Pending;
        }

        internal LocalPlayerJoinOperationId OperationId { get; }

        internal LocalPlayerJoinRequest Request { get; }

        internal PlayerParticipationOperationResult ReservationResult { get; }

        internal PlayerSlotReservationToken ReservationToken =>
            ReservationResult != null ? ReservationResult.ReservationToken : default;

        internal PlayerInput DirectPlayerInput { get; private set; }

        internal PlayerInput CallbackPlayerInput { get; private set; }

        internal LocalPlayerJoinCallbackConfirmation CallbackConfirmation { get; private set; }

        internal bool HasCallbackEvidence =>
            !ReferenceEquals(CallbackPlayerInput, null);

        internal void RecordDirectResult(PlayerInput playerInput)
        {
            DirectPlayerInput = playerInput;
            ResolveCorrelationWhenPossible();
        }

        internal bool TryRecordCallback(PlayerInput playerInput)
        {
            if (!HasCallbackEvidence)
            {
                CallbackPlayerInput = playerInput;
                ResolveCorrelationWhenPossible();
                return CallbackConfirmation !=
                    LocalPlayerJoinCallbackConfirmation.RejectedDifferentPlayerInput;
            }

            if (ReferenceEquals(CallbackPlayerInput, playerInput))
            {
                ResolveCorrelationWhenPossible();
                return true;
            }

            CallbackConfirmation =
                LocalPlayerJoinCallbackConfirmation.RejectedDifferentPlayerInput;
            return false;
        }

        internal void MarkConfirmed()
        {
            CallbackConfirmation =
                LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput;
        }

        private void ResolveCorrelationWhenPossible()
        {
            if (ReferenceEquals(DirectPlayerInput, null) ||
                ReferenceEquals(CallbackPlayerInput, null))
            {
                CallbackConfirmation = LocalPlayerJoinCallbackConfirmation.Pending;
                return;
            }

            CallbackConfirmation = ReferenceEquals(
                    DirectPlayerInput,
                    CallbackPlayerInput)
                ? LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput
                : LocalPlayerJoinCallbackConfirmation.RejectedDifferentPlayerInput;
        }
    }
}
