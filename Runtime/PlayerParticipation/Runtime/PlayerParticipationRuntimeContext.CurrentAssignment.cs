using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerParticipationRuntimeContext :
        ISceneLocalPlayerAssignmentReleaseRuntimePort
    {
        private sealed class CurrentAssignmentRecord
        {
            internal CurrentAssignmentRecord(
                SlotRecord slot,
                PlayerSlotAssignmentOrigin origin,
                RuntimeContentOwner owner,
                int sequence,
                int assignmentRevision,
                PlayerSlotAssignmentToken token,
                PlayerHostBindingIdentity hostBindingIdentity,
                string source,
                string reason)
            {
                Slot = slot;
                Origin = origin;
                Owner = owner;
                Sequence = sequence;
                AssignmentRevision = assignmentRevision;
                Token = token;
                HostBindingIdentity = hostBindingIdentity;
                Source = source;
                Reason = reason;
            }

            internal SlotRecord Slot { get; }
            internal PlayerSlotAssignmentOrigin Origin { get; }
            internal RuntimeContentOwner Owner { get; }
            internal int Sequence { get; }
            internal int AssignmentRevision { get; }
            internal PlayerSlotAssignmentToken Token { get; }
            internal PlayerHostBindingIdentity HostBindingIdentity { get; }
            internal string Source { get; }
            internal string Reason { get; }
        }

        private readonly Dictionary<PlayerSlotId, CurrentAssignmentRecord>
            currentAssignments = new();
        private int assignmentSequence;
        private int hostBindingSequence;

        internal RuntimeContentOwner CreateSessionAssignmentOwner()
        {
            return RuntimeContentOwner.Session(
                contextId,
                "Player Participation Session");
        }

        internal PlayerHostBindingIdentity CreateHostBindingIdentity()
        {
            hostBindingSequence++;
            return new PlayerHostBindingIdentity(contextId, hostBindingSequence);
        }

        internal PlayerSlotAssignmentResult BeginAssignment(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin origin,
            RuntimeContentOwner owner,
            PlayerHostBindingIdentity hostBindingIdentity,
            string source,
            string reason)
        {
            const string operation = "BeginAssignment";
            string resolvedSource = source.NormalizeText();
            string resolvedReason = reason.NormalizeText();

            if (!playerSlotId.IsValid)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedInvalidSlot,
                    operation,
                    default,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Current assignment requires a valid Player Slot identity.");
            }

            SlotRecord slot = FindSlot(playerSlotId);
            if (slot == null)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedSlotNotConfigured,
                    operation,
                    default,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session.");
            }

            PlayerSlotAssignmentSnapshot unassigned = CreateUnassignedAssignmentSnapshot(slot);
            if (origin == PlayerSlotAssignmentOrigin.SessionPersistent)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedUnsupportedOrigin,
                    operation,
                    unassigned,
                    unassigned,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "SessionPersistent assignment origin is reserved but not implemented in CPSA-1.");
            }

            if (origin is not
                PlayerSlotAssignmentOrigin.ManagerProvisioned and not
                PlayerSlotAssignmentOrigin.SceneProvided)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedInvalidOrigin,
                    operation,
                    unassigned,
                    unassigned,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Current assignment origin must be ManagerProvisioned or SceneProvided.");
            }

            if (!IsAssignmentOwnerValidForOrigin(origin, owner))
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedInvalidOwner,
                    operation,
                    unassigned,
                    unassigned,
                    default,
                    resolvedSource,
                    resolvedReason,
                    origin == PlayerSlotAssignmentOrigin.ManagerProvisioned
                        ? "Manager-provisioned assignment owner must be the explicit owner of this Session participation context."
                        : "Scene-provided assignment owner must be an explicit Activity or Route owner.");
            }

            if (!hostBindingIdentity.IsValid ||
                !string.Equals(
                    hostBindingIdentity.SessionContextId,
                    contextId,
                    StringComparison.Ordinal))
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedInvalidHostBinding,
                    operation,
                    unassigned,
                    unassigned,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Host binding identity is invalid or belongs to another Session context.");
            }

            if (string.IsNullOrEmpty(resolvedSource) ||
                string.IsNullOrEmpty(resolvedReason))
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedAssignmentConflict,
                    operation,
                    unassigned,
                    unassigned,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Current assignment source and reason are required evidence.");
            }

            if (slot.AllocationState != PlayerSlotAllocationState.Joined)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedSlotNotJoined,
                    operation,
                    unassigned,
                    unassigned,
                    default,
                    resolvedSource,
                    resolvedReason,
                    $"Player Slot '{playerSlotId.StableText}' must be Joined before assignment begins.");
            }

            if (currentAssignments.TryGetValue(
                    playerSlotId,
                    out CurrentAssignmentRecord existing))
            {
                PlayerSlotAssignmentSnapshot existingSnapshot =
                    CreateAssignmentSnapshot(existing);
                bool sameEvidence =
                    existing.Origin == origin &&
                    existing.Owner == owner &&
                    existing.HostBindingIdentity == hostBindingIdentity;
                return AssignmentResult(
                    sameEvidence
                        ? PlayerSlotAssignmentStatus.SucceededAlreadyAssigned
                        : PlayerSlotAssignmentStatus.RejectedAssignmentConflict,
                    operation,
                    existingSnapshot,
                    existingSnapshot,
                    existing.Token,
                    resolvedSource,
                    resolvedReason,
                    sameEvidence
                        ? "The same current assignment domain evidence is already committed."
                        : $"Player Slot '{playerSlotId.StableText}' already has another current assignment.");
            }

            foreach (KeyValuePair<PlayerSlotId, CurrentAssignmentRecord> pair in
                     currentAssignments)
            {
                if (pair.Value.HostBindingIdentity == hostBindingIdentity)
                {
                    PlayerSlotAssignmentSnapshot conflict =
                        CreateAssignmentSnapshot(pair.Value);
                    return AssignmentResult(
                        PlayerSlotAssignmentStatus.RejectedHostBindingConflict,
                        operation,
                        unassigned,
                        unassigned,
                        default,
                        resolvedSource,
                        resolvedReason,
                        $"Host binding '{hostBindingIdentity.StableText}' is already assigned to Player Slot '{conflict.PlayerSlotId.StableText}'.");
                }
            }

            assignmentSequence++;
            const int initialAssignmentRevision = 1;
            var token = new PlayerSlotAssignmentToken(
                contextId,
                playerSlotId,
                assignmentSequence,
                initialAssignmentRevision,
                hostBindingIdentity);
            var record = new CurrentAssignmentRecord(
                slot,
                origin,
                owner,
                assignmentSequence,
                initialAssignmentRevision,
                token,
                hostBindingIdentity,
                resolvedSource,
                resolvedReason);
            currentAssignments.Add(playerSlotId, record);
            PlayerSlotAssignmentSnapshot current = CreateAssignmentSnapshot(record);
            return AssignmentResult(
                PlayerSlotAssignmentStatus.SucceededAssigned,
                operation,
                unassigned,
                current,
                token,
                resolvedSource,
                resolvedReason,
                "Current Player Slot assignment committed.");
        }

        internal bool TryGetCurrentAssignment(
            PlayerSlotId playerSlotId,
            out PlayerSlotAssignmentSnapshot assignment)
        {
            if (playerSlotId.IsValid &&
                currentAssignments.TryGetValue(
                    playerSlotId,
                    out CurrentAssignmentRecord record))
            {
                assignment = CreateAssignmentSnapshot(record);
                return assignment.IsAssigned;
            }

            assignment = default;
            return false;
        }

        internal PlayerSlotAssignmentResult TryConfirmCurrentAssignment(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason)
        {
            return ResolveExpectedAssignment(
                "ConfirmCurrentAssignment",
                playerSlotId,
                expectedToken,
                source,
                reason,
                release: false);
        }

        /// <summary>
        /// Promotes an already-correlated Scene-provided assignment from its contextual
        /// Activity/Route owner to this Session. The opaque assignment token is intentionally
        /// retained: it is the physical Host/Actor correlation, not Activity ownership.
        /// </summary>
        internal PlayerSlotAssignmentResult TryPromoteSceneProvidedAssignmentToSession(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason)
        {
            const string operation = "PromoteSceneProvidedAssignmentToSession";
            PlayerSlotAssignmentResult confirmation = ResolveExpectedAssignment(
                operation,
                playerSlotId,
                expectedToken,
                source,
                reason,
                release: false);
            if (confirmation == null || !confirmation.Succeeded ||
                confirmation.CurrentAssignment.AssignmentOrigin !=
                    PlayerSlotAssignmentOrigin.SceneProvided)
            {
                return confirmation;
            }

            CurrentAssignmentRecord record = currentAssignments[playerSlotId];
            RuntimeContentOwner sessionOwner = CreateSessionAssignmentOwner();
            if (record.Owner == sessionOwner)
            {
                return confirmation;
            }

            currentAssignments[playerSlotId] = new CurrentAssignmentRecord(
                record.Slot,
                record.Origin,
                sessionOwner,
                record.Sequence,
                record.AssignmentRevision,
                record.Token,
                record.HostBindingIdentity,
                source.NormalizeTextOrFallback(nameof(PlayerParticipationRuntimeContext)),
                reason.NormalizeTextOrFallback("promote-scene-provided-assignment"));
            CurrentAssignmentRecord promoted = currentAssignments[playerSlotId];
            return AssignmentResult(
                PlayerSlotAssignmentStatus.SucceededConfirmed,
                operation,
                confirmation.PreviousAssignment,
                CreateAssignmentSnapshot(promoted),
                expectedToken,
                source.NormalizeText(),
                reason.NormalizeText(),
                "Scene-provided physical assignment ownership promoted from contextual Activity/Route to the Session.");
        }

        internal PlayerSlotAssignmentResult ReleaseAssignment(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason)
        {
            return ResolveExpectedAssignment(
                "ReleaseAssignment",
                playerSlotId,
                expectedToken,
                source,
                reason,
                release: true);
        }

        PlayerSlotAssignmentResult
            ISceneLocalPlayerAssignmentReleaseRuntimePort.ReleaseAssignment(
                PlayerSlotId playerSlotId,
                PlayerSlotAssignmentToken expectedToken,
                string source,
                string reason)
        {
            return ReleaseAssignment(
                playerSlotId,
                expectedToken,
                source,
                reason);
        }

        internal PlayerParticipationOperationResult TryAbandonJoinedSlotAfterAssignmentFailure(
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "assignment-admission-rollback");
            int previousRevision = revision;
            SlotRecord slot = playerSlotId.IsValid ? FindSlot(playerSlotId) : null;
            if (slot == null ||
                slot.AllocationState != PlayerSlotAllocationState.Joined ||
                currentAssignments.ContainsKey(playerSlotId))
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    "AbandonJoinedSlotAfterAssignmentFailure",
                    resolvedSource,
                    resolvedReason,
                    "Joined Slot assignment rollback requires a Joined Slot with no current assignment.",
                    previousRevision,
                    slot != null ? CreateSlotSnapshot(slot) : default,
                    default);
            }

            bool actorSelectionCleared = slot.SelectedActorProfile != null;
            if (actorSelectionCleared)
            {
                CommitActorSelection(
                    slot,
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            slot.AllocationState = PlayerSlotAllocationState.Available;
            slot.ReservationToken = default;
            slot.Revision++;
            slot.Source = resolvedSource;
            slot.Reason = resolvedReason;
            revision++;
            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "AbandonJoinedSlotAfterAssignmentFailure",
                resolvedSource,
                resolvedReason,
                actorSelectionCleared
                    ? "Persistent Actor selection cleared and Joined Slot admission rolled back after assignment failure."
                    : "Joined Slot admission rolled back after assignment failure.",
                previousRevision,
                CreateSlotSnapshot(slot),
                default);
        }

        internal PlayerParticipationOperationResult TryRestoreJoinedSlotAfterAssignmentReleaseFailure(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken expectedAssignmentToken,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "assignment-release-rollback");
            int previousRevision = revision;
            SlotRecord slot = playerSlotId.IsValid ? FindSlot(playerSlotId) : null;
            bool assignmentCurrent =
                currentAssignments.TryGetValue(
                    playerSlotId,
                    out CurrentAssignmentRecord assignment) &&
                assignment.Token == expectedAssignmentToken;
            if (slot == null ||
                slot.AllocationState != PlayerSlotAllocationState.Available ||
                !assignmentCurrent)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    "RestoreJoinedSlotAfterAssignmentReleaseFailure",
                    resolvedSource,
                    resolvedReason,
                    "Scene release compensation requires an Available Slot and the unchanged expected assignment.",
                    previousRevision,
                    slot != null ? CreateSlotSnapshot(slot) : default,
                    default);
            }

            slot.AllocationState = PlayerSlotAllocationState.Joined;
            slot.Revision++;
            slot.Source = resolvedSource;
            slot.Reason = resolvedReason;
            revision++;
            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "RestoreJoinedSlotAfterAssignmentReleaseFailure",
                resolvedSource,
                resolvedReason,
                "Scene Slot restored to Joined after assignment release failure.",
                previousRevision,
                CreateSlotSnapshot(slot),
                default);
        }

        private PlayerSlotAssignmentResult ResolveExpectedAssignment(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason,
            bool release)
        {
            string resolvedSource = source.NormalizeText();
            string resolvedReason = reason.NormalizeText();
            SlotRecord slot = playerSlotId.IsValid ? FindSlot(playerSlotId) : null;
            PlayerSlotAssignmentSnapshot unassigned = slot != null
                ? CreateUnassignedAssignmentSnapshot(slot)
                : default;

            if (!playerSlotId.IsValid)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedInvalidSlot,
                    operation,
                    default,
                    default,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    "Assignment operation requires a valid Player Slot identity.");
            }

            if (slot == null)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedSlotNotConfigured,
                    operation,
                    default,
                    default,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session.");
            }

            if (expectedToken.PlayerSlotId.IsValid &&
                expectedToken.PlayerSlotId != playerSlotId)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedTokenSlotMismatch,
                    operation,
                    unassigned,
                    unassigned,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    "Assignment token belongs to another Player Slot.");
            }

            if (!expectedToken.IsValid ||
                !string.Equals(
                    expectedToken.SessionContextId,
                    contextId,
                    StringComparison.Ordinal))
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedForeignToken,
                    operation,
                    unassigned,
                    unassigned,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    "Assignment token is invalid or belongs to another Session context.");
            }

            if (!currentAssignments.TryGetValue(
                    playerSlotId,
                    out CurrentAssignmentRecord record))
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedStaleToken,
                    operation,
                    unassigned,
                    unassigned,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    "Assignment token is stale because the Player Slot has no current assignment.");
            }

            PlayerSlotAssignmentSnapshot current = CreateAssignmentSnapshot(record);
            if (record.Token != expectedToken)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedStaleToken,
                    operation,
                    current,
                    current,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    "Assignment token is stale for the current Player Slot assignment.");
            }

            if (!release)
            {
                return AssignmentResult(
                    PlayerSlotAssignmentStatus.SucceededConfirmed,
                    operation,
                    current,
                    current,
                    expectedToken,
                    resolvedSource,
                    resolvedReason,
                    "Expected token confirms the current Player Slot assignment.");
            }

            currentAssignments.Remove(playerSlotId);
            return AssignmentResult(
                PlayerSlotAssignmentStatus.SucceededReleased,
                operation,
                current,
                CreateUnassignedAssignmentSnapshot(slot),
                expectedToken,
                resolvedSource,
                resolvedReason,
                "Current Player Slot assignment released; the previous token is now stale.");
        }

        private bool IsAssignmentOwnerValidForOrigin(
            PlayerSlotAssignmentOrigin origin,
            RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                return false;
            }

            return origin switch
            {
                PlayerSlotAssignmentOrigin.ManagerProvisioned =>
                    owner.Scope == RuntimeContentScope.Session &&
                    string.Equals(
                        owner.OwnerId,
                        contextId,
                        StringComparison.Ordinal),
                PlayerSlotAssignmentOrigin.SceneProvided =>
                    owner.Scope is RuntimeContentScope.Activity or
                        RuntimeContentScope.Route,
                _ => false
            };
        }

        private PlayerSlotAssignmentSnapshot CreateAssignmentSnapshot(
            CurrentAssignmentRecord record)
        {
            return new PlayerSlotAssignmentSnapshot(
                contextId,
                record.Slot.PlayerSlotId,
                record.Slot.ConfiguredIndex,
                PlayerSlotAssignmentState.Assigned,
                record.Origin,
                record.Owner,
                record.Sequence,
                record.AssignmentRevision,
                record.Token,
                record.HostBindingIdentity,
                record.Source,
                record.Reason);
        }

        private PlayerSlotAssignmentSnapshot CreateUnassignedAssignmentSnapshot(
            SlotRecord slot)
        {
            return new PlayerSlotAssignmentSnapshot(
                contextId,
                slot.PlayerSlotId,
                slot.ConfiguredIndex,
                PlayerSlotAssignmentState.Unassigned,
                PlayerSlotAssignmentOrigin.None,
                default,
                0,
                0,
                default,
                default,
                string.Empty,
                string.Empty);
        }

        private static PlayerSlotAssignmentResult AssignmentResult(
            PlayerSlotAssignmentStatus status,
            string operation,
            PlayerSlotAssignmentSnapshot previousAssignment,
            PlayerSlotAssignmentSnapshot currentAssignment,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason,
            string message)
        {
            return new PlayerSlotAssignmentResult(
                status,
                operation,
                previousAssignment,
                currentAssignment,
                expectedToken,
                source,
                reason,
                message);
        }
    }
}
