using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority for the effective relation between one configured Player Slot
    /// and one current prepared Logical Player Actor. It does not materialize Actors, bind
    /// input, publish camera requests or mutate passive scene components.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.2 Session effective Player gameplay occupancy authority.")]
    internal sealed class PlayerGameplayOccupancyRuntimeContext
    {
        private readonly string _sessionContextId;
        private readonly PlayerSlotId[] _orderedSlots;
        private readonly Dictionary<PlayerSlotId, PlayerGameplayOccupancySummary> _slots;

        private int _revision = 1;
        private int _occupancySequence;
        private PlayerGameplayOccupancyStatus _lastOperationStatus;
        private string _lastOperationMessage =
            "Player gameplay occupancy runtime initialized.";

        private PlayerGameplayOccupancyRuntimeContext(
            string sessionContextId,
            PlayerSlotId[] orderedSlots)
        {
            this._sessionContextId = sessionContextId;
            this._orderedSlots = orderedSlots;
            _slots = new Dictionary<PlayerSlotId, PlayerGameplayOccupancySummary>(
                orderedSlots.Length);

            for (int index = 0; index < orderedSlots.Length; index++)
            {
                PlayerSlotId slot = orderedSlots[index];
                _slots.Add(
                    slot,
                    PlayerGameplayOccupancySummary.Vacant(
                        sessionContextId,
                        slot,
                        0,
                        nameof(PlayerGameplayOccupancyRuntimeContext),
                        "runtime-initialization",
                        "Configured Player Slot has no effective gameplay occupancy."));
            }
        }

        internal string SessionContextId => _sessionContextId;
        internal int Revision => _revision;

        internal static bool TryCreate(
            PlayerActorPreparationSnapshot preparationSnapshot,
            out PlayerGameplayOccupancyRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (preparationSnapshot == null ||
                !preparationSnapshot.IsInitialized ||
                string.IsNullOrEmpty(preparationSnapshot.SessionContextId))
            {
                issue =
                    "Player gameplay occupancy requires an initialized Player Actor preparation snapshot.";
                return false;
            }

            if (preparationSnapshot.ConfiguredSlotCount <= 0)
            {
                issue =
                    "Player gameplay occupancy requires at least one configured Player Slot.";
                return false;
            }

            var ordered = new PlayerSlotId[preparationSnapshot.ConfiguredSlotCount];
            var unique = new HashSet<PlayerSlotId>();
            for (int index = 0; index < preparationSnapshot.Slots.Count; index++)
            {
                PlayerActorPreparationSummary preparation =
                    preparationSnapshot.Slots[index];
                if (!preparation.IsValid ||
                    preparation.PlayerSlotId.IsValid == false ||
                    !string.Equals(
                        preparation.SessionContextId,
                        preparationSnapshot.SessionContextId,
                        StringComparison.Ordinal))
                {
                    issue =
                        $"Player gameplay occupancy rejected invalid preparation Slot evidence at index '{index}'.";
                    return false;
                }

                if (!unique.Add(preparation.PlayerSlotId))
                {
                    issue =
                        $"Player gameplay occupancy rejected duplicate configured Slot '{preparation.PlayerSlotId.StableText}'.";
                    return false;
                }

                ordered[index] = preparation.PlayerSlotId;
            }

            context = new PlayerGameplayOccupancyRuntimeContext(
                preparationSnapshot.SessionContextId,
                ordered);
            return true;
        }

        internal PlayerGameplayOccupancyResult TryConfirmOccupancy(
            PlayerActorPreparationSummary preparation,
            string source,
            string reason)
        {
            const string operation = "ConfirmOccupancy";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayOccupancyRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "confirm-player-gameplay-occupancy");

            if (!preparation.IsValid ||
                !preparation.PlayerSlotId.IsValid ||
                !preparation.Token.IsValid)
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedInvalidRequest,
                    operation,
                    preparation.PlayerSlotId,
                    default,
                    "Effective occupancy requires valid current preparation evidence.");
            }

            if (!string.Equals(
                    preparation.SessionContextId,
                    _sessionContextId,
                    StringComparison.Ordinal))
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedSessionMismatch,
                    operation,
                    preparation.PlayerSlotId,
                    GetSummaryOrDefault(preparation.PlayerSlotId),
                    "Preparation belongs to another Session context.");
            }

            if (!_slots.TryGetValue(
                    preparation.PlayerSlotId,
                    out PlayerGameplayOccupancySummary previous))
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedSlotNotConfigured,
                    operation,
                    preparation.PlayerSlotId,
                    default,
                    $"Player Slot '{preparation.PlayerSlotId.StableText}' is not configured in this occupancy context.");
            }

            if (!preparation.IsPrepared ||
                !preparation.Materialization.IsActive ||
                preparation.PreparedActorProfileId != preparation.SelectedActorProfileId)
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedPreparationNotReady,
                    operation,
                    preparation.PlayerSlotId,
                    previous,
                    "Effective occupancy requires an Active prepared Logical Player Actor.");
            }

            if (!IsPreparationCoherent(preparation))
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedForeignOrStalePreparation,
                    operation,
                    preparation.PlayerSlotId,
                    previous,
                    "Preparation identity is incoherent or stale.");
            }

            if (TryFindConflictingOccupancy(
                    preparation,
                    out PlayerGameplayOccupancySummary conflicting))
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedPreparationAlreadyOccupied,
                    operation,
                    preparation.PlayerSlotId,
                    previous,
                    $"Prepared Actor identity is already occupied by Slot '{conflicting.PlayerSlotId.StableText}'.");
            }

            if (previous.IsOccupied)
            {
                if (previous.PreparationToken == preparation.Token &&
                    previous.ActorProfileId == preparation.PreparedActorProfileId &&
                    previous.ActorId == preparation.Materialization.ActorId &&
                    previous.Owner == preparation.Materialization.Owner &&
                    previous.RuntimeContentIdentity ==
                        preparation.Materialization.RuntimeContentIdentity)
                {
                    _lastOperationStatus =
                        PlayerGameplayOccupancyStatus.SucceededAlreadyOccupied;
                    _lastOperationMessage =
                        "Prepared Logical Player Actor is already the effective occupant.";
                    return Result(
                        _lastOperationStatus,
                        operation,
                        preparation.PlayerSlotId,
                        previous,
                        previous,
                        _lastOperationMessage);
                }

                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedSlotAlreadyOccupied,
                    operation,
                    preparation.PlayerSlotId,
                    previous,
                    $"Player Slot '{preparation.PlayerSlotId.StableText}' is already occupied by another preparation.");
            }

            _occupancySequence++;
            _revision++;
            var token = new PlayerGameplayOccupancyToken(
                _sessionContextId,
                preparation.Materialization.Owner,
                preparation.PlayerSlotId,
                preparation.PreparedActorProfileId,
                preparation.Materialization.ActorId,
                preparation.Token,
                preparation.Materialization.RuntimeContentIdentity,
                preparation.Materialization.MaterializationRevision,
                _occupancySequence);
            var current = new PlayerGameplayOccupancySummary(
                _sessionContextId,
                preparation.PlayerSlotId,
                PlayerGameplayOccupancyState.Occupied,
                preparation.PreparedActorProfileId,
                preparation.Materialization.ActorId,
                preparation.Materialization.Owner,
                preparation.Materialization.RuntimeContentIdentity,
                preparation.Token,
                token,
                _occupancySequence,
                resolvedSource,
                resolvedReason,
                "Prepared Logical Player Actor confirmed as the effective Slot occupant.");

            if (!current.IsValid)
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedForeignOrStalePreparation,
                    operation,
                    preparation.PlayerSlotId,
                    previous,
                    "Effective occupancy token creation produced incoherent identity evidence.");
            }

            _slots[preparation.PlayerSlotId] = current;
            _lastOperationStatus = PlayerGameplayOccupancyStatus.SucceededOccupied;
            _lastOperationMessage = current.Message;
            return Result(
                _lastOperationStatus,
                operation,
                preparation.PlayerSlotId,
                previous,
                current,
                _lastOperationMessage);
        }

        internal PlayerGameplayOccupancyResult TryReleaseOccupancy(
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancyToken expectedOccupancy,
            string source,
            string reason)
        {
            const string operation = "ReleaseOccupancy";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayOccupancyRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-player-gameplay-occupancy");

            if (!playerSlotId.IsValid)
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    default,
                    "Effective occupancy release requires a valid Player Slot identity.");
            }

            if (!_slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayOccupancySummary previous))
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedSlotNotConfigured,
                    operation,
                    playerSlotId,
                    default,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this occupancy context.");
            }

            if (previous.IsVacant)
            {
                if (expectedOccupancy.IsValid)
                {
                    return Reject(
                        PlayerGameplayOccupancyStatus.RejectedForeignOrStaleOccupancy,
                        operation,
                        playerSlotId,
                        previous,
                        "Expected occupancy token is foreign or stale because the Slot is already vacant.");
                }

                _lastOperationStatus =
                    PlayerGameplayOccupancyStatus.SucceededAlreadyReleased;
                _lastOperationMessage =
                    "Player Slot occupancy was already released.";
                return Result(
                    _lastOperationStatus,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    _lastOperationMessage);
            }

            if (!expectedOccupancy.IsValid ||
                expectedOccupancy != previous.Token)
            {
                return Reject(
                    PlayerGameplayOccupancyStatus.RejectedForeignOrStaleOccupancy,
                    operation,
                    playerSlotId,
                    previous,
                    "Effective occupancy release rejected a foreign or stale occupancy token.");
            }

            _revision++;
            var current = PlayerGameplayOccupancySummary.Vacant(
                _sessionContextId,
                playerSlotId,
                previous.OccupancyRevision,
                resolvedSource,
                resolvedReason,
                "Effective Player Slot occupancy released.");
            _slots[playerSlotId] = current;
            _lastOperationStatus = PlayerGameplayOccupancyStatus.SucceededReleased;
            _lastOperationMessage = current.Message;
            return Result(
                _lastOperationStatus,
                operation,
                playerSlotId,
                previous,
                current,
                _lastOperationMessage);
        }

        internal bool TryGetSummary(
            PlayerSlotId playerSlotId,
            out PlayerGameplayOccupancySummary summary)
        {
            return _slots.TryGetValue(playerSlotId, out summary);
        }

        internal PlayerGameplayOccupancySnapshot CreateSnapshot()
        {
            var summaries =
                new PlayerGameplayOccupancySummary[_orderedSlots.Length];
            for (int index = 0; index < _orderedSlots.Length; index++)
            {
                summaries[index] = _slots[_orderedSlots[index]];
            }

            return new PlayerGameplayOccupancySnapshot(
                _sessionContextId,
                _revision,
                summaries,
                _lastOperationStatus,
                _lastOperationMessage);
        }

        private bool TryFindConflictingOccupancy(
            PlayerActorPreparationSummary preparation,
            out PlayerGameplayOccupancySummary conflicting)
        {
            for (int index = 0; index < _orderedSlots.Length; index++)
            {
                PlayerGameplayOccupancySummary current =
                    _slots[_orderedSlots[index]];
                if (!current.IsOccupied ||
                    current.PlayerSlotId == preparation.PlayerSlotId)
                {
                    continue;
                }

                if (current.PreparationToken == preparation.Token ||
                    current.RuntimeContentIdentity ==
                        preparation.Materialization.RuntimeContentIdentity ||
                    current.ActorId == preparation.Materialization.ActorId)
                {
                    conflicting = current;
                    return true;
                }
            }

            conflicting = default;
            return false;
        }

        private static bool IsPreparationCoherent(
            PlayerActorPreparationSummary preparation)
        {
            PlayerActorPreparationToken token = preparation.Token;
            PlayerActorMaterializationSnapshot materialization =
                preparation.Materialization;
            return token.IsValid &&
                materialization.IsValid &&
                materialization.IsActive &&
                token.PlayerSlotId == preparation.PlayerSlotId &&
                token.ActorId == materialization.ActorId &&
                token.RuntimeContentIdentity ==
                    materialization.RuntimeContentIdentity &&
                token.MaterializationRevision ==
                    materialization.MaterializationRevision &&
                materialization.PlayerSlotId == preparation.PlayerSlotId &&
                materialization.ActorProfileId ==
                    preparation.PreparedActorProfileId &&
                materialization.Owner ==
                    materialization.RuntimeContentIdentity.Owner;
        }

        private PlayerGameplayOccupancySummary GetSummaryOrDefault(
            PlayerSlotId playerSlotId)
        {
            return playerSlotId.IsValid &&
                _slots.TryGetValue(playerSlotId, out PlayerGameplayOccupancySummary summary)
                ? summary
                : default;
        }

        private PlayerGameplayOccupancyResult Reject(
            PlayerGameplayOccupancyStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancySummary current,
            string message)
        {
            _lastOperationStatus = status;
            _lastOperationMessage = message;
            return Result(
                status,
                operation,
                playerSlotId,
                current,
                current,
                message);
        }

        private PlayerGameplayOccupancyResult Result(
            PlayerGameplayOccupancyStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancySummary previous,
            PlayerGameplayOccupancySummary current,
            string message)
        {
            return new PlayerGameplayOccupancyResult(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                CreateSnapshot(),
                message);
        }
    }
}
