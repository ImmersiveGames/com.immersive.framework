using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped synchronous authority for one reversible current-to-candidate
    /// Player gameplay chain handoff. It does not yield frames, mutate ActivityFlow,
    /// load scenes or execute visual transitions.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7D reversible P3J/P3K current-to-candidate gameplay handoff authority.")]
    internal sealed class PlayerGameplayChainHandoffRuntimeContext
    {
        private sealed class ChainEvidence
        {
            internal PlayerActorPreparationSummary Preparation;
            internal PlayerGameplayOccupancySummary Occupancy;
            internal PlayerGameplayInputBindingSummary Input;
            internal PlayerGameplayCameraEligibilitySummary Camera;
            internal PlayerGameplayAdmissionSummary Admission;
            internal bool OccupancyCreated;
            internal bool InputCreated;
            internal bool CameraCreated;
            internal bool AdmissionCreated;
        }

        private sealed class HandoffRecord
        {
            internal PlayerGameplayChainHandoffToken Token;
            internal PlayerGameplayChainHandoffSnapshot Snapshot;
            internal PlayerActorCandidatePromotionHandle Candidate;
            internal PlayerActorPreparationHandoff PreparationHandoff;
            internal PlayerActorPreparationSummary PreviousPreparation;
            internal PlayerGameplayAdmissionSummary PreviousAdmission;
            internal ChainEvidence CandidateChain;
            internal ChainEvidence RestoredChain;
        }

        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly PlayerActorCandidateRuntimeHostModule candidateModule;
        private readonly IPlayerGameplayChainHandoffEndpointSource endpointSource;
        private readonly PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext inputContext;
        private readonly PlayerGameplayCameraEligibilityRuntimeContext cameraContext;
        private readonly PlayerGameplayAdmissionRuntimeContext admissionContext;
        private readonly string sessionContextId;
        private readonly Dictionary<PlayerSlotId, HandoffRecord> active =
            new Dictionary<PlayerSlotId, HandoffRecord>();
        private readonly Dictionary<PlayerSlotId, PlayerGameplayChainHandoffSnapshot>
            committedHandoffs =
                new Dictionary<PlayerSlotId, PlayerGameplayChainHandoffSnapshot>();

        private int revision = 1;
        private int handoffSequence;
        private PlayerGameplayChainHandoffStatus lastOperationStatus;
        private string lastOperationMessage =
            "Player gameplay chain handoff runtime initialized.";

        private PlayerGameplayChainHandoffRuntimeContext(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerActorCandidateRuntimeHostModule candidateModule,
            IPlayerGameplayChainHandoffEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            string sessionContextId)
        {
            this.preparationModule = preparationModule;
            this.candidateModule = candidateModule;
            this.endpointSource = endpointSource;
            this.occupancyContext = occupancyContext;
            this.inputContext = inputContext;
            this.cameraContext = cameraContext;
            this.admissionContext = admissionContext;
            this.sessionContextId = sessionContextId;
        }

        internal string SessionContextId => sessionContextId;
        internal int Revision => revision;
        internal int ActiveHandoffCount => active.Count;

        internal static bool TryCreate(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerActorCandidateRuntimeHostModule candidateModule,
            IPlayerGameplayChainHandoffEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            out PlayerGameplayChainHandoffRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;
            if (preparationModule == null || !preparationModule.IsReady ||
                candidateModule == null || !candidateModule.IsReady ||
                endpointSource == null ||
                occupancyContext == null || inputContext == null ||
                cameraContext == null || admissionContext == null)
            {
                issue =
                    "Gameplay handoff requires ready preparation/candidate modules, endpoint source and all P3K authorities.";
                return false;
            }

            if (!preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationHost) ||
                preparationHost == null || !preparationHost.IsInitialized ||
                !candidateModule.TryGetSnapshot(
                    out PlayerActorCandidateRuntimeHostSnapshot candidateHost) ||
                candidateHost == null || !candidateHost.IsInitialized)
            {
                issue = "Gameplay handoff requires initialized P3J and candidate host snapshots.";
                return false;
            }

            PlayerGameplayOccupancySnapshot occupancy = occupancyContext.CreateSnapshot();
            PlayerGameplayInputBindingSnapshot input = inputContext.CreateSnapshot();
            PlayerGameplayCameraEligibilitySnapshot camera = cameraContext.CreateSnapshot();
            PlayerGameplayAdmissionSnapshot admission = admissionContext.CreateSnapshot();
            string session = preparationHost.SessionContextId;
            if (string.IsNullOrEmpty(session) ||
                !string.Equals(session, candidateHost.SessionContextId, StringComparison.Ordinal) ||
                occupancy == null || input == null || camera == null || admission == null ||
                !occupancy.IsInitialized || !input.IsInitialized ||
                !camera.IsInitialized || !admission.IsInitialized ||
                !string.Equals(session, occupancy.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, input.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, camera.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, admission.SessionContextId, StringComparison.Ordinal))
            {
                issue = "Gameplay handoff authorities belong to different or uninitialized Session identities.";
                return false;
            }

            context = new PlayerGameplayChainHandoffRuntimeContext(
                preparationModule,
                candidateModule,
                endpointSource,
                occupancyContext,
                inputContext,
                cameraContext,
                admissionContext,
                session);
            return true;
        }

        internal PlayerGameplayChainHandoffResult TryPromote(
            PlayerActorCandidateStageToken expectedCandidate,
            PlayerGameplayAdmissionToken expectedCurrentAdmission,
            string source,
            string reason)
        {
            const string Operation = "PromoteGameplayChain";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayChainHandoffRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "promote-target-activity-player-gameplay-chain");
            PlayerGameplayChainHandoffSnapshot empty =
                PlayerGameplayChainHandoffSnapshot.Empty(
                    resolvedSource,
                    resolvedReason,
                    "No gameplay handoff exists.");

            if (!expectedCandidate.IsValid ||
                !expectedCurrentAdmission.IsValid ||
                !string.Equals(
                    expectedCandidate.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    expectedCurrentAdmission.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                expectedCandidate.PlayerSlotId !=
                    expectedCurrentAdmission.PlayerSlotId)
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedInvalidRequest,
                    Operation,
                    empty,
                    empty,
                    "Gameplay handoff requires exact candidate and current admission tokens for one Session/Slot.");
            }

            PlayerSlotId slotId = expectedCandidate.PlayerSlotId;
            if (committedHandoffs.TryGetValue(
                    slotId,
                    out PlayerGameplayChainHandoffSnapshot committed) &&
                committed.Token.CandidateToken == expectedCandidate)
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.SucceededAlreadyCommitted,
                    Operation,
                    committed,
                    committed,
                    "Candidate gameplay handoff is already committed.");
            }

            if (active.ContainsKey(slotId))
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedHandoffAlreadyActive,
                    Operation,
                    empty,
                    active[slotId].Snapshot,
                    "Player Slot already has an active rollback or commit-cleanup handoff.");
            }

            if (!preparationModule.TryGetCurrentPreparation(
                    slotId,
                    out PlayerActorPreparationSummary previousPreparation,
                    out string preparationIssue) ||
                !previousPreparation.IsPrepared)
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedRuntimeUnavailable,
                    Operation,
                    empty,
                    empty,
                    preparationIssue);
            }

            if (!TryResolveCurrentAdmission(
                    slotId,
                    expectedCurrentAdmission,
                    out PlayerGameplayAdmissionSummary previousAdmission,
                    out string admissionIssue))
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedForeignOrStaleAdmission,
                    Operation,
                    empty,
                    empty,
                    admissionIssue);
            }

            if (!candidateModule.TryBeginCandidatePromotion(
                    expectedCandidate,
                    resolvedSource,
                    resolvedReason,
                    out PlayerActorCandidatePromotionHandle candidate,
                    out string candidateIssue))
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedForeignOrStaleCandidate,
                    Operation,
                    empty,
                    empty,
                    candidateIssue);
            }

            if (candidate.Snapshot.CurrentPreparationToken !=
                previousPreparation.Token)
            {
                candidate.TryCancel(
                    resolvedSource,
                    "candidate-current-preparation-mismatch",
                    out _);
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedForeignOrStaleCandidate,
                    Operation,
                    empty,
                    empty,
                    "Candidate was staged beside another current preparation token.");
            }

            handoffSequence++;
            var token = new PlayerGameplayChainHandoffToken(
                sessionContextId,
                slotId,
                expectedCandidate,
                previousPreparation.Token,
                previousAdmission.Token,
                handoffSequence);
            var record = new HandoffRecord
            {
                Token = token,
                Candidate = candidate,
                PreviousPreparation = previousPreparation,
                PreviousAdmission = previousAdmission,
                CandidateChain = new ChainEvidence(),
                RestoredChain = new ChainEvidence()
            };
            active.Add(slotId, record);

            PlayerGameplayAdmissionResult releasedCurrent =
                admissionContext.TryRelease(
                    slotId,
                    expectedCurrentAdmission,
                    resolvedSource,
                    "release-current-chain-before-candidate-cutover");
            if (!releasedCurrent.Succeeded)
            {
                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.RollbackFailed,
                    previousPreparation.Token,
                    previousAdmission.Token,
                    false,
                    false,
                    false,
                    false,
                    false,
                    true,
                    false,
                    resolvedSource,
                    resolvedReason,
                    releasedCurrent.ToDiagnosticString());
                revision++;
                return Result(
                    PlayerGameplayChainHandoffStatus.FailedCurrentChainRelease,
                    Operation,
                    empty,
                    record.Snapshot,
                    record.Snapshot.Message);
            }

            record.Snapshot = Snapshot(
                record,
                PlayerGameplayChainHandoffState.CurrentChainReleased,
                default,
                default,
                true,
                false,
                false,
                false,
                false,
                false,
                false,
                resolvedSource,
                resolvedReason,
                "Current gameplay chain released before candidate cutover.");

            if (!preparationModule.TryBeginCandidateHandoff(
                    candidate,
                    previousPreparation.Token,
                    resolvedSource,
                    resolvedReason,
                    out PlayerActorPreparationHandoff preparationHandoff,
                    out string handoffIssue))
            {
                bool restored = TryRestorePrevious(record, resolvedSource, resolvedReason, out string restoreIssue);
                if (restored)
                {
                    active.Remove(slotId);
                }
                return Result(
                    restored
                        ? PlayerGameplayChainHandoffStatus.FailedPreparationHandoff
                        : PlayerGameplayChainHandoffStatus.FailedRollback,
                    Operation,
                    empty,
                    record.Snapshot,
                    Join(handoffIssue, restoreIssue));
            }

            record.PreparationHandoff = preparationHandoff;
            record.Snapshot = Snapshot(
                record,
                PlayerGameplayChainHandoffState.CandidatePreparationCurrent,
                preparationHandoff.CurrentPreparation.Token,
                default,
                true,
                true,
                false,
                false,
                false,
                false,
                false,
                resolvedSource,
                resolvedReason,
                "Candidate is current P3J preparation; no frame was yielded during cutover.");

            if (!TryBuildChain(
                    preparationHandoff.CurrentPreparation,
                    record.CandidateChain,
                    resolvedSource,
                    resolvedReason,
                    out string candidateChainIssue))
            {
                bool rolledBack = TryRollbackRecord(
                    record,
                    resolvedSource,
                    resolvedReason,
                    out string rollbackIssue);
                if (rolledBack)
                {
                    active.Remove(slotId);
                }
                return Result(
                    rolledBack
                        ? PlayerGameplayChainHandoffStatus.FailedCandidateChain
                        : PlayerGameplayChainHandoffStatus.FailedRollback,
                    Operation,
                    empty,
                    record.Snapshot,
                    Join(candidateChainIssue, rollbackIssue));
            }

            record.Snapshot = Snapshot(
                record,
                PlayerGameplayChainHandoffState.CandidateChainReady,
                preparationHandoff.CurrentPreparation.Token,
                record.CandidateChain.Admission.Token,
                true,
                true,
                true,
                false,
                false,
                false,
                false,
                resolvedSource,
                resolvedReason,
                "Candidate P3K.2-P3K.5 gameplay chain is current and ready.");

            if (!preparationModule.TryCommitCandidateHandoff(
                    preparationHandoff,
                    resolvedSource,
                    resolvedReason,
                    out string commitIssue))
            {
                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.CommitCleanupFailed,
                    preparationHandoff.CurrentPreparation.Token,
                    record.CandidateChain.Admission.Token,
                    true,
                    true,
                    true,
                    preparationHandoff.CandidateOwnershipCompleted,
                    preparationHandoff.PreviousActorReleased,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    commitIssue);
                revision++;
                return Result(
                    PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease,
                    Operation,
                    empty,
                    record.Snapshot,
                    commitIssue);
            }

            record.Snapshot = CreateCommittedSnapshot(
                token,
                resolvedSource,
                resolvedReason,
                "Candidate promotion committed; previous Actor released and candidate chain is authoritative.",
                preparationHandoff.CurrentPreparation.Token,
                record.CandidateChain.Admission.Token);
            committedHandoffs[slotId] = record.Snapshot;
            active.Remove(slotId);
            revision++;
            return Result(
                PlayerGameplayChainHandoffStatus.SucceededCommitted,
                Operation,
                empty,
                record.Snapshot,
                record.Snapshot.Message);
        }

        internal PlayerGameplayChainHandoffResult TryRetryRollback(
            PlayerGameplayChainHandoffToken expectedHandoff,
            string source,
            string reason)
        {
            const string Operation = "RetryGameplayHandoffRollback";
            if (expectedHandoff.IsValid &&
                committedHandoffs.TryGetValue(
                    expectedHandoff.PlayerSlotId,
                    out PlayerGameplayChainHandoffSnapshot committed) &&
                committed.Token == expectedHandoff)
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedRollbackNotAvailable,
                    Operation,
                    committed,
                    committed,
                    "Committed gameplay handoff cannot rollback; Activity exit owns later release.");
            }

            if (!TryResolveActive(expectedHandoff, out HandoffRecord record, out string issue))
            {
                var empty = PlayerGameplayChainHandoffSnapshot.Empty(source, reason, issue);
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedForeignOrStaleHandoff,
                    Operation,
                    empty,
                    empty,
                    issue);
            }

            if (record.Snapshot.IsCommitCleanupFailed ||
                record.PreparationHandoff?.CandidateOwnershipCompleted == true)
            {
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedRollbackNotAvailable,
                    Operation,
                    record.Snapshot,
                    record.Snapshot,
                    "Gameplay handoff cannot rollback after candidate ownership completion; retry commit cleanup instead.");
            }

            bool rolledBack = TryRollbackRecord(record, source, reason, out issue);
            if (rolledBack)
            {
                active.Remove(expectedHandoff.PlayerSlotId);
            }
            return Result(
                rolledBack
                    ? PlayerGameplayChainHandoffStatus.SucceededRolledBack
                    : PlayerGameplayChainHandoffStatus.FailedRollback,
                Operation,
                record.Snapshot,
                record.Snapshot,
                issue);
        }

        internal PlayerGameplayChainHandoffResult TryRetryCommitCleanup(
            PlayerGameplayChainHandoffToken expectedHandoff,
            string source,
            string reason)
        {
            const string Operation = "RetryGameplayHandoffCommitCleanup";
            if (!TryResolveActive(expectedHandoff, out HandoffRecord record, out string issue) ||
                record.PreparationHandoff == null ||
                !record.Snapshot.IsCommitCleanupFailed)
            {
                var empty = PlayerGameplayChainHandoffSnapshot.Empty(source, reason, issue);
                return Result(
                    PlayerGameplayChainHandoffStatus.RejectedForeignOrStaleHandoff,
                    Operation,
                    empty,
                    empty,
                    string.IsNullOrEmpty(issue)
                        ? "Handoff is not waiting for commit cleanup."
                        : issue);
            }

            if (!preparationModule.TryCommitCandidateHandoff(
                    record.PreparationHandoff,
                    source,
                    reason,
                    out issue))
            {
                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.CommitCleanupFailed,
                    record.PreparationHandoff.CurrentPreparation.Token,
                    record.CandidateChain.Admission.Token,
                    true,
                    true,
                    true,
                    record.PreparationHandoff.CandidateOwnershipCompleted,
                    record.PreparationHandoff.PreviousActorReleased,
                    false,
                    false,
                    source,
                    reason,
                    issue);
                return Result(
                    PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease,
                    Operation,
                    record.Snapshot,
                    record.Snapshot,
                    issue);
            }

            record.Snapshot = CreateCommittedSnapshot(
                expectedHandoff,
                source,
                reason,
                "Deferred previous Actor cleanup completed.",
                record.PreparationHandoff.CurrentPreparation.Token,
                record.CandidateChain.Admission.Token);
            committedHandoffs[expectedHandoff.PlayerSlotId] = record.Snapshot;
            active.Remove(expectedHandoff.PlayerSlotId);
            revision++;
            return Result(
                PlayerGameplayChainHandoffStatus.SucceededCommitted,
                Operation,
                record.Snapshot,
                record.Snapshot,
                record.Snapshot.Message);
        }

        private bool TryRollbackRecord(
            HandoffRecord record,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            bool rollbackAttempted = true;

            if (!record.Snapshot.CurrentChainReleased)
            {
                PlayerGameplayAdmissionResult releaseCurrent =
                    admissionContext.TryRelease(
                        record.Token.PlayerSlotId,
                        record.PreviousAdmission.Token,
                        source,
                        "retry-current-chain-release-before-handoff-rollback");
                if (!releaseCurrent.Succeeded)
                {
                    issue = releaseCurrent.ToDiagnosticString();
                    record.Snapshot = Snapshot(
                        record,
                        PlayerGameplayChainHandoffState.RollbackFailed,
                        record.PreviousPreparation.Token,
                        record.PreviousAdmission.Token,
                        false,
                        false,
                        false,
                        false,
                        false,
                        rollbackAttempted,
                        false,
                        source,
                        reason,
                        issue);
                    return false;
                }

                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.CurrentChainReleased,
                    record.PreviousPreparation.Token,
                    default,
                    true,
                    false,
                    false,
                    false,
                    false,
                    rollbackAttempted,
                    false,
                    source,
                    reason,
                    "Current gameplay chain release completed during rollback retry.");
            }
            if (!TryReleaseChain(record.CandidateChain, source, reason, out string candidateReleaseIssue))
            {
                issue = candidateReleaseIssue;
                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.RollbackFailed,
                    record.PreparationHandoff != null
                        ? record.PreparationHandoff.CurrentPreparation.Token
                        : default,
                    default,
                    true,
                    record.PreparationHandoff != null,
                    false,
                    false,
                    false,
                    rollbackAttempted,
                    false,
                    source,
                    reason,
                    issue);
                return false;
            }

            if (record.PreparationHandoff != null)
            {
                if (!preparationModule.TryRollbackCandidateHandoff(
                        record.PreparationHandoff,
                        source,
                        reason,
                        out string preparationRollbackIssue))
                {
                    issue = preparationRollbackIssue;
                    record.Snapshot = Snapshot(
                        record,
                        PlayerGameplayChainHandoffState.RollbackFailed,
                        record.PreparationHandoff.CurrentPreparation.Token,
                        default,
                        true,
                        true,
                        false,
                        false,
                        false,
                        rollbackAttempted,
                        false,
                        source,
                        reason,
                        issue);
                    return false;
                }

                record.PreparationHandoff = null;
                record.Candidate = null;
            }
            else if (record.Candidate != null)
            {
                if (!record.Candidate.TryDeactivate(
                        source,
                        "deactivate-uncommitted-candidate-before-cancel",
                        out string deactivationIssue))
                {
                    issue = deactivationIssue;
                    return false;
                }

                if (!record.Candidate.TryCancel(
                        source,
                        reason,
                        out string cancellationIssue))
                {
                    issue = cancellationIssue;
                    return false;
                }

                record.Candidate = null;
            }

            if (!TryBuildChain(
                    record.PreviousPreparation,
                    record.RestoredChain,
                    source,
                    "restore-previous-player-gameplay-chain",
                    out string restoreIssue))
            {
                TryReleaseChain(record.RestoredChain, source, reason, out _);
                issue = restoreIssue;
                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.RollbackFailed,
                    record.PreviousPreparation.Token,
                    default,
                    true,
                    false,
                    false,
                    false,
                    false,
                    rollbackAttempted,
                    false,
                    source,
                    reason,
                    issue);
                return false;
            }

            record.Snapshot = Snapshot(
                record,
                PlayerGameplayChainHandoffState.RolledBack,
                record.PreviousPreparation.Token,
                record.RestoredChain.Admission.Token,
                false,
                false,
                false,
                false,
                false,
                rollbackAttempted,
                true,
                source,
                reason,
                "Candidate handoff rolled back; previous Actor and gameplay chain restored.");
            revision++;
            issue = record.Snapshot.Message;
            return true;
        }

        private bool TryRestorePrevious(
            HandoffRecord record,
            string source,
            string reason,
            out string issue)
        {
            if (record.Candidate != null)
            {
                if (!record.Candidate.TryDeactivate(
                        source,
                        "deactivate-rejected-candidate-before-cancel",
                        out string deactivationIssue))
                {
                    issue = deactivationIssue;
                    return false;
                }

                if (!record.Candidate.TryCancel(
                        source,
                        reason,
                        out string cancellationIssue))
                {
                    issue = cancellationIssue;
                    return false;
                }

                record.Candidate = null;
            }

            if (!TryBuildChain(
                    record.PreviousPreparation,
                    record.RestoredChain,
                    source,
                    "restore-current-chain-after-preparation-handoff-rejection",
                    out issue))
            {
                TryReleaseChain(record.RestoredChain, source, reason, out _);
                record.Snapshot = Snapshot(
                    record,
                    PlayerGameplayChainHandoffState.RollbackFailed,
                    record.PreviousPreparation.Token,
                    default,
                    true,
                    false,
                    false,
                    false,
                    false,
                    true,
                    false,
                    source,
                    reason,
                    issue);
                return false;
            }

            record.Snapshot = Snapshot(
                record,
                PlayerGameplayChainHandoffState.RolledBack,
                record.PreviousPreparation.Token,
                record.RestoredChain.Admission.Token,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                source,
                reason,
                "Previous gameplay chain restored after candidate preparation handoff rejection.");
            issue = record.Snapshot.Message;
            revision++;
            return true;
        }

        private bool TryBuildChain(
            PlayerActorPreparationSummary preparation,
            ChainEvidence chain,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            chain.Preparation = preparation;
            PlayerGameplayOccupancyResult occupancy =
                occupancyContext.TryConfirmOccupancy(preparation, source, reason);
            if (!occupancy.Succeeded)
            {
                issue = occupancy.ToDiagnosticString();
                return false;
            }
            chain.Occupancy = occupancy.CurrentSummary;
            chain.OccupancyCreated =
                !occupancy.PreviousSummary.IsOccupied &&
                occupancy.CurrentSummary.IsOccupied;

            if (!endpointSource.TryResolveGameplayEndpoints(
                    preparation,
                    out LocalPlayerHostAuthoring host,
                    out PlayerActorDeclaration actorDeclaration,
                    out UnityPlayerInputGateAdapter gateAdapter,
                    out PlayerGameplayCameraAuthoring cameraAuthoring,
                    out PlayerGameplayCameraRequiredness cameraRequiredness,
                    out CameraOutputSessionBinding outputSession,
                    out issue))
            {
                return false;
            }

            PlayerGameplayInputBindingResult input = inputContext.TryBind(
                preparation,
                chain.Occupancy,
                host,
                actorDeclaration,
                gateAdapter,
                source,
                reason);
            if (!input.Succeeded)
            {
                issue = input.ToDiagnosticString();
                return false;
            }
            chain.Input = input.CurrentSummary;
            chain.InputCreated =
                !input.PreviousSummary.IsBound && input.CurrentSummary.IsBound;

            PlayerGameplayCameraEligibilityResult camera;
            if (cameraAuthoring != null)
            {
                camera = cameraContext.TryConfirmEligibility(
                    preparation,
                    chain.Occupancy,
                    chain.Input,
                    actorDeclaration,
                    cameraAuthoring,
                    source,
                    reason);
            }
            else if (cameraRequiredness == PlayerGameplayCameraRequiredness.Optional)
            {
                camera = cameraContext.TrySkipOptional(
                    preparation,
                    chain.Occupancy,
                    chain.Input,
                    cameraRequiredness,
                    source,
                    reason);
            }
            else
            {
                issue = "Required Player camera has no explicit authoring endpoint during gameplay handoff.";
                return false;
            }

            if (!camera.Succeeded)
            {
                issue = camera.ToDiagnosticString();
                return false;
            }
            chain.Camera = camera.CurrentSummary;
            chain.CameraCreated =
                !camera.PreviousSummary.HasCurrentDecision &&
                camera.CurrentSummary.HasCurrentDecision;

            PlayerGameplayAdmissionResult admission = admissionContext.TryAdmit(
                chain.Occupancy,
                chain.Input,
                chain.Camera,
                outputSession,
                source,
                reason);
            if (!admission.Succeeded)
            {
                if (admission.RollbackAttempted && admission.RollbackSucceeded)
                {
                    chain.CameraCreated = false;
                    chain.InputCreated = false;
                    chain.OccupancyCreated = false;
                }
                else if (admission.CurrentSummary.IsAdmitted)
                {
                    chain.Admission = admission.CurrentSummary;
                    chain.AdmissionCreated = true;
                }
                issue = admission.ToDiagnosticString();
                return false;
            }

            chain.Admission = admission.CurrentSummary;
            chain.AdmissionCreated =
                !admission.PreviousSummary.IsAdmitted &&
                admission.CurrentSummary.IsAdmitted;
            return true;
        }

        private bool TryReleaseChain(
            ChainEvidence chain,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (chain == null)
            {
                return true;
            }

            if (chain.AdmissionCreated && chain.Admission.Token.IsValid)
            {
                PlayerGameplayAdmissionResult release = admissionContext.TryRelease(
                    chain.Admission.PlayerSlotId,
                    chain.Admission.Token,
                    source,
                    reason);
                if (!release.Succeeded)
                {
                    issue = release.ToDiagnosticString();
                    return false;
                }
                chain.AdmissionCreated = false;
                chain.CameraCreated = false;
                chain.InputCreated = false;
                chain.OccupancyCreated = false;
                return true;
            }

            if (chain.CameraCreated && chain.Camera.Token.IsValid)
            {
                PlayerGameplayCameraEligibilityResult release =
                    cameraContext.TryRelease(
                        chain.Camera.PlayerSlotId,
                        chain.Camera.Token,
                        source,
                        reason);
                if (!release.Succeeded)
                {
                    issue = release.ToDiagnosticString();
                    return false;
                }
                chain.CameraCreated = false;
            }

            if (chain.InputCreated && chain.Input.Token.IsValid)
            {
                PlayerGameplayInputBindingResult release = inputContext.TryRelease(
                    chain.Input.PlayerSlotId,
                    chain.Input.Token,
                    source,
                    reason);
                if (!release.Succeeded)
                {
                    issue = release.ToDiagnosticString();
                    return false;
                }
                chain.InputCreated = false;
            }

            if (chain.OccupancyCreated && chain.Occupancy.Token.IsValid)
            {
                PlayerGameplayOccupancyResult release =
                    occupancyContext.TryReleaseOccupancy(
                        chain.Occupancy.PlayerSlotId,
                        chain.Occupancy.Token,
                        source,
                        reason);
                if (!release.Succeeded)
                {
                    issue = release.ToDiagnosticString();
                    return false;
                }
                chain.OccupancyCreated = false;
            }

            return true;
        }

        private bool TryResolveCurrentAdmission(
            PlayerSlotId slotId,
            PlayerGameplayAdmissionToken expected,
            out PlayerGameplayAdmissionSummary admission,
            out string issue)
        {
            admission = default;
            PlayerGameplayAdmissionSnapshot snapshot = admissionContext.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized ||
                !snapshot.TryGetSummary(slotId, out admission) ||
                !admission.IsAdmitted || admission.IsReleaseFailed ||
                admission.Token != expected)
            {
                issue = "Current gameplay admission is missing, release-failed, foreign or stale.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private PlayerActorPreparationToken ResolveCurrentPreparationToken(
            PlayerSlotId slotId)
        {
            return preparationModule.TryGetCurrentPreparation(
                    slotId,
                    out PlayerActorPreparationSummary preparation,
                    out _) && preparation.IsPrepared
                ? preparation.Token
                : default;
        }

        private bool TryResolveActive(
            PlayerGameplayChainHandoffToken expected,
            out HandoffRecord record,
            out string issue)
        {
            record = null;
            if (!expected.IsValid ||
                !string.Equals(expected.SessionContextId, sessionContextId, StringComparison.Ordinal) ||
                !active.TryGetValue(expected.PlayerSlotId, out record) ||
                record.Token != expected)
            {
                record = null;
                issue = "Gameplay handoff token is foreign, stale or no longer active.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static PlayerGameplayChainHandoffSnapshot Snapshot(
            HandoffRecord record,
            PlayerGameplayChainHandoffState state,
            PlayerActorPreparationToken currentPreparation,
            PlayerGameplayAdmissionToken currentAdmission,
            bool currentChainReleased,
            bool preparationSwapped,
            bool candidateChainReady,
            bool candidateOwnershipCompleted,
            bool previousActorReleased,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string source,
            string reason,
            string message)
        {
            return new PlayerGameplayChainHandoffSnapshot(
                record.Token,
                state,
                currentPreparation,
                currentAdmission,
                currentChainReleased,
                preparationSwapped,
                candidateChainReady,
                candidateOwnershipCompleted,
                previousActorReleased,
                rollbackAttempted,
                rollbackSucceeded,
                source,
                reason,
                message);
        }

        private static PlayerGameplayChainHandoffSnapshot CreateCommittedSnapshot(
            PlayerGameplayChainHandoffToken token,
            string source,
            string reason,
            string message,
            PlayerActorPreparationToken currentPreparation = default,
            PlayerGameplayAdmissionToken currentAdmission = default)
        {
            return new PlayerGameplayChainHandoffSnapshot(
                token,
                PlayerGameplayChainHandoffState.Committed,
                currentPreparation,
                currentAdmission,
                true,
                true,
                true,
                true,
                true,
                false,
                false,
                source,
                reason,
                message);
        }

        private PlayerGameplayChainHandoffResult Result(
            PlayerGameplayChainHandoffStatus status,
            string operation,
            PlayerGameplayChainHandoffSnapshot previous,
            PlayerGameplayChainHandoffSnapshot current,
            string message)
        {
            lastOperationStatus = status;
            lastOperationMessage = message.NormalizeText();
            return new PlayerGameplayChainHandoffResult(
                status,
                operation,
                previous,
                current,
                lastOperationMessage);
        }

        private static string Join(string left, string right)
        {
            string a = left.NormalizeText();
            string b = right.NormalizeText();
            if (string.IsNullOrEmpty(a)) return b;
            if (string.IsNullOrEmpty(b)) return a;
            return a + " " + b;
        }
    }
}
