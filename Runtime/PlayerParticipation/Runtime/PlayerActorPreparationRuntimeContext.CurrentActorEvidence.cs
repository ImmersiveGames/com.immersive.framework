using System;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeContext
    {
        internal bool TryGetCurrentActorEvidence(
            PlayerSlotId playerSlotId,
            out PlayerActorCorrelationEvidence evidence,
            out PlayerCurrentActorEvidenceResult result)
        {
            result = ConfirmCurrentActorEvidence(
                playerSlotId,
                default,
                nameof(PlayerActorPreparationRuntimeContext),
                "lookup-current-actor-evidence");
            evidence = result.Succeeded
                ? result.RetainedEvidence
                : default;
            return result.Succeeded;
        }

        internal bool TryGetRetainedActorEvidence(
            PlayerSlotId playerSlotId,
            out PlayerActorCorrelationEvidence evidence)
        {
            if (playerSlotId.IsValid &&
                _records.TryGetValue(playerSlotId, out PreparationRecord record) &&
                record.Summary.ActorEvidence.IsValid)
            {
                evidence = record.Summary.ActorEvidence;
                return true;
            }

            evidence = default;
            return false;
        }

        internal PlayerCurrentActorEvidenceResult ConfirmCurrentActorEvidence(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            const string operation = "ConfirmCurrentActorEvidence";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "confirm-current-actor-evidence");

            if (!playerSlotId.IsValid)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedInvalidRequest,
                    operation,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Current Actor evidence confirmation requires a valid Player Slot identity.");
            }

            if (expectedPreparation.IsValid)
            {
                if (!string.Equals(
                        expectedPreparation.SessionContextId,
                        _sessionContextId,
                        StringComparison.Ordinal))
                {
                    return ActorEvidenceResult(
                        PlayerCurrentActorEvidenceStatus.RejectedForeignPreparation,
                        operation,
                        default,
                        default,
                        resolvedSource,
                        resolvedReason,
                        "Expected preparation token belongs to another Session context.");
                }

                if (expectedPreparation.PlayerSlotId != playerSlotId)
                {
                    return ActorEvidenceResult(
                        PlayerCurrentActorEvidenceStatus.RejectedOtherSlotPreparation,
                        operation,
                        default,
                        default,
                        resolvedSource,
                        resolvedReason,
                        "Expected preparation token belongs to another Player Slot.");
                }
            }

            if (!_records.TryGetValue(playerSlotId, out PreparationRecord record))
            {
                _participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot unpreparedSlot);
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.NoPreparedActor,
                    operation,
                    default,
                    unpreparedSlot.IsValid
                        ? CreateUnpreparedSummary(
                            unpreparedSlot,
                            resolvedSource,
                            resolvedReason,
                            "Logical Player Actor is not prepared.")
                        : default,
                    resolvedSource,
                    resolvedReason,
                    "Player Slot has no retained prepared Actor evidence.");
            }

            PlayerActorPreparationSummary summary = record.Summary;
            PlayerActorCorrelationEvidence retained = summary.ActorEvidence;
            if (expectedPreparation.IsValid)
            {
                if (expectedPreparation != summary.Token)
                {
                    return ActorEvidenceResult(
                        PlayerCurrentActorEvidenceStatus.RejectedPreparationStale,
                        operation,
                        retained,
                        summary,
                        resolvedSource,
                        resolvedReason,
                        "Expected preparation token is stale for the retained Actor.");
                }
            }

            if (summary.IsReleaseFailed)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedReleaseFailed,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Actor release failed; retained evidence is diagnostic and not current.");
            }

            if (!summary.IsPrepared ||
                !retained.IsValid ||
                summary.Token != retained.PreparationToken)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedPreparationStale,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Retained Actor preparation evidence is incomplete or stale.");
            }

            if (!_participationContext.TryGetCurrentAssignment(
                    playerSlotId,
                    out PlayerSlotAssignmentSnapshot assignment))
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedAssignmentDivergence,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Session physical evidence has no current Activity contextual assignment.");
            }

            if (!assignment.IsAssigned ||
                assignment.AssignmentOrigin !=
                    ToAssignmentOrigin(retained.ProvisioningOrigin))
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedAssignmentDivergence,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Current Activity contextual assignment does not match the physical provisioning origin.");
            }

            PlayerHostEvidenceResult hostConfirmation =
                _hostEvidenceProjection.ConfirmHostEvidence(
                    playerSlotId,
                    resolvedSource,
                    resolvedReason);
            if (hostConfirmation == null ||
                !hostConfirmation.Succeeded ||
                hostConfirmation.CurrentEvidence.AssignmentToken !=
                    assignment.AssignmentToken ||
                hostConfirmation.CurrentEvidence.HostBindingIdentity !=
                    assignment.HostBindingIdentity ||
                !ReferenceEquals(hostConfirmation.CurrentEvidence.Host, record.Host) ||
                !ReferenceEquals(record.Handle.LocalPlayerHost, record.Host))
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedHostDivergence,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    hostConfirmation != null
                        ? hostConfirmation.ToDiagnosticString()
                        : "Physical Host evidence confirmation returned no result.");
            }

            if (!_participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsJoined ||
                !slot.HasSelectedActor ||
                slot.SelectedActorProfileId != retained.ActorProfileId ||
                slot.SelectionRevision != retained.SelectionRevision)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedSelectionStale,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Current Actor selection Profile or revision differs from retained preparation evidence.");
            }

            if (record.Handle.Request.Owner != retained.Owner)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedOwnerMismatch,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Actor owner differs from retained correlation evidence.");
            }

            if (retained.PhysicalOwnership !=
                PlayerActorPhysicalOwnership.FrameworkOwned)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedPhysicalEvidenceMismatch,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Actor physical ownership is incompatible with the post-admission Session ownership contract.");
            }

            if (record.Handle.Request.RuntimeContentIdentity !=
                    retained.RuntimeContentIdentity ||
                record.Handle.Request.ActorId != retained.ActorId ||
                record.Handle.Request.MaterializationRevision !=
                    retained.MaterializationRevision)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedRuntimeContentMismatch,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Actor runtime content or Actor identity differs from retained evidence.");
            }

            if (record.Handle.State != PlayerActorMaterializationState.Active ||
                record.Handle.PlayerActorDeclaration == null ||
                record.Handle.PlayerActorRuntimeHost == null ||
                record.Handle.Presentation == null)
            {
                return ActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedPhysicalEvidenceMismatch,
                    operation,
                    retained,
                    summary,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Actor physical handle is unavailable or not active.");
            }

            return ActorEvidenceResult(
                PlayerCurrentActorEvidenceStatus.SucceededCurrent,
                operation,
                retained,
                summary,
                resolvedSource,
                resolvedReason,
                "Prepared Actor evidence is current and fully correlated.");
        }

        internal bool TryGetCurrentSlotActorSnapshot(
            PlayerSlotId playerSlotId,
            out CurrentPlayerSlotActorSnapshot snapshot)
        {
            snapshot = default;
            if (!_participationContext.TryGetCurrentAssignment(
                    playerSlotId,
                    out PlayerSlotAssignmentSnapshot assignment))
            {
                return false;
            }

            PlayerHostEvidenceResult hostResult =
                _hostEvidenceProjection.ConfirmHostEvidence(
                    playerSlotId,
                    nameof(PlayerActorPreparationRuntimeContext),
                    "aggregate-current-slot-host-actor");
            PlayerHostEvidenceSnapshot retainedHost =
                hostResult != null && hostResult.CurrentEvidence.IsRecorded
                    ? hostResult.CurrentEvidence
                    : _hostEvidenceProjection.TryGetRetainedEvidence(
                        playerSlotId,
                        out PlayerHostEvidenceSnapshot fallback)
                        ? fallback
                        : default;
            var hostSummary = new PlayerHostEvidenceSummary(
                retainedHost.PlayerSlotId,
                retainedHost.AssignmentOrigin,
                retainedHost.AssignmentToken,
                retainedHost.HostBindingIdentity,
                hostResult != null && hostResult.Succeeded,
                retainedHost.Source,
                retainedHost.Reason,
                hostResult != null ? hostResult.Message : string.Empty);

            TryGetPreparationSummary(
                playerSlotId,
                out PlayerActorPreparationSummary preparation);
            PlayerCurrentActorEvidenceResult actorResult =
                ConfirmCurrentActorEvidence(
                    playerSlotId,
                    default,
                    nameof(PlayerActorPreparationRuntimeContext),
                    "aggregate-current-slot-host-actor");
            snapshot = new CurrentPlayerSlotActorSnapshot(
                assignment,
                hostSummary,
                preparation,
                actorResult.RetainedEvidence,
                actorResult.Status,
                actorResult.Message);
            return snapshot.IsReadable;
        }

        private static PlayerCurrentActorEvidenceResult ActorEvidenceResult(
            PlayerCurrentActorEvidenceStatus status,
            string operation,
            PlayerActorCorrelationEvidence retainedEvidence,
            PlayerActorPreparationSummary preparation,
            string source,
            string reason,
            string message)
        {
            return new PlayerCurrentActorEvidenceResult(
                status,
                operation,
                retainedEvidence,
                preparation,
                source,
                reason,
                message);
        }
    }
}
