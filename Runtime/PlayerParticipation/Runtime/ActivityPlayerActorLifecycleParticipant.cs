using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Pause;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Required Activity Content Execution participant that projects Session Slots and coordinates
    /// Activity-owned Logical Player Actor selection, preparation and release.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.6 Activity-scoped Player Actor lifecycle participant and explicit participant source.")]
    internal sealed partial class ActivityPlayerActorLifecycleParticipant :
        IActivityContentExecutionParticipant,
        IActivityContentExecutionParticipantSource,
        IPauseActivityBindingPlayerEvidence
    {
        private const string ParticipantContentId =
            "framework.player-actor.activity-lifecycle";
        private const int ParticipantOrder = -200;

        private sealed class ActiveActivityRecord
        {
            internal ActiveActivityRecord(
                ActivityAsset activity,
                RuntimeContentOwner owner,
                PlayerParticipationRequirementLevel requirementLevel,
                int projectedSlotCount,
                int selectedCount,
                List<PreparedSlotRecord> preparedSlots,
                IReadOnlyList<LocalPlayerHostAuthoring> admittedHosts)
            {
                Activity = activity;
                Owner = owner;
                RequirementLevel = requirementLevel;
                ProjectedSlotCount = projectedSlotCount;
                SelectedCount = selectedCount;
                PreparedSlots = preparedSlots ?? new List<PreparedSlotRecord>();
                AdmittedHosts = admittedHosts ?? Array.Empty<LocalPlayerHostAuthoring>();
            }

            internal ActivityAsset Activity { get; }
            internal RuntimeContentOwner Owner { get; }
            internal PlayerParticipationRequirementLevel RequirementLevel { get; }
            internal int ProjectedSlotCount { get; }
            internal int SelectedCount { get; }
            internal List<PreparedSlotRecord> PreparedSlots { get; }
            internal IReadOnlyList<LocalPlayerHostAuthoring> AdmittedHosts { get; }
        }

        private readonly struct PreparedSlotRecord
        {
            internal PreparedSlotRecord(
                PlayerSlotId playerSlotId,
                PlayerActorPreparationToken token,
                bool createdByEnter)
            {
                PlayerSlotId = playerSlotId;
                Token = token;
                CreatedByEnter = createdByEnter;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerActorPreparationToken Token { get; }
            internal bool CreatedByEnter { get; }
        }

        private readonly struct AppliedSelectionRecord
        {
            internal AppliedSelectionRecord(PlayerSlotId playerSlotId, int selectionRevision)
            {
                PlayerSlotId = playerSlotId;
                SelectionRevision = selectionRevision;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal int SelectionRevision { get; }
        }

        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly PlayerParticipationRuntimeContext participationContext;
        private ActiveActivityRecord activeRecord;
        private ActivityPlayerActorLifecycleSnapshot lastSnapshot =
            ActivityPlayerActorLifecycleSnapshot.Empty(
                "Activity Player Actor lifecycle has not executed.");

        internal ActivityPlayerActorLifecycleParticipant(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerParticipationRuntimeContext participationContext)
        {
            this.preparationModule = preparationModule ??
                throw new ArgumentNullException(nameof(preparationModule));
            this.participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
        }

        internal ActivityPlayerActorLifecycleSnapshot Snapshot => lastSnapshot;

        public bool TryResolveAdmittedHosts(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            out IReadOnlyList<LocalPlayerHostAuthoring> hosts,
            out string diagnostic)
        {
            hosts = Array.Empty<LocalPlayerHostAuthoring>();
            diagnostic = string.Empty;
            if (activeRecord == null ||
                !ReferenceEquals(activeRecord.Activity, activity) ||
                activeRecord.Owner != owner)
            {
                diagnostic = "Official Activity Player lifecycle has no admitted Host evidence for this Activity owner.";
                return false;
            }

            hosts = activeRecord.AdmittedHosts;
            return true;
        }

        public ActivityContentExecutionParticipantSourceResult
            ResolveActivityContentExecutionParticipants(
                ActivityContentExecutionParticipantSourceRequest request)
        {
            if (!request.IsValid)
            {
                return ActivityContentExecutionParticipantSourceResult.RejectedInvalidRequest(
                    request,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-source-invalid-request",
                    "Activity Player Actor lifecycle requires an Activity transition request.");
            }

            var participants = new IActivityContentExecutionParticipant[] { this };
            ActivityContentExecutionParticipantCollection collection =
                ActivityContentExecutionParticipantCollection.FromParticipants(participants);
            return ActivityContentExecutionParticipantSourceResult.FromCollection(
                request,
                collection,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-source",
                "Activity Player Actor lifecycle participant supplied explicitly by the FrameworkRuntimeHost.");
        }

        public ActivityContentExecutionParticipantDescriptor
            GetActivityContentExecutionDescriptor()
        {
            return ActivityContentExecutionParticipantDescriptor.Required(
                RuntimeContentId.From(ParticipantContentId),
                supportsEnter: true,
                supportsExit: true,
                order: ParticipantOrder,
                displayName: "Activity Player Actor Lifecycle",
                source: nameof(ActivityPlayerActorLifecycleParticipant),
                reason: "activity-player-actor-lifecycle");
        }

        public ActivityContentExecutionResult ExecuteActivityContent(
            ActivityContentExecutionRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException(
                    "Activity Player Actor lifecycle received an invalid Activity Content Execution request.",
                    nameof(request));
            }

            if (request.Scope != RuntimeContentScope.Activity ||
                !request.RuntimeScopeContext.IsValid)
            {
                return ActivityContentExecutionResult.BlockingFailure(
                    request,
                    1,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-invalid-context",
                    "Activity Player Actor lifecycle requires the exact valid Activity RuntimeScopeContext supplied by ActivityFlow.");
            }

            return request.Phase switch
            {
                ActivityContentExecutionPhase.Enter => ExecuteEnter(request),
                ActivityContentExecutionPhase.Exit => ExecuteExit(request),
                _ => ActivityContentExecutionResult.BlockingFailure(
                    request,
                    1,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-unsupported-phase",
                    $"Unsupported Activity Player Actor lifecycle phase '{request.Phase}'.")
            };
        }

        private ActivityContentExecutionResult ExecuteEnter(
            ActivityContentExecutionRequest request)
        {
            ActivityAsset activity = request.Activity;
            RuntimeContentOwner owner = request.Owner;
            if (activeRecord != null)
            {
                if (ReferenceEquals(activeRecord.Activity, activity) &&
                    activeRecord.Owner == owner)
                {
                    return ActivityContentExecutionResult.SucceededNoOp(
                        request,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-player-actor-enter-idempotent",
                        "Activity Player Actor lifecycle is already entered for the same Activity owner.");
                }

                lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus.RejectedForeignOrStaleActivity,
                    activity != null ? activity.ActivityName : string.Empty,
                    owner,
                    PlayerParticipationRequirementLevel.None,
                    0,
                    0,
                    0,
                    0,
                    1,
                    Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                    $"Activity enter owner '{owner.StableText}' cannot replace retained owner '{activeRecord.Owner.StableText}' without an exit phase.");
                return ActivityContentExecutionResult.BlockingFailure(
                    request,
                    1,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-stale-active-owner",
                    lastSnapshot.Message);
            }

            if (!TryResolveProjection(
                    activity,
                    out PlayerParticipationRequirementLevel requirementLevel,
                    out List<PlayerSlotRuntimeSnapshot> projectedSlots,
                    out string projectionIssue))
            {
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedProjection,
                    activity,
                    owner,
                    requirementLevel,
                    projectedSlots,
                    projectionIssue);
                return Blocking(request, "activity-player-actor-projection-failed", projectionIssue);
            }

            if (requirementLevel == PlayerParticipationRequirementLevel.GameplayReady)
            {
                return ExecuteGameplayReadyAdoptionEnter(
                    request,
                    activity,
                    owner,
                    projectedSlots);
            }

            if (projectedSlots.Count == 0)
            {
                activeRecord = new ActiveActivityRecord(
                    activity,
                    owner,
                    requirementLevel,
                    0,
                    0,
                    new List<PreparedSlotRecord>(),
                    Array.Empty<LocalPlayerHostAuthoring>());
                lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus.SucceededEnteredNoParticipants,
                    activity.ActivityName,
                    owner,
                    requirementLevel,
                    0,
                    0,
                    0,
                    0,
                    0,
                    Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                    "Activity Player Actor lifecycle entered with no projected Player Slots.");
                return ActivityContentExecutionResult.SucceededNoOp(
                    request,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-enter-no-participants",
                    lastSnapshot.Message);
            }

            var prepared = new List<PreparedSlotRecord>();
            var appliedSelections = new List<AppliedSelectionRecord>();
            var slotEvidence = new List<ActivityPlayerActorSlotLifecycleSnapshot>();
            var admittedHosts = new List<LocalPlayerHostAuthoring>();
            int selectedCount = 0;
            int preparedCount = 0;

            for (int index = 0; index < projectedSlots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                if ((int)requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.JoinedSlots &&
                    !slot.IsJoined)
                {
                    return FailEnterAndRollback(
                        request,
                        activity,
                        owner,
                        requirementLevel,
                        projectedSlots.Count,
                        prepared,
                        appliedSelections,
                        slotEvidence,
                        ActivityPlayerActorLifecycleStatus.FailedRequirement,
                        $"Projected Player Slot '{slot.PlayerSlotId.StableText}' is not Joined.");
                }

                if (slot.IsJoined)
                {
                    if (!preparationModule.TryGetRegisteredHost(
                            slot.PlayerSlotId,
                            out LocalPlayerHostAuthoring host,
                            out string hostIssue))
                    {
                        return FailEnterAndRollback(
                            request,
                            activity,
                            owner,
                            requirementLevel,
                            projectedSlots.Count,
                            prepared,
                            appliedSelections,
                            slotEvidence,
                            ActivityPlayerActorLifecycleStatus.FailedRequirement,
                            hostIssue);
                    }

                    admittedHosts.Add(host);
                }

                bool selectionApplied = false;
                if ((int)requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.SelectedActors)
                {
                    if (!slot.HasSelectedActor)
                    {
                        PlayerActorSelectionResult selection =
                            preparationModule.TrySelectDefaultActor(
                                slot.PlayerSlotId,
                                slot.SelectionRevision,
                                nameof(ActivityPlayerActorLifecycleParticipant),
                                "activity-enter-select-default-actor");
                        if (selection == null || !selection.Succeeded)
                        {
                            string issue = selection != null
                                ? selection.ToDiagnosticString()
                                : $"Default Actor selection returned no result for Slot '{slot.PlayerSlotId.StableText}'.";
                            return FailEnterAndRollback(
                                request,
                                activity,
                                owner,
                                requirementLevel,
                                projectedSlots.Count,
                                prepared,
                                appliedSelections,
                                slotEvidence,
                                ActivityPlayerActorLifecycleStatus.FailedSelection,
                                issue);
                        }

                        slot = selection.Slot;
                        selectionApplied = selection.StateChanged;
                        if (selectionApplied)
                        {
                            appliedSelections.Add(new AppliedSelectionRecord(
                                slot.PlayerSlotId,
                                selection.SelectionRevision));
                        }
                    }

                    if (!slot.HasSelectedActor)
                    {
                        return FailEnterAndRollback(
                            request,
                            activity,
                            owner,
                            requirementLevel,
                            projectedSlots.Count,
                            prepared,
                            appliedSelections,
                            slotEvidence,
                            ActivityPlayerActorLifecycleStatus.FailedSelection,
                            $"Projected Player Slot '{slot.PlayerSlotId.StableText}' has no selected Actor after default selection.");
                    }

                    selectedCount++;
                }

                PlayerActorPreparationToken token = default;
                bool preparationApplied = false;
                PlayerActorPreparationStatus preparationStatus =
                    PlayerActorPreparationStatus.None;
                string message = "Requirement satisfied without Logical Actor preparation.";

                if ((int)requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.LogicalActorsPrepared)
                {
                    PlayerActorPreparationResult preparation =
                        preparationModule.TryPrepareSelectedActor(
                            request.RuntimeScopeContext,
                            slot.PlayerSlotId,
                            nameof(ActivityPlayerActorLifecycleParticipant),
                            "activity-enter-prepare-selected-actor");
                    if (preparation == null || !preparation.Succeeded)
                    {
                        string issue = preparation != null
                            ? preparation.ToDiagnosticString()
                            : $"Logical Actor preparation returned no result for Slot '{slot.PlayerSlotId.StableText}'.";
                        return FailEnterAndRollback(
                            request,
                            activity,
                            owner,
                            requirementLevel,
                            projectedSlots.Count,
                            prepared,
                            appliedSelections,
                            slotEvidence,
                            ActivityPlayerActorLifecycleStatus.FailedPreparation,
                            issue);
                    }

                    token = preparation.CurrentSummary.Token;
                    preparationApplied =
                        preparation.Status ==
                        PlayerActorPreparationStatus.SucceededPrepared;
                    preparationStatus = preparation.Status;
                    message = preparation.Message;
                    prepared.Add(new PreparedSlotRecord(
                        slot.PlayerSlotId,
                        token,
                        preparationApplied));
                    preparedCount++;
                }

                slotEvidence.Add(new ActivityPlayerActorSlotLifecycleSnapshot(
                    slot.PlayerSlotId,
                    slot.IsJoined,
                    slot.SelectedActorProfileId,
                    selectionApplied,
                    token,
                    preparationApplied,
                    false,
                    preparationStatus,
                    message));
            }

            activeRecord = new ActiveActivityRecord(
                activity,
                owner,
                requirementLevel,
                projectedSlots.Count,
                selectedCount,
                prepared,
                admittedHosts);
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                ActivityPlayerActorLifecycleStatus.SucceededEntered,
                activity.ActivityName,
                owner,
                requirementLevel,
                projectedSlots.Count,
                selectedCount,
                preparedCount,
                0,
                0,
                slotEvidence.ToArray(),
                "Activity Player Actor lifecycle entered successfully.");
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-entered",
                lastSnapshot.ToDiagnosticString());
        }

        private ActivityContentExecutionResult ExecuteExit(
            ActivityContentExecutionRequest request)
        {
            if (activeRecord == null &&
                TryExecuteCommittedGameplayHandoffExit(
                    request,
                    out ActivityContentExecutionResult
                        handoffExitWithoutRetainedRecord))
            {
                return handoffExitWithoutRetainedRecord;
            }

            if (activeRecord == null)
            {
                lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus.SucceededExitedNoActors,
                    request.Activity.ActivityName,
                    request.Owner,
                    ResolveRequirementLevel(request.Activity),
                    0,
                    0,
                    0,
                    0,
                    0,
                    Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                    "Activity exit had no Activity-owned Player Actors to release.");
                return ActivityContentExecutionResult.SucceededNoOp(
                    request,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-exit-no-actors",
                    lastSnapshot.Message);
            }

            if (!ReferenceEquals(activeRecord.Activity, request.Activity) ||
                activeRecord.Owner != request.Owner)
            {
                string issue =
                    $"Activity exit owner '{request.Owner.StableText}' does not match retained Player Actor owner '{activeRecord.Owner.StableText}'.";
                lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus.RejectedForeignOrStaleActivity,
                    request.Activity.ActivityName,
                    request.Owner,
                    activeRecord.RequirementLevel,
                    activeRecord.ProjectedSlotCount,
                    activeRecord.SelectedCount,
                    activeRecord.PreparedSlots.Count,
                    0,
                    1,
                    Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                    issue);
                return Blocking(request, "activity-player-actor-exit-stale-owner", issue);
            }

            if (TryExecuteCommittedGameplayHandoffExit(
                    request,
                    out ActivityContentExecutionResult handoffExitResult))
            {
                return handoffExitResult;
            }

            var evidence = new List<ActivityPlayerActorSlotLifecycleSnapshot>();
            var failures = new List<string>();
            int releasedCount = 0;
            for (int index = 0; index < activeRecord.PreparedSlots.Count; index++)
            {
                PreparedSlotRecord prepared = activeRecord.PreparedSlots[index];
                if (!TryReleaseGameplayBeforePreparedActor(
                        prepared,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-exit-release-gameplay-before-actor",
                        out string gameplayReleaseIssue))
                {
                    failures.Add(gameplayReleaseIssue);
                    evidence.Add(new ActivityPlayerActorSlotLifecycleSnapshot(
                        prepared.PlayerSlotId,
                        true,
                        default,
                        false,
                        prepared.Token,
                        prepared.CreatedByEnter,
                        false,
                        PlayerActorPreparationStatus.FailedRelease,
                        gameplayReleaseIssue));
                    continue;
                }

                PlayerActorPreparationResult release =
                    preparationModule.TryReleasePreparedActor(
                        prepared.PlayerSlotId,
                        prepared.Token,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-exit-release-prepared-actor");
                bool succeeded = release != null && release.Succeeded;
                if (succeeded)
                {
                    releasedCount++;
                }
                else
                {
                    failures.Add(release != null
                        ? release.ToDiagnosticString()
                        : $"Release returned no result for Slot '{prepared.PlayerSlotId.StableText}'.");
                }

                evidence.Add(new ActivityPlayerActorSlotLifecycleSnapshot(
                    prepared.PlayerSlotId,
                    true,
                    release != null
                        ? release.PreviousSummary.SelectedActorProfileId
                        : default,
                    false,
                    prepared.Token,
                    prepared.CreatedByEnter,
                    succeeded,
                    release != null
                        ? release.Status
                        : PlayerActorPreparationStatus.FailedRelease,
                    release != null
                        ? release.Message
                        : "Prepared Actor release returned no result."));
            }

            if (failures.Count > 0)
            {
                string issue = string.Join(" | ", failures);
                lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRelease,
                    request.Activity.ActivityName,
                    request.Owner,
                    activeRecord.RequirementLevel,
                    activeRecord.ProjectedSlotCount,
                    activeRecord.SelectedCount,
                    activeRecord.PreparedSlots.Count,
                    releasedCount,
                    failures.Count,
                    evidence.ToArray(),
                    issue);
                return Blocking(request, "activity-player-actor-release-failed", issue);
            }

            PlayerParticipationRequirementLevel requirementLevel =
                activeRecord.RequirementLevel;
            int activeRecordProjectedSlotCount = activeRecord.ProjectedSlotCount;
            int activeRecordSelectedCount = activeRecord.SelectedCount;
            int preparedCount = activeRecord.PreparedSlots.Count;
            activeRecord = null;
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                preparedCount > 0
                    ? ActivityPlayerActorLifecycleStatus.SucceededExited
                    : ActivityPlayerActorLifecycleStatus.SucceededExitedNoActors,
                request.Activity.ActivityName,
                request.Owner,
                requirementLevel,
                activeRecordProjectedSlotCount,
                activeRecordSelectedCount,
                preparedCount,
                releasedCount,
                0,
                evidence.ToArray(),
                preparedCount > 0
                    ? "Activity-owned Player Actors released before Activity scope exit."
                    : "Activity exit completed with no prepared Player Actors.");
            return preparedCount > 0
                ? ActivityContentExecutionResult.Success(
                    request,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-exited",
                    lastSnapshot.ToDiagnosticString())
                : ActivityContentExecutionResult.SucceededNoOp(
                    request,
                    nameof(ActivityPlayerActorLifecycleParticipant),
                    "activity-player-actor-exit-no-actors",
                    lastSnapshot.Message);
        }

        private bool TryResolveProjection(
            ActivityAsset activity,
            out PlayerParticipationRequirementLevel requirementLevel,
            out List<PlayerSlotRuntimeSnapshot> projectedSlots,
            out string issue)
        {
            requirementLevel = ResolveRequirementLevel(activity);
            projectedSlots = new List<PlayerSlotRuntimeSnapshot>();
            issue = string.Empty;

            if (activity == null)
            {
                issue = "Activity Player Actor lifecycle requires an Activity.";
                return false;
            }

            if (!activity.TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out issue))
            {
                return false;
            }

            if (!activity.HasDefinedPlayerParticipationRequirementLevel)
            {
                issue =
                    $"Activity '{activity.ActivityName}' has an invalid Player participation Requirement Level.";
                return false;
            }

            requirementLevel = activity.PlayerParticipationRequirementLevel;
            PlayerParticipationSnapshot session = participationContext.CreateSnapshot();
            if (session == null || !session.IsInitialized)
            {
                issue = "Session Player participation snapshot is unavailable.";
                return false;
            }

            if (descriptor.ProjectsNoSlots)
            {
                if (requirementLevel != PlayerParticipationRequirementLevel.None)
                {
                    issue =
                        $"Activity '{activity.ActivityName}' projects no Slots but requires '{requirementLevel}'.";
                    return false;
                }

                return true;
            }

            if (descriptor.ProjectsAllJoinedSlots)
            {
                for (int index = 0; index < session.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot slot = session.Slots[index];
                    if (slot.IsJoined)
                    {
                        projectedSlots.Add(slot);
                    }
                }

                if (projectedSlots.Count == 0 && !descriptor.AllowsZeroParticipants)
                {
                    issue =
                        $"Activity '{activity.ActivityName}' rejects zero projected participants.";
                    return false;
                }

                return true;
            }

            if (!descriptor.ProjectsExplicitSlots)
            {
                issue =
                    $"Activity '{activity.ActivityName}' has unsupported projection mode '{descriptor.Mode}'.";
                return false;
            }

            for (int explicitIndex = 0;
                 explicitIndex < descriptor.ExplicitSlotProfiles.Count;
                 explicitIndex++)
            {
                PlayerSlotProfile profile =
                    descriptor.ExplicitSlotProfiles[explicitIndex];
                string profileIssue = string.Empty;
                PlayerSlotId expectedSlotId = default;
                if (profile == null ||
                    !profile.TryGetPlayerSlotId(
                        out expectedSlotId,
                        out profileIssue))
                {
                    issue = string.IsNullOrWhiteSpace(profileIssue)
                        ? $"Explicit projection entry '{explicitIndex}' is invalid."
                        : profileIssue;
                    return false;
                }

                bool found = false;
                for (int slotIndex = 0; slotIndex < session.Slots.Count; slotIndex++)
                {
                    PlayerSlotRuntimeSnapshot slot = session.Slots[slotIndex];
                    if (slot.PlayerSlotId != expectedSlotId)
                    {
                        continue;
                    }

                    projectedSlots.Add(slot);
                    found = true;
                    break;
                }

                if (!found)
                {
                    issue =
                        $"Explicit projected Player Slot '{expectedSlotId.StableText}' is not configured in the Session.";
                    return false;
                }
            }

            return true;
        }

        private ActivityContentExecutionResult FailEnterAndRollback(
            ActivityContentExecutionRequest request,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            PlayerParticipationRequirementLevel requirementLevel,
            int projectedSlotCount,
            List<PreparedSlotRecord> prepared,
            List<AppliedSelectionRecord> appliedSelections,
            List<ActivityPlayerActorSlotLifecycleSnapshot> evidence,
            ActivityPlayerActorLifecycleStatus failureStatus,
            string issue)
        {
            var rollbackIssues = new List<string>();
            for (int index = prepared.Count - 1; index >= 0; index--)
            {
                PreparedSlotRecord item = prepared[index];
                if (!item.CreatedByEnter)
                {
                    continue;
                }

                PlayerActorPreparationResult release =
                    preparationModule.TryReleasePreparedActor(
                        item.PlayerSlotId,
                        item.Token,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-rollback-release");
                if (release == null || !release.Succeeded)
                {
                    rollbackIssues.Add(release != null
                        ? release.ToDiagnosticString()
                        : $"Rollback release returned no result for Slot '{item.PlayerSlotId.StableText}'.");
                }
            }

            for (int index = appliedSelections.Count - 1; index >= 0; index--)
            {
                AppliedSelectionRecord selection = appliedSelections[index];
                PlayerActorSelectionResult clear =
                    preparationModule.TryClearActorSelection(
                        new PlayerActorSelectionRequest(
                            selection.PlayerSlotId,
                            null,
                            nameof(ActivityPlayerActorLifecycleParticipant),
                            "activity-enter-rollback-selection",
                            selection.SelectionRevision));
                if (clear == null || !clear.Succeeded)
                {
                    rollbackIssues.Add(clear != null
                        ? clear.ToDiagnosticString()
                        : $"Selection rollback returned no result for Slot '{selection.PlayerSlotId.StableText}'.");
                }
            }

            ActivityPlayerActorLifecycleStatus finalStatus =
                rollbackIssues.Count == 0
                    ? failureStatus
                    : ActivityPlayerActorLifecycleStatus.FailedRollback;
            string finalIssue = rollbackIssues.Count == 0
                ? issue
                : $"{issue} Rollback failures: {string.Join(" | ", rollbackIssues)}";
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                finalStatus,
                activity != null ? activity.ActivityName : string.Empty,
                owner,
                requirementLevel,
                projectedSlotCount,
                appliedSelections.Count,
                prepared.Count,
                0,
                1 + rollbackIssues.Count,
                evidence.ToArray(),
                finalIssue);
            return Blocking(
                request,
                finalStatus == ActivityPlayerActorLifecycleStatus.FailedRollback
                    ? "activity-player-actor-enter-rollback-failed"
                    : "activity-player-actor-enter-failed",
                finalIssue,
                1 + rollbackIssues.Count);
        }

        private ActivityPlayerActorLifecycleSnapshot FailureSnapshot(
            ActivityPlayerActorLifecycleStatus status,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            PlayerParticipationRequirementLevel requirementLevel,
            List<PlayerSlotRuntimeSnapshot> projectedSlots,
            string issue)
        {
            return new ActivityPlayerActorLifecycleSnapshot(
                status,
                activity != null ? activity.ActivityName : string.Empty,
                owner,
                requirementLevel,
                projectedSlots != null ? projectedSlots.Count : 0,
                0,
                0,
                0,
                1,
                Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                issue);
        }

        private static PlayerParticipationRequirementLevel ResolveRequirementLevel(
            ActivityAsset activity)
        {
            return activity != null &&
                   activity.HasDefinedPlayerParticipationRequirementLevel
                ? activity.PlayerParticipationRequirementLevel
                : PlayerParticipationRequirementLevel.None;
        }

        private static ActivityContentExecutionResult Blocking(
            ActivityContentExecutionRequest request,
            string reason,
            string message,
            int issueCount = 1)
        {
            return ActivityContentExecutionResult.BlockingFailure(
                request,
                issueCount,
                nameof(ActivityPlayerActorLifecycleParticipant),
                reason,
                message);
        }
    }
}
