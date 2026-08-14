using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Same-host, read-only projection for the existing Session-scoped Local
    /// Player provisioning module. It owns no lifecycle state and performs no
    /// mutation. Every value is read from the already established runtime
    /// authorities when the caller requests a snapshot.
    /// </summary>
    internal static class
        LocalPlayerProvisioningLifecycleProjectionExtensions
    {
        internal static bool TryGetLifecycleSnapshot(
            this LocalPlayerProvisioningRuntimeHostModule module,
            out ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (module == null || !module.IsReady)
            {
                snapshot =
                    ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                        module != null
                            ? module.Diagnostic
                            : "Local Player provisioning runtime module is unavailable.");
                return false;
            }

            if (!module.TryGetSnapshot(
                    out PlayerParticipationSnapshot participation) ||
                participation == null ||
                !participation.IsInitialized)
            {
                snapshot =
                    ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                        "Session Player participation snapshot is unavailable.");
                return false;
            }

            FrameworkRuntimeHost runtimeHost =
                module.GetComponent<FrameworkRuntimeHost>();
            if (runtimeHost == null)
            {
                snapshot =
                    ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                        "Local Player provisioning module is not attached to its FrameworkRuntimeHost.");
                return false;
            }

            PlayerActorPreparationRuntimeHostModule preparation = null;
            PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot =
                null;
            bool preparationAvailable =
                runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out preparation) &&
                preparation != null &&
                preparation.TryGetSnapshot(out preparationSnapshot) &&
                preparationSnapshot != null &&
                preparationSnapshot.IsInitialized;

            if (!preparationAvailable)
            {
                preparationSnapshot = null;
            }

            PlayerGameplayRuntimeHostSnapshot gameplaySnapshot = null;
            bool gameplayAvailable =
                runtimeHost.TryGetPlayerGameplayRuntimeSnapshot(
                    out gameplaySnapshot) &&
                gameplaySnapshot != null &&
                gameplaySnapshot.IsInitialized;

            if (!gameplayAvailable)
            {
                gameplaySnapshot = null;
            }

            PlayerActivityReconciliationRuntimeHostSnapshot
                reconciliationSnapshot = null;
            bool reconciliationAvailable =
                runtimeHost.TryGetPlayerActivityReconciliationSnapshot(
                    out reconciliationSnapshot) &&
                reconciliationSnapshot != null;

            if (!reconciliationAvailable)
            {
                reconciliationSnapshot = null;
            }

            ActivityPlayerReadinessContributionRuntimeSnapshot
                readinessContribution = null;
            bool readinessContributionAvailable =
                preparationAvailable &&
                preparation
                    .TryGetActivityPlayerReadinessContributionSnapshot(
                        out readinessContribution) &&
                readinessContribution != null &&
                readinessContribution.IsAvailable;

            if (!readinessContributionAvailable)
            {
                readinessContribution = null;
            }

            ActivityPlayerActorReconcileResult reconcileResult =
                reconciliationSnapshot?.ReconcileResult;
            ActivityPlayerActorLifecycleSnapshot reconciledLifecycle =
                reconcileResult?.LifecycleSnapshot;
            ActivityPlayerActorLifecycleSnapshot directLifecycle = null;
            if (preparationAvailable &&
                !preparation.TryGetActivityPlayerActorLifecycleSnapshot(
                    out directLifecycle))
            {
                directLifecycle = null;
            }

            var currentOccurrence =
                runtimeHost.CurrentGameFlowRuntime?.CurrentOccurrence ??
                default;
            RuntimeContentOwner currentActivityOwner =
                currentOccurrence.IsValid &&
                currentOccurrence.Activity != null &&
                currentOccurrence.Activity.HasValidActivityId
                    ? RuntimeContentOwner.Activity(
                        currentOccurrence.Activity.ActivityId.StableText,
                        currentOccurrence.Activity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(currentOccurrence.Activity))
                    : default;

            ActivityPlayerActorLifecycleSnapshot lifecycle =
                MatchesCurrentActivityOwner(
                    directLifecycle,
                    currentActivityOwner)
                    ? directLifecycle
                    : MatchesCurrentActivityOwner(
                        reconciledLifecycle,
                        currentActivityOwner)
                        ? reconciledLifecycle
                        : IsReleased(reconciledLifecycle)
                            ? reconciledLifecycle
                            : IsReleased(directLifecycle)
                                ? directLifecycle
                                : null;
            bool lifecycleMatchesCurrentOccurrence =
                MatchesCurrentActivityOwner(
                    lifecycle,
                    currentActivityOwner);

            bool lifecycleReleased = IsReleased(lifecycle);
            IReadOnlyList<ActivityPlayerActorSlotLifecycleSnapshot>
                activitySlots =
                    lifecycleReleased || lifecycle == null
                        ? Array.Empty<
                            ActivityPlayerActorSlotLifecycleSnapshot>()
                        : lifecycle.Slots;

            var projectedSlots =
                new List<ManagerProvisionedPlayerLifecycleSlotSnapshot>(
                    activitySlots.Count);

            bool projectedSlotMissingSessionEvidence = false;
            int projectedJoinedCount = 0;
            int projectedJoinedWithoutSelectedActorCount = 0;
            bool joinedSlotMissingLogicalPreparation = false;
            bool joinedSlotMissingPhysicalMaterialization = false;
            bool joinedSlotMissingGameplayAdmission = false;
            bool projectedPreparationFailed = false;
            bool projectedGameplayAdmissionFailed = false;

            for (int index = 0;
                 index < activitySlots.Count;
                 index++)
            {
                ActivityPlayerActorSlotLifecycleSnapshot activitySlot =
                    activitySlots[index];

                if (!TryGetSessionSlot(
                        participation,
                        activitySlot.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    projectedSlotMissingSessionEvidence = true;
                    projectedSlots.Add(
                        new ManagerProvisionedPlayerLifecycleSlotSnapshot(
                            activitySlot.PlayerSlotId.IsValid
                                ? activitySlot.PlayerSlotId.StableText
                                : string.Empty,
                            string.Empty,
                            false,
                            activitySlot.SelectedActorProfileId.IsValid
                                ? activitySlot.SelectedActorProfileId
                                    .StableText
                                : string.Empty,
                            false,
                            false,
                            false,
                            "Activity projection Slot has no matching " +
                            "Session Slot evidence."));
                    continue;
                }

                bool hasTechnicalHost =
                    preparationAvailable &&
                    preparation.TryGetRetainedHostEvidence(
                        slot.PlayerSlotId,
                        out _);

                PlayerActorPreparationSummary preparationSummary =
                    default;
                bool hasPreparation =
                    preparationAvailable &&
                    TryGetPreparation(
                        preparationSnapshot.Preparation,
                        slot.PlayerSlotId,
                        out preparationSummary);

                bool logicalActorPrepared =
                    hasPreparation &&
                    preparationSummary.IsPrepared;

                bool physicalActorMaterialized =
                    hasPreparation &&
                    preparationSummary.HasMaterialization;

                PlayerGameplayAdmissionSummary gameplayAdmission =
                    default;
                bool hasGameplayAdmission =
                    gameplayAvailable &&
                    gameplaySnapshot.Admission != null &&
                    gameplaySnapshot.Admission.TryGetSummary(
                        slot.PlayerSlotId,
                        out gameplayAdmission);
                bool gameplayAdmitted =
                    hasGameplayAdmission &&
                    gameplayAdmission.IsAdmitted;

                if (slot.IsJoined)
                {
                    projectedJoinedCount++;
                    projectedJoinedWithoutSelectedActorCount +=
                        slot.HasSelectedActor
                            ? 0
                            : 1;
                    joinedSlotMissingLogicalPreparation |=
                        !logicalActorPrepared;
                    joinedSlotMissingPhysicalMaterialization |=
                        logicalActorPrepared &&
                        !physicalActorMaterialized;
                    joinedSlotMissingGameplayAdmission |=
                        physicalActorMaterialized &&
                        !gameplayAdmitted;
                    projectedPreparationFailed |=
                        hasPreparation &&
                        preparationSummary.IsReleaseFailed;
                    projectedGameplayAdmissionFailed |=
                        hasGameplayAdmission &&
                        gameplayAdmission.IsReleaseFailed;
                }

                projectedSlots.Add(
                    new ManagerProvisionedPlayerLifecycleSlotSnapshot(
                        slot.PlayerSlotId.IsValid
                            ? slot.PlayerSlotId.StableText
                            : string.Empty,
                        slot.AllocationState.ToString(),
                        hasTechnicalHost,
                        slot.SelectedActorProfileId.IsValid
                            ? slot.SelectedActorProfileId.StableText
                            : string.Empty,
                        logicalActorPrepared,
                        physicalActorMaterialized,
                        gameplayAdmitted,
                        BuildSlotDiagnostic(
                            slot,
                            preparationAvailable,
                            hasPreparation,
                            preparationSummary,
                            gameplayAvailable,
                            gameplayAdmitted)));
            }

            string activityName =
                !string.IsNullOrWhiteSpace(reconcileResult?.ActivityName)
                    ? reconcileResult.ActivityName
                    : reconciliationSnapshot?.Activity != null
                        ? reconciliationSnapshot.Activity.ActivityName
                        : readinessContributionAvailable
                            ? readinessContribution.ActivityName
                            : lifecycle != null &&
                              (lifecycleMatchesCurrentOccurrence ||
                               IsReleased(lifecycle))
                                ? lifecycle.ActivityName
                                : string.Empty;

            int occurrence =
                reconcileResult != null
                    ? reconcileResult.Occurrence
                    : reconciliationSnapshot?.Occurrence > 0
                        ? reconciliationSnapshot.Occurrence
                        : readinessContribution?.Occurrence > 0
                            ? readinessContribution.Occurrence
                            : lifecycleMatchesCurrentOccurrence
                                ? currentOccurrence.TransitionSequence
                                : 0;

            bool readinessMatchesProjectedActivity =
                readinessContributionAvailable &&
                readinessContribution.HasOccurrence &&
                (occurrence <= 0 ||
                 readinessContribution.Occurrence == occurrence) &&
                (string.IsNullOrWhiteSpace(activityName) ||
                 string.IsNullOrWhiteSpace(
                     readinessContribution.ActivityName) ||
                 string.Equals(
                     activityName,
                     readinessContribution.ActivityName,
                     StringComparison.Ordinal));

            ManagerProvisionedPlayerGateEvidenceScope gateEvidenceScope =
                readinessMatchesProjectedActivity
                    ? ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution
                    : ManagerProvisionedPlayerGateEvidenceScope.None;

            bool hasGateEvidence =
                readinessMatchesProjectedActivity;
            bool gateHeld =
                hasGateEvidence &&
                readinessContribution.GateHeld;

            PlayerParticipationRequirementLevel requirementLevel =
                ResolveRequirementLevel(
                    lifecycle,
                    reconciliationSnapshot,
                    readinessMatchesProjectedActivity
                        ? readinessContribution
                        : null);

            ManagerProvisionedPlayerLifecycleStatus status =
                ResolveStatus(
                    reconciliationSnapshot,
                    reconcileResult,
                    lifecycle,
                    lifecycleMatchesCurrentOccurrence,
                    readinessMatchesProjectedActivity
                        ? readinessContribution
                        : null,
                    projectedSlots.Count,
                    projectedJoinedCount,
                    projectedJoinedWithoutSelectedActorCount,
                    requirementLevel,
                    projectedSlotMissingSessionEvidence,
                    projectedPreparationFailed,
                    projectedGameplayAdmissionFailed,
                    joinedSlotMissingLogicalPreparation,
                    joinedSlotMissingPhysicalMaterialization,
                    joinedSlotMissingGameplayAdmission);

            string entryPolicy =
                readinessMatchesProjectedActivity &&
                !string.IsNullOrWhiteSpace(
                    readinessContribution.RequirementLevel)
                    ? readinessContribution.RequirementLevel
                    : (lifecycleMatchesCurrentOccurrence ||
                       IsReleased(lifecycle) ||
                       reconciliationSnapshot?.Activity != null)
                        ? requirementLevel.ToString()
                        : string.Empty;

            string readinessStatus =
                readinessMatchesProjectedActivity
                    ? readinessContribution.State.ToString()
                    : reconcileResult != null
                        ? reconcileResult.Status.ToString()
                        : lifecycle != null &&
                          (lifecycleMatchesCurrentOccurrence ||
                           IsReleased(lifecycle))
                            ? lifecycle.Status.ToString()
                            : reconciliationSnapshot != null
                                ? reconciliationSnapshot.Status.ToString()
                                : string.Empty;

            string readinessReason =
                readinessMatchesProjectedActivity
                    ? readinessContribution.LastReason
                    : reconcileResult != null
                        ? reconcileResult.ReadinessReason.ToString()
                        : string.Empty;

            int hostCount =
                preparationSnapshot?.RegisteredHostCount ?? 0;

            string diagnostic = BuildAggregateDiagnostic(
                participation,
                preparationAvailable,
                preparationSnapshot,
                gameplayAvailable,
                gameplaySnapshot,
                reconciliationAvailable,
                reconciliationSnapshot,
                readinessContributionAvailable,
                readinessContribution,
                readinessMatchesProjectedActivity,
                projectedSlots.Count,
                projectedSlotMissingSessionEvidence);

            snapshot =
                new ManagerProvisionedPlayerLifecycleSnapshot(
                    true,
                    status,
                    activityName,
                    occurrence,
                    participation.Revision,
                    reconcileResult?.RequestedSessionRevision ?? 0,
                    reconcileResult?.AppliedSessionRevision ?? 0,
                    entryPolicy,
                    readinessStatus,
                    readinessReason,
                    gateEvidenceScope,
                    hasGateEvidence,
                    gateHeld,
                    participation.JoiningOpen,
                    hostCount,
                    projectedSlots,
                    diagnostic);
            return true;
        }

        private static bool MatchesCurrentActivityOwner(
            ActivityPlayerActorLifecycleSnapshot lifecycle,
            RuntimeContentOwner currentActivityOwner)
        {
            return lifecycle != null &&
                currentActivityOwner.IsValid &&
                lifecycle.Owner.IsValid &&
                lifecycle.Owner == currentActivityOwner;
        }

        private static bool IsReleased(
            ActivityPlayerActorLifecycleSnapshot lifecycle)
        {
            return lifecycle != null &&
                (lifecycle.Status ==
                    ActivityPlayerActorLifecycleStatus.SucceededExited ||
                 lifecycle.Status ==
                    ActivityPlayerActorLifecycleStatus
                        .SucceededExitedNoActors);
        }

        private static bool IsReadinessReleased(
            ActivityPlayerReadinessContributionRuntimeSnapshot readiness)
        {
            return readiness != null &&
                string.Equals(
                    readiness.State.ToString(),
                    "Released",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetSessionSlot(
            PlayerParticipationSnapshot participation,
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot slot)
        {
            if (participation != null && playerSlotId.IsValid)
            {
                for (int index = 0;
                     index < participation.Slots.Count;
                     index++)
                {
                    PlayerSlotRuntimeSnapshot candidate =
                        participation.Slots[index];
                    if (candidate.PlayerSlotId == playerSlotId)
                    {
                        slot = candidate;
                        return true;
                    }
                }
            }

            slot = default;
            return false;
        }

        private static bool TryGetPreparation(
            PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary)
        {
            if (snapshot != null)
            {
                for (int index = 0;
                     index < snapshot.Slots.Count;
                     index++)
                {
                    PlayerActorPreparationSummary candidate =
                        snapshot.Slots[index];
                    if (candidate.PlayerSlotId == playerSlotId)
                    {
                        summary = candidate;
                        return true;
                    }
                }
            }

            summary = default;
            return false;
        }

        private static ManagerProvisionedPlayerLifecycleStatus ResolveStatus(
            PlayerActivityReconciliationRuntimeHostSnapshot reconciliation,
            ActivityPlayerActorReconcileResult reconcileResult,
            ActivityPlayerActorLifecycleSnapshot lifecycle,
            bool lifecycleMatchesCurrentOccurrence,
            ActivityPlayerReadinessContributionRuntimeSnapshot readiness,
            int projectedSlotCount,
            int projectedJoinedCount,
            int projectedJoinedWithoutSelectedActorCount,
            PlayerParticipationRequirementLevel requirementLevel,
            bool projectedSlotMissingSessionEvidence,
            bool projectedPreparationFailed,
            bool projectedGameplayAdmissionFailed,
            bool missingLogicalPreparation,
            bool missingPhysicalMaterialization,
            bool missingGameplayAdmission)
        {
            bool requiresJoinedSlots = Requires(
                requirementLevel,
                PlayerParticipationRequirementLevel.JoinedSlots);
            bool requiresSelectedActors = Requires(
                requirementLevel,
                PlayerParticipationRequirementLevel.SelectedActors);
            bool requiresLogicalActors = Requires(
                requirementLevel,
                PlayerParticipationRequirementLevel.LogicalActorsPrepared);
            bool requiresGameplayReady = Requires(
                requirementLevel,
                PlayerParticipationRequirementLevel.GameplayReady);

            if (projectedSlotMissingSessionEvidence ||
                readiness?.Failed == true ||
                reconcileResult?.Failed == true ||
                (requiresLogicalActors && projectedPreparationFailed) ||
                (requiresGameplayReady &&
                 projectedGameplayAdmissionFailed))
            {
                return ManagerProvisionedPlayerLifecycleStatus.Failed;
            }

            if (IsReleased(lifecycle) ||
                IsReadinessReleased(readiness))
            {
                return ManagerProvisionedPlayerLifecycleStatus.Released;
            }

            if (!lifecycleMatchesCurrentOccurrence &&
                (reconciliation == null ||
                 reconciliation.Activity == null ||
                 reconciliation.Occurrence <= 0))
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForActivity;
            }

            if (requiresJoinedSlots &&
                projectedJoinedCount < projectedSlotCount)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForJoin;
            }

            if (requiresSelectedActors &&
                projectedJoinedWithoutSelectedActorCount > 0)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForActorSelection;
            }

            if (requiresLogicalActors &&
                missingLogicalPreparation)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .PreparingLogicalActor;
            }

            if (requiresLogicalActors &&
                missingPhysicalMaterialization)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .MaterializingPhysicalActor;
            }

            if (requiresGameplayReady &&
                missingGameplayAdmission)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .PreparingGameplayAdmission;
            }

            bool readinessCompleted =
                requirementLevel ==
                    PlayerParticipationRequirementLevel.None ||
                projectedSlotCount == 0 ||
                readiness?.Completed == true ||
                reconcileResult?.Completed == true;

            if (!readinessCompleted)
            {
                return requiresGameplayReady
                    ? ManagerProvisionedPlayerLifecycleStatus
                        .PreparingGameplayAdmission
                    : requiresLogicalActors
                        ? ManagerProvisionedPlayerLifecycleStatus
                            .PreparingLogicalActor
                        : requiresSelectedActors
                            ? ManagerProvisionedPlayerLifecycleStatus
                                .WaitingForActorSelection
                            : ManagerProvisionedPlayerLifecycleStatus
                                .WaitingForJoin;
            }

            return ManagerProvisionedPlayerLifecycleStatus.Ready;
        }

        private static bool Requires(
            PlayerParticipationRequirementLevel actual,
            PlayerParticipationRequirementLevel required)
        {
            return (int)actual >= (int)required;
        }

        private static PlayerParticipationRequirementLevel
            ResolveRequirementLevel(
                ActivityPlayerActorLifecycleSnapshot lifecycle,
                PlayerActivityReconciliationRuntimeHostSnapshot reconciliation,
                ActivityPlayerReadinessContributionRuntimeSnapshot readiness)
        {
            if (lifecycle != null &&
                Enum.IsDefined(
                    typeof(PlayerParticipationRequirementLevel),
                    lifecycle.RequirementLevel))
            {
                return lifecycle.RequirementLevel;
            }

            if (readiness != null &&
                Enum.TryParse(
                    readiness.RequirementLevel,
                    false,
                    out PlayerParticipationRequirementLevel parsed) &&
                Enum.IsDefined(
                    typeof(PlayerParticipationRequirementLevel),
                    parsed))
            {
                return parsed;
            }

            if (reconciliation?.Activity != null &&
                reconciliation.Activity
                    .HasDefinedPlayerParticipationRequirementLevel)
            {
                return reconciliation.Activity
                    .PlayerParticipationRequirementLevel;
            }

            return PlayerParticipationRequirementLevel.None;
        }

        private static string BuildSlotDiagnostic(
            PlayerSlotRuntimeSnapshot slot,
            bool preparationAvailable,
            bool hasPreparation,
            PlayerActorPreparationSummary preparation,
            bool gameplayAvailable,
            bool gameplayAdmitted)
        {
            string preparationDiagnostic =
                !preparationAvailable
                    ? "preparation-authority-unavailable"
                    : hasPreparation
                        ? preparation.ToDiagnosticString()
                        : "no-preparation-summary";

            string gameplayDiagnostic =
                !gameplayAvailable
                    ? "gameplay-authority-unavailable"
                    : gameplayAdmitted
                        ? "gameplay-admitted"
                        : "gameplay-not-admitted";

            return
                $"slotRevision='{slot.Revision}' " +
                $"selectionRevision='{slot.SelectionRevision}' " +
                $"selectionSource='{slot.SelectionSource}' " +
                $"selectionReason='{slot.SelectionReason}' " +
                $"preparation='{preparationDiagnostic}' " +
                $"gameplay='{gameplayDiagnostic}'.";
        }

        private static string BuildAggregateDiagnostic(
            PlayerParticipationSnapshot participation,
            bool preparationAvailable,
            PlayerActorPreparationRuntimeHostSnapshot preparation,
            bool gameplayAvailable,
            PlayerGameplayRuntimeHostSnapshot gameplay,
            bool reconciliationAvailable,
            PlayerActivityReconciliationRuntimeHostSnapshot reconciliation,
            bool readinessContributionAvailable,
            ActivityPlayerReadinessContributionRuntimeSnapshot
                readinessContribution,
            bool readinessMatchesProjectedActivity,
            int projectedSlotCount,
            bool projectedSlotMissingSessionEvidence)
        {
            string preparationDiagnostic =
                preparationAvailable
                    ? preparation.Diagnostic
                    : "Player Actor preparation authority is unavailable.";

            string gameplayDiagnostic =
                gameplayAvailable
                    ? gameplay.Diagnostic
                    : "Player gameplay authority is unavailable.";

            string reconciliationDiagnostic =
                reconciliationAvailable
                    ? reconciliation.ToDiagnosticString()
                    : "Player Activity reconciliation has not produced evidence.";

            string readinessDiagnostic =
                readinessContributionAvailable
                    ? readinessContribution.ToDiagnosticString()
                    : "Player readiness contribution has not produced evidence.";

            return
                $"session='{participation.ContextId}' " +
                $"sessionRevision='{participation.Revision}' " +
                $"joined='{participation.JoinedCount}' " +
                $"selected='{participation.SelectedActorCount}' " +
                $"activityProjected='{projectedSlotCount}' " +
                $"activitySessionMismatch='" +
                $"{projectedSlotMissingSessionEvidence}' " +
                $"preparation='{preparationDiagnostic}' " +
                $"gameplay='{gameplayDiagnostic}' " +
                $"reconciliation='{reconciliationDiagnostic}' " +
                $"readinessContribution='{readinessDiagnostic}' " +
                $"readinessMatchesActivity='" +
                $"{readinessMatchesProjectedActivity}' " +
                "gateEvidenceScope='ActivityPlayerReadinessContribution; " +
                "not-aggregate-activity-gate'.";
        }
    }

    /// <summary>
    /// Read-only composition over existing Player authorities for the P1
    /// scoped consumer endpoint. No values are cached or advanced here.
    /// </summary>
    internal static class LocalPlayerProvisioningConsumerObservationProjection
    {
        internal static bool TryGetObservation(
            this LocalPlayerProvisioningRuntimeHostModule module,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner scopeOwner,
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            if (module == null || !module.IsReady)
            {
                observation = LocalPlayerProvisioningConsumerObservationSnapshot
                    .Unavailable(scope, scopeOwner, module != null
                        ? module.Diagnostic
                        : "Local Player provisioning runtime module is unavailable.");
                return false;
            }

            if (!module.TryGetSnapshot(out PlayerParticipationSnapshot participation) ||
                participation == null || !participation.IsInitialized)
            {
                observation = LocalPlayerProvisioningConsumerObservationSnapshot
                    .Unavailable(scope, scopeOwner,
                        "Session Player participation snapshot is unavailable.");
                return false;
            }

            FrameworkRuntimeHost runtimeHost = module.GetComponent<FrameworkRuntimeHost>();
            if (runtimeHost == null)
            {
                observation = LocalPlayerProvisioningConsumerObservationSnapshot
                    .Unavailable(scope, scopeOwner,
                        "Local Player provisioning module is not attached to its FrameworkRuntimeHost.");
                return false;
            }

            module.TryGetLifecycleSnapshot(
                out ManagerProvisionedPlayerLifecycleSnapshot lifecycle);
            PlayerParticipationRuntimeHostModule participationModule =
                runtimeHost.GetComponent<PlayerParticipationRuntimeHostModule>();
            EffectivePlayerSessionConfiguration initializationConfiguration =
                participationModule != null && participationModule.IsInitialized
                    ? participationModule.EffectiveConfiguration
                    : null;
            var occurrence = runtimeHost.CurrentGameFlowRuntime?.CurrentOccurrence ?? default;
            RuntimeContentOwner activityOwner = occurrence.IsValid &&
                occurrence.Activity != null && occurrence.Activity.HasValidActivityId
                    ? RuntimeContentOwner.Activity(
                        occurrence.Activity.ActivityId.StableText,
                        occurrence.Activity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(occurrence.Activity))
                    : default;
            int activityOccurrence = occurrence.IsValid
                ? occurrence.TransitionSequence
                : 0;

            PlayerActorPreparationRuntimeHostModule preparation = null;
            PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot = null;
            bool preparationAvailable =
                runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out preparation) &&
                preparation != null && preparation.TryGetSnapshot(
                    out preparationSnapshot) &&
                preparationSnapshot != null && preparationSnapshot.IsInitialized;

            PlayerGameplayRuntimeHostSnapshot gameplaySnapshot = null;
            bool gameplayAvailable =
                runtimeHost.TryGetPlayerGameplayRuntimeSnapshot(
                    out gameplaySnapshot) &&
                gameplaySnapshot != null && gameplaySnapshot.IsInitialized &&
                gameplaySnapshot.Admission != null;

            var slots = new List<LocalPlayerProvisioningConsumerSlotObservation>(
                participation.Slots.Count);
            for (int index = 0; index < participation.Slots.Count; index++)
            {
                slots.Add(CreateSlotObservation(
                    participation.Slots[index],
                    preparationAvailable ? preparation : null,
                    preparationAvailable ? preparationSnapshot : null,
                    gameplayAvailable ? gameplaySnapshot : null,
                    activityOwner));
            }

            observation = new LocalPlayerProvisioningConsumerObservationSnapshot(
                true,
                scope,
                scopeOwner,
                participation,
                initializationConfiguration,
                lifecycle,
                activityOwner,
                activityOccurrence,
                slots,
                BuildDiagnostic(module.Diagnostic, preparationAvailable,
                    gameplayAvailable, lifecycle));
            return true;
        }

        private static LocalPlayerProvisioningConsumerSlotObservation
            CreateSlotObservation(
                PlayerSlotRuntimeSnapshot slot,
                PlayerActorPreparationRuntimeHostModule preparation,
                PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot,
                PlayerGameplayRuntimeHostSnapshot gameplaySnapshot,
                RuntimeContentOwner currentActivityOwner)
        {
            PlayerHostEvidenceSummary hostEvidence = default;
            bool hasHostEvidence = false;
            PlayerActorPreparationSummary preparationSummary = default;
            bool hasPreparationEvidence = false;
            CurrentPlayerSlotActorSnapshot currentActor = default;
            bool hasCurrentActorEvidence = false;
            PlayerGameplayAdmissionSummary gameplayAdmission = default;
            bool hasGameplayAdmissionEvidence = false;

            if (preparation != null && preparation.TryGetRetainedHostEvidence(
                    slot.PlayerSlotId, out PlayerHostEvidenceSnapshot retainedHost))
            {
                hostEvidence = new PlayerHostEvidenceSummary(
                    retainedHost.PlayerSlotId,
                    retainedHost.AssignmentOrigin,
                    retainedHost.AssignmentToken,
                    retainedHost.HostBindingIdentity,
                    retainedHost.HostIsAvailable,
                    retainedHost.Source,
                    retainedHost.Reason,
                    retainedHost.HostIsAvailable
                        ? "Retained Local Player Host evidence is available."
                        : "Retained Local Player Host evidence references an unavailable Host.");
                hasHostEvidence = true;
            }

            if (TryGetPreparation(preparationSnapshot?.Preparation, slot.PlayerSlotId,
                    out PlayerActorPreparationSummary candidatePreparation) &&
                IsCurrentPreparation(candidatePreparation))
            {
                preparationSummary = candidatePreparation;
                hasPreparationEvidence = true;
            }

            if (preparation != null && preparation.TryGetCurrentSlotActorSnapshot(
                    slot.PlayerSlotId, out CurrentPlayerSlotActorSnapshot candidateActor) &&
                IsCurrentActor(candidateActor))
            {
                currentActor = candidateActor;
                hasCurrentActorEvidence = true;
            }

            if (gameplaySnapshot?.Admission != null &&
                gameplaySnapshot.Admission.TryGetSummary(slot.PlayerSlotId,
                    out PlayerGameplayAdmissionSummary candidateAdmission) &&
                IsCurrentGameplayAdmission(candidateAdmission, currentActivityOwner))
            {
                gameplayAdmission = candidateAdmission;
                hasGameplayAdmissionEvidence = true;
            }

            return new LocalPlayerProvisioningConsumerSlotObservation(
                slot, hostEvidence, hasHostEvidence, preparationSummary,
                hasPreparationEvidence, currentActor, hasCurrentActorEvidence,
                gameplayAdmission, hasGameplayAdmissionEvidence);
        }

        private static bool TryGetPreparation(
            PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary)
        {
            if (snapshot != null)
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    PlayerActorPreparationSummary candidate = snapshot.Slots[index];
                    if (candidate.PlayerSlotId == playerSlotId)
                    {
                        summary = candidate;
                        return true;
                    }
                }
            }

            summary = default;
            return false;
        }

        private static bool IsCurrentPreparation(
            PlayerActorPreparationSummary summary)
        {
            return summary.IsValid && (summary.IsUnprepared ||
                (summary.HasActorEvidence &&
                 summary.ActorEvidence.Owner.Scope == RuntimeContentScope.Session));
        }

        private static bool IsCurrentActor(
            CurrentPlayerSlotActorSnapshot snapshot)
        {
            return snapshot.HasCurrentActor &&
                snapshot.ActorEvidence.Owner.Scope == RuntimeContentScope.Session;
        }

        private static bool IsCurrentGameplayAdmission(
            PlayerGameplayAdmissionSummary summary,
            RuntimeContentOwner currentActivityOwner)
        {
            return currentActivityOwner.IsValid && summary.IsValid &&
                summary.Owner == currentActivityOwner;
        }

        private static string BuildDiagnostic(
            string moduleDiagnostic,
            bool preparationAvailable,
            bool gameplayAvailable,
            ManagerProvisionedPlayerLifecycleSnapshot lifecycle)
        {
            return $"Local Player provisioning observation: preparation='" +
                $"{(preparationAvailable ? "available" : "unavailable")}' gameplay='" +
                $"{(gameplayAvailable ? "available" : "unavailable")}' lifecycle='" +
                $"{(lifecycle != null ? lifecycle.Diagnostic : "unavailable")}' module='" +
                $"{moduleDiagnostic ?? string.Empty}'.";
        }
    }
}
