using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped coordinator for one ordered reversible Activity Player handoff group.
    /// It keeps every per-Slot P3K.7D handoff rollbackable until all Slots and the P3K.6
    /// Activity admission evaluation are ready, then crosses the commit boundary as a group.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal,
        "P3K.7E reversible multi-Slot Activity Player handoff group authority.")]
    internal sealed class ActivityPlayerHandoffGroupRuntimeContext
    {
        private sealed class SlotRecord
        {
            internal ActivityPlayerHandoffSlotRequest Request;
            internal PlayerGameplayChainHandoffToken HandoffToken;
            internal PlayerGameplayChainHandoffStatus LastStatus;
            internal bool Began;
            internal bool Committed;
            internal bool RolledBack;
            internal bool CleanupPending;
            internal string Message;
        }

        private sealed class GroupRecord
        {
            internal ActivityAsset Activity;
            internal RuntimeContentOwner TargetOwner;
            internal ActivityPlayerHandoffGroupToken Token;
            internal ActivityPlayerHandoffGroupState State;
            internal ActivityPlayerAdmissionFlowDecision Decision;
            internal List<SlotRecord> Slots;
            internal string Source;
            internal string Reason;
            internal string Message;
        }

        private readonly IPlayerGameplayChainPromotionRuntime promotionRuntime;
        private readonly IActivityPlayerHandoffEvidenceSource evidenceSource;
        private readonly ActivityPlayerAdmissionFlowGate admissionGate =
            new ActivityPlayerAdmissionFlowGate();
        private GroupRecord active;
        private ActivityPlayerHandoffGroupSnapshot committed;
        private int groupSequence;
        private int revision = 1;

        private ActivityPlayerHandoffGroupRuntimeContext(
            IPlayerGameplayChainPromotionRuntime promotionRuntime,
            IActivityPlayerHandoffEvidenceSource evidenceSource)
        {
            this.promotionRuntime = promotionRuntime;
            this.evidenceSource = evidenceSource;
        }

        internal int Revision => revision;
        internal bool HasActiveGroup => active != null;

        internal static bool TryCreate(
            IPlayerGameplayChainPromotionRuntime promotionRuntime,
            IActivityPlayerHandoffEvidenceSource evidenceSource,
            out ActivityPlayerHandoffGroupRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;
            if (promotionRuntime == null || evidenceSource == null)
            {
                issue = "Activity Player handoff group requires explicit promotion and evidence authorities.";
                return false;
            }
            context = new ActivityPlayerHandoffGroupRuntimeContext(
                promotionRuntime, evidenceSource);
            return true;
        }

        internal ActivityPlayerHandoffGroupResult TryBegin(
            ActivityAsset activity,
            RuntimeContentOwner targetOwner,
            IReadOnlyList<ActivityPlayerHandoffSlotRequest> orderedSlots,
            string source,
            string reason)
        {
            const string Operation = "BeginActivityPlayerHandoffGroup";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerHandoffGroupRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "begin-activity-player-handoff-group");
            ActivityPlayerHandoffGroupSnapshot empty =
                ActivityPlayerHandoffGroupSnapshot.Empty(
                    resolvedSource, resolvedReason, "No Activity Player handoff group exists.");

            if (activity == null || !targetOwner.IsValid ||
                targetOwner.Scope != RuntimeContentScope.Activity ||
                orderedSlots == null)
            {
                return Result(ActivityPlayerHandoffGroupStatus.RejectedInvalidRequest,
                    Operation, empty, empty,
                    "Activity Player handoff group requires an Activity, Activity owner and ordered Slot requests.");
            }

            if (active != null)
            {
                if (active.State == ActivityPlayerHandoffGroupState.ReadyToCommit &&
                    active.TargetOwner == targetOwner &&
                    MatchesRequests(active.Slots, orderedSlots))
                {
                    ActivityPlayerHandoffGroupSnapshot snapshot = Snapshot(active);
                    return Result(
                        ActivityPlayerHandoffGroupStatus.SucceededAlreadyReadyToCommit,
                        Operation, snapshot, snapshot,
                        "The same Activity Player handoff group is already ready to commit.");
                }
                return Result(ActivityPlayerHandoffGroupStatus.RejectedAnotherGroupActive,
                    Operation, empty, Snapshot(active),
                    "Another Activity Player handoff group is active.");
            }

            if (!TryValidateRequests(targetOwner, orderedSlots,
                    out string sessionContextId, out string validationIssue))
            {
                return Result(ActivityPlayerHandoffGroupStatus.RejectedInvalidRequest,
                    Operation, empty, empty, validationIssue);
            }

            groupSequence++;
            var record = new GroupRecord
            {
                Activity = activity,
                TargetOwner = targetOwner,
                Token = new ActivityPlayerHandoffGroupToken(
                    sessionContextId, targetOwner, orderedSlots.Count, groupSequence),
                State = ActivityPlayerHandoffGroupState.Beginning,
                Slots = new List<SlotRecord>(orderedSlots.Count),
                Source = resolvedSource,
                Reason = resolvedReason,
                Message = "Activity Player handoff group is beginning."
            };
            active = record;

            for (int index = 0; index < orderedSlots.Count; index++)
            {
                ActivityPlayerHandoffSlotRequest request = orderedSlots[index];
                PlayerGameplayChainHandoffResult begin =
                    promotionRuntime.TryBeginPromotion(
                        request.CandidateToken,
                        request.CurrentAdmissionToken,
                        resolvedSource,
                        $"{resolvedReason}:slot:{index}");
                if (begin == null || !begin.ReadyToCommit ||
                    begin.CurrentSnapshot == null ||
                    !begin.CurrentSnapshot.Token.IsValid)
                {
                    string issue = begin != null
                        ? begin.ToDiagnosticString()
                        : $"Per-Slot promotion begin returned no result at index '{index}'.";
                    bool rolledBack = TryRollbackRecord(
                        record, resolvedSource, "group-begin-failure", out string rollbackIssue);
                    if (rolledBack)
                    {
                        active = null;
                    }
                    return Result(
                        rolledBack
                            ? ActivityPlayerHandoffGroupStatus.FailedSlotBegin
                            : ActivityPlayerHandoffGroupStatus.FailedRollback,
                        Operation, empty, Snapshot(record),
                        Join(issue, rollbackIssue));
                }

                record.Slots.Add(new SlotRecord
                {
                    Request = request,
                    HandoffToken = begin.CurrentSnapshot.Token,
                    LastStatus = begin.Status,
                    Began = true,
                    Message = begin.Message
                });
            }

            if (!TryEvaluate(record, resolvedSource, resolvedReason,
                    out ActivityPlayerAdmissionFlowDecision decision,
                    out string evidenceIssue))
            {
                bool rolledBack = TryRollbackRecord(
                    record, resolvedSource, "group-evidence-failure", out string rollbackIssue);
                if (rolledBack)
                {
                    active = null;
                }
                return Result(
                    rolledBack
                        ? ActivityPlayerHandoffGroupStatus.FailedEvidence
                        : ActivityPlayerHandoffGroupStatus.FailedRollback,
                    Operation, empty, Snapshot(record),
                    Join(evidenceIssue, rollbackIssue));
            }

            record.Decision = decision;
            if (!decision.CanProceed)
            {
                bool rolledBack = TryRollbackRecord(
                    record, resolvedSource, "group-admission-rejected", out string rollbackIssue);
                if (rolledBack)
                {
                    active = null;
                }
                return Result(
                    rolledBack
                        ? ActivityPlayerHandoffGroupStatus.RejectedAdmission
                        : ActivityPlayerHandoffGroupStatus.FailedRollback,
                    Operation, empty, Snapshot(record),
                    Join(decision.ToDiagnosticString(), rollbackIssue));
            }

            record.State = ActivityPlayerHandoffGroupState.ReadyToCommit;
            record.Message = "All ordered Player handoffs and Activity admission are ready to commit.";
            revision++;
            return Result(ActivityPlayerHandoffGroupStatus.SucceededReadyToCommit,
                Operation, empty, Snapshot(record), record.Message);
        }

        internal ActivityPlayerHandoffGroupResult TryCommit(
            ActivityPlayerHandoffGroupToken expectedGroup,
            string source,
            string reason)
        {
            const string Operation = "CommitActivityPlayerHandoffGroup";
            if (committed != null && committed.Token == expectedGroup)
            {
                return Result(ActivityPlayerHandoffGroupStatus.SucceededAlreadyCommitted,
                    Operation, committed, committed,
                    "Activity Player handoff group is already committed.");
            }
            if (!TryResolveActive(expectedGroup, out GroupRecord record, out string issue))
            {
                ActivityPlayerHandoffGroupSnapshot empty =
                    ActivityPlayerHandoffGroupSnapshot.Empty(source, reason, issue);
                return Result(ActivityPlayerHandoffGroupStatus.RejectedForeignOrStaleGroup,
                    Operation, empty, empty, issue);
            }
            ActivityPlayerHandoffGroupSnapshot previous = Snapshot(record);
            if (record.State != ActivityPlayerHandoffGroupState.ReadyToCommit)
            {
                return Result(ActivityPlayerHandoffGroupStatus.RejectedNotReadyToCommit,
                    Operation, previous, previous,
                    "Activity Player handoff group is not at ReadyToCommit.");
            }

            if (!TryEvaluate(record, source, reason,
                    out ActivityPlayerAdmissionFlowDecision decision,
                    out string evidenceIssue))
            {
                return RollbackBeforeCommitFailure(record, previous, Operation,
                    ActivityPlayerHandoffGroupStatus.FailedEvidence,
                    evidenceIssue, source, reason);
            }
            record.Decision = decision;
            if (!decision.CanProceed)
            {
                return RollbackBeforeCommitFailure(record, previous, Operation,
                    ActivityPlayerHandoffGroupStatus.RejectedAdmission,
                    decision.ToDiagnosticString(), source, reason);
            }

            for (int index = 0; index < record.Slots.Count; index++)
            {
                if (!promotionRuntime.TryValidateCommitPromotion(
                        record.Slots[index].HandoffToken, out string validationIssue))
                {
                    return RollbackBeforeCommitFailure(record, previous, Operation,
                        ActivityPlayerHandoffGroupStatus.FailedCommitValidation,
                        $"Slot '{record.Slots[index].Request.PlayerSlotId.StableText}' commit validation failed. {validationIssue}",
                        source, reason);
                }
            }

            record.State = ActivityPlayerHandoffGroupState.Committing;
            bool cleanupPending = false;
            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                PlayerGameplayChainHandoffResult commit =
                    promotionRuntime.TryCommitPromotion(
                        slot.HandoffToken, source, $"{reason}:slot:{index}");
                slot.LastStatus = commit?.Status ?? PlayerGameplayChainHandoffStatus.FailedCommit;
                slot.Message = commit?.Message ?? "Per-Slot promotion commit returned no result.";
                bool targetAuthoritative = commit != null &&
                    (commit.Committed ||
                     (commit.Status == PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease &&
                      commit.CurrentSnapshot?.CandidateOwnershipCompleted == true));
                if (!targetAuthoritative)
                {
                    record.Message = slot.Message;
                    record.State = ActivityPlayerHandoffGroupState.CommitCleanupFailed;
                    revision++;
                    return Result(ActivityPlayerHandoffGroupStatus.FailedCommit,
                        Operation, previous, Snapshot(record),
                        "A prevalidated Slot commit failed after group commit began. " + slot.Message);
                }

                slot.Committed = true;
                slot.CleanupPending =
                    commit.Status == PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease;
                cleanupPending |= slot.CleanupPending;
            }

            if (cleanupPending)
            {
                record.State = ActivityPlayerHandoffGroupState.CommitCleanupFailed;
                record.Message =
                    "All target Player handoffs are authoritative; previous Actor cleanup remains pending.";
                revision++;
                return Result(ActivityPlayerHandoffGroupStatus.FailedCommitCleanup,
                    Operation, previous, Snapshot(record), record.Message);
            }

            record.State = ActivityPlayerHandoffGroupState.Committed;
            record.Message = "All ordered Activity Player handoffs committed.";
            committed = Snapshot(record);
            active = null;
            revision++;
            return Result(ActivityPlayerHandoffGroupStatus.SucceededCommitted,
                Operation, previous, committed, record.Message);
        }

        internal ActivityPlayerHandoffGroupResult TryRetryCommitCleanup(
            ActivityPlayerHandoffGroupToken expectedGroup,
            string source,
            string reason)
        {
            const string Operation = "RetryActivityPlayerHandoffGroupCommitCleanup";
            if (!TryResolveActive(expectedGroup, out GroupRecord record, out string issue) ||
                record.State != ActivityPlayerHandoffGroupState.CommitCleanupFailed)
            {
                ActivityPlayerHandoffGroupSnapshot empty =
                    ActivityPlayerHandoffGroupSnapshot.Empty(source, reason, issue);
                return Result(ActivityPlayerHandoffGroupStatus.RejectedForeignOrStaleGroup,
                    Operation, empty, empty,
                    string.IsNullOrEmpty(issue)
                        ? "Group is not waiting for commit cleanup."
                        : issue);
            }
            ActivityPlayerHandoffGroupSnapshot previous = Snapshot(record);
            bool failed = false;
            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                if (!slot.CleanupPending)
                {
                    continue;
                }
                PlayerGameplayChainHandoffResult retry =
                    promotionRuntime.TryRetryCommitCleanup(
                        slot.HandoffToken, source, $"{reason}:slot:{index}");
                slot.LastStatus = retry?.Status ?? PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease;
                slot.Message = retry?.Message ?? "Commit cleanup retry returned no result.";
                if (retry != null && retry.Committed)
                {
                    slot.CleanupPending = false;
                    slot.Committed = true;
                }
                else
                {
                    failed = true;
                }
            }
            if (failed)
            {
                record.Message = "One or more previous Actor cleanup operations remain pending.";
                revision++;
                return Result(ActivityPlayerHandoffGroupStatus.FailedCommitCleanup,
                    Operation, previous, Snapshot(record), record.Message);
            }
            record.State = ActivityPlayerHandoffGroupState.Committed;
            record.Message = "Activity Player handoff group commit cleanup completed.";
            committed = Snapshot(record);
            active = null;
            revision++;
            return Result(ActivityPlayerHandoffGroupStatus.SucceededCommitted,
                Operation, previous, committed, record.Message);
        }

        internal ActivityPlayerHandoffGroupResult TryRollback(
            ActivityPlayerHandoffGroupToken expectedGroup,
            string source,
            string reason)
        {
            const string Operation = "RollbackActivityPlayerHandoffGroup";
            if (committed != null && committed.Token == expectedGroup)
            {
                return Result(ActivityPlayerHandoffGroupStatus.RejectedRollbackNotAvailable,
                    Operation, committed, committed,
                    "Committed Activity Player handoff group cannot rollback.");
            }
            if (!TryResolveActive(expectedGroup, out GroupRecord record, out string issue))
            {
                ActivityPlayerHandoffGroupSnapshot empty =
                    ActivityPlayerHandoffGroupSnapshot.Empty(source, reason, issue);
                return Result(ActivityPlayerHandoffGroupStatus.RejectedForeignOrStaleGroup,
                    Operation, empty, empty, issue);
            }
            if (HasCommittedSlot(record))
            {
                return Result(ActivityPlayerHandoffGroupStatus.RejectedRollbackNotAvailable,
                    Operation, Snapshot(record), Snapshot(record),
                    "Activity Player handoff group cannot rollback after target ownership commit.");
            }
            ActivityPlayerHandoffGroupSnapshot previous = Snapshot(record);
            bool rolledBack = TryRollbackRecord(record, source, reason, out issue);
            if (rolledBack)
            {
                active = null;
            }
            return Result(
                rolledBack
                    ? ActivityPlayerHandoffGroupStatus.SucceededRolledBack
                    : ActivityPlayerHandoffGroupStatus.FailedRollback,
                Operation, previous, Snapshot(record), issue);
        }

        private ActivityPlayerHandoffGroupResult RollbackBeforeCommitFailure(
            GroupRecord record,
            ActivityPlayerHandoffGroupSnapshot previous,
            string operation,
            ActivityPlayerHandoffGroupStatus originalStatus,
            string originalIssue,
            string source,
            string reason)
        {
            bool rolledBack = TryRollbackRecord(record, source,
                "group-precommit-validation-failure", out string rollbackIssue);
            if (rolledBack)
            {
                active = null;
            }
            return Result(rolledBack ? originalStatus : ActivityPlayerHandoffGroupStatus.FailedRollback,
                operation, previous, Snapshot(record), Join(originalIssue, rollbackIssue));
        }

        private bool TryEvaluate(
            GroupRecord record,
            string source,
            string reason,
            out ActivityPlayerAdmissionFlowDecision decision,
            out string issue)
        {
            decision = null;
            if (!evidenceSource.TryCapture(
                    out PlayerParticipationSnapshot participation,
                    out PlayerActorPreparationSnapshot preparation,
                    out PlayerGameplayAdmissionSnapshot admission,
                    out issue))
            {
                return false;
            }
            decision = admissionGate.Evaluate(record.Activity, participation,
                preparation, admission, source, reason);
            if (decision == null)
            {
                issue = "Activity Player admission gate returned no decision.";
                return false;
            }
            if (decision.ProjectedSlotCount != record.Slots.Count ||
                decision.Evaluation == null ||
                decision.Evaluation.Slots.Count != record.Slots.Count)
            {
                issue =
                    $"Activity admission projected '{decision.ProjectedSlotCount}' Slots, " +
                    $"but the handoff group contains '{record.Slots.Count}'.";
                return false;
            }
            for (int index = 0; index < record.Slots.Count; index++)
            {
                if (decision.Evaluation.Slots[index].PlayerSlotId !=
                    record.Slots[index].Request.PlayerSlotId)
                {
                    issue =
                        $"Activity admission Slot order differs from the handoff group at index '{index}'.";
                    return false;
                }
            }
            issue = string.Empty;
            return true;
        }

        private bool TryRollbackRecord(
            GroupRecord record,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            record.State = ActivityPlayerHandoffGroupState.RollingBack;
            var failures = new List<string>();
            for (int index = record.Slots.Count - 1; index >= 0; index--)
            {
                SlotRecord slot = record.Slots[index];
                if (!slot.Began || slot.RolledBack)
                {
                    continue;
                }
                if (slot.Committed)
                {
                    failures.Add($"Slot '{slot.Request.PlayerSlotId.StableText}' is already committed.");
                    continue;
                }
                PlayerGameplayChainHandoffResult rollback =
                    promotionRuntime.TryRollbackPromotion(
                        slot.HandoffToken, source, $"{reason}:slot:{index}");
                slot.LastStatus = rollback?.Status ?? PlayerGameplayChainHandoffStatus.FailedRollback;
                slot.Message = rollback?.Message ?? "Per-Slot rollback returned no result.";
                if (rollback != null &&
                    rollback.Status == PlayerGameplayChainHandoffStatus.SucceededRolledBack)
                {
                    slot.RolledBack = true;
                }
                else
                {
                    failures.Add(slot.Message);
                }
            }
            if (failures.Count > 0)
            {
                record.State = ActivityPlayerHandoffGroupState.RollbackFailed;
                record.Message = string.Join(" | ", failures);
                issue = record.Message;
                revision++;
                return false;
            }
            record.State = ActivityPlayerHandoffGroupState.RolledBack;
            record.Message = "All begun Player handoffs rolled back in reverse order.";
            issue = string.Empty;
            revision++;
            return true;
        }

        private bool TryResolveActive(
            ActivityPlayerHandoffGroupToken expected,
            out GroupRecord record,
            out string issue)
        {
            record = active;
            if (record == null || !expected.IsValid || record.Token != expected)
            {
                issue = "Activity Player handoff group token is foreign or stale.";
                return false;
            }
            issue = string.Empty;
            return true;
        }

        private static bool TryValidateRequests(
            RuntimeContentOwner targetOwner,
            IReadOnlyList<ActivityPlayerHandoffSlotRequest> requests,
            out string sessionContextId,
            out string issue)
        {
            sessionContextId = string.Empty;
            issue = string.Empty;
            var unique = new HashSet<PlayerSlotId>();
            for (int index = 0; index < requests.Count; index++)
            {
                ActivityPlayerHandoffSlotRequest request = requests[index];
                if (!request.IsValid || request.CandidateToken.Owner != targetOwner)
                {
                    issue = $"Invalid or foreign Slot request at index '{index}'.";
                    return false;
                }
                if (!unique.Add(request.PlayerSlotId))
                {
                    issue = $"Duplicate Player Slot '{request.PlayerSlotId.StableText}' in handoff group.";
                    return false;
                }
                if (index == 0)
                {
                    sessionContextId = request.CandidateToken.SessionContextId;
                }
                else if (!string.Equals(sessionContextId,
                    request.CandidateToken.SessionContextId, StringComparison.Ordinal))
                {
                    issue = "Activity Player handoff group requests belong to different Sessions.";
                    return false;
                }
            }
            return true;
        }

        private static bool MatchesRequests(
            IReadOnlyList<SlotRecord> existing,
            IReadOnlyList<ActivityPlayerHandoffSlotRequest> requested)
        {
            if (existing.Count != requested.Count)
            {
                return false;
            }
            for (int index = 0; index < requested.Count; index++)
            {
                if (existing[index].Request.CandidateToken != requested[index].CandidateToken ||
                    existing[index].Request.CurrentAdmissionToken != requested[index].CurrentAdmissionToken)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasCommittedSlot(GroupRecord record)
        {
            for (int index = 0; index < record.Slots.Count; index++)
            {
                if (record.Slots[index].Committed)
                {
                    return true;
                }
            }
            return false;
        }

        private static ActivityPlayerHandoffGroupSnapshot Snapshot(GroupRecord record)
        {
            if (record == null)
            {
                return ActivityPlayerHandoffGroupSnapshot.Empty(
                    string.Empty, string.Empty, "No Activity Player handoff group exists.");
            }
            var slots = new ActivityPlayerHandoffGroupSlotSnapshot[record.Slots.Count];
            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                slots[index] = new ActivityPlayerHandoffGroupSlotSnapshot(
                    slot.Request.PlayerSlotId, slot.HandoffToken, slot.LastStatus,
                    slot.Began, slot.Committed, slot.RolledBack,
                    slot.CleanupPending, slot.Message);
            }
            return new ActivityPlayerHandoffGroupSnapshot(
                record.Token, record.State, record.Decision, slots,
                record.Activity != null ? record.Activity.ActivityName : string.Empty,
                record.Source, record.Reason, record.Message);
        }

        private static ActivityPlayerHandoffGroupResult Result(
            ActivityPlayerHandoffGroupStatus status,
            string operation,
            ActivityPlayerHandoffGroupSnapshot previous,
            ActivityPlayerHandoffGroupSnapshot current,
            string message) =>
            new ActivityPlayerHandoffGroupResult(
                status, operation, previous, current, message);

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
