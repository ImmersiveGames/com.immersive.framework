using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority that binds one current effective Player occupancy to the stable
    /// Local Player Host PlayerInput. Domain identity and lifecycle remain owned here; every
    /// concrete PlayerInput/InputActionMap side effect is delegated to UnityPlayerInputStateWriter.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "IC1 typed gameplay input binding using one Unity PlayerInput physical writer.")]
    internal sealed class PlayerGameplayInputBindingRuntimeContext
    {
        private sealed class BindingRecord
        {
            internal LocalPlayerHostAuthoring Host;
            internal PlayerActorDeclaration ActorDeclaration;
            internal PlayerInput PlayerInput;
            internal UnityPlayerInputGateAdapter GateAdapter;
            internal UnityPlayerInputActionMapWriteReceipt ActionMapWrite;
        }

        private readonly string sessionContextId;
        private readonly PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private readonly PlayerSlotId[] orderedSlots;
        private readonly Dictionary<PlayerSlotId, PlayerGameplayInputBindingSummary> slots;
        private readonly Dictionary<PlayerSlotId, BindingRecord> records;

        private int revision = 1;
        private int bindingSequence;
        private PlayerGameplayInputBindingStatus lastOperationStatus;
        private string lastOperationMessage =
            "Player gameplay input binding runtime initialized.";

        private PlayerGameplayInputBindingRuntimeContext(
            string sessionContextId,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerSlotId[] orderedSlots)
        {
            this.sessionContextId = sessionContextId;
            this.occupancyContext = occupancyContext;
            this.orderedSlots = orderedSlots;
            slots = new Dictionary<PlayerSlotId, PlayerGameplayInputBindingSummary>(
                orderedSlots.Length);
            records = new Dictionary<PlayerSlotId, BindingRecord>(
                orderedSlots.Length);

            for (int index = 0; index < orderedSlots.Length; index++)
            {
                PlayerSlotId slot = orderedSlots[index];
                slots.Add(
                    slot,
                    PlayerGameplayInputBindingSummary.Unbound(
                        sessionContextId,
                        slot,
                        0,
                        nameof(PlayerGameplayInputBindingRuntimeContext),
                        "runtime-initialization",
                        "Configured Player Slot has no gameplay input binding."));
            }
        }

        internal string SessionContextId => sessionContextId;
        internal int Revision => revision;

        internal static bool TryCreate(
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            out PlayerGameplayInputBindingRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (occupancyContext == null)
            {
                issue =
                    "Gameplay input binding requires an explicit effective occupancy authority.";
                return false;
            }

            PlayerGameplayOccupancySnapshot occupancySnapshot =
                occupancyContext.CreateSnapshot();
            if (occupancySnapshot == null ||
                !occupancySnapshot.IsInitialized ||
                string.IsNullOrEmpty(occupancySnapshot.SessionContextId))
            {
                issue =
                    "Gameplay input binding requires an initialized effective occupancy snapshot.";
                return false;
            }

            if (occupancySnapshot.ConfiguredSlotCount <= 0)
            {
                issue =
                    "Gameplay input binding requires at least one configured Player Slot.";
                return false;
            }

            var ordered = new PlayerSlotId[
                occupancySnapshot.ConfiguredSlotCount];
            var unique = new HashSet<PlayerSlotId>();
            for (int index = 0;
                 index < occupancySnapshot.Slots.Count;
                 index++)
            {
                PlayerGameplayOccupancySummary occupancy =
                    occupancySnapshot.Slots[index];
                if (!occupancy.IsValid ||
                    !occupancy.PlayerSlotId.IsValid ||
                    !string.Equals(
                        occupancy.SessionContextId,
                        occupancySnapshot.SessionContextId,
                        StringComparison.Ordinal))
                {
                    issue =
                        $"Gameplay input binding rejected invalid occupancy Slot evidence at index '{index}'.";
                    return false;
                }

                if (!unique.Add(occupancy.PlayerSlotId))
                {
                    issue =
                        $"Gameplay input binding rejected duplicate configured Slot '{occupancy.PlayerSlotId.StableText}'.";
                    return false;
                }

                ordered[index] = occupancy.PlayerSlotId;
            }

            context = new PlayerGameplayInputBindingRuntimeContext(
                occupancySnapshot.SessionContextId,
                occupancyContext,
                ordered);
            return true;
        }

        internal PlayerGameplayInputBindingResult TryBind(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration actorDeclaration,
            UnityPlayerInputGateAdapter gateAdapter,
            string source,
            string reason)
        {
            const string Operation = "BindGameplayInput";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayInputBindingRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "bind-player-gameplay-input");
            PlayerSlotId requestedSlot = occupancy.PlayerSlotId.IsValid
                ? occupancy.PlayerSlotId
                : preparation.PlayerSlotId;

            if (!requestedSlot.IsValid ||
                !preparation.IsValid ||
                !occupancy.IsValid)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedInvalidRequest,
                    Operation,
                    requestedSlot,
                    GetSummaryOrDefault(requestedSlot),
                    "Gameplay input binding requires valid preparation and occupancy evidence.");
            }

            if (!string.Equals(
                    preparation.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    occupancy.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedSessionMismatch,
                    Operation,
                    requestedSlot,
                    GetSummaryOrDefault(requestedSlot),
                    "Preparation or occupancy belongs to another Session context.");
            }

            if (!slots.TryGetValue(
                    requestedSlot,
                    out PlayerGameplayInputBindingSummary previous))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedSlotNotConfigured,
                    Operation,
                    requestedSlot,
                    default,
                    $"Player Slot '{requestedSlot.StableText}' is not configured in this input binding context.");
            }

            if (!preparation.IsPrepared ||
                !preparation.Materialization.IsActive ||
                !preparation.Token.IsValid)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedPreparationNotReady,
                    Operation,
                    requestedSlot,
                    previous,
                    "Gameplay input binding requires an Active prepared Logical Player Actor.");
            }

            if (!occupancy.IsOccupied || !occupancy.Token.IsValid)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedOccupancyNotReady,
                    Operation,
                    requestedSlot,
                    previous,
                    "Gameplay input binding requires current effective occupancy.");
            }

            if (!occupancyContext.TryGetSummary(
                    requestedSlot,
                    out PlayerGameplayOccupancySummary currentOccupancy) ||
                !currentOccupancy.IsOccupied ||
                currentOccupancy.Token != occupancy.Token)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedForeignOrStaleOccupancy,
                    Operation,
                    requestedSlot,
                    previous,
                    "Supplied occupancy is no longer current in the effective occupancy authority.");
            }

            occupancy = currentOccupancy;
            if (!IsPreparationAndOccupancyCoherent(
                    preparation,
                    occupancy))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedForeignOrStaleOccupancy,
                    Operation,
                    requestedSlot,
                    previous,
                    "Preparation and occupancy identities are foreign, mismatched or stale.");
            }

            if (!TryValidateHost(
                    requestedSlot,
                    host,
                    out PlayerInput playerInput,
                    out string hostIssue))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedHostMismatch,
                    Operation,
                    requestedSlot,
                    previous,
                    hostIssue);
            }

            if (!TryValidateActor(
                    occupancy,
                    host,
                    actorDeclaration,
                    playerInput,
                    out PlayerGameplayInputBindingStatus actorStatus,
                    out string actorIssue))
            {
                return Reject(
                    actorStatus,
                    Operation,
                    requestedSlot,
                    previous,
                    actorIssue);
            }

            if (playerInput.actions == null)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedMissingActionAsset,
                    Operation,
                    requestedSlot,
                    previous,
                    "Stable Local Player Host PlayerInput has no InputActionAsset.");
            }

            if (gateAdapter == null ||
                !ReferenceEquals(gateAdapter.PlayerInput, playerInput))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedGateAdapterMismatch,
                    Operation,
                    requestedSlot,
                    previous,
                    "Gameplay input binding requires an explicit Gate adapter targeting the same stable-host PlayerInput.");
            }

            string actionMapName =
                gateAdapter.GameplayActionMapName.NormalizeText();
            if (string.IsNullOrEmpty(actionMapName))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedMissingActionMap,
                    Operation,
                    requestedSlot,
                    previous,
                    "Gate adapter has no configured gameplay action map name.");
            }

            InputActionMap gameplayActionMap =
                playerInput.actions.FindActionMap(
                    actionMapName,
                    throwIfNotFound: false);
            if (gameplayActionMap == null)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedMissingActionMap,
                    Operation,
                    requestedSlot,
                    previous,
                    $"Stable-host PlayerInput has no gameplay action map '{actionMapName}'.");
            }

            if (previous.IsBound || previous.IsReleaseFailed)
            {
                if (previous.OccupancyToken == occupancy.Token &&
                    previous.PreparationToken == preparation.Token &&
                    previous.ActorId == occupancy.ActorId &&
                    previous.ActionMapName == actionMapName &&
                    records.TryGetValue(
                        requestedSlot,
                        out BindingRecord existing) &&
                    ReferenceEquals(existing.Host, host) &&
                    ReferenceEquals(
                        existing.ActorDeclaration,
                        actorDeclaration) &&
                    ReferenceEquals(existing.PlayerInput, playerInput) &&
                    ReferenceEquals(existing.GateAdapter, gateAdapter))
                {
                    gateAdapter.ApplyCurrentGate();
                    PlayerGameplayInputBindingSummary refreshed =
                        RefreshSummaryAvailability(
                            previous,
                            gateAdapter,
                            resolvedSource,
                            resolvedReason,
                            "Gameplay input binding is already current.");
                    slots[requestedSlot] = refreshed;
                    lastOperationStatus =
                        PlayerGameplayInputBindingStatus
                            .SucceededAlreadyBound;
                    lastOperationMessage = refreshed.Message;
                    return Result(
                        lastOperationStatus,
                        Operation,
                        requestedSlot,
                        previous,
                        refreshed,
                        false,
                        true,
                        string.Empty,
                        lastOperationMessage);
                }

                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedSlotAlreadyBound,
                    Operation,
                    requestedSlot,
                    previous,
                    $"Player Slot '{requestedSlot.StableText}' already has another gameplay input binding.");
            }

            foreach (KeyValuePair<PlayerSlotId, BindingRecord> pair in records)
            {
                if (ReferenceEquals(pair.Value.PlayerInput, playerInput))
                {
                    return Reject(
                        PlayerGameplayInputBindingStatus
                            .RejectedPlayerInputAlreadyBound,
                        Operation,
                        requestedSlot,
                        previous,
                        $"PlayerInput '{playerInput.name}' is already bound to Slot '{pair.Key.StableText}'.");
                }
            }

            if (!gateAdapter.TrySelectActionMap(
                    actionMapName,
                    resolvedSource,
                    resolvedReason,
                    out UnityPlayerInputActionMapWriteReceipt actionMapWrite,
                    out string activationIssue))
            {
                return Failure(
                    PlayerGameplayInputBindingStatus.FailedActionMapActivation,
                    Operation,
                    requestedSlot,
                    previous,
                    false,
                    true,
                    string.Empty,
                    $"Gameplay action map activation failed. {activationIssue}");
            }

            try
            {
                gateAdapter.ApplyCurrentGate();
            }
            catch (Exception exception)
            {
                bool rollbackSucceeded =
                    gateAdapter.TryRestoreActionMap(
                        actionMapWrite,
                        resolvedSource,
                        "gate-apply-failed-rollback",
                        out string rollbackIssue);
                return Failure(
                    rollbackSucceeded
                        ? PlayerGameplayInputBindingStatus
                            .FailedActionMapActivation
                        : PlayerGameplayInputBindingStatus.FailedRollback,
                    Operation,
                    requestedSlot,
                    previous,
                    actionMapWrite.StateChanged,
                    rollbackSucceeded,
                    rollbackIssue,
                    $"Gameplay Gate application failed. {exception.Message}");
            }

            bindingSequence++;
            revision++;
            var token = new PlayerGameplayInputBindingToken(
                sessionContextId,
                occupancy.Owner,
                requestedSlot,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                preparation.Token,
                occupancy.Token,
                occupancy.RuntimeContentIdentity,
                occupancy.Token.MaterializationRevision,
                occupancy.OccupancyRevision,
                bindingSequence);
            PlayerGameplayInputAvailability availability =
                gateAdapter.IsBlockedByAdapter
                    ? PlayerGameplayInputAvailability.BlockedByGate
                    : PlayerGameplayInputAvailability.Allowed;
            var current = new PlayerGameplayInputBindingSummary(
                sessionContextId,
                requestedSlot,
                PlayerGameplayInputBindingState.Bound,
                availability,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.Owner,
                occupancy.RuntimeContentIdentity,
                preparation.Token,
                occupancy.Token,
                token,
                actionMapName,
                actionMapWrite.PreviousActionMapName,
                playerInput.name,
                bindingSequence,
                resolvedSource,
                resolvedReason,
                "Prepared Logical Player Actor is bound to the stable-host PlayerInput through the canonical physical writer.");

            records.Add(
                requestedSlot,
                new BindingRecord
                {
                    Host = host,
                    ActorDeclaration = actorDeclaration,
                    PlayerInput = playerInput,
                    GateAdapter = gateAdapter,
                    ActionMapWrite = actionMapWrite
                });
            slots[requestedSlot] = current;
            lastOperationStatus =
                PlayerGameplayInputBindingStatus.SucceededBound;
            lastOperationMessage = current.Message;
            return Result(
                lastOperationStatus,
                Operation,
                requestedSlot,
                previous,
                current,
                false,
                true,
                string.Empty,
                lastOperationMessage);
        }

        internal PlayerGameplayInputBindingResult TryRefreshAvailability(
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken expectedBinding,
            string source,
            string reason)
        {
            const string Operation = "RefreshGameplayInputAvailability";
            if (!slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary previous))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedSlotNotConfigured,
                    Operation,
                    playerSlotId,
                    default,
                    "Availability refresh targets an unconfigured Player Slot.");
            }

            if (!previous.IsBound ||
                !expectedBinding.IsValid ||
                previous.Token != expectedBinding ||
                !records.TryGetValue(
                    playerSlotId,
                    out BindingRecord record))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedForeignOrStaleBinding,
                    Operation,
                    playerSlotId,
                    previous,
                    "Availability refresh requires the exact current input binding token.");
            }

            record.GateAdapter.ApplyCurrentGate();
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayInputBindingRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "refresh-player-gameplay-input-availability");
            PlayerGameplayInputBindingSummary current =
                RefreshSummaryAvailability(
                    previous,
                    record.GateAdapter,
                    resolvedSource,
                    resolvedReason,
                    record.GateAdapter.IsBlockedByAdapter
                        ? "Gameplay input is blocked by the current Gate state."
                        : "Gameplay input is allowed by the current Gate state.");

            if (current.Availability != previous.Availability)
            {
                revision++;
            }

            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayInputBindingStatus
                    .SucceededAvailabilityRefreshed;
            lastOperationMessage = current.Message;
            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                false,
                true,
                string.Empty,
                lastOperationMessage);
        }

        internal PlayerGameplayInputBindingResult TryRelease(
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken expectedBinding,
            string source,
            string reason)
        {
            const string Operation = "ReleaseGameplayInput";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayInputBindingRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-player-gameplay-input");

            if (!slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary previous))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedSlotNotConfigured,
                    Operation,
                    playerSlotId,
                    default,
                    "Gameplay input release targets an unconfigured Player Slot.");
            }

            if (previous.IsUnbound)
            {
                if (expectedBinding.IsValid)
                {
                    return Reject(
                        PlayerGameplayInputBindingStatus
                            .RejectedForeignOrStaleBinding,
                        Operation,
                        playerSlotId,
                        previous,
                        "Gameplay input binding is already released and the supplied token is stale.");
                }

                lastOperationStatus =
                    PlayerGameplayInputBindingStatus
                        .SucceededAlreadyReleased;
                lastOperationMessage =
                    "Gameplay input binding is already released.";
                return Result(
                    lastOperationStatus,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    true,
                    string.Empty,
                    lastOperationMessage);
            }

            if (!expectedBinding.IsValid ||
                previous.Token != expectedBinding)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedForeignOrStaleBinding,
                    Operation,
                    playerSlotId,
                    previous,
                    "Gameplay input release requires the exact current binding token.");
            }

            if (!records.TryGetValue(
                    playerSlotId,
                    out BindingRecord record))
            {
                return Failure(
                    PlayerGameplayInputBindingStatus.FailedRelease,
                    Operation,
                    playerSlotId,
                    previous,
                    false,
                    false,
                    "Physical gameplay input binding record is missing.",
                    "Gameplay input release could not resolve its internal binding record.");
            }

            try
            {
                record.GateAdapter.Restore();
                if (record.GateAdapter.IsBlockedByAdapter)
                {
                    return MarkReleaseFailed(
                        Operation,
                        playerSlotId,
                        previous,
                        resolvedSource,
                        resolvedReason,
                        "Gate-owned block state could not be released.");
                }

                if (!record.GateAdapter.TryRestoreActionMap(
                        record.ActionMapWrite,
                        resolvedSource,
                        resolvedReason,
                        out string restoreIssue))
                {
                    return MarkReleaseFailed(
                        Operation,
                        playerSlotId,
                        previous,
                        resolvedSource,
                        resolvedReason,
                        restoreIssue);
                }
            }
            catch (Exception exception)
            {
                return MarkReleaseFailed(
                    Operation,
                    playerSlotId,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    exception.Message);
            }

            records.Remove(playerSlotId);
            revision++;
            PlayerGameplayInputBindingSummary current =
                PlayerGameplayInputBindingSummary.Unbound(
                    sessionContextId,
                    playerSlotId,
                    previous.BindingRevision,
                    resolvedSource,
                    resolvedReason,
                    "Gameplay input binding released and previous action-map state restored through the canonical writer.");
            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayInputBindingStatus.SucceededReleased;
            lastOperationMessage = current.Message;
            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                false,
                true,
                string.Empty,
                lastOperationMessage);
        }

        internal bool TryReleaseAll(
            string source,
            string reason,
            out int releasedCount,
            out int failedCount,
            out string issue)
        {
            releasedCount = 0;
            failedCount = 0;
            var failures = new List<string>();
            PlayerGameplayInputBindingSnapshot snapshot = CreateSnapshot();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerGameplayInputBindingSummary summary =
                    snapshot.Slots[index];
                if (!summary.IsBound && !summary.IsReleaseFailed)
                {
                    continue;
                }

                PlayerGameplayInputBindingResult result = TryRelease(
                    summary.PlayerSlotId,
                    summary.Token,
                    source,
                    reason);
                if (result.Succeeded)
                {
                    releasedCount++;
                }
                else
                {
                    failedCount++;
                    failures.Add(result.ToDiagnosticString());
                }
            }

            issue = failures.Count == 0
                ? string.Empty
                : string.Join(" | ", failures);
            return failedCount == 0;
        }

        internal PlayerGameplayInputBindingSnapshot CreateSnapshot()
        {
            var ordered = new PlayerGameplayInputBindingSummary[
                orderedSlots.Length];
            for (int index = 0; index < orderedSlots.Length; index++)
            {
                ordered[index] = slots[orderedSlots[index]];
            }

            return new PlayerGameplayInputBindingSnapshot(
                sessionContextId,
                revision,
                ordered,
                lastOperationStatus,
                lastOperationMessage);
        }

        private static bool IsPreparationAndOccupancyCoherent(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy)
        {
            return preparation.PlayerSlotId == occupancy.PlayerSlotId &&
                preparation.PreparedActorProfileId == occupancy.ActorProfileId &&
                preparation.Materialization.ActorId == occupancy.ActorId &&
                preparation.Materialization.Owner == occupancy.Owner &&
                preparation.Materialization.RuntimeContentIdentity ==
                    occupancy.RuntimeContentIdentity &&
                preparation.Token == occupancy.PreparationToken &&
                occupancy.Token.PreparationToken == preparation.Token &&
                occupancy.Token.ActorId == preparation.Materialization.ActorId &&
                occupancy.Token.RuntimeContentIdentity ==
                    preparation.Materialization.RuntimeContentIdentity;
        }

        private static bool TryValidateHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring host,
            out PlayerInput playerInput,
            out string issue)
        {
            playerInput = null;
            if (host == null || !host.IsJoined || !host.HasJoinedSlot)
            {
                issue =
                    "Gameplay input binding requires a joined stable Local Player Host.";
                return false;
            }

            if (host.JoinedPlayerSlotId != playerSlotId)
            {
                issue =
                    "Stable Local Player Host joined Slot evidence does not match the requested occupancy.";
                return false;
            }

            playerInput = host.PlayerInput;
            if (playerInput == null ||
                !ReferenceEquals(playerInput.gameObject, host.gameObject))
            {
                playerInput = null;
                issue =
                    "Stable Local Player Host has no matching PlayerInput authority.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryValidateActor(
            PlayerGameplayOccupancySummary occupancy,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration actorDeclaration,
            PlayerInput playerInput,
            out PlayerGameplayInputBindingStatus status,
            out string issue)
        {
            if (actorDeclaration == null)
            {
                status =
                    PlayerGameplayInputBindingStatus.RejectedActorMismatch;
                issue =
                    "Gameplay input binding requires the prepared PlayerActorDeclaration.";
                return false;
            }

            if (host == null ||
                host.ActorMount == null ||
                !actorDeclaration.transform.IsChildOf(host.ActorMount))
            {
                status =
                    PlayerGameplayInputBindingStatus.RejectedActorMismatch;
                issue =
                    "Prepared PlayerActorDeclaration is not owned by the stable host Actor Mount.";
                return false;
            }

            ActorId actorId;
            try
            {
                actorId = actorDeclaration.ActorId;
            }
            catch (Exception exception)
            {
                status =
                    PlayerGameplayInputBindingStatus.RejectedActorMismatch;
                issue =
                    $"Prepared PlayerActorDeclaration has an invalid ActorId. {exception.Message}";
                return false;
            }

            if (actorId != occupancy.ActorId)
            {
                status =
                    PlayerGameplayInputBindingStatus.RejectedActorMismatch;
                issue =
                    "Prepared PlayerActorDeclaration ActorId does not match effective occupancy.";
                return false;
            }

            if (!actorDeclaration.HasPlayerInputEvidence ||
                !ReferenceEquals(actorDeclaration.PlayerInput, playerInput))
            {
                status =
                    PlayerGameplayInputBindingStatus
                        .RejectedPlayerInputMismatch;
                issue =
                    "Prepared PlayerActorDeclaration does not reference the stable-host PlayerInput.";
                return false;
            }

            status = PlayerGameplayInputBindingStatus.SucceededBound;
            issue = string.Empty;
            return true;
        }

        private static PlayerGameplayInputBindingSummary
            RefreshSummaryAvailability(
                PlayerGameplayInputBindingSummary previous,
                UnityPlayerInputGateAdapter gateAdapter,
                string source,
                string reason,
                string message)
        {
            return new PlayerGameplayInputBindingSummary(
                previous.SessionContextId,
                previous.PlayerSlotId,
                previous.State,
                gateAdapter != null && gateAdapter.IsBlockedByAdapter
                    ? PlayerGameplayInputAvailability.BlockedByGate
                    : PlayerGameplayInputAvailability.Allowed,
                previous.ActorProfileId,
                previous.ActorId,
                previous.Owner,
                previous.RuntimeContentIdentity,
                previous.PreparationToken,
                previous.OccupancyToken,
                previous.Token,
                previous.ActionMapName,
                previous.PreviousActionMapName,
                previous.PlayerInputName,
                previous.BindingRevision,
                source,
                reason,
                message);
        }

        private PlayerGameplayInputBindingResult MarkReleaseFailed(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingSummary previous,
            string source,
            string reason,
            string issue)
        {
            revision++;
            var current = new PlayerGameplayInputBindingSummary(
                previous.SessionContextId,
                previous.PlayerSlotId,
                PlayerGameplayInputBindingState.ReleaseFailed,
                previous.Availability,
                previous.ActorProfileId,
                previous.ActorId,
                previous.Owner,
                previous.RuntimeContentIdentity,
                previous.PreparationToken,
                previous.OccupancyToken,
                previous.Token,
                previous.ActionMapName,
                previous.PreviousActionMapName,
                previous.PlayerInputName,
                previous.BindingRevision,
                source,
                reason,
                $"Gameplay input release failed. {issue}");
            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayInputBindingStatus.FailedRelease;
            lastOperationMessage = current.Message;
            return Result(
                lastOperationStatus,
                operation,
                playerSlotId,
                previous,
                current,
                false,
                false,
                issue,
                lastOperationMessage);
        }

        private PlayerGameplayInputBindingSummary GetSummaryOrDefault(
            PlayerSlotId playerSlotId)
        {
            return playerSlotId.IsValid &&
                slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary summary)
                ? summary
                : default;
        }

        private PlayerGameplayInputBindingResult Reject(
            PlayerGameplayInputBindingStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingSummary current,
            string message)
        {
            lastOperationStatus = status;
            lastOperationMessage = message;
            return Result(
                status,
                operation,
                playerSlotId,
                current,
                current,
                false,
                true,
                string.Empty,
                message);
        }

        private PlayerGameplayInputBindingResult Failure(
            PlayerGameplayInputBindingStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingSummary current,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            string message)
        {
            lastOperationStatus = status;
            lastOperationMessage = message;
            return Result(
                status,
                operation,
                playerSlotId,
                current,
                current,
                rollbackAttempted,
                rollbackSucceeded,
                rollbackMessage,
                message);
        }

        private PlayerGameplayInputBindingResult Result(
            PlayerGameplayInputBindingStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingSummary previous,
            PlayerGameplayInputBindingSummary current,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            string message)
        {
            return new PlayerGameplayInputBindingResult(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                CreateSnapshot(),
                rollbackAttempted,
                rollbackSucceeded,
                rollbackMessage,
                message);
        }
    }
}
