using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorCandidateStageRuntimeContext
    {
        private readonly Dictionary<PlayerSlotId, PlayerActorCandidateStageToken>
            lastPromotedTokens =
                new Dictionary<PlayerSlotId, PlayerActorCandidateStageToken>();

        internal bool TryBeginPromotion(
            PlayerActorCandidateStageToken expectedCandidate,
            string source,
            string reason,
            out PlayerActorCandidatePromotionHandle handle,
            out string issue)
        {
            handle = null;
            issue = string.Empty;
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorCandidateStageRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "begin-player-gameplay-handoff");

            if (!expectedCandidate.IsValid ||
                !string.Equals(
                    expectedCandidate.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !records.TryGetValue(
                    expectedCandidate.PlayerSlotId,
                    out CandidateRecord record) ||
                record.Snapshot.Token != expectedCandidate)
            {
                issue = "Candidate promotion requires the exact current staged token from this Session.";
                return false;
            }

            if (!record.Snapshot.IsStagedInactive ||
                record.Handle.State != PlayerActorMaterializationState.StagedInactive)
            {
                issue = record.Snapshot.IsPromoting
                    ? "Candidate is already owned by another promotion attempt."
                    : "Candidate promotion requires a staged inactive materialization.";
                return false;
            }

            var promoting = new PlayerActorCandidateStageSnapshot(
                record.Snapshot.Token,
                PlayerActorCandidateStageState.Promoting,
                record.Handle.CreateSnapshot(),
                record.Snapshot.CurrentPreparationToken,
                record.Snapshot.CurrentActorId,
                record.Snapshot.CurrentOwner,
                resolvedSource,
                resolvedReason,
                "Candidate is reserved by an exact reversible gameplay handoff.");
            record.Snapshot = promoting;
            revision++;
            handle = new PlayerActorCandidatePromotionHandle(
                this,
                expectedCandidate,
                record.Handle,
                record.Host,
                promoting);
            return true;
        }

        internal bool TryCancelPromotion(
            PlayerActorCandidatePromotionHandle handle,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!TryResolvePromotionRecord(handle, out CandidateRecord record, out issue))
            {
                return false;
            }

            if (handle.MaterializationHandle.State !=
                PlayerActorMaterializationState.StagedInactive)
            {
                issue = "Candidate promotion can only cancel after the candidate is inactive again.";
                return false;
            }

            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorCandidateStageRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "cancel-player-gameplay-handoff");
            var staged = new PlayerActorCandidateStageSnapshot(
                record.Snapshot.Token,
                PlayerActorCandidateStageState.StagedInactive,
                record.Handle.CreateSnapshot(),
                record.Snapshot.CurrentPreparationToken,
                record.Snapshot.CurrentActorId,
                record.Snapshot.CurrentOwner,
                resolvedSource,
                resolvedReason,
                "Candidate promotion cancelled; candidate returned to staged inactive ownership.");
            record.Snapshot = staged;
            handle.UpdateSnapshot(staged);
            revision++;
            return true;
        }

        internal bool TryCompletePromotion(
            PlayerActorCandidatePromotionHandle handle,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (handle != null && handle.IsCompleted)
            {
                return true;
            }

            if (!TryResolvePromotionRecord(handle, out CandidateRecord record, out issue))
            {
                return false;
            }

            if (handle.MaterializationHandle.State !=
                PlayerActorMaterializationState.Active)
            {
                issue = "Candidate promotion completion requires the promoted Actor to be active.";
                return false;
            }

            records.Remove(handle.Token.PlayerSlotId);
            lastPromotedTokens[handle.Token.PlayerSlotId] = handle.Token;
            revision++;
            handle.MarkCompleted();
            return true;
        }

        internal bool WasPromoted(PlayerActorCandidateStageToken token) =>
            token.IsValid &&
            lastPromotedTokens.TryGetValue(token.PlayerSlotId, out var promoted) &&
            promoted == token;

        private bool TryResolvePromotionRecord(
            PlayerActorCandidatePromotionHandle handle,
            out CandidateRecord record,
            out string issue)
        {
            record = null;
            if (handle == null || !handle.IsValid || handle.IsCompleted)
            {
                issue = "Candidate promotion handle is missing, invalid or already completed.";
                return false;
            }

            if (!records.TryGetValue(handle.Token.PlayerSlotId, out record) ||
                record.Snapshot.Token != handle.Token ||
                !record.Snapshot.IsPromoting ||
                !ReferenceEquals(record.Handle, handle.MaterializationHandle) ||
                !ReferenceEquals(record.Host, handle.Host))
            {
                record = null;
                issue = "Candidate promotion handle is foreign or stale for the current promotion record.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
