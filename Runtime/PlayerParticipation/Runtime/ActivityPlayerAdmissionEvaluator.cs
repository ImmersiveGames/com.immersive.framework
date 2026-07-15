using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Pure Activity-scoped evaluator for the progressive Player participation requirement chain.
    /// It reads immutable snapshots only. It does not mutate Session state, prepare Actors,
    /// publish cameras, create gameplay admissions or control Activity transitions.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.6 Activity GameplayReady admission evaluator.")]
    internal static class ActivityPlayerAdmissionEvaluator
    {
        private readonly struct ProjectedSlot
        {
            internal ProjectedSlot(int projectedIndex, PlayerSlotRuntimeSnapshot slot)
            {
                ProjectedIndex = projectedIndex;
                Slot = slot;
            }

            internal int ProjectedIndex { get; }
            internal PlayerSlotRuntimeSnapshot Slot { get; }
        }

        internal static ActivityPlayerAdmissionEvaluationResult Evaluate(
            ActivityAsset activity,
            PlayerParticipationSnapshot participationSnapshot,
            PlayerActorPreparationSnapshot preparationSnapshot,
            PlayerGameplayAdmissionSnapshot gameplayAdmissionSnapshot)
        {
            if (activity == null)
            {
                return GlobalFailure(
                    "<missing>",
                    ActivityPlayerAdmissionEvaluationCode.MissingActivity,
                    "Activity Player admission evaluation requires an explicit ActivityAsset.");
            }

            if (activity.PlayerParticipationProjectionProfile == null)
            {
                return GlobalFailure(
                    activity.ActivityName,
                    ActivityPlayerAdmissionEvaluationCode.MissingProjectionProfile,
                    $"Activity '{activity.ActivityName}' is missing its mandatory participation Projection Profile.");
            }

            PlayerParticipationRequirementsProfile requirements =
                activity.PlayerParticipationRequirementsProfile;
            if (requirements == null)
            {
                return GlobalFailure(
                    activity.ActivityName,
                    ActivityPlayerAdmissionEvaluationCode.MissingRequirementsProfile,
                    $"Activity '{activity.ActivityName}' is missing its mandatory Player Participation Requirements Profile.");
            }

            if (!requirements.HasDefinedRequirementLevel)
            {
                return GlobalFailure(
                    activity.ActivityName,
                    ActivityPlayerAdmissionEvaluationCode.InvalidRequirementLevel,
                    $"Activity '{activity.ActivityName}' has an undefined Player participation requirement level.");
            }

            if (!activity.TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string projectionIssue))
            {
                return new ActivityPlayerAdmissionEvaluationResult(
                    activity.ActivityName,
                    string.Empty,
                    activity.PlayerParticipationProjectionProfile.ProjectionMode,
                    activity.PlayerParticipationProjectionProfile.ZeroParticipantPolicy,
                    requirements.RequirementLevel,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.InvalidProjection,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    projectionIssue);
            }

            PlayerParticipationRequirementLevel level = requirements.RequirementLevel;
            if (descriptor.ProjectsNoSlots && level != PlayerParticipationRequirementLevel.None)
            {
                return new ActivityPlayerAdmissionEvaluationResult(
                    activity.ActivityName,
                    string.Empty,
                    descriptor.Mode,
                    descriptor.ZeroParticipantPolicy,
                    level,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.ContradictoryNoSlotsRequirement,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    "NoSlots projection can only be paired with the explicit None requirements Profile.");
            }

            if (descriptor.ProjectsNoSlots)
            {
                return new ActivityPlayerAdmissionEvaluationResult(
                    activity.ActivityName,
                    string.Empty,
                    descriptor.Mode,
                    descriptor.ZeroParticipantPolicy,
                    level,
                    ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    "Activity projects no Player Slots and explicitly requires no Player participation.");
            }

            if (participationSnapshot == null || !participationSnapshot.IsInitialized)
            {
                return new ActivityPlayerAdmissionEvaluationResult(
                    activity.ActivityName,
                    string.Empty,
                    descriptor.Mode,
                    descriptor.ZeroParticipantPolicy,
                    level,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingParticipationSnapshot,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    "Activity Player admission evaluation requires an initialized Session participation snapshot.");
            }

            if (Requires(level, PlayerParticipationRequirementLevel.LogicalActorsPrepared))
            {
                if (preparationSnapshot == null || !preparationSnapshot.IsInitialized)
                {
                    return SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.MissingPreparationSnapshot,
                        "LogicalActorsPrepared or GameplayReady requires an initialized Player Actor preparation snapshot.");
                }

                if (!string.Equals(
                        preparationSnapshot.SessionContextId,
                        participationSnapshot.ContextId,
                        StringComparison.Ordinal))
                {
                    return SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.SessionMismatch,
                        "Player participation and Actor preparation snapshots belong to different Session contexts.");
                }

                if (preparationSnapshot.ConfiguredSlotCount != participationSnapshot.ConfiguredSlotCount)
                {
                    return SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.SnapshotRosterMismatch,
                        "Player participation and Actor preparation snapshots expose different configured Slot rosters.");
                }
            }

            if (Requires(level, PlayerParticipationRequirementLevel.GameplayReady))
            {
                if (gameplayAdmissionSnapshot == null || !gameplayAdmissionSnapshot.IsInitialized)
                {
                    return SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.MissingGameplayAdmissionSnapshot,
                        "GameplayReady requires an initialized P3K.5 gameplay admission snapshot.");
                }

                if (!string.Equals(
                        gameplayAdmissionSnapshot.SessionContextId,
                        participationSnapshot.ContextId,
                        StringComparison.Ordinal))
                {
                    return SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.SessionMismatch,
                        "Player participation and gameplay admission snapshots belong to different Session contexts.");
                }

                if (gameplayAdmissionSnapshot.ConfiguredSlotCount != participationSnapshot.ConfiguredSlotCount)
                {
                    return SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.SnapshotRosterMismatch,
                        "Player participation and gameplay admission snapshots expose different configured Slot rosters.");
                }
            }

            if (!TryProjectSlots(
                    descriptor,
                    participationSnapshot,
                    out ProjectedSlot[] projected,
                    out ActivityPlayerAdmissionEvaluationResult projectionFailure,
                    activity,
                    level))
            {
                return projectionFailure;
            }

            if (projected.Length == 0)
            {
                bool allowed = descriptor.AllowsZeroParticipants;
                return new ActivityPlayerAdmissionEvaluationResult(
                    activity.ActivityName,
                    participationSnapshot.ContextId,
                    descriptor.Mode,
                    descriptor.ZeroParticipantPolicy,
                    level,
                    allowed
                        ? ActivityPlayerAdmissionEvaluationStatus.Satisfied
                        : ActivityPlayerAdmissionEvaluationStatus.Blocked,
                    allowed
                        ? ActivityPlayerAdmissionEvaluationCode.Satisfied
                        : ActivityPlayerAdmissionEvaluationCode.ZeroParticipantsRejected,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    allowed
                        ? "Activity projection is empty and its zero-participant policy allows activation."
                        : "Activity projection is empty and its zero-participant policy rejects activation.");
            }

            var results = new ActivityPlayerAdmissionSlotResult[projected.Length];
            for (int index = 0; index < projected.Length; index++)
            {
                results[index] = EvaluateSlot(
                    projected[index],
                    level,
                    preparationSnapshot,
                    gameplayAdmissionSnapshot);
            }

            ActivityPlayerAdmissionEvaluationStatus aggregate = Aggregate(results);
            ActivityPlayerAdmissionEvaluationCode code = AggregateCode(results, aggregate);
            return new ActivityPlayerAdmissionEvaluationResult(
                activity.ActivityName,
                participationSnapshot.ContextId,
                descriptor.Mode,
                descriptor.ZeroParticipantPolicy,
                level,
                aggregate,
                code,
                results,
                BuildAggregateMessage(activity.ActivityName, aggregate, results));
        }

        private static ActivityPlayerAdmissionSlotResult EvaluateSlot(
            ProjectedSlot projected,
            PlayerParticipationRequirementLevel level,
            PlayerActorPreparationSnapshot preparationSnapshot,
            PlayerGameplayAdmissionSnapshot gameplayAdmissionSnapshot)
        {
            PlayerSlotRuntimeSnapshot slot = projected.Slot;
            if (!slot.IsValid)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.JoinedSlot,
                    ActivityPlayerAdmissionEvaluationCode.InvalidSlotEvidence,
                    default,
                    default,
                    false,
                    false,
                    false,
                    false,
                    "Projected Session Slot evidence is invalid.");
            }

            if (!Requires(level, PlayerParticipationRequirementLevel.JoinedSlots))
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Satisfied,
                    ActivityPlayerAdmissionMissingRequirement.None,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied,
                    slot.SelectedActorProfileId,
                    default,
                    slot.IsJoined,
                    slot.HasSelectedActor,
                    false,
                    false,
                    "Projected Slot has no progressive Player readiness requirement.");
            }

            if (!slot.IsJoined)
            {
                bool pending =
                    slot.AllocationState == PlayerSlotAllocationState.Available ||
                    slot.AllocationState == PlayerSlotAllocationState.Reserved;
                return Result(
                    projected,
                    level,
                    pending
                        ? ActivityPlayerAdmissionSlotStatus.PendingResolution
                        : ActivityPlayerAdmissionSlotStatus.Blocked,
                    ActivityPlayerAdmissionMissingRequirement.JoinedSlot,
                    pending
                        ? ActivityPlayerAdmissionEvaluationCode.SlotNotJoined
                        : ActivityPlayerAdmissionEvaluationCode.SlotUnavailable,
                    slot.SelectedActorProfileId,
                    default,
                    false,
                    slot.HasSelectedActor,
                    false,
                    false,
                    pending
                        ? "Projected Slot has not completed Session join admission."
                        : $"Projected Slot cannot currently join from allocation state '{slot.AllocationState}'.");
            }

            if (!Requires(level, PlayerParticipationRequirementLevel.SelectedActors))
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Satisfied,
                    ActivityPlayerAdmissionMissingRequirement.None,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied,
                    slot.SelectedActorProfileId,
                    default,
                    true,
                    slot.HasSelectedActor,
                    false,
                    false,
                    "Projected Slot satisfies JoinedSlots.");
            }

            if (!slot.HasSelectedActor)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.PendingResolution,
                    ActivityPlayerAdmissionMissingRequirement.SelectedActor,
                    ActivityPlayerAdmissionEvaluationCode.SelectedActorMissing,
                    default,
                    default,
                    true,
                    false,
                    false,
                    false,
                    "Joined projected Slot has no selected ActorProfile.");
            }

            if (!Requires(level, PlayerParticipationRequirementLevel.LogicalActorsPrepared))
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Satisfied,
                    ActivityPlayerAdmissionMissingRequirement.None,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied,
                    slot.SelectedActorProfileId,
                    default,
                    true,
                    true,
                    false,
                    false,
                    "Projected Slot satisfies SelectedActors.");
            }

            if (!TryGetPreparation(
                    preparationSnapshot,
                    slot.PlayerSlotId,
                    out PlayerActorPreparationSummary preparation))
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.LogicalActorPrepared,
                    ActivityPlayerAdmissionEvaluationCode.PreparationMissing,
                    slot.SelectedActorProfileId,
                    default,
                    true,
                    true,
                    false,
                    false,
                    "Actor preparation snapshot has no entry for the projected Slot.");
            }

            if (!preparation.IsValid)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.LogicalActorPrepared,
                    ActivityPlayerAdmissionEvaluationCode.PreparationIdentityMismatch,
                    slot.SelectedActorProfileId,
                    default,
                    true,
                    true,
                    false,
                    false,
                    "Actor preparation evidence is structurally invalid.");
            }

            if (preparation.SelectionRevision != slot.SelectionRevision ||
                preparation.SelectedActorProfileId != slot.SelectedActorProfileId)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.LogicalActorPrepared,
                    ActivityPlayerAdmissionEvaluationCode.PreparationIdentityMismatch,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    false,
                    false,
                    "Actor preparation evidence is stale relative to the current Session Actor selection.");
            }

            if (preparation.IsReleaseFailed)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.LogicalActorPrepared,
                    ActivityPlayerAdmissionEvaluationCode.PreparationReleaseFailed,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    false,
                    false,
                    "Current Logical Actor preparation is retained in ReleaseFailed state.");
            }

            if (!preparation.IsPrepared)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.PendingResolution,
                    ActivityPlayerAdmissionMissingRequirement.LogicalActorPrepared,
                    ActivityPlayerAdmissionEvaluationCode.PreparationPending,
                    slot.SelectedActorProfileId,
                    default,
                    true,
                    true,
                    false,
                    false,
                    "Selected Logical Actor is not prepared yet.");
            }

            if (!preparation.Materialization.IsActive ||
                preparation.PreparedActorProfileId != slot.SelectedActorProfileId)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.LogicalActorPrepared,
                    ActivityPlayerAdmissionEvaluationCode.PreparationIdentityMismatch,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    false,
                    false,
                    "Prepared Logical Actor identity is incoherent or no longer Active.");
            }

            if (!Requires(level, PlayerParticipationRequirementLevel.GameplayReady))
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Satisfied,
                    ActivityPlayerAdmissionMissingRequirement.None,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    true,
                    false,
                    "Projected Slot satisfies LogicalActorsPrepared.");
            }

            if (!gameplayAdmissionSnapshot.TryGetSummary(
                    slot.PlayerSlotId,
                    out PlayerGameplayAdmissionSummary admission))
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.GameplayReady,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionMissing,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    true,
                    false,
                    "Gameplay admission snapshot has no entry for the projected Slot.");
            }

            if (!admission.IsValid ||
                admission.ActorProfileId.IsValid &&
                    admission.ActorProfileId != slot.SelectedActorProfileId ||
                admission.ActorId.IsValid &&
                    admission.ActorId != preparation.Materialization.ActorId ||
                admission.PreparationToken.IsValid &&
                    admission.PreparationToken != preparation.Token)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.GameplayReady,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionIdentityMismatch,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    true,
                    false,
                    "P3K.5 gameplay admission evidence is invalid, foreign or stale relative to the current preparation.");
            }

            if (admission.IsReleaseFailed)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.Failed,
                    ActivityPlayerAdmissionMissingRequirement.GameplayReady,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionReleaseFailed,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    true,
                    false,
                    "Gameplay admission is retained in ReleaseFailed state.");
            }

            if (admission.IsBlockedByInputGate)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.PendingResolution,
                    ActivityPlayerAdmissionMissingRequirement.GameplayReady,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionBlockedByInputGate,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    true,
                    false,
                    "Gameplay admission remains current but input is temporarily blocked by Gate.");
            }

            if (!admission.GameplayReady)
            {
                return Result(
                    projected,
                    level,
                    ActivityPlayerAdmissionSlotStatus.PendingResolution,
                    ActivityPlayerAdmissionMissingRequirement.GameplayReady,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionPending,
                    slot.SelectedActorProfileId,
                    preparation.Materialization.ActorId,
                    true,
                    true,
                    true,
                    false,
                    "Current Slot has not completed P3K.5 gameplay admission.");
            }

            return Result(
                projected,
                level,
                ActivityPlayerAdmissionSlotStatus.Satisfied,
                ActivityPlayerAdmissionMissingRequirement.None,
                ActivityPlayerAdmissionEvaluationCode.Satisfied,
                slot.SelectedActorProfileId,
                preparation.Materialization.ActorId,
                true,
                true,
                true,
                true,
                "Projected Slot satisfies GameplayReady with current P3K.5 evidence.");
        }

        private static bool TryProjectSlots(
            ActivityParticipationProjectionDescriptor descriptor,
            PlayerParticipationSnapshot participationSnapshot,
            out ProjectedSlot[] projected,
            out ActivityPlayerAdmissionEvaluationResult failure,
            ActivityAsset activity,
            PlayerParticipationRequirementLevel level)
        {
            failure = null;
            var values = new List<ProjectedSlot>();

            if (descriptor.ProjectsAllJoinedSlots)
            {
                for (int index = 0; index < participationSnapshot.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot slot = participationSnapshot.Slots[index];
                    if (slot.IsJoined)
                    {
                        values.Add(new ProjectedSlot(values.Count, slot));
                    }
                }

                projected = values.ToArray();
                return true;
            }

            if (!descriptor.ProjectsExplicitSlots)
            {
                projected = Array.Empty<ProjectedSlot>();
                return true;
            }

            for (int index = 0; index < descriptor.ExplicitSlotProfiles.Count; index++)
            {
                PlayerSlotProfile profile = descriptor.ExplicitSlotProfiles[index];
                PlayerSlotId slotId = default;
                string issue = "Explicit projection contains invalid PlayerSlotProfile evidence.";
                if (profile == null ||
                    !profile.TryGetPlayerSlotId(out slotId, out issue))
                {
                    projected = Array.Empty<ProjectedSlot>();
                    failure = SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.InvalidProjection,
                        issue);
                    return false;
                }

                if (!TryGetParticipationSlot(participationSnapshot, slotId, out PlayerSlotRuntimeSnapshot slot))
                {
                    projected = Array.Empty<ProjectedSlot>();
                    failure = SnapshotFailure(
                        activity,
                        participationSnapshot.ContextId,
                        descriptor,
                        level,
                        ActivityPlayerAdmissionEvaluationCode.SlotNotConfigured,
                        $"Explicitly projected Player Slot '{slotId.StableText}' is not configured in the current Session roster.");
                    return false;
                }

                values.Add(new ProjectedSlot(index, slot));
            }

            projected = values.ToArray();
            return true;
        }

        private static bool TryGetParticipationSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot result)
        {
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                {
                    result = snapshot.Slots[index];
                    return true;
                }
            }

            result = default;
            return false;
        }

        private static bool TryGetPreparation(
            PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary result)
        {
            if (snapshot != null)
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                    {
                        result = snapshot.Slots[index];
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private static bool Requires(
            PlayerParticipationRequirementLevel current,
            PlayerParticipationRequirementLevel required)
        {
            return (int)current >= (int)required;
        }

        private static ActivityPlayerAdmissionSlotResult Result(
            ProjectedSlot projected,
            PlayerParticipationRequirementLevel requirement,
            ActivityPlayerAdmissionSlotStatus status,
            ActivityPlayerAdmissionMissingRequirement missing,
            ActivityPlayerAdmissionEvaluationCode code,
            ActorProfileId selectedActorProfileId,
            ActorId preparedActorId,
            bool joined,
            bool selected,
            bool prepared,
            bool ready,
            string message)
        {
            return new ActivityPlayerAdmissionSlotResult(
                projected.ProjectedIndex,
                projected.Slot.ConfiguredIndex,
                projected.Slot.PlayerSlotId,
                requirement,
                status,
                missing,
                code,
                selectedActorProfileId,
                preparedActorId,
                joined,
                selected,
                prepared,
                ready,
                message);
        }

        private static ActivityPlayerAdmissionEvaluationStatus Aggregate(
            ActivityPlayerAdmissionSlotResult[] results)
        {
            bool blocked = false;
            bool pending = false;
            for (int index = 0; index < results.Length; index++)
            {
                if (results[index].IsFailed)
                {
                    return ActivityPlayerAdmissionEvaluationStatus.Failed;
                }

                blocked |= results[index].IsBlocked;
                pending |= results[index].IsPendingResolution;
            }

            if (blocked)
            {
                return ActivityPlayerAdmissionEvaluationStatus.Blocked;
            }

            return pending
                ? ActivityPlayerAdmissionEvaluationStatus.PendingResolution
                : ActivityPlayerAdmissionEvaluationStatus.Satisfied;
        }

        private static ActivityPlayerAdmissionEvaluationCode AggregateCode(
            ActivityPlayerAdmissionSlotResult[] results,
            ActivityPlayerAdmissionEvaluationStatus aggregate)
        {
            ActivityPlayerAdmissionSlotStatus target = aggregate switch
            {
                ActivityPlayerAdmissionEvaluationStatus.Failed => ActivityPlayerAdmissionSlotStatus.Failed,
                ActivityPlayerAdmissionEvaluationStatus.Blocked => ActivityPlayerAdmissionSlotStatus.Blocked,
                ActivityPlayerAdmissionEvaluationStatus.PendingResolution => ActivityPlayerAdmissionSlotStatus.PendingResolution,
                _ => ActivityPlayerAdmissionSlotStatus.Satisfied
            };

            for (int index = 0; index < results.Length; index++)
            {
                if (results[index].Status == target)
                {
                    return results[index].Code;
                }
            }

            return ActivityPlayerAdmissionEvaluationCode.Satisfied;
        }

        private static string BuildAggregateMessage(
            string activityName,
            ActivityPlayerAdmissionEvaluationStatus status,
            ActivityPlayerAdmissionSlotResult[] results)
        {
            int satisfied = 0;
            int pending = 0;
            int blocked = 0;
            int failed = 0;
            for (int index = 0; index < results.Length; index++)
            {
                switch (results[index].Status)
                {
                    case ActivityPlayerAdmissionSlotStatus.Satisfied: satisfied++; break;
                    case ActivityPlayerAdmissionSlotStatus.PendingResolution: pending++; break;
                    case ActivityPlayerAdmissionSlotStatus.Blocked: blocked++; break;
                    case ActivityPlayerAdmissionSlotStatus.Failed: failed++; break;
                }
            }

            return
                $"Activity '{activityName}' Player admission evaluation is '{status}'. " +
                $"projected='{results.Length}' satisfied='{satisfied}' pending='{pending}' blocked='{blocked}' failed='{failed}'.";
        }

        private static ActivityPlayerAdmissionEvaluationResult SnapshotFailure(
            ActivityAsset activity,
            string sessionContextId,
            ActivityParticipationProjectionDescriptor descriptor,
            PlayerParticipationRequirementLevel level,
            ActivityPlayerAdmissionEvaluationCode code,
            string message)
        {
            return new ActivityPlayerAdmissionEvaluationResult(
                activity.ActivityName,
                sessionContextId,
                descriptor.Mode,
                descriptor.ZeroParticipantPolicy,
                level,
                ActivityPlayerAdmissionEvaluationStatus.Failed,
                code,
                Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                message);
        }

        private static ActivityPlayerAdmissionEvaluationResult GlobalFailure(
            string activityName,
            ActivityPlayerAdmissionEvaluationCode code,
            string message)
        {
            return new ActivityPlayerAdmissionEvaluationResult(
                activityName,
                string.Empty,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                PlayerParticipationRequirementLevel.None,
                ActivityPlayerAdmissionEvaluationStatus.Failed,
                code,
                Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                message);
        }
    }
}
