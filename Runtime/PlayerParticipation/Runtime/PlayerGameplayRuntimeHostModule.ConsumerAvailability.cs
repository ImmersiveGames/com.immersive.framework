using System.Collections.Generic;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerGameplayRuntimeHostModule
    {
        private readonly Dictionary<PlayerOccurrenceId,
            HashSet<PlayerGameplayAvailabilityBlockToken>>
            _consumerAvailabilityBlocks = new();
        private int _consumerAvailabilityBlockSequence;

        internal PlayerGameplayAvailabilityBlockResult RequestBlockRuntimeGameplay(
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            if (!IsReady)
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    playerSlotId,
                    _diagnostic);
            }

            if (!playerSlotId.IsValid)
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedInvalidRequest,
                    playerSlotId,
                    "Runtime gameplay block requires a valid Player Slot.");
            }

            if (!TryGetPlayerOccurrence(
                    playerSlotId,
                    requireJoined: true,
                    out PlayerOccurrenceId occurrenceId))
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedPlayerNotJoined,
                    playerSlotId,
                    "Runtime gameplay can only be blocked for the exact current joined Player occurrence.");
            }

            bool firstBlock = !_consumerAvailabilityBlocks.TryGetValue(
                occurrenceId,
                out HashSet<PlayerGameplayAvailabilityBlockToken> tokens);
            if (firstBlock)
            {
                tokens = new HashSet<PlayerGameplayAvailabilityBlockToken>();
                _consumerAvailabilityBlocks.Add(occurrenceId, tokens);
            }

            var token = new PlayerGameplayAvailabilityBlockToken(
                _participationContext.CreateSnapshot().ContextId,
                occurrenceId,
                ++_consumerAvailabilityBlockSequence);
            tokens.Add(token);

            if (firstBlock &&
                !_inputContext.TrySetConsumerAvailabilityBlocked(
                    playerSlotId,
                    true,
                    source,
                    reason,
                    out string issue))
            {
                _consumerAvailabilityBlocks.Remove(occurrenceId);
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.FailedInputProjection,
                    playerSlotId,
                    issue);
            }

            return new PlayerGameplayAvailabilityBlockResult(
                PlayerGameplayAvailabilityBlockStatus.SucceededBlocked,
                playerSlotId,
                token,
                $"Runtime gameplay is blocked for Player occurrence '{occurrenceId}'.");
        }

        internal PlayerGameplayAvailabilityBlockResult RequestReleaseRuntimeGameplay(
            PlayerGameplayAvailabilityBlockToken blockToken,
            string source,
            string reason)
        {
            PlayerSlotId playerSlotId = default;
            if (!IsReady)
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedRuntimeUnavailable,
                    playerSlotId,
                    _diagnostic);
            }

            if (!blockToken.IsValid ||
                blockToken.SessionContextId != _participationContext.CreateSnapshot().ContextId ||
                !TryGetPlayerOccurrence(
                    blockToken.PlayerOccurrenceId,
                    requireJoined: true,
                    out playerSlotId))
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken,
                    playerSlotId,
                    "Runtime gameplay block token is foreign, stale or belongs to a Player occurrence that is no longer joined.");
            }

            if (!_consumerAvailabilityBlocks.TryGetValue(
                    blockToken.PlayerOccurrenceId,
                    out HashSet<PlayerGameplayAvailabilityBlockToken> tokens) ||
                !tokens.Contains(blockToken))
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken,
                    playerSlotId,
                    "Runtime gameplay block token is not active in this Session occurrence.");
            }

            if (tokens.Count == 1 &&
                !_inputContext.TrySetConsumerAvailabilityBlocked(
                    playerSlotId,
                    false,
                    source,
                    reason,
                    out string issue))
            {
                return PlayerGameplayAvailabilityBlockResult.Failure(
                    PlayerGameplayAvailabilityBlockStatus.FailedInputProjection,
                    playerSlotId,
                    issue);
            }

            tokens.Remove(blockToken);
            if (tokens.Count == 0)
            {
                _consumerAvailabilityBlocks.Remove(blockToken.PlayerOccurrenceId);
            }

            return new PlayerGameplayAvailabilityBlockResult(
                PlayerGameplayAvailabilityBlockStatus.SucceededReleased,
                playerSlotId,
                blockToken,
                "Runtime gameplay block was released for its exact Player occurrence.");
        }

        private bool TryClearConsumerGameplayBlocksForLeave(
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!TryGetPlayerOccurrence(
                    playerSlotId,
                    requireJoined: false,
                    out PlayerOccurrenceId occurrenceId))
            {
                return true;
            }

            if (!_inputContext.TrySetConsumerAvailabilityBlocked(
                    playerSlotId,
                    false,
                    source,
                    reason,
                    out issue))
            {
                return false;
            }

            _consumerAvailabilityBlocks.Remove(occurrenceId);
            _inputContext.ForgetConsumerAvailabilityProjection(playerSlotId);
            return true;
        }

        private void ReleaseAllConsumerGameplayBlocksForShutdown()
        {
            if (_inputContext == null)
            {
                _consumerAvailabilityBlocks.Clear();
                return;
            }

            _inputContext.ClearConsumerAvailabilityProjections(
                nameof(PlayerGameplayRuntimeHostModule),
                "runtime-host-shutdown");
            _consumerAvailabilityBlocks.Clear();
        }

        private bool TryGetPlayerOccurrence(
            PlayerSlotId playerSlotId,
            bool requireJoined,
            out PlayerOccurrenceId occurrenceId)
        {
            occurrenceId = default;
            if (_participationContext == null)
            {
                return false;
            }

            PlayerParticipationSnapshot snapshot =
                _participationContext.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized)
            {
                return false;
            }

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (slot.PlayerSlotId != playerSlotId ||
                    (requireJoined && !slot.IsJoined))
                {
                    continue;
                }

                occurrenceId = slot.PlayerOccurrenceId;
                return occurrenceId.IsValid;
            }

            return false;
        }

        private bool TryGetPlayerOccurrence(
            PlayerOccurrenceId occurrenceId,
            bool requireJoined,
            out PlayerSlotId playerSlotId)
        {
            playerSlotId = default;
            if (!occurrenceId.IsValid || _participationContext == null)
            {
                return false;
            }

            PlayerParticipationSnapshot snapshot =
                _participationContext.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized)
            {
                return false;
            }

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (slot.PlayerOccurrenceId != occurrenceId ||
                    (requireJoined && !slot.IsJoined))
                {
                    continue;
                }

                playerSlotId = slot.PlayerSlotId;
                return true;
            }

            return false;
        }
    }
}
