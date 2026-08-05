using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;

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
            ActivityPlayerActorLifecycleSnapshot lifecycle =
                reconcileResult?.LifecycleSnapshot;

            var projectedSlots =
                new List<ManagerProvisionedPlayerLifecycleSlotSnapshot>(
                    participation.ConfiguredSlotCount);

            bool joinedSlotMissingLogicalPreparation = false;
            bool joinedSlotMissingPhysicalMaterialization = false;
            bool joinedSlotMissingGameplayAdmission = false;

            for (int index = 0;
                 index < participation.Slots.Count;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot =
                    participation.Slots[index];

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

                bool gameplayAdmitted =
                    gameplayAvailable &&
                    gameplaySnapshot.Admission != null &&
                    gameplaySnapshot.Admission.TryGetSummary(
                        slot.PlayerSlotId,
                        out PlayerGameplayAdmissionSummary
                            gameplayAdmission) &&
                    gameplayAdmission.IsAdmitted;

                if (slot.IsJoined)
                {
                    joinedSlotMissingLogicalPreparation |=
                        !logicalActorPrepared;
                    joinedSlotMissingPhysicalMaterialization |=
                        logicalActorPrepared &&
                        !physicalActorMaterialized;
                    joinedSlotMissingGameplayAdmission |=
                        physicalActorMaterialized &&
                        !gameplayAdmitted;
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
                            : string.Empty;

            int occurrence =
                reconcileResult != null
                    ? reconcileResult.Occurrence
                    : reconciliationSnapshot?.Occurrence > 0
                        ? reconciliationSnapshot.Occurrence
                        : readinessContribution?.Occurrence ?? 0;

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

            ManagerProvisionedPlayerLifecycleStatus status =
                ResolveStatus(
                    participation,
                    preparationSnapshot,
                    gameplaySnapshot,
                    reconciliationSnapshot,
                    reconcileResult,
                    lifecycle,
                    readinessMatchesProjectedActivity
                        ? readinessContribution
                        : null,
                    joinedSlotMissingLogicalPreparation,
                    joinedSlotMissingPhysicalMaterialization,
                    joinedSlotMissingGameplayAdmission);

            string entryPolicy =
                readinessContributionAvailable &&
                !string.IsNullOrWhiteSpace(
                    readinessContribution.RequirementLevel)
                    ? readinessContribution.RequirementLevel
                    : lifecycle != null
                        ? lifecycle.RequirementLevel.ToString()
                        : string.Empty;

            string readinessStatus =
                readinessContributionAvailable
                    ? readinessContribution.State.ToString()
                    : reconcileResult != null
                        ? reconcileResult.Status.ToString()
                        : reconciliationSnapshot != null
                            ? reconciliationSnapshot.Status.ToString()
                            : string.Empty;

            string readinessReason =
                readinessContributionAvailable
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
                readinessMatchesProjectedActivity);

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
            PlayerParticipationSnapshot participation,
            PlayerActorPreparationRuntimeHostSnapshot preparation,
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerActivityReconciliationRuntimeHostSnapshot reconciliation,
            ActivityPlayerActorReconcileResult reconcileResult,
            ActivityPlayerActorLifecycleSnapshot lifecycle,
            ActivityPlayerReadinessContributionRuntimeSnapshot readiness,
            bool missingLogicalPreparation,
            bool missingPhysicalMaterialization,
            bool missingGameplayAdmission)
        {
            if (readiness?.Failed == true ||
                reconcileResult?.Failed == true ||
                (preparation?.ReleaseFailedCount ?? 0) > 0 ||
                (gameplay?.Admission?.ReleaseFailedCount ?? 0) > 0)
            {
                return ManagerProvisionedPlayerLifecycleStatus.Failed;
            }

            if (lifecycle != null &&
                (lifecycle.Status ==
                    ActivityPlayerActorLifecycleStatus.SucceededExited ||
                 lifecycle.Status ==
                    ActivityPlayerActorLifecycleStatus
                        .SucceededExitedNoActors))
            {
                return ManagerProvisionedPlayerLifecycleStatus.Released;
            }

            if (reconciliation == null ||
                reconciliation.Activity == null ||
                reconciliation.Occurrence <= 0)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForActivity;
            }

            if (participation.JoinedCount == 0)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForJoin;
            }

            if (participation.JoinedWithoutSelectedActorCount > 0)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .WaitingForActorSelection;
            }

            if (missingLogicalPreparation)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .PreparingLogicalActor;
            }

            if (missingPhysicalMaterialization)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .MaterializingPhysicalActor;
            }

            if (readiness?.GateHeld == true ||
                missingGameplayAdmission ||
                reconcileResult == null ||
                !reconcileResult.Completed)
            {
                return ManagerProvisionedPlayerLifecycleStatus
                    .PreparingGameplayAdmission;
            }

            return ManagerProvisionedPlayerLifecycleStatus.Ready;
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
            bool readinessMatchesProjectedActivity)
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
}
