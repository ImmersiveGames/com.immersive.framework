using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority for inactive target-Activity Logical Player Actor candidates.
    /// It permits one candidate per Slot while the current P3J preparation remains untouched.
    /// Candidate promotion and gameplay-chain handoff are intentionally outside this authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7C concurrent target Activity Logical Player Actor candidate staging authority.")]
    internal sealed partial class PlayerActorCandidateStageRuntimeContext
    {
        private sealed class CandidateRecord
        {
            internal CandidateRecord(
                PlayerActorMaterializationHandle handle,
                PlayerActorCandidateStageSnapshot snapshot,
                LocalPlayerHostAuthoring host)
            {
                Handle = handle ?? throw new ArgumentNullException(nameof(handle));
                Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
                Host = host != null ? host : throw new ArgumentNullException(nameof(host));
            }

            internal PlayerActorMaterializationHandle Handle { get; }
            internal PlayerActorCandidateStageSnapshot Snapshot { get; set; }
            internal LocalPlayerHostAuthoring Host { get; }
        }

        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly AttachedPlayerActorMaterializationAdapter materializationAdapter;
        private readonly string sessionContextId;
        private readonly Dictionary<PlayerSlotId, CandidateRecord> records =
            new Dictionary<PlayerSlotId, CandidateRecord>();
        private readonly Dictionary<PlayerSlotId, PlayerActorCandidateStageToken>
            lastRolledBackTokens =
                new Dictionary<PlayerSlotId, PlayerActorCandidateStageToken>();

        private int revision = 1;
        private int candidateSequence;
        private PlayerActorCandidateStageStatus lastOperationStatus;
        private string lastOperationMessage =
            "Player Actor candidate staging runtime initialized.";

        private PlayerActorCandidateStageRuntimeContext(
            PlayerParticipationRuntimeContext participationContext,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            AttachedPlayerActorMaterializationAdapter materializationAdapter,
            string sessionContextId)
        {
            this.participationContext = participationContext;
            this.preparationModule = preparationModule;
            this.materializationAdapter = materializationAdapter;
            this.sessionContextId = sessionContextId;
        }

        internal string SessionContextId => sessionContextId;
        internal int Revision => revision;
        internal int CandidateCount => records.Count;

        internal static bool TryCreate(
            PlayerParticipationRuntimeContext participationContext,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            AttachedPlayerActorMaterializationAdapter materializationAdapter,
            out PlayerActorCandidateStageRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (participationContext == null)
            {
                issue = "Player Actor candidate staging requires Player participation authority.";
                return false;
            }

            if (preparationModule == null || !preparationModule.IsReady)
            {
                issue = "Player Actor candidate staging requires a ready P3J preparation host module.";
                return false;
            }

            if (materializationAdapter == null)
            {
                issue = "Player Actor candidate staging requires an attached materialization adapter.";
                return false;
            }

            PlayerParticipationSnapshot participation = participationContext.CreateSnapshot();
            if (participation == null ||
                !participation.IsInitialized ||
                string.IsNullOrEmpty(participation.ContextId))
            {
                issue = "Player Actor candidate staging requires an initialized participation snapshot.";
                return false;
            }

            if (!preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparation) ||
                preparation == null ||
                !preparation.IsInitialized)
            {
                issue = "Player Actor candidate staging requires initialized P3J preparation evidence.";
                return false;
            }

            if (!string.Equals(
                    participation.ContextId,
                    preparation.SessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    participation.ContextId,
                    materializationAdapter.SessionContextId,
                    StringComparison.Ordinal))
            {
                issue = "Participation, preparation and candidate materialization belong to different Session identities.";
                return false;
            }

            context = new PlayerActorCandidateStageRuntimeContext(
                participationContext,
                preparationModule,
                materializationAdapter,
                participation.ContextId);
            return true;
        }

        internal PlayerActorCandidateStageResult TryStage(
            RuntimeScopeContext targetActivityContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            const string Operation = "StageCandidate";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorCandidateStageRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "stage-target-activity-player-actor-candidate");

            PlayerActorCandidateStageSnapshot empty =
                PlayerActorCandidateStageSnapshot.Empty(
                    resolvedSource,
                    resolvedReason,
                    "No Player Actor candidate is staged.");

            if (!targetActivityContext.IsValid ||
                targetActivityContext.Owner.Scope != RuntimeContentScope.Activity ||
                !playerSlotId.IsValid)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedInvalidRequest,
                    Operation,
                    empty,
                    "Candidate staging requires a valid target Activity scope context and Player Slot identity.");
            }

            if (!participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsValid)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedSlotNotConfigured,
                    Operation,
                    empty,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session.");
            }

            if (!slot.IsJoined)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedSlotNotJoined,
                    Operation,
                    empty,
                    "Candidate staging requires a Joined Player Slot.");
            }

            if (!slot.HasSelectedActor || slot.SelectedActorProfile == null)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedActorSelectionMissing,
                    Operation,
                    empty,
                    "Candidate staging requires an explicit current ActorProfile selection.");
            }

            if (!slot.SelectedActorProfile.TryGetActorProfileId(
                    out ActorProfileId selectedActorProfileId,
                    out string actorProfileIssue))
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedActorSelectionMissing,
                    Operation,
                    empty,
                    actorProfileIssue);
            }

            if (!preparationModule.TryGetRegisteredHost(
                    playerSlotId,
                    out LocalPlayerHostAuthoring host,
                    out string hostIssue))
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedHostUnavailable,
                    Operation,
                    empty,
                    hostIssue);
            }

            if (!TryResolveCurrentPreparation(
                    playerSlotId,
                    out PlayerActorPreparationSummary currentPreparation,
                    out string preparationIssue))
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedRuntimeUnavailable,
                    Operation,
                    empty,
                    preparationIssue);
            }

            if (currentPreparation.HasMaterialization &&
                currentPreparation.Materialization.Owner == targetActivityContext.Owner)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedTargetOwnerMatchesCurrent,
                    Operation,
                    empty,
                    "Target candidate owner matches the current prepared Actor owner. A candidate must represent a distinct target Activity.");
            }

            if (records.TryGetValue(playerSlotId, out CandidateRecord existing))
            {
                if (IsIdempotentStage(
                        existing,
                        targetActivityContext.Owner,
                        selectedActorProfileId,
                        host))
                {
                    lastOperationStatus =
                        PlayerActorCandidateStageStatus.SucceededAlreadyStaged;
                    lastOperationMessage =
                        "The same target Activity Player Actor candidate is already staged inactive.";
                    return Result(
                        lastOperationStatus,
                        Operation,
                        existing.Snapshot,
                        existing.Snapshot,
                        lastOperationMessage);
                }

                return Reject(
                    PlayerActorCandidateStageStatus.RejectedAnotherCandidateActive,
                    Operation,
                    existing.Snapshot,
                    "Player Slot already has another staged or rollback-failed candidate. Roll it back before staging another target.");
            }

            PlayerActorMaterializationResult materialized =
                materializationAdapter.TryMaterialize(
                    targetActivityContext,
                    slot,
                    slot.SelectedActorProfile,
                    host,
                    resolvedSource,
                    resolvedReason);
            if (materialized == null ||
                !materialized.Succeeded ||
                materialized.Handle == null ||
                !materialized.Snapshot.IsStaged)
            {
                string message = materialized != null
                    ? materialized.ToDiagnosticString()
                    : "Candidate materialization returned no result.";
                return Reject(
                    PlayerActorCandidateStageStatus.FailedMaterialization,
                    Operation,
                    empty,
                    message);
            }

            candidateSequence++;
            PlayerActorMaterializationSnapshot candidateMaterialization =
                materialized.Snapshot;
            var token = new PlayerActorCandidateStageToken(
                sessionContextId,
                candidateMaterialization.Owner,
                playerSlotId,
                candidateMaterialization.ActorProfileId,
                candidateMaterialization.ActorId,
                candidateMaterialization.RuntimeContentIdentity,
                candidateSequence);
            var staged = new PlayerActorCandidateStageSnapshot(
                token,
                PlayerActorCandidateStageState.StagedInactive,
                candidateMaterialization,
                currentPreparation.Token,
                currentPreparation.Materialization.ActorId,
                currentPreparation.Materialization.Owner,
                resolvedSource,
                resolvedReason,
                "Target Activity Logical Player Actor candidate staged inactive while current preparation remains unchanged.");
            records.Add(
                playerSlotId,
                new CandidateRecord(materialized.Handle, staged, host));
            revision++;
            lastOperationStatus = PlayerActorCandidateStageStatus.SucceededStaged;
            lastOperationMessage = staged.Message;
            return Result(
                lastOperationStatus,
                Operation,
                empty,
                staged,
                lastOperationMessage);
        }

        internal PlayerActorCandidateStageResult TryRollback(
            PlayerActorCandidateStageToken expectedCandidate,
            string source,
            string reason)
        {
            const string Operation = "RollbackCandidate";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorCandidateStageRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "rollback-target-activity-player-actor-candidate");
            PlayerActorCandidateStageSnapshot empty =
                PlayerActorCandidateStageSnapshot.Empty(
                    resolvedSource,
                    resolvedReason,
                    "No Player Actor candidate is staged.");

            if (!expectedCandidate.IsValid ||
                !string.Equals(
                    expectedCandidate.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedForeignOrStaleCandidate,
                    Operation,
                    empty,
                    "Candidate rollback requires an exact current token from this Session.");
            }

            if (!records.TryGetValue(
                    expectedCandidate.PlayerSlotId,
                    out CandidateRecord record))
            {
                if (lastRolledBackTokens.TryGetValue(
                        expectedCandidate.PlayerSlotId,
                        out PlayerActorCandidateStageToken lastRolledBack) &&
                    lastRolledBack == expectedCandidate)
                {
                    lastOperationStatus =
                        PlayerActorCandidateStageStatus.SucceededAlreadyRolledBack;
                    lastOperationMessage = "Player Actor candidate is already rolled back.";
                    return Result(
                        lastOperationStatus,
                        Operation,
                        empty,
                        empty,
                        lastOperationMessage);
                }

                return Reject(
                    PlayerActorCandidateStageStatus.RejectedForeignOrStaleCandidate,
                    Operation,
                    empty,
                    "Candidate token is stale because no matching staged candidate exists.");
            }

            if (record.Snapshot.Token != expectedCandidate)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedForeignOrStaleCandidate,
                    Operation,
                    record.Snapshot,
                    "Candidate token is foreign or stale for the current staged candidate.");
            }

            if (record.Snapshot.IsPromoting)
            {
                return Reject(
                    PlayerActorCandidateStageStatus.RejectedPromotionInProgress,
                    Operation,
                    record.Snapshot,
                    "Candidate rollback is blocked while an exact gameplay handoff promotion owns the candidate.");
            }

            PlayerActorCandidateStageSnapshot previous = record.Snapshot;
            if (!materializationAdapter.TryRollbackMaterialization(
                    record.Handle,
                    resolvedSource,
                    resolvedReason,
                    out string rollbackIssue))
            {
                var failed = new PlayerActorCandidateStageSnapshot(
                    previous.Token,
                    PlayerActorCandidateStageState.RollbackFailed,
                    record.Handle.CreateSnapshot(),
                    previous.CurrentPreparationToken,
                    previous.CurrentActorId,
                    previous.CurrentOwner,
                    resolvedSource,
                    resolvedReason,
                    rollbackIssue);
                record.Snapshot = failed;
                revision++;
                lastOperationStatus = PlayerActorCandidateStageStatus.FailedRollback;
                lastOperationMessage = rollbackIssue;
                return Result(
                    lastOperationStatus,
                    Operation,
                    previous,
                    failed,
                    rollbackIssue);
            }

            records.Remove(expectedCandidate.PlayerSlotId);
            lastRolledBackTokens[expectedCandidate.PlayerSlotId] =
                expectedCandidate;
            revision++;
            var rolledBack = new PlayerActorCandidateStageSnapshot(
                previous.Token,
                PlayerActorCandidateStageState.RolledBack,
                record.Handle.CreateSnapshot(),
                previous.CurrentPreparationToken,
                previous.CurrentActorId,
                previous.CurrentOwner,
                resolvedSource,
                resolvedReason,
                "Target Activity Player Actor candidate rolled back without mutating the current preparation.");
            lastOperationStatus = PlayerActorCandidateStageStatus.SucceededRolledBack;
            lastOperationMessage = rolledBack.Message;
            return Result(
                lastOperationStatus,
                Operation,
                previous,
                rolledBack,
                lastOperationMessage);
        }

        internal bool TryRollbackAll(
            string source,
            string reason,
            out int rolledBackCount,
            out int failedCount,
            out string issue)
        {
            rolledBackCount = 0;
            failedCount = 0;
            issue = string.Empty;

            var tokens = new PlayerActorCandidateStageToken[records.Count];
            int index = 0;
            foreach (CandidateRecord record in records.Values)
            {
                tokens[index++] = record.Snapshot.Token;
            }

            var failures = new List<string>();
            for (index = 0; index < tokens.Length; index++)
            {
                PlayerActorCandidateStageResult result = TryRollback(
                    tokens[index],
                    source,
                    reason);
                if (result.Succeeded)
                {
                    rolledBackCount++;
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

        internal bool TryGetPhysicalEvidence(
            PlayerActorCandidateStageToken expectedCandidate,
            out LocalPlayerHostAuthoring host,
            out PlayerInput playerInput,
            out PlayerActorDeclaration declaration,
            out GameObject logicalActorHost,
            out string issue)
        {
            host = null;
            playerInput = null;
            declaration = null;
            logicalActorHost = null;
            issue = string.Empty;

            if (!expectedCandidate.IsValid ||
                !records.TryGetValue(
                    expectedCandidate.PlayerSlotId,
                    out CandidateRecord record) ||
                record.Snapshot.Token != expectedCandidate)
            {
                issue = "Candidate physical evidence requires the exact current staged token.";
                return false;
            }

            if (!record.Snapshot.IsStagedInactive ||
                record.Handle.State != PlayerActorMaterializationState.StagedInactive)
            {
                issue = "Candidate physical evidence is unavailable because the candidate is not staged inactive.";
                return false;
            }

            host = record.Host;
            playerInput = record.Handle.PlayerInput;
            declaration = record.Handle.PlayerActorDeclaration;
            logicalActorHost = record.Handle.LogicalActorHost;
            if (host == null ||
                playerInput == null ||
                declaration == null ||
                logicalActorHost == null)
            {
                issue = "Candidate physical evidence is incomplete.";
                return false;
            }

            return true;
        }

        internal PlayerActorCandidateRuntimeHostSnapshot CreateSnapshot()
        {
            var candidates = new PlayerActorCandidateStageSnapshot[records.Count];
            int index = 0;
            foreach (CandidateRecord record in records.Values)
            {
                candidates[index++] = record.Snapshot;
            }

            Array.Sort(
                candidates,
                (left, right) =>
                {
                    string leftText = left != null
                        ? left.Token.PlayerSlotId.StableText
                        : string.Empty;
                    string rightText = right != null
                        ? right.Token.PlayerSlotId.StableText
                        : string.Empty;
                    return string.CompareOrdinal(leftText, rightText);
                });
            return new PlayerActorCandidateRuntimeHostSnapshot(
                true,
                sessionContextId,
                revision,
                candidates,
                lastOperationStatus,
                lastOperationMessage);
        }

        private bool TryResolveCurrentPreparation(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary,
            out string issue)
        {
            summary = default;
            issue = string.Empty;

            if (!preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot hostSnapshot) ||
                hostSnapshot?.Preparation == null ||
                !hostSnapshot.Preparation.IsInitialized)
            {
                issue = "P3J preparation snapshot is unavailable for candidate staging.";
                return false;
            }

            IReadOnlyList<PlayerActorPreparationSummary> slots =
                hostSnapshot.Preparation.Slots;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].PlayerSlotId == playerSlotId)
                {
                    summary = slots[index];
                    if (!summary.IsValid)
                    {
                        issue =
                            $"P3J preparation evidence is invalid for Slot '{playerSlotId.StableText}'.";
                        return false;
                    }

                    return true;
                }
            }

            issue =
                $"P3J preparation snapshot has no configured Slot '{playerSlotId.StableText}'.";
            return false;
        }

        private static bool IsIdempotentStage(
            CandidateRecord record,
            RuntimeContentOwner targetOwner,
            ActorProfileId actorProfileId,
            LocalPlayerHostAuthoring host)
        {
            return record != null &&
                record.Snapshot.IsStagedInactive &&
                record.Snapshot.Token.Owner == targetOwner &&
                record.Snapshot.Token.ActorProfileId == actorProfileId &&
                ReferenceEquals(record.Host, host) &&
                record.Handle.State == PlayerActorMaterializationState.StagedInactive;
        }

        private PlayerActorCandidateStageResult Reject(
            PlayerActorCandidateStageStatus status,
            string operation,
            PlayerActorCandidateStageSnapshot current,
            string message)
        {
            lastOperationStatus = status;
            lastOperationMessage = message.NormalizeText();
            return Result(
                status,
                operation,
                current,
                current,
                lastOperationMessage);
        }

        private static PlayerActorCandidateStageResult Result(
            PlayerActorCandidateStageStatus status,
            string operation,
            PlayerActorCandidateStageSnapshot previous,
            PlayerActorCandidateStageSnapshot current,
            string message)
        {
            return new PlayerActorCandidateStageResult(
                status,
                operation,
                previous,
                current,
                message);
        }
    }
}
