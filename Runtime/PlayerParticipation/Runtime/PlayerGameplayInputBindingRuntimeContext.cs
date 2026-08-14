using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
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
            internal UnityPlayerInputActionMapWriteReceipt ReleaseActionMapWrite;
        }

        private readonly string sessionContextId;
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
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
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerSlotId[] orderedSlots)
        {
            this.sessionContextId = sessionContextId;
            this.preparationModule = preparationModule;
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
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            out PlayerGameplayInputBindingRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (preparationModule == null || !preparationModule.IsReady)
            {
                issue =
                    "Gameplay input binding requires the current Actor preparation authority.";
                return false;
            }

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
                preparationModule,
                occupancyContext,
                ordered);
            return true;
        }

        internal PlayerGameplayInputBindingResult TryBind(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            RuntimeContentOwner contextualOwner,
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

            if (!requestedSlot.IsValid || !contextualOwner.IsValid ||
                contextualOwner.Scope != RuntimeContentScope.Activity ||
                !preparation.IsValid ||
                !occupancy.IsValid)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedInvalidRequest,
                    Operation,
                    requestedSlot,
                    GetSummaryOrDefault(requestedSlot),
                    "Gameplay input binding requires a valid Activity owner, preparation and occupancy evidence.");
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

            PlayerCurrentActorEvidenceResult actorConfirmation =
                preparationModule.ConfirmCurrentActorEvidence(
                    requestedSlot,
                    preparation.Token,
                    resolvedSource,
                    resolvedReason);
            if (actorConfirmation == null || !actorConfirmation.Succeeded)
            {
                return Reject(
                    ActorConfirmationFailureStatus(actorConfirmation),
                    Operation,
                    requestedSlot,
                    previous,
                    actorConfirmation != null
                        ? actorConfirmation.ToDiagnosticString()
                        : "Current Actor evidence confirmation returned no result.");
            }

            PlayerActorCorrelationEvidence actorEvidence =
                actorConfirmation.RetainedEvidence;
            preparation = actorConfirmation.Preparation;
            if (!TryGetCurrentContextualHostEvidence(
                    requestedSlot,
                    resolvedSource,
                    resolvedReason,
                    out PlayerHostEvidenceSnapshot contextualHostEvidence,
                    out string contextualHostIssue))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedHostMismatch,
                    Operation,
                    requestedSlot,
                    previous,
                    contextualHostIssue);
            }
            if (!preparationModule.TryGetPreparedPhysicalEvidence(
                    requestedSlot,
                    preparation.Token,
                    out LocalPlayerHostAuthoring currentHost,
                    out PlayerInput currentPlayerInput,
                    out PlayerActorDeclaration currentActorDeclaration,
                    out _,
                    out string physicalIssue))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedPhysicalBindingDivergence,
                    Operation,
                    requestedSlot,
                    previous,
                    physicalIssue);
            }

            if (!ReferenceEquals(host, currentHost) ||
                !ReferenceEquals(actorDeclaration, currentActorDeclaration))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedPhysicalBindingDivergence,
                    Operation,
                    requestedSlot,
                    previous,
                    "Supplied Host or Actor declaration is not the exact current CPSA-3 physical evidence.");
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

            if (!ReferenceEquals(playerInput, currentPlayerInput))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedPlayerInputMismatch,
                    Operation,
                    requestedSlot,
                    previous,
                    "Supplied PlayerInput is not the exact current CPSA-3 physical PlayerInput evidence.");
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

            if (previous.IsBound || previous.IsReleaseFailed || previous.IsDivergent)
            {
                BindingRecord existing = null;
                bool sameStructuralEvidence =
                    previous.Owner == contextualOwner &&
                    previous.AssignmentToken == contextualHostEvidence.AssignmentToken &&
                    previous.HostBindingIdentity ==
                        contextualHostEvidence.HostBindingIdentity &&
                    previous.PreparationToken == preparation.Token &&
                    previous.ActorId == occupancy.ActorId &&
                    records.TryGetValue(
                        requestedSlot,
                        out existing) &&
                    ReferenceEquals(existing.Host, host) &&
                    ReferenceEquals(
                        existing.ActorDeclaration,
                        actorDeclaration) &&
                    ReferenceEquals(existing.PlayerInput, playerInput) &&
                    ReferenceEquals(existing.GateAdapter, gateAdapter);
                if (sameStructuralEvidence &&
                    previous.ActionMapName == actionMapName)
                {
                    gateAdapter.ApplyCurrentGate();
                    PlayerGameplayInputBindingSummary refreshed =
                        RefreshSummaryAvailability(
                            previous,
                            gateAdapter,
                            resolvedSource,
                            resolvedReason,
                            "Gameplay input binding is already current.",
                            PlayerGameplayInputBindingState.Bound);
                    if (refreshed.State != previous.State ||
                        refreshed.Availability != previous.Availability)
                    {
                        revision++;
                    }
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

                if (sameStructuralEvidence &&
                    previous.ActionMapName != actionMapName &&
                    !previous.IsReleaseFailed)
                {
                    return TryReconfigureDesiredActionMap(
                        requestedSlot,
                        previous,
                        existing,
                        actorEvidence,
                        preparation,
                        occupancy,
                        contextualOwner,
                        actionMapName,
                        resolvedSource,
                        resolvedReason);
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
                contextualOwner,
                requestedSlot,
                contextualHostEvidence.AssignmentToken,
                contextualHostEvidence.HostBindingIdentity,
                preparation.Token,
                bindingSequence);
            PlayerGameplayInputAvailability availability =
                ResolveAvailability(
                    playerInput,
                    gateAdapter,
                    actionMapName);
            var current = new PlayerGameplayInputBindingSummary(
                sessionContextId,
                requestedSlot,
                PlayerGameplayInputBindingState.Bound,
                availability,
                contextualHostEvidence.AssignmentToken,
                contextualHostEvidence.HostBindingIdentity,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                contextualOwner,
                occupancy.RuntimeContentIdentity,
                preparation.Token,
                occupancy.Token,
                token,
                actionMapName,
                CurrentActionMapName(playerInput),
                actionMapWrite.PreviousActionMapName,
                playerInput.name,
                bindingSequence,
                1,
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
                    ReleaseActionMapWrite = actionMapWrite
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

        private PlayerGameplayInputBindingResult
            TryReconfigureDesiredActionMap(
                PlayerSlotId playerSlotId,
                PlayerGameplayInputBindingSummary previous,
                BindingRecord record,
            PlayerActorCorrelationEvidence actorEvidence,
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            RuntimeContentOwner contextualOwner,
            string desiredActionMapName,
                string source,
                string reason)
        {
            const string Operation = "ReconfigureGameplayInputActionMap";
            if (!TryGetCurrentContextualHostEvidence(
                    playerSlotId,
                    source,
                    reason,
                    out PlayerHostEvidenceSnapshot contextualHostEvidence,
                    out string contextualHostIssue))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedHostMismatch,
                    Operation,
                    playerSlotId,
                    previous,
                    contextualHostIssue);
            }

            if (record.GateAdapter == null ||
                record.GateAdapter.IsBlockedByAdapter)
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedGateAdapterMismatch,
                    Operation,
                    playerSlotId,
                    previous,
                    "Desired action-map reconfiguration requires the exact Gate adapter in an unblocked state.");
            }

            if (!record.GateAdapter.TrySelectActionMap(
                    desiredActionMapName,
                    source,
                    reason,
                    out UnityPlayerInputActionMapWriteReceipt newWrite,
                    out string activationIssue))
            {
                bool rollbackSucceeded =
                    record.GateAdapter.TrySelectActionMap(
                        previous.DesiredActionMapName,
                        source,
                        "desired-action-map-reconfiguration-rollback",
                        out _,
                        out string rollbackIssue);

                return rollbackSucceeded
                    ? Failure(
                        PlayerGameplayInputBindingStatus
                            .FailedActionMapActivation,
                        Operation,
                        playerSlotId,
                        previous,
                        true,
                        true,
                        string.Empty,
                        $"Desired action map '{desiredActionMapName}' could not be activated. {activationIssue}")
                    : MarkReleaseFailed(
                        Operation,
                        playerSlotId,
                        previous,
                        source,
                        reason,
                        $"{activationIssue} Rollback failed. {rollbackIssue}");
            }

            try
            {
                record.GateAdapter.ApplyCurrentGate();
            }
            catch (Exception exception)
            {
                bool restoredNewWrite =
                    record.GateAdapter.TryRestoreActionMap(
                        newWrite,
                        source,
                        "desired-action-map-gate-application-rollback",
                        out string restoreNewIssue);
                string rollbackIssue = string.Empty;
                bool reboundPrevious =
                    restoredNewWrite &&
                    record.GateAdapter.TrySelectActionMap(
                        previous.DesiredActionMapName,
                        source,
                        "desired-action-map-reconfiguration-rollback",
                        out _,
                        out rollbackIssue);
                if (reboundPrevious)
                {
                    return Failure(
                        PlayerGameplayInputBindingStatus
                            .FailedActionMapActivation,
                        Operation,
                        playerSlotId,
                        previous,
                        true,
                        true,
                        string.Empty,
                        $"The desired action map was selected, but its Gate state could not be materialized. {exception.Message}");
                }

                string rollbackDiagnostic =
                    restoredNewWrite
                        ? rollbackIssue
                        : restoreNewIssue;
                return MarkReleaseFailed(
                    Operation,
                    playerSlotId,
                    previous,
                    source,
                    reason,
                    $"The desired action map Gate materialization failed and rollback could not restore the previous binding. {exception.Message} {rollbackDiagnostic}");
            }

            bindingSequence++;
            revision++;
            var token = new PlayerGameplayInputBindingToken(
                sessionContextId,
                contextualOwner,
                playerSlotId,
                contextualHostEvidence.AssignmentToken,
                contextualHostEvidence.HostBindingIdentity,
                preparation.Token,
                bindingSequence);
            var current = new PlayerGameplayInputBindingSummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayInputBindingState.Bound,
                ResolveAvailability(
                    record.PlayerInput,
                    record.GateAdapter,
                    desiredActionMapName),
                contextualHostEvidence.AssignmentToken,
                contextualHostEvidence.HostBindingIdentity,
                actorEvidence.ActorProfileId,
                actorEvidence.ActorId,
                contextualOwner,
                actorEvidence.RuntimeContentIdentity,
                preparation.Token,
                occupancy.Token,
                token,
                desiredActionMapName,
                CurrentActionMapName(record.PlayerInput),
                newWrite.PreviousActionMapName,
                record.PlayerInput.name,
                bindingSequence,
                1,
                source,
                reason,
                "Desired gameplay action map was explicitly reconfigured with a new Input binding identity.");
            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayInputBindingStatus.SucceededBound;
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

        internal bool TryGetCurrentInputBinding(
            PlayerSlotId playerSlotId,
            out PlayerGameplayInputBindingSummary summary,
            out PlayerGameplayInputBindingResult confirmation)
        {
            if (!playerSlotId.IsValid ||
                !slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary retained))
            {
                confirmation = Reject(
                    PlayerGameplayInputBindingStatus.RejectedInvalidRequest,
                    "LookupCurrentInputBinding",
                    playerSlotId,
                    default,
                    "Current Input binding lookup requires a configured Player Slot.");
                summary = default;
                return false;
            }

            confirmation = ConfirmCurrentInputBinding(
                playerSlotId,
                retained.Token,
                nameof(PlayerGameplayInputBindingRuntimeContext),
                "lookup-current-input-binding");
            summary = confirmation != null && confirmation.Succeeded
                ? confirmation.CurrentSummary
                : default;
            return confirmation != null && confirmation.Succeeded;
        }

        internal bool TryGetRetainedInputBinding(
            PlayerSlotId playerSlotId,
            out PlayerGameplayInputBindingSummary summary)
        {
            if (playerSlotId.IsValid &&
                slots.TryGetValue(playerSlotId, out summary) &&
                !summary.IsUnbound &&
                records.ContainsKey(playerSlotId))
            {
                return true;
            }

            summary = default;
            return false;
        }

        internal PlayerGameplayInputBindingResult ConfirmCurrentInputBinding(
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken expectedBinding,
            string source,
            string reason)
        {
            const string Operation = "ConfirmCurrentInputBinding";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayInputBindingRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "confirm-current-input-binding");

            if (!playerSlotId.IsValid ||
                !slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary previous))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus.RejectedInvalidRequest,
                    Operation,
                    playerSlotId,
                    default,
                    "Current Input binding confirmation requires a configured Player Slot.");
            }

            if (previous.IsUnbound ||
                !expectedBinding.IsValid ||
                expectedBinding.PlayerSlotId != playerSlotId ||
                expectedBinding != previous.Token ||
                !records.TryGetValue(playerSlotId, out BindingRecord record))
            {
                return Reject(
                    PlayerGameplayInputBindingStatus
                        .RejectedForeignOrStaleBinding,
                    Operation,
                    playerSlotId,
                    previous,
                    "Current Input binding confirmation requires the exact retained binding evidence.");
            }

            PlayerCurrentActorEvidenceResult actorConfirmation =
                preparationModule.ConfirmCurrentActorEvidence(
                    playerSlotId,
                    previous.PreparationToken,
                    resolvedSource,
                    resolvedReason);
            if (actorConfirmation == null || !actorConfirmation.Succeeded)
            {
                return MarkDivergent(
                    ActorConfirmationFailureStatus(actorConfirmation),
                    Operation,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    actorConfirmation != null
                        ? actorConfirmation.ToDiagnosticString()
                        : "Current Actor evidence confirmation returned no result.");
            }

            if (!TryGetCurrentContextualHostEvidence(
                    playerSlotId,
                    resolvedSource,
                    resolvedReason,
                    out PlayerHostEvidenceSnapshot contextualHostEvidence,
                    out string contextualHostIssue))
            {
                return MarkDivergent(
                    PlayerGameplayInputBindingStatus.RejectedHostDivergence,
                    Operation,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    contextualHostIssue);
            }

            if (contextualHostEvidence.AssignmentToken != previous.AssignmentToken)
            {
                return MarkDivergent(
                    PlayerGameplayInputBindingStatus
                        .RejectedAssignmentDivergence,
                    Operation,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    "Current assignment differs from the retained Input binding.");
            }

            if (contextualHostEvidence.HostBindingIdentity != previous.HostBindingIdentity)
            {
                return MarkDivergent(
                    PlayerGameplayInputBindingStatus.RejectedHostDivergence,
                    Operation,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    "Current Host binding identity differs from retained Input evidence.");
            }

            PlayerActorCorrelationEvidence evidence = actorConfirmation.RetainedEvidence;
            if (evidence.PreparationToken != previous.PreparationToken ||
                evidence.ActorProfileId != previous.ActorProfileId ||
                evidence.ActorId != previous.ActorId ||
                evidence.RuntimeContentIdentity != previous.RuntimeContentIdentity)
            {
                return MarkDivergent(
                    PlayerGameplayInputBindingStatus.RejectedActorDivergence,
                    Operation,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    "Current Actor correlation differs from retained Input evidence.");
            }

            if (!preparationModule.TryGetPreparedPhysicalEvidence(
                    playerSlotId,
                    previous.PreparationToken,
                    out LocalPlayerHostAuthoring currentHost,
                    out PlayerInput currentPlayerInput,
                    out PlayerActorDeclaration currentDeclaration,
                    out _,
                    out string physicalIssue) ||
                !ReferenceEquals(record.Host, currentHost) ||
                !ReferenceEquals(record.PlayerInput, currentPlayerInput) ||
                !ReferenceEquals(record.ActorDeclaration, currentDeclaration) ||
                record.GateAdapter == null ||
                !ReferenceEquals(
                    record.GateAdapter.PlayerInput,
                    currentPlayerInput))
            {
                return MarkDivergent(
                    PlayerGameplayInputBindingStatus
                        .RejectedPhysicalBindingDivergence,
                    Operation,
                    previous,
                    resolvedSource,
                    resolvedReason,
                    string.IsNullOrWhiteSpace(physicalIssue)
                        ? "Retained Input physical evidence differs from the current Actor and Host."
                        : physicalIssue);
            }

            PlayerGameplayInputBindingSummary current =
                RefreshSummaryAvailability(
                    previous,
                    record.GateAdapter,
                    resolvedSource,
                    resolvedReason,
                    "Input binding is current; availability was observed without changing binding identity.",
                    PlayerGameplayInputBindingState.Bound);
            if (current.State != previous.State ||
                current.Availability != previous.Availability)
            {
                revision++;
            }

            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayInputBindingStatus.SucceededConfirmedCurrent;
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

            if (record.GateAdapter != null)
            {
                record.GateAdapter.ApplyCurrentGate();
            }
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
                    AvailabilityMessage(
                        ResolveAvailability(
                            record.PlayerInput,
                            record.GateAdapter,
                            previous.DesiredActionMapName)));

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
                        record.ReleaseActionMapWrite,
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
                if (!summary.IsBound &&
                    !summary.IsReleaseFailed &&
                    !summary.IsDivergent)
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
                string message,
                PlayerGameplayInputBindingState? state = null)
        {
            PlayerGameplayInputAvailability availability =
                ResolveAvailability(
                    gateAdapter != null ? gateAdapter.PlayerInput : null,
                    gateAdapter,
                    previous.DesiredActionMapName);
            return new PlayerGameplayInputBindingSummary(
                previous.SessionContextId,
                previous.PlayerSlotId,
                state ?? previous.State,
                availability,
                previous.AssignmentToken,
                previous.HostBindingIdentity,
                previous.ActorProfileId,
                previous.ActorId,
                previous.Owner,
                previous.RuntimeContentIdentity,
                previous.PreparationToken,
                previous.OccupancyToken,
                previous.Token,
                previous.ActionMapName,
                CurrentActionMapName(
                    gateAdapter != null ? gateAdapter.PlayerInput : null),
                previous.PreviousActionMapName,
                previous.PlayerInputName,
                previous.BindingRevision,
                availability != previous.Availability
                    ? previous.AvailabilityRevision + 1
                    : previous.AvailabilityRevision,
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
                previous.AssignmentToken,
                previous.HostBindingIdentity,
                previous.ActorProfileId,
                previous.ActorId,
                previous.Owner,
                previous.RuntimeContentIdentity,
                previous.PreparationToken,
                previous.OccupancyToken,
                previous.Token,
                previous.ActionMapName,
                previous.CurrentActionMapName,
                previous.PreviousActionMapName,
                previous.PlayerInputName,
                previous.BindingRevision,
                previous.AvailabilityRevision,
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

        private PlayerGameplayInputBindingResult MarkDivergent(
            PlayerGameplayInputBindingStatus status,
            string operation,
            PlayerGameplayInputBindingSummary previous,
            string source,
            string reason,
            string issue)
        {
            revision++;
            PlayerGameplayInputAvailability availability =
                records.TryGetValue(
                    previous.PlayerSlotId,
                    out BindingRecord record)
                    ? ResolveAvailability(
                        record.PlayerInput,
                        record.GateAdapter,
                        previous.DesiredActionMapName)
                    : PlayerGameplayInputAvailability.GateUnavailable;
            var current = new PlayerGameplayInputBindingSummary(
                previous.SessionContextId,
                previous.PlayerSlotId,
                PlayerGameplayInputBindingState.Divergent,
                availability,
                previous.AssignmentToken,
                previous.HostBindingIdentity,
                previous.ActorProfileId,
                previous.ActorId,
                previous.Owner,
                previous.RuntimeContentIdentity,
                previous.PreparationToken,
                previous.OccupancyToken,
                previous.Token,
                previous.ActionMapName,
                previous.CurrentActionMapName,
                previous.PreviousActionMapName,
                previous.PlayerInputName,
                previous.BindingRevision,
                availability != previous.Availability
                    ? previous.AvailabilityRevision + 1
                    : previous.AvailabilityRevision,
                source,
                reason,
                $"Retained Input binding is divergent and cannot be used as current. {issue}");
            slots[previous.PlayerSlotId] = current;
            lastOperationStatus = status;
            lastOperationMessage = current.Message;
            return Result(
                status,
                operation,
                previous.PlayerSlotId,
                previous,
                current,
                false,
                true,
                string.Empty,
                lastOperationMessage);
        }

        private static PlayerGameplayInputBindingStatus
            ActorConfirmationFailureStatus(
                PlayerCurrentActorEvidenceResult confirmation)
        {
            if (confirmation == null)
            {
                return PlayerGameplayInputBindingStatus
                    .RejectedActorDivergence;
            }

            return confirmation.Status switch
            {
                PlayerCurrentActorEvidenceStatus
                    .RejectedAssignmentDivergence =>
                    PlayerGameplayInputBindingStatus
                        .RejectedAssignmentDivergence,
                PlayerCurrentActorEvidenceStatus
                    .RejectedHostDivergence =>
                    PlayerGameplayInputBindingStatus
                        .RejectedHostDivergence,
                _ => PlayerGameplayInputBindingStatus
                    .RejectedActorDivergence
            };
        }

        private static PlayerGameplayInputAvailability ResolveAvailability(
            PlayerInput playerInput,
            UnityPlayerInputGateAdapter gateAdapter,
            string desiredActionMapName)
        {
            if (gateAdapter == null ||
                playerInput == null ||
                !ReferenceEquals(gateAdapter.PlayerInput, playerInput))
            {
                return PlayerGameplayInputAvailability.GateUnavailable;
            }

            if (!playerInput.enabled)
            {
                return PlayerGameplayInputAvailability.PlayerInputDisabled;
            }

            if (gateAdapter.IsBlockedByAdapter)
            {
                return PlayerGameplayInputAvailability.BlockedByGate;
            }

            if (playerInput.actions == null)
            {
                return PlayerGameplayInputAvailability.ActionsUnavailable;
            }

            InputActionMap desiredMap = playerInput.actions.FindActionMap(
                desiredActionMapName.NormalizeText(),
                throwIfNotFound: false);
            return desiredMap != null && desiredMap.enabled
                ? PlayerGameplayInputAvailability.Allowed
                : PlayerGameplayInputAvailability.ActionsUnavailable;
        }

        private static string CurrentActionMapName(PlayerInput playerInput)
        {
            return playerInput != null &&
                playerInput.currentActionMap != null
                    ? playerInput.currentActionMap.name.NormalizeText()
                    : string.Empty;
        }

        private static string AvailabilityMessage(
            PlayerGameplayInputAvailability availability)
        {
            return availability switch
            {
                PlayerGameplayInputAvailability.Allowed =>
                    "Gameplay input is currently allowed.",
                PlayerGameplayInputAvailability.BlockedByGate =>
                    "Gameplay input is currently blocked by the Gate.",
                PlayerGameplayInputAvailability.PlayerInputDisabled =>
                    "Gameplay input binding is retained while PlayerInput is disabled.",
                PlayerGameplayInputAvailability.ActionsUnavailable =>
                    "Gameplay input binding is retained while the desired action map is unavailable.",
                PlayerGameplayInputAvailability.GateUnavailable =>
                    "Gameplay input binding is retained while its Gate adapter is unavailable.",
                _ => "Gameplay input availability is unknown."
            };
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

        private bool TryGetCurrentContextualHostEvidence(
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out PlayerHostEvidenceSnapshot evidence,
            out string issue)
        {
            evidence = default;
            issue = string.Empty;
            PlayerHostEvidenceResult confirmation = preparationModule.ConfirmHostEvidence(
                playerSlotId,
                source,
                reason + "; confirm-contextual-host-evidence");
            if (confirmation == null || !confirmation.Succeeded ||
                !confirmation.CurrentEvidence.IsRecorded)
            {
                issue = confirmation != null
                    ? confirmation.ToDiagnosticString()
                    : "Current contextual Host evidence confirmation returned no result.";
                return false;
            }

            evidence = confirmation.CurrentEvidence;
            return true;
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
