using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeContext
    {
        private sealed class PromotionRecord
        {
            internal PromotionRecord(
                PreparationRecord previous,
                PreparationRecord candidate,
                PlayerActorPreparationHandoff handoff)
            {
                Previous = previous;
                Candidate = candidate;
                Handoff = handoff;
            }

            internal PreparationRecord Previous { get; }
            internal PreparationRecord Candidate { get; }
            internal PlayerActorPreparationHandoff Handoff { get; }
        }

        private readonly Dictionary<PlayerSlotId, PromotionRecord> promotionRecords =
            new Dictionary<PlayerSlotId, PromotionRecord>();
        private int promotionSequence;

        internal bool TryBeginCandidateHandoff(
            PlayerActorCandidatePromotionHandle candidate,
            PlayerActorPreparationToken expectedCurrent,
            string source,
            string reason,
            out PlayerActorPreparationHandoff handoff,
            out string issue)
        {
            handoff = null;
            issue = string.Empty;
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "promote-target-activity-player-actor");

            if (candidate == null || !candidate.IsValid || candidate.IsCompleted ||
                !expectedCurrent.IsValid ||
                !string.Equals(
                    candidate.Token.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                candidate.Token.PlayerSlotId != expectedCurrent.PlayerSlotId)
            {
                issue = "Preparation handoff requires exact current preparation and candidate promotion evidence from one Session/Slot.";
                return false;
            }

            PlayerSlotId slotId = candidate.Token.PlayerSlotId;
            if (promotionRecords.ContainsKey(slotId))
            {
                issue = "Player Slot already has an active preparation handoff.";
                return false;
            }

            if (!participationContext.TryGetActorSelection(
                    slotId,
                    out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsValid || !slot.IsJoined || !slot.HasSelectedActor)
            {
                issue = "Preparation handoff requires a current Joined Slot with explicit Actor selection.";
                return false;
            }

            if (!records.TryGetValue(slotId, out PreparationRecord previous) ||
                !previous.Summary.IsPrepared ||
                previous.Summary.Token != expectedCurrent ||
                previous.Handle.State != PlayerActorMaterializationState.Active)
            {
                issue = "Current P3J preparation is missing, inactive, foreign or stale.";
                return false;
            }

            if (candidate.Snapshot.CurrentPreparationToken != expectedCurrent ||
                candidate.Token.ActorProfileId != slot.SelectedActorProfileId ||
                candidate.MaterializationHandle.Request.Owner == previous.Handle.Request.Owner ||
                candidate.MaterializationHandle.State !=
                    PlayerActorMaterializationState.StagedInactive)
            {
                issue = "Candidate no longer matches the current preparation, selected Profile or distinct target Activity owner.";
                return false;
            }

            if (!candidate.TryActivate(
                    resolvedSource,
                    resolvedReason,
                    out string candidateActivationIssue))
            {
                issue = $"Candidate activation failed. {candidateActivationIssue}";
                return false;
            }

            if (!previous.Handle.TryDeactivate(
                    resolvedSource,
                    "deactivate-previous-player-actor-for-handoff",
                    out string previousDeactivationIssue))
            {
                candidate.TryDeactivate(
                    resolvedSource,
                    "candidate-activation-rollback",
                    out string candidateRollbackIssue);
                issue =
                    $"Previous Actor deactivation failed. {previousDeactivationIssue} " +
                    $"Candidate rollback='{candidateRollbackIssue}'.";
                return false;
            }

            PlayerActorPreparationSummary promotedSummary = CreatePreparedSummary(
                slot,
                candidate.MaterializationHandle,
                PlayerActorPreparationState.Prepared,
                resolvedSource,
                resolvedReason,
                "Target Activity candidate is the current active P3J preparation during gameplay handoff.");
            var promotedRecord = new PreparationRecord(
                candidate.MaterializationHandle,
                promotedSummary);
            records[slotId] = promotedRecord;
            promotionSequence++;
            revision++;
            handoff = new PlayerActorPreparationHandoff(
                this,
                slotId,
                previous.Summary,
                promotedSummary,
                candidate,
                promotionSequence);
            promotionRecords.Add(
                slotId,
                new PromotionRecord(previous, promotedRecord, handoff));
            return true;
        }

        internal bool TryRollbackCandidateHandoff(
            PlayerActorPreparationHandoff handoff,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!TryResolvePromotion(handoff, out PromotionRecord promotion, out issue))
            {
                return false;
            }

            if (handoff.CandidateOwnershipCompleted)
            {
                issue = "Preparation handoff cannot rollback after candidate ownership was completed.";
                return false;
            }

            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "rollback-player-actor-preparation-handoff");

            if (!promotion.Candidate.Handle.TryDeactivate(
                    resolvedSource,
                    resolvedReason,
                    out string candidateIssue))
            {
                issue = $"Candidate deactivation failed during handoff rollback. {candidateIssue}";
                return false;
            }

            if (!promotion.Previous.Handle.TryActivate(
                    resolvedSource,
                    resolvedReason,
                    out string previousIssue))
            {
                promotion.Candidate.Handle.TryActivate(
                    resolvedSource,
                    "restore-candidate-after-previous-reactivation-failure",
                    out _);
                issue = $"Previous Actor reactivation failed during handoff rollback. {previousIssue}";
                return false;
            }

            records[handoff.PlayerSlotId] = promotion.Previous;
            if (!handoff.Candidate.TryCancel(
                    resolvedSource,
                    resolvedReason,
                    out string cancellationIssue))
            {
                promotion.Previous.Handle.TryDeactivate(
                    resolvedSource,
                    "cancel-promotion-failure-rollback",
                    out _);
                promotion.Candidate.Handle.TryActivate(
                    resolvedSource,
                    "cancel-promotion-failure-rollback",
                    out _);
                records[handoff.PlayerSlotId] = promotion.Candidate;
                issue = $"Candidate promotion ownership could not return to staging. {cancellationIssue}";
                return false;
            }

            promotionRecords.Remove(handoff.PlayerSlotId);
            revision++;
            return true;
        }

        internal bool TryCommitCandidateHandoff(
            PlayerActorPreparationHandoff handoff,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (handoff != null && handoff.IsCompleted)
            {
                return true;
            }

            if (!TryResolvePromotion(handoff, out PromotionRecord promotion, out issue))
            {
                return false;
            }

            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "commit-player-actor-preparation-handoff");

            if (!handoff.CandidateOwnershipCompleted)
            {
                if (!handoff.Candidate.TryComplete(
                        resolvedSource,
                        resolvedReason,
                        out issue))
                {
                    return false;
                }

                handoff.CandidateOwnershipCompleted = true;
            }

            if (!handoff.PreviousActorReleased)
            {
                if (!materializationAdapter.TryReleaseMaterialization(
                        promotion.Previous.Handle,
                        resolvedSource,
                        "release-previous-player-actor-after-handoff",
                        out string releaseIssue))
                {
                    // The handoff lease retains the exact previous physical handle for retry.
                    // Do not also add it to the generic retained-release list or a successful
                    // retry would leave duplicate failure diagnostics behind.
                    issue = releaseIssue;
                    return false;
                }

                handoff.PreviousActorReleased = true;
            }

            promotionRecords.Remove(handoff.PlayerSlotId);
            revision++;
            return true;
        }

        internal bool TryGetPreparedPhysicalEvidence(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
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

            if (!playerSlotId.IsValid || !expectedPreparation.IsValid ||
                !records.TryGetValue(playerSlotId, out PreparationRecord record) ||
                record.Summary.Token != expectedPreparation ||
                !record.Summary.IsPrepared)
            {
                issue = "Prepared physical evidence requires the exact current P3J preparation token.";
                return false;
            }

            host = record.Handle.LocalPlayerHost;
            playerInput = record.Handle.PlayerInput;
            declaration = record.Handle.PlayerActorDeclaration;
            logicalActorHost = record.Handle.LogicalActorHost;
            if (host == null || playerInput == null || declaration == null ||
                logicalActorHost == null)
            {
                issue = "Current prepared Actor physical evidence is incomplete.";
                return false;
            }

            return true;
        }

        private bool TryResolvePromotion(
            PlayerActorPreparationHandoff handoff,
            out PromotionRecord promotion,
            out string issue)
        {
            promotion = null;
            if (handoff == null || !ReferenceEquals(handoff.Owner, this) ||
                !handoff.PlayerSlotId.IsValid ||
                !promotionRecords.TryGetValue(handoff.PlayerSlotId, out promotion) ||
                !ReferenceEquals(promotion.Handoff, handoff) ||
                !records.TryGetValue(handoff.PlayerSlotId, out PreparationRecord current) ||
                !ReferenceEquals(current, promotion.Candidate))
            {
                promotion = null;
                issue = "Preparation handoff lease is foreign, stale or no longer current.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
