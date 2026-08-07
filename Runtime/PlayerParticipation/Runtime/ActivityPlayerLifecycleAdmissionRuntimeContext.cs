using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.GameFlow.Diagnostics;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority for GameplayReady Activity-owner handoff across same-Route and Route Startup flows.
    /// It stages the target owner and reaches P3K.7E ReadyToCommit before transition
    /// presentation, commits only after target scene preparation, and supplies exact
    /// adoption evidence to the P3J.6 lifecycle participant.
    /// </summary>
    internal sealed class ActivityPlayerLifecycleAdmissionRuntimeContext :
        IActivityPlayerLifecycleAdmissionRuntime,
        IActivityPlayerGameplayLifecycleRuntime
    {
        private sealed class SlotRecord
        {
            internal PlayerSlotRuntimeSnapshot Slot;
            internal PlayerGameplayAdmissionToken PreviousAdmissionToken;
            internal PlayerActorCandidateStageToken CandidateToken;
            internal PlayerActorPreparationToken TargetPreparationToken;
            internal PlayerGameplayAdmissionToken TargetAdmissionToken;
            internal bool Staged;
            internal bool GroupBegan;
            internal bool Committed;
            internal bool Adopted;
            internal bool Released;
            internal string Message;
        }

        private sealed class TransactionRecord
        {
            internal ActivityPlayerLifecycleAdmissionFlowKind FlowKind;
            internal RouteAsset PreviousRoute;
            internal RouteAsset TargetRoute;
            internal ActivityAsset PreviousActivity;
            internal ActivityAsset TargetActivity;
            internal RuntimeContentOwner PreviousOwner;
            internal RuntimeContentOwner TargetOwner;
            internal RuntimeScopeContext TargetContext;
            internal ActivityPlayerLifecycleAdmissionToken Token;
            internal ActivityPlayerLifecycleAdmissionState State;
            internal ActivityPlayerLifecycleAdmissionStatus LastStatus;
            internal PlayerParticipationRequirementLevel RequirementLevel;
            internal ActivityPlayerHandoffGroupToken GroupToken;
            internal ActivityPlayerHandoffGroupSnapshot GroupSnapshot;
            internal List<SlotRecord> Slots;
            internal bool TransitionAuthorized;
            internal bool PreviousExitAcknowledged;
            internal ActivityPlayerPreviousExitDisposition PreviousExitDisposition;
            internal bool TargetEnterAdopted;
            internal bool CommitCleanupPending;
            internal string Source;
            internal string Reason;
            internal string Message;
        }

        private readonly RuntimeContentRuntime runtimeContentRuntime;
        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly PlayerActorCandidateRuntimeHostModule candidateModule;
        private readonly PlayerGameplayAdmissionRuntimeContext admissionContext;
        private readonly PlayerGameplayChainHandoffRuntimeContext handoffContext;
        private readonly ActivityPlayerHandoffGroupRuntimeContext groupContext;
        private IGameFlowDiagnosticFaultPlan diagnosticFaultPlan =
            NoOpGameFlowDiagnosticFaultPlan.Instance;

        private TransactionRecord active;
        private ActivityPlayerLifecycleAdmissionSnapshot completed;
        private ActivityPlayerLifecycleAdmissionSnapshot lastSnapshot;
        private int transactionSequence;

        private ActivityPlayerLifecycleAdmissionRuntimeContext(
            RuntimeContentRuntime runtimeContentRuntime,
            PlayerParticipationRuntimeContext participationContext,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerActorCandidateRuntimeHostModule candidateModule,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            PlayerGameplayChainHandoffRuntimeContext handoffContext,
            ActivityPlayerHandoffGroupRuntimeContext groupContext)
        {
            this.runtimeContentRuntime = runtimeContentRuntime;
            this.participationContext = participationContext;
            this.preparationModule = preparationModule;
            this.candidateModule = candidateModule;
            this.admissionContext = admissionContext;
            this.handoffContext = handoffContext;
            this.groupContext = groupContext;
            lastSnapshot = ActivityPlayerLifecycleAdmissionSnapshot.Empty(
                ActivityPlayerLifecycleAdmissionStatus.None,
                nameof(ActivityPlayerLifecycleAdmissionRuntimeContext),
                "runtime-initialization",
                "No Activity Player lifecycle admission transaction has executed.");
        }

        internal static bool TryCreate(
            RuntimeContentRuntime runtimeContentRuntime,
            PlayerParticipationRuntimeContext participationContext,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerActorCandidateRuntimeHostModule candidateModule,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            PlayerGameplayChainHandoffRuntimeContext handoffContext,
            ActivityPlayerHandoffGroupRuntimeContext groupContext,
            out ActivityPlayerLifecycleAdmissionRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;
            if (runtimeContentRuntime == null ||
                participationContext == null ||
                preparationModule == null ||
                candidateModule == null ||
                admissionContext == null ||
                handoffContext == null ||
                groupContext == null)
            {
                issue =
                    "Activity Player lifecycle admission requires explicit RuntimeContent, participation, P3J, candidate, admission, handoff and group authorities.";
                return false;
            }

            context = new ActivityPlayerLifecycleAdmissionRuntimeContext(
                runtimeContentRuntime,
                participationContext,
                preparationModule,
                candidateModule,
                admissionContext,
                handoffContext,
                groupContext);
            return true;
        }

        internal void SetDiagnosticFaultPlan(IGameFlowDiagnosticFaultPlan plan)
        {
            diagnosticFaultPlan = plan ?? NoOpGameFlowDiagnosticFaultPlan.Instance;
            groupContext.SetDiagnosticFaultPlan(diagnosticFaultPlan);
        }

        internal bool HasActiveTransaction => active != null;

        private bool TryConsumeDiagnosticFault(
            GameFlowDiagnosticFaultCheckpoint checkpoint,
            string operation,
            string transaction,
            PlayerSlotId slot,
            out string diagnostic)
        {
            GameFlowDiagnosticFaultDecision decision = diagnosticFaultPlan.Evaluate(
                new GameFlowDiagnosticFaultRequest(
                    checkpoint, operation, transaction,
                    slot.IsValid ? slot.StableText : string.Empty));
            diagnostic = decision.Diagnostic;
            return decision.ShouldFail;
        }

        public ActivityPlayerLifecycleAdmissionResult TryPrepareSameRouteSwitch(
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            const string Operation = "PrepareSameRouteActivityPlayerAdmission";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerLifecycleAdmissionRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "prepare-same-route-activity-player-admission");

            if (targetActivity == null)
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Target Activity is missing.");
            }

            if (!targetActivity.HasDefinedPlayerParticipationRequirementLevel)
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    $"Activity '{targetActivity.ActivityName}' has an invalid Player participation Requirement Level.");
            }

            if (previousActivity != null &&
                ReferenceEquals(previousActivity, targetActivity))
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedUnsupportedFlow,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Same-Route Activity Player admission requires a different currently active Activity.");
            }

            if (active != null)
            {
                if ((active.State ==
                         ActivityPlayerLifecycleAdmissionState.ReadyToCommit ||
                    active.State ==
                         ActivityPlayerLifecycleAdmissionState.TransitionAuthorized) &&
                    previousActivity != null &&
                    active.PreviousActivity != null &&
                    ReferenceEquals(active.PreviousActivity, previousActivity) &&
                    active.TargetActivity != null &&
                    ReferenceEquals(active.TargetActivity, targetActivity))
                {
                    ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                        Snapshot(active);
                    return Result(
                        ActivityPlayerLifecycleAdmissionStatus
                            .SucceededAlreadyReadyToCommit,
                        Operation,
                        snapshot,
                        snapshot,
                        "The same Activity Player lifecycle admission transaction is already ready.");
                }

                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidState,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Another Activity Player lifecycle admission transaction is active.");
            }

            // Every target projection is resolved before deciding whether
            // GameplayReady handoff is required. Invalid authoring and a
            // rejected runtime-zero projection must fail before transition.
            if (!ActivityPlayerParticipationProjectionResolver.TryResolve(
                    targetActivity,
                    participationContext,
                    out PlayerParticipationRequirementLevel requirementLevel,
                    out List<PlayerSlotRuntimeSnapshot> projectedSlots,
                    out string projectionIssue))
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    projectionIssue);
            }

            if (requirementLevel !=
                targetActivity.PlayerParticipationRequirementLevel)
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Resolved target requirement changed while preparing Activity Player admission.");
            }

            if (requirementLevel !=
                PlayerParticipationRequirementLevel.GameplayReady)
            {
                return PublishSameRouteNotRequired(
                    previousActivity,
                    targetActivity,
                    requirementLevel,
                    resolvedSource,
                    resolvedReason,
                    $"Activity '{targetActivity.ActivityName}' does not require GameplayReady; Activity Player handoff is not required.");
            }

            if (previousActivity == null)
            {
                return PublishSameRouteNotRequired(
                    null,
                    targetActivity,
                    requirementLevel,
                    resolvedSource,
                    resolvedReason,
                    "No previous Activity ownership is available; target Activity will calculate GameplayReady readiness without handoff.");
            }

            RuntimeContentOwner previousOwner =
                CreateActivityOwner(previousActivity);
            RuntimeContentOwner targetOwner =
                CreateActivityOwner(targetActivity);
            if (previousOwner == targetOwner)
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Previous and target Activities resolve to the same RuntimeContent owner.");
            }

            if (projectedSlots.Count == 0)
            {
                return PublishSameRouteNotRequired(
                    previousActivity,
                    targetActivity,
                    requirementLevel,
                    resolvedSource,
                    resolvedReason,
                    "GameplayReady target projects no Player Slots; no Player handoff is required.");
            }

            PlayerGameplayAdmissionSnapshot currentAdmission =
                admissionContext.CreateSnapshot();
            if (currentAdmission == null ||
                !currentAdmission.IsInitialized)
            {
                return PublishSameRouteNotRequired(
                    previousActivity,
                    targetActivity,
                    requirementLevel,
                    resolvedSource,
                    resolvedReason,
                    "Current gameplay admission is unavailable; no transferable GameplayReady ownership exists.");
            }

            int transferableSlotCount = 0;
            for (int index = 0; index < projectedSlots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                if (!slot.IsJoined || !slot.HasSelectedActor)
                {
                    continue;
                }

                if (!currentAdmission.TryGetSummary(
                        slot.PlayerSlotId,
                        out PlayerGameplayAdmissionSummary admission) ||
                    !admission.GameplayReady)
                {
                    continue;
                }

                if (!admission.Token.IsValid || admission.Owner != previousOwner)
                {
                    return Reject(
                        ActivityPlayerLifecycleAdmissionStatus.RejectedCurrentGameplayNotReady,
                        Operation,
                        resolvedSource,
                        resolvedReason,
                        $"Projected Slot '{slot.PlayerSlotId.StableText}' has contradictory GameplayReady admission evidence.");
                }

                if (!preparationModule.TryGetCurrentPreparation(
                        slot.PlayerSlotId,
                        out PlayerActorPreparationSummary preparation,
                        out _ ) || !preparation.IsPrepared)
                {
                    continue;
                }

                if (preparation.Token != admission.PreparationToken ||
                    preparation.Materialization.Owner != admission.Owner ||
                    preparation.Materialization.Owner != previousOwner)
                {
                    return Reject(
                        ActivityPlayerLifecycleAdmissionStatus.RejectedCurrentGameplayNotReady,
                        Operation,
                        resolvedSource,
                        resolvedReason,
                        $"Projected Slot '{slot.PlayerSlotId.StableText}' has contradictory GameplayReady/P3J ownership evidence.");
                }

                bool tokenFault = TryConsumeDiagnosticFault(
                    GameFlowDiagnosticFaultCheckpoint.CurrentPreparationTokenValidation,
                    Operation, string.Empty, slot.PlayerSlotId,
                    out string tokenDiagnostic);
                bool ownerFault = TryConsumeDiagnosticFault(
                    GameFlowDiagnosticFaultCheckpoint.CurrentOwnershipValidation,
                    Operation, string.Empty, slot.PlayerSlotId,
                    out string ownerDiagnostic);
                if (tokenFault || ownerFault)
                {
                    return Reject(
                        ActivityPlayerLifecycleAdmissionStatus.RejectedCurrentGameplayNotReady,
                        Operation, resolvedSource, resolvedReason,
                        string.IsNullOrEmpty(tokenDiagnostic) ? ownerDiagnostic : tokenDiagnostic);
                }

                transferableSlotCount++;
            }

            if (transferableSlotCount != projectedSlots.Count)
            {
                return PublishSameRouteNotRequired(
                    previousActivity,
                    targetActivity,
                    requirementLevel,
                    resolvedSource,
                    resolvedReason,
                    "The relevant Player Slot set has no complete transferable GameplayReady ownership; target Activity will calculate its own readiness without partial handoff.");
            }

            var record = new TransactionRecord
            {
                FlowKind = ActivityPlayerLifecycleAdmissionFlowKind
                    .SameRouteActivitySwitch,
                PreviousActivity = previousActivity,
                TargetActivity = targetActivity,
                PreviousOwner = previousOwner,
                TargetOwner = targetOwner,
                State = ActivityPlayerLifecycleAdmissionState.Preparing,
                LastStatus = ActivityPlayerLifecycleAdmissionStatus.None,
                RequirementLevel = requirementLevel,
                Slots = new List<SlotRecord>(projectedSlots.Count),
                Source = resolvedSource,
                Reason = resolvedReason,
                Message = "Preparing same-Route Activity Player lifecycle admission."
            };

            for (int index = 0; index < projectedSlots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                if (!slot.IsJoined ||
                    !slot.HasSelectedActor ||
                    !currentAdmission.TryGetSummary(
                        slot.PlayerSlotId,
                        out PlayerGameplayAdmissionSummary admission) ||
                    !admission.GameplayReady ||
                    !admission.Token.IsValid ||
                    admission.Owner != previousOwner)
                {
                    return FailAndCleanup(
                        record,
                        ActivityPlayerLifecycleAdmissionStatus
                            .RejectedCurrentGameplayNotReady,
                        Operation,
                        $"Projected Slot '{slot.PlayerSlotId.StableText}' has no current GameplayReady admission owned by '{previousOwner.StableText}'.",
                        false);
                }

                if (!preparationModule.TryGetCurrentPreparation(
                        slot.PlayerSlotId,
                        out PlayerActorPreparationSummary preparation,
                        out string preparationIssue) ||
                    !preparation.IsPrepared ||
                    preparation.Token != admission.PreparationToken ||
                    preparation.Materialization.Owner != previousOwner)
                {
                    return FailAndCleanup(
                        record,
                        ActivityPlayerLifecycleAdmissionStatus
                            .RejectedCurrentGameplayNotReady,
                        Operation,
                        $"Projected Slot '{slot.PlayerSlotId.StableText}' has incoherent current P3J/P3K evidence. {preparationIssue}",
                        false);
                }

                record.Slots.Add(new SlotRecord
                {
                    Slot = slot,
                    PreviousAdmissionToken = admission.Token,
                    Message = "Current GameplayReady admission validated."
                });
            }

            RuntimeRootRegistryOperationResult targetRoot =
                runtimeContentRuntime.CreateScopeRoot(
                    targetOwner,
                    resolvedSource,
                    resolvedReason);
            if (targetRoot == null || !targetRoot.Applied)
            {
                return FailAndCleanup(
                    record,
                    ActivityPlayerLifecycleAdmissionStatus
                        .FailedScopePreparation,
                    Operation,
                    targetRoot != null
                        ? "Target Activity scope root must be newly created. " +
                          targetRoot.ToDiagnosticString()
                        : "Target Activity scope root creation returned no result.",
                    false);
            }

            if (!runtimeContentRuntime.TryCreateScopeContext(
                    targetOwner,
                    resolvedSource,
                    resolvedReason,
                    out RuntimeScopeContext targetContext))
            {
                return FailAndCleanup(
                    record,
                    ActivityPlayerLifecycleAdmissionStatus
                        .FailedScopePreparation,
                    Operation,
                    "Target Activity RuntimeScopeContext could not be created.",
                    true);
            }

            record.TargetContext = targetContext;
            transactionSequence++;
            PlayerParticipationSnapshot participation =
                participationContext.CreateSnapshot();
            record.Token = new ActivityPlayerLifecycleAdmissionToken(
                participation.ContextId,
                previousOwner,
                targetOwner,
                ActivityPlayerLifecycleAdmissionFlowKind
                    .SameRouteActivitySwitch,
                default,
                default,
                transactionSequence);
            active = record;

            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                if (TryConsumeDiagnosticFault(
                        GameFlowDiagnosticFaultCheckpoint.BeforeCandidateStaging,
                        Operation, record.Token.ToString(), slot.Slot.PlayerSlotId,
                        out string stagingDiagnostic))
                {
                    return FailAndCleanup(
                        record,
                        ActivityPlayerLifecycleAdmissionStatus.FailedCandidateStaging,
                        Operation, stagingDiagnostic, true);
                }
                PlayerActorCandidateStageResult stage =
                    candidateModule.TryStageCandidate(
                        targetContext,
                        slot.Slot.PlayerSlotId,
                        resolvedSource,
                        $"{resolvedReason}:stage-slot:{index}");
                if (stage == null ||
                    !stage.Succeeded ||
                    stage.CurrentSnapshot == null ||
                    !stage.CurrentSnapshot.IsStagedInactive ||
                    !stage.CurrentSnapshot.Token.IsValid)
                {
                    string issue = stage != null
                        ? stage.ToDiagnosticString()
                        : $"Candidate staging returned no result for Slot '{slot.Slot.PlayerSlotId.StableText}'.";
                    return FailAndCleanup(
                        record,
                        ActivityPlayerLifecycleAdmissionStatus
                            .FailedCandidateStaging,
                        Operation,
                        issue,
                        true);
                }

                slot.CandidateToken = stage.CurrentSnapshot.Token;
                slot.Staged = true;
                slot.Message = stage.Message;
            }

            var requests =
                new ActivityPlayerHandoffSlotRequest[record.Slots.Count];
            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                requests[index] = new ActivityPlayerHandoffSlotRequest(
                    slot.CandidateToken,
                    slot.PreviousAdmissionToken);
            }

            ActivityPlayerHandoffGroupResult group =
                groupContext.TryBegin(
                    targetActivity,
                    targetOwner,
                    requests,
                    resolvedSource,
                    resolvedReason);
            if (group == null ||
                !group.ReadyToCommit ||
                group.CurrentSnapshot == null ||
                !group.CurrentSnapshot.IsReadyToCommit ||
                !group.CurrentSnapshot.Token.IsValid)
            {
                string issue = group != null
                    ? group.ToDiagnosticString()
                    : "Activity Player handoff group Begin returned no result.";
                return FailAndCleanup(
                    record,
                    ActivityPlayerLifecycleAdmissionStatus.FailedGroupBegin,
                    Operation,
                    issue,
                    true);
            }

            record.GroupToken = group.CurrentSnapshot.Token;
            record.GroupSnapshot = group.CurrentSnapshot;
            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                slot.GroupBegan = true;
                if (!TryCaptureTargetEvidence(record, slot, out string evidenceIssue))
                {
                    return FailAndCleanup(
                        record,
                        ActivityPlayerLifecycleAdmissionStatus.FailedGroupBegin,
                        Operation,
                        evidenceIssue,
                        true);
                }
            }

            record.State =
                ActivityPlayerLifecycleAdmissionState.ReadyToCommit;
            record.LastStatus =
                ActivityPlayerLifecycleAdmissionStatus
                    .SucceededReadyToCommit;
            record.Message =
                "Target Activity Player handoff group is ReadyToCommit before transition presentation.";
            lastSnapshot = Snapshot(record);
            return Result(
                record.LastStatus,
                Operation,
                ActivityPlayerLifecycleAdmissionSnapshot.Empty(
                    ActivityPlayerLifecycleAdmissionStatus.None,
                    resolvedSource,
                    resolvedReason,
                    "No previous transaction."),
                lastSnapshot,
                record.Message);
        }


        public ActivityPlayerLifecycleAdmissionResult
            TryPrepareRouteStartupSwitch(
                RouteAsset previousRoute,
                RouteAsset targetRoute,
                ActivityAsset previousActivity,
                ActivityAsset targetActivity,
                string source,
                string reason)
        {
            const string Operation =
                "PrepareRouteStartupActivityPlayerAdmission";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerLifecycleAdmissionRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "prepare-route-startup-activity-player-admission");

            if (previousRoute == null ||
                targetRoute == null ||
                ReferenceEquals(previousRoute, targetRoute))
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedUnsupportedFlow,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Player Gameplay Admission requires distinct previous and target Routes.");
            }

            if (!previousRoute.HasValidRouteId || !targetRoute.HasValidRouteId)
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Route Startup Activity Player admission requires valid previous and target RouteIds.");
            }

            if (!targetRoute.HasStartupActivity ||
                targetActivity == null ||
                !ReferenceEquals(
                    targetRoute.StartupActivity,
                    targetActivity))
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Target Route must retain the exact target Startup Activity.");
            }

            RuntimeContentOwner previousRouteOwner =
                RuntimeContentOwner.Route(
                    previousRoute.RouteId.StableText,
                    previousRoute.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(previousRoute));
            RuntimeContentOwner targetRouteOwner =
                RuntimeContentOwner.Route(
                    targetRoute.RouteId.StableText,
                    targetRoute.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(targetRoute));
            if (previousRouteOwner == targetRouteOwner)
            {
                return Reject(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedInvalidRequest,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Previous and target Routes resolve to the same RuntimeContent owner.");
            }

            if (previousActivity == null)
            {
                // Route Startup without previous Activity still requires a
                // valid target Player projection before transition.
                if (!ActivityPlayerParticipationProjectionResolver.TryResolve(
                        targetActivity,
                        participationContext,
                        out _,
                        out _,
                        out string projectionIssue))
                {
                    return Reject(
                        ActivityPlayerLifecycleAdmissionStatus
                            .RejectedInvalidRequest,
                        Operation,
                        resolvedSource,
                        resolvedReason,
                        projectionIssue);
                }

                return PublishRouteStartupNotRequired(
                    previousRoute,
                    targetRoute,
                    null,
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    "No previous Activity ownership is available; Route Startup Player handoff is not required.");
            }

            ActivityPlayerLifecycleAdmissionResult preparation =
                TryPrepareSameRouteSwitch(
                    previousActivity,
                    targetActivity,
                    resolvedSource,
                    resolvedReason);
            if (preparation == null)
            {
                return preparation;
            }

            if (preparation.NotRequired)
            {
                return PublishRouteStartupNotRequired(
                    previousRoute,
                    targetRoute,
                    previousActivity,
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    preparation.Message);
            }

            if (!preparation.ReadyForTransition || active == null)
            {
                return preparation;
            }

            if (!ReferenceEquals(
                    active.PreviousActivity,
                    previousActivity) ||
                !ReferenceEquals(
                    active.TargetActivity,
                    targetActivity))
            {
                return RejectCurrent(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedInvalidState,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Activity-owner transaction does not match the Route Startup request.");
            }

            if (active.FlowKind ==
                    ActivityPlayerLifecycleAdmissionFlowKind
                        .RouteStartupActivitySwitch &&
                ((active.PreviousRoute != null &&
                  !ReferenceEquals(
                      active.PreviousRoute,
                      previousRoute)) ||
                 (active.TargetRoute != null &&
                  !ReferenceEquals(
                      active.TargetRoute,
                      targetRoute))))
            {
                return RejectCurrent(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedForeignOrStaleTransaction,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Route Startup transaction belongs to different Route assets.");
            }

            active.FlowKind =
                ActivityPlayerLifecycleAdmissionFlowKind
                    .RouteStartupActivitySwitch;
            active.PreviousRoute = previousRoute;
            active.TargetRoute = targetRoute;
            active.Token = new ActivityPlayerLifecycleAdmissionToken(
                active.Token.SessionContextId,
                active.PreviousOwner,
                active.TargetOwner,
                active.FlowKind,
                previousRoute.RouteId,
                targetRoute.RouteId,
                active.Token.Sequence);
            active.Message =
                "Target Route Startup Activity Player handoff group is ReadyToCommit before Route transition presentation.";
            lastSnapshot = Snapshot(active);
            return Result(
                preparation.Status,
                Operation,
                preparation.PreviousSnapshot,
                lastSnapshot,
                active.Message);
        }

        public ActivityPlayerLifecycleAdmissionResult TryAuthorizeTransition(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason)
        {
            const string Operation = "AuthorizeActivityTransition";
            if (!TryResolveActive(
                    expectedTransaction,
                    out TransactionRecord record,
                    out string issue))
            {
                return RejectCurrent(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedForeignOrStaleTransaction,
                    Operation,
                    source,
                    reason,
                    issue);
            }

            ActivityPlayerLifecycleAdmissionSnapshot previous =
                Snapshot(record);
            if (record.State ==
                ActivityPlayerLifecycleAdmissionState.TransitionAuthorized)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .SucceededAlreadyTransitionAuthorized,
                    Operation,
                    previous,
                    previous,
                    "Transition is already authorized for the same ReadyToCommit transaction.");
            }

            if (record.State !=
                    ActivityPlayerLifecycleAdmissionState.ReadyToCommit ||
                record.GroupSnapshot == null ||
                !record.GroupSnapshot.IsReadyToCommit)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .FailedTransitionAuthorization,
                    Operation,
                    previous,
                    previous,
                    "Transition authorization requires exact P3K.7E ReadyToCommit evidence.");
            }

            record.State =
                ActivityPlayerLifecycleAdmissionState.TransitionAuthorized;
            record.TransitionAuthorized = true;
            record.LastStatus =
                ActivityPlayerLifecycleAdmissionStatus
                    .SucceededTransitionAuthorized;
            record.Message =
                "Transition presentation authorized after P3K.7E ReadyToCommit.";
            lastSnapshot = Snapshot(record);
            return Result(
                record.LastStatus,
                Operation,
                previous,
                lastSnapshot,
                record.Message);
        }

        public ActivityPlayerLifecycleAdmissionResult TryCommit(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason)
        {
            const string Operation = "CommitActivityPlayerLifecycleAdmission";
            if (completed != null &&
                completed.Token == expectedTransaction &&
                completed.IsCompleted)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .SucceededAlreadyCommitted,
                    Operation,
                    completed,
                    completed,
                    "Activity Player lifecycle admission is already committed and adopted.");
            }

            if (!TryResolveActive(
                    expectedTransaction,
                    out TransactionRecord record,
                    out string issue))
            {
                return RejectCurrent(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedForeignOrStaleTransaction,
                    Operation,
                    source,
                    reason,
                    issue);
            }

            ActivityPlayerLifecycleAdmissionSnapshot previous =
                Snapshot(record);
            if (record.State is
                ActivityPlayerLifecycleAdmissionState
                    .CommittedAwaitingLifecycle or
                ActivityPlayerLifecycleAdmissionState
                    .CommitCleanupPending)
            {
                return Result(
                    record.CommitCleanupPending
                        ? ActivityPlayerLifecycleAdmissionStatus
                            .SucceededCommitCleanupPending
                        : ActivityPlayerLifecycleAdmissionStatus
                            .SucceededAlreadyCommitted,
                    Operation,
                    previous,
                    previous,
                    record.Message);
            }

            if (record.State !=
                    ActivityPlayerLifecycleAdmissionState
                        .TransitionAuthorized ||
                !record.TransitionAuthorized)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedInvalidState,
                    Operation,
                    previous,
                    previous,
                    "Commit requires transition authorization from the exact ReadyToCommit transaction.");
            }

            record.State =
                ActivityPlayerLifecycleAdmissionState.Committing;
            ActivityPlayerHandoffGroupResult commit =
                groupContext.TryCommit(
                    record.GroupToken,
                    source,
                    reason);
            record.GroupSnapshot = commit?.CurrentSnapshot;

            bool cleanupPending =
                commit != null &&
                commit.Status ==
                    ActivityPlayerHandoffGroupStatus
                        .FailedCommitCleanup &&
                commit.CurrentSnapshot != null &&
                commit.CurrentSnapshot.IsCommitCleanupFailed;

            if (commit == null ||
                (!commit.Committed && !cleanupPending))
            {
                string commitIssue = commit != null
                    ? commit.ToDiagnosticString()
                    : "Activity Player handoff group Commit returned no result.";
                bool hasCommittedSlot = HasCommittedGroupSlot(
                    commit != null ? commit.CurrentSnapshot : null);
                if (!hasCommittedSlot)
                {
                    bool rolledBack = TryRollbackRecord(
                        record,
                        source,
                        "commit-failure-rollback",
                        out string rollbackIssue);
                    record.LastStatus = rolledBack
                        ? ActivityPlayerLifecycleAdmissionStatus.FailedCommit
                        : ActivityPlayerLifecycleAdmissionStatus.FailedRollback;
                    record.Message = Join(commitIssue, rollbackIssue);
                    lastSnapshot = Snapshot(record);
                    if (rolledBack)
                    {
                        active = null;
                    }

                    return Result(
                        record.LastStatus,
                        Operation,
                        previous,
                        lastSnapshot,
                        record.Message);
                }

                record.State =
                    ActivityPlayerLifecycleAdmissionState.Failed;
                record.LastStatus =
                    ActivityPlayerLifecycleAdmissionStatus.FailedCommit;
                record.Message =
                    "A Slot crossed the irreversible ownership boundary before group Commit failed. " +
                    commitIssue;
                lastSnapshot = Snapshot(record);
                return Result(
                    record.LastStatus,
                    Operation,
                    previous,
                    lastSnapshot,
                    record.Message);
            }

            for (int index = 0; index < record.Slots.Count; index++)
            {
                SlotRecord slot = record.Slots[index];
                slot.Committed = true;
                if (!TryCaptureTargetEvidence(
                        record,
                        slot,
                        out string evidenceIssue))
                {
                    record.State =
                        ActivityPlayerLifecycleAdmissionState.Failed;
                    record.LastStatus =
                        ActivityPlayerLifecycleAdmissionStatus.FailedCommit;
                    record.Message =
                        "Committed target evidence is incomplete. " +
                        evidenceIssue;
                    lastSnapshot = Snapshot(record);
                    return Result(
                        record.LastStatus,
                        Operation,
                        previous,
                        lastSnapshot,
                        record.Message);
                }
            }

            record.CommitCleanupPending = cleanupPending;
            record.State = cleanupPending
                ? ActivityPlayerLifecycleAdmissionState
                    .CommitCleanupPending
                : ActivityPlayerLifecycleAdmissionState
                    .CommittedAwaitingLifecycle;
            record.LastStatus = cleanupPending
                ? ActivityPlayerLifecycleAdmissionStatus
                    .SucceededCommitCleanupPending
                : ActivityPlayerLifecycleAdmissionStatus
                    .SucceededCommitted;
            record.Message = cleanupPending
                ? "Target Player ownership is authoritative; previous Actor cleanup remains pending."
                : "Target Player ownership committed and awaits Activity lifecycle adoption.";
            lastSnapshot = Snapshot(record);
            return Result(
                record.LastStatus,
                Operation,
                previous,
                lastSnapshot,
                record.Message);
        }

        public ActivityPlayerLifecycleAdmissionResult TryRetryCommitCleanup(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason)
        {
            const string Operation =
                "RetryActivityPlayerLifecycleCommitCleanup";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerLifecycleAdmissionRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "retry-activity-player-lifecycle-commit-cleanup");

            if (completed != null &&
                completed.Token == expectedTransaction &&
                completed.IsCompleted)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .SucceededAlreadyCommitted,
                    Operation,
                    completed,
                    completed,
                    "Activity Player lifecycle commit cleanup is already complete.");
            }

            if (!TryResolveActive(
                    expectedTransaction,
                    out TransactionRecord record,
                    out string issue))
            {
                return RejectCurrent(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedForeignOrStaleTransaction,
                    Operation,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            ActivityPlayerLifecycleAdmissionSnapshot previous =
                Snapshot(record);
            if (record.State !=
                    ActivityPlayerLifecycleAdmissionState
                        .CommitCleanupPending ||
                !record.CommitCleanupPending ||
                !record.GroupToken.IsValid)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedInvalidState,
                    Operation,
                    previous,
                    previous,
                    "Lifecycle commit-cleanup retry requires an active CommitCleanupPending transaction with a valid handoff group token.");
            }

            ActivityPlayerHandoffGroupResult retry =
                groupContext.TryRetryCommitCleanup(
                    record.GroupToken,
                    resolvedSource,
                    resolvedReason);
            record.GroupSnapshot = retry?.CurrentSnapshot;

            if (retry == null || !retry.Committed)
            {
                record.State =
                    ActivityPlayerLifecycleAdmissionState
                        .CommitCleanupPending;
                record.CommitCleanupPending = true;
                record.LastStatus =
                    ActivityPlayerLifecycleAdmissionStatus
                        .SucceededCommitCleanupPending;
                record.Message =
                    "Target Player ownership remains authoritative; previous Actor cleanup is still pending. " +
                    (retry != null
                        ? retry.ToDiagnosticString()
                        : "Activity Player handoff group cleanup retry returned no result.");
                lastSnapshot = Snapshot(record);
                return Result(
                    record.LastStatus,
                    Operation,
                    previous,
                    lastSnapshot,
                    record.Message);
            }

            record.CommitCleanupPending = false;
            record.State =
                ActivityPlayerLifecycleAdmissionState
                    .CommittedAwaitingLifecycle;
            record.LastStatus =
                ActivityPlayerLifecycleAdmissionStatus
                    .SucceededCommitted;
            record.Message =
                "Activity Player handoff group commit cleanup completed; lifecycle completion is being finalized.";
            lastSnapshot = Snapshot(record);

            TryCompleteLifecycle(record);

            ActivityPlayerLifecycleAdmissionSnapshot current =
                CreateSnapshot();
            return Result(
                record.LastStatus,
                Operation,
                previous,
                current,
                record.Message);
        }

        public ActivityPlayerLifecycleAdmissionResult TryRollback(
            ActivityPlayerLifecycleAdmissionToken expectedTransaction,
            string source,
            string reason)
        {
            const string Operation = "RollbackActivityPlayerLifecycleAdmission";
            if (completed != null &&
                completed.Token == expectedTransaction)
            {
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedRollbackNotAvailable,
                    Operation,
                    completed,
                    completed,
                    "Completed Activity Player lifecycle admission cannot rollback.");
            }

            if (!TryResolveActive(
                    expectedTransaction,
                    out TransactionRecord record,
                    out string issue))
            {
                return RejectCurrent(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedForeignOrStaleTransaction,
                    Operation,
                    source,
                    reason,
                    issue);
            }

            if (record.State is
                ActivityPlayerLifecycleAdmissionState
                    .CommittedAwaitingLifecycle or
                ActivityPlayerLifecycleAdmissionState
                    .CommitCleanupPending or
                ActivityPlayerLifecycleAdmissionState.Completed)
            {
                ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                    Snapshot(record);
                return Result(
                    ActivityPlayerLifecycleAdmissionStatus
                        .RejectedRollbackNotAvailable,
                    Operation,
                    snapshot,
                    snapshot,
                    "Target ownership is already committed; rollback is unavailable.");
            }

            ActivityPlayerLifecycleAdmissionSnapshot previous =
                Snapshot(record);
            bool rolledBack = TryRollbackRecord(
                record,
                source,
                reason,
                out string rollbackIssue);
            ActivityPlayerLifecycleAdmissionSnapshot current =
                Snapshot(record);
            if (rolledBack)
            {
                active = null;
                lastSnapshot = current;
            }

            return Result(
                rolledBack
                    ? ActivityPlayerLifecycleAdmissionStatus
                        .SucceededRolledBack
                    : ActivityPlayerLifecycleAdmissionStatus
                        .FailedRollback,
                Operation,
                previous,
                current,
                rollbackIssue);
        }

        public ActivityPlayerLifecycleAdmissionSnapshot CreateSnapshot()
        {
            if (active != null)
            {
                return Snapshot(active);
            }

            return completed ?? lastSnapshot;
        }

        public bool TryHandleSupersededPreviousExit(
            ActivityContentExecutionRequest request,
            out bool handled,
            out ActivityPlayerPreviousExitDisposition disposition,
            out string issue)
        {
            handled = false;
            disposition = ActivityPlayerPreviousExitDisposition.None;
            issue = string.Empty;
            if (active == null)
            {
                return true;
            }

            bool routeStartupAwaitingCommit =
                active.FlowKind ==
                    ActivityPlayerLifecycleAdmissionFlowKind
                        .RouteStartupActivitySwitch &&
                active.State ==
                    ActivityPlayerLifecycleAdmissionState
                        .TransitionAuthorized;
            bool committed = active.State is
                ActivityPlayerLifecycleAdmissionState
                    .CommittedAwaitingLifecycle or
                ActivityPlayerLifecycleAdmissionState
                    .CommitCleanupPending;
            if (!routeStartupAwaitingCommit && !committed)
            {
                return true;
            }

            bool nextActivityMatches = active.FlowKind ==
                    ActivityPlayerLifecycleAdmissionFlowKind
                        .RouteStartupActivitySwitch
                ? request.NextActivity == null
                : ReferenceEquals(
                    request.NextActivity,
                    active.TargetActivity);
            if (request.Phase !=
                    ActivityContentExecutionPhase.Exit ||
                !ReferenceEquals(
                    request.Activity,
                    active.PreviousActivity) ||
                !nextActivityMatches ||
                request.Owner != active.PreviousOwner)
            {
                return true;
            }

            active.PreviousExitAcknowledged = true;
            disposition = routeStartupAwaitingCommit
                ? ActivityPlayerPreviousExitDisposition
                    .SupersededAwaitingCommit
                : ActivityPlayerPreviousExitDisposition
                    .SupersededByCommittedHandoff;
            active.PreviousExitDisposition = disposition;
            active.Message = routeStartupAwaitingCommit
                ? "Previous Activity lifecycle exit transferred Player ownership to the reversible Route Startup handoff; physical previous Actors remain retained until commit."
                : "Previous Activity lifecycle exit acknowledged after P3K.7E commit; previous Player Actors were already released by the handoff.";
            handled = true;
            lastSnapshot = Snapshot(active);
            TryCompleteLifecycle(active);
            return true;
        }

        public bool TryAdoptCommittedTarget(
            ActivityContentExecutionRequest request,
            IReadOnlyList<PlayerSlotRuntimeSnapshot> projectedSlots,
            out IReadOnlyList<ActivityPlayerGameplayAdoptedSlot> adoptedSlots,
            out string issue)
        {
            var adopted =
                new List<ActivityPlayerGameplayAdoptedSlot>();
            adoptedSlots = adopted;
            issue = string.Empty;

            if (active == null ||
                active.State is not (
                    ActivityPlayerLifecycleAdmissionState
                        .CommittedAwaitingLifecycle or
                    ActivityPlayerLifecycleAdmissionState
                        .CommitCleanupPending))
            {
                issue =
                    "No committed Activity Player lifecycle admission transaction is available for target adoption.";
                return false;
            }

            bool previousActivityMatches = active.FlowKind ==
                    ActivityPlayerLifecycleAdmissionFlowKind
                        .RouteStartupActivitySwitch
                ? request.PreviousActivity == null
                : ReferenceEquals(
                    request.PreviousActivity,
                    active.PreviousActivity);
            if (request.Phase !=
                    ActivityContentExecutionPhase.Enter ||
                !ReferenceEquals(
                    request.Activity,
                    active.TargetActivity) ||
                !previousActivityMatches ||
                request.Owner != active.TargetOwner)
            {
                issue =
                    "Target Activity lifecycle request does not match the committed admission transaction.";
                return FailLifecycleAdoption(issue);
            }

            if (projectedSlots == null ||
                projectedSlots.Count != active.Slots.Count)
            {
                issue =
                    "Target Activity projection differs from the committed Player handoff Slot count.";
                return FailLifecycleAdoption(issue);
            }

            for (int index = 0;
                 index < active.Slots.Count;
                 index++)
            {
                SlotRecord slot = active.Slots[index];
                if (projectedSlots[index].PlayerSlotId !=
                    slot.Slot.PlayerSlotId)
                {
                    issue =
                        $"Target Activity projection order differs at Slot index '{index}'.";
                    return FailLifecycleAdoption(issue);
                }

                if (!TryCaptureTargetEvidence(
                        active,
                        slot,
                        out string evidenceIssue) ||
                    !slot.TargetPreparationToken.IsValid ||
                    !slot.TargetAdmissionToken.IsValid)
                {
                    issue = evidenceIssue;
                    return FailLifecycleAdoption(issue);
                }

                PlayerGameplayAdmissionSnapshot admissions =
                    admissionContext.CreateSnapshot();
                if (admissions == null ||
                    !admissions.TryGetSummary(
                        slot.Slot.PlayerSlotId,
                        out PlayerGameplayAdmissionSummary admission) ||
                    !admission.GameplayReady ||
                    admission.Token != slot.TargetAdmissionToken ||
                    admission.Owner != active.TargetOwner)
                {
                    issue =
                        $"Target Slot '{slot.Slot.PlayerSlotId.StableText}' lost GameplayReady admission before lifecycle adoption.";
                    return FailLifecycleAdoption(issue);
                }

                adopted.Add(new ActivityPlayerGameplayAdoptedSlot(
                    slot.Slot.PlayerSlotId,
                    admission.ActorProfileId,
                    slot.TargetPreparationToken,
                    slot.TargetAdmissionToken,
                    "Committed P3J/P3K target evidence adopted by P3J.6."));
            }

            for (int index = 0;
                 index < active.Slots.Count;
                 index++)
            {
                active.Slots[index].Adopted = true;
            }

            active.TargetEnterAdopted = true;
            active.LastStatus = active.CommitCleanupPending
                ? ActivityPlayerLifecycleAdmissionStatus
                    .SucceededCommitCleanupPending
                : ActivityPlayerLifecycleAdmissionStatus
                    .SucceededCommitted;
            active.Message = active.PreviousExitAcknowledged
                ? "Committed GameplayReady Player evidence adopted by target Activity lifecycle; transaction completion is being finalized."
                : "Committed GameplayReady Player evidence adopted by target Activity lifecycle; previous Activity exit acknowledgement remains pending.";
            lastSnapshot = Snapshot(active);
            TryCompleteLifecycle(active);
            adoptedSlots = adopted;
            return true;
        }

        public bool TryReleaseGameplayBeforeActor(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason,
            out bool released,
            out string issue)
        {
            released = false;
            issue = string.Empty;
            if (!playerSlotId.IsValid ||
                !expectedPreparation.IsValid)
            {
                issue =
                    "Gameplay-before-Actor release requires a valid Slot and exact preparation token.";
                return false;
            }

            if (!preparationModule.TryGetCurrentPreparation(
                    playerSlotId,
                    out PlayerActorPreparationSummary preparation,
                    out string preparationIssue) ||
                preparation.Token != expectedPreparation)
            {
                issue =
                    "Gameplay-before-Actor release requires the exact current P3J preparation. " +
                    preparationIssue;
                return false;
            }

            PlayerGameplayAdmissionSnapshot snapshot =
                admissionContext.CreateSnapshot();
            if (snapshot == null ||
                !snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary admission) ||
                !admission.IsAdmitted)
            {
                return true;
            }

            if (admission.PreparationToken != expectedPreparation ||
                admission.ActorId !=
                    preparation.Materialization.ActorId ||
                admission.Owner !=
                    preparation.Materialization.Owner)
            {
                issue =
                    "Current P3K.5 admission does not match the Actor being released.";
                return false;
            }

            PlayerGameplayAdmissionResult result =
                handoffContext.TryReleaseCurrentGameplayChain(
                    playerSlotId,
                    admission.Token,
                    source,
                    reason);
            if (!result.Succeeded)
            {
                issue = result.ToDiagnosticString();
                return false;
            }

            released = true;
            return true;
        }

        private bool FailLifecycleAdoption(string issue)
        {
            if (active != null)
            {
                active.State =
                    ActivityPlayerLifecycleAdmissionState.Failed;
                active.LastStatus =
                    ActivityPlayerLifecycleAdmissionStatus
                        .FailedLifecycleAdoption;
                active.Message = issue.NormalizeTextOrFallback(
                    "Target Activity lifecycle adoption failed after ownership commit.");
                lastSnapshot = Snapshot(active);
            }

            return false;
        }

        private bool TryCaptureTargetEvidence(
            TransactionRecord record,
            SlotRecord slot,
            out string issue)
        {
            issue = string.Empty;
            if (!preparationModule.TryGetCurrentPreparation(
                    slot.Slot.PlayerSlotId,
                    out PlayerActorPreparationSummary preparation,
                    out issue) ||
                !preparation.IsPrepared ||
                preparation.Materialization.Owner !=
                    record.TargetOwner ||
                preparation.Materialization.ActorId !=
                    slot.CandidateToken.ActorId)
            {
                issue =
                    $"Target P3J preparation is invalid for Slot '{slot.Slot.PlayerSlotId.StableText}'. {issue}";
                return false;
            }

            PlayerGameplayAdmissionSnapshot admissions =
                admissionContext.CreateSnapshot();
            if (admissions == null ||
                !admissions.TryGetSummary(
                    slot.Slot.PlayerSlotId,
                    out PlayerGameplayAdmissionSummary admission) ||
                !admission.GameplayReady ||
                admission.PreparationToken != preparation.Token ||
                admission.Owner != record.TargetOwner)
            {
                issue =
                    $"Target P3K.5 GameplayReady admission is invalid for Slot '{slot.Slot.PlayerSlotId.StableText}'.";
                return false;
            }

            slot.TargetPreparationToken = preparation.Token;
            slot.TargetAdmissionToken = admission.Token;
            slot.Message =
                "Target P3J preparation and P3K.5 admission captured.";
            return true;
        }

        private ActivityPlayerLifecycleAdmissionResult PublishSameRouteNotRequired(
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            PlayerParticipationRequirementLevel requirementLevel,
            string source,
            string reason,
            string message)
        {
            RuntimeContentOwner previousOwner = previousActivity != null &&
                previousActivity.HasValidActivityId
                ? CreateActivityOwner(previousActivity)
                : default;
            RuntimeContentOwner targetOwner = CreateActivityOwner(targetActivity);
            PlayerParticipationSnapshot participation = participationContext.CreateSnapshot();
            transactionSequence++;
            var token = new ActivityPlayerLifecycleAdmissionToken(
                participation.ContextId,
                previousOwner,
                targetOwner,
                ActivityPlayerLifecycleAdmissionFlowKind.SameRouteActivitySwitch,
                default,
                default,
                transactionSequence);
            var snapshot = new ActivityPlayerLifecycleAdmissionSnapshot(
                token,
                ActivityPlayerLifecycleAdmissionState.NotRequired,
                ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                ActivityPlayerLifecycleAdmissionFlowKind.SameRouteActivitySwitch,
                string.Empty,
                string.Empty,
                previousActivity != null ? previousActivity.ActivityName : string.Empty,
                targetActivity.ActivityName,
                previousOwner,
                targetOwner,
                requirementLevel,
                null,
                Array.Empty<ActivityPlayerLifecycleAdmissionSlotSnapshot>(),
                false,
                false,
                ActivityPlayerPreviousExitDisposition.None,
                false,
                false,
                source,
                reason,
                message);
            lastSnapshot = snapshot;
            return Result(
                ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                "PrepareSameRouteActivityPlayerAdmission",
                snapshot,
                snapshot,
                message);
        }

        private ActivityPlayerLifecycleAdmissionResult PublishRouteStartupNotRequired(
            RouteAsset previousRoute,
            RouteAsset targetRoute,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason,
            string message)
        {
            RuntimeContentOwner previousOwner = previousActivity != null &&
                previousActivity.HasValidActivityId
                ? CreateActivityOwner(previousActivity)
                : default;
            RuntimeContentOwner targetOwner = CreateActivityOwner(targetActivity);
            PlayerParticipationSnapshot participation = participationContext.CreateSnapshot();
            transactionSequence++;
            var token = new ActivityPlayerLifecycleAdmissionToken(
                participation.ContextId,
                previousOwner,
                targetOwner,
                ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch,
                previousRoute.RouteId,
                targetRoute.RouteId,
                transactionSequence);
            var snapshot = new ActivityPlayerLifecycleAdmissionSnapshot(
                token,
                ActivityPlayerLifecycleAdmissionState.NotRequired,
                ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch,
                previousRoute.RouteName,
                targetRoute.RouteName,
                previousActivity != null ? previousActivity.ActivityName : string.Empty,
                targetActivity.ActivityName,
                previousOwner,
                targetOwner,
                PlayerParticipationRequirementLevel.GameplayReady,
                null,
                Array.Empty<ActivityPlayerLifecycleAdmissionSlotSnapshot>(),
                false,
                false,
                ActivityPlayerPreviousExitDisposition.None,
                false,
                false,
                source,
                reason,
                message);
            lastSnapshot = snapshot;
            return Result(
                ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                "PrepareRouteStartupActivityPlayerAdmission",
                snapshot,
                snapshot,
                message);
        }

        private ActivityPlayerLifecycleAdmissionResult FailAndCleanup(
            TransactionRecord record,
            ActivityPlayerLifecycleAdmissionStatus status,
            string operation,
            string issue,
            bool removeTargetScope)
        {
            bool cleanupSucceeded = TryRollbackRecord(
                record,
                record.Source,
                "prepare-failure-cleanup",
                out string rollbackIssue,
                removeTargetScope);
            record.State = cleanupSucceeded
                ? ActivityPlayerLifecycleAdmissionState.RolledBack
                : ActivityPlayerLifecycleAdmissionState.Failed;
            record.LastStatus = cleanupSucceeded
                ? status
                : ActivityPlayerLifecycleAdmissionStatus.FailedRollback;
            record.Message = Join(issue, rollbackIssue);
            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                Snapshot(record);
            active = null;
            lastSnapshot = snapshot;
            return Result(
                record.LastStatus,
                operation,
                ActivityPlayerLifecycleAdmissionSnapshot.Empty(
                    ActivityPlayerLifecycleAdmissionStatus.None,
                    record.Source,
                    record.Reason,
                    "No previous transaction."),
                snapshot,
                record.Message);
        }

        private bool TryRollbackRecord(
            TransactionRecord record,
            string source,
            string reason,
            out string issue,
            bool removeTargetScope = true)
        {
            issue = string.Empty;
            record.State =
                ActivityPlayerLifecycleAdmissionState.RollingBack;
            var failures = new List<string>();

            if (record.GroupToken.IsValid &&
                groupContext.CreateSnapshot() is
                    ActivityPlayerHandoffGroupSnapshot group &&
                group.Token == record.GroupToken &&
                !group.IsCommitted)
            {
                ActivityPlayerHandoffGroupResult rollback =
                    groupContext.TryRollback(
                        record.GroupToken,
                        source,
                        reason);
                if (rollback == null ||
                    rollback.Status !=
                        ActivityPlayerHandoffGroupStatus
                            .SucceededRolledBack)
                {
                    failures.Add(
                        rollback != null
                            ? rollback.ToDiagnosticString()
                            : "Activity Player handoff group rollback returned no result.");
                }
                else
                {
                    record.GroupSnapshot =
                        rollback.CurrentSnapshot;
                }
            }

            for (int index = record.Slots.Count - 1;
                 index >= 0;
                 index--)
            {
                SlotRecord slot = record.Slots[index];
                if (!slot.Staged ||
                    !slot.CandidateToken.IsValid ||
                    slot.Committed)
                {
                    continue;
                }

                PlayerActorCandidateStageResult rollback =
                    candidateModule.TryRollbackCandidate(
                        slot.CandidateToken,
                        source,
                        $"{reason}:candidate:{index}");
                if (rollback == null ||
                    !rollback.Succeeded)
                {
                    failures.Add(
                        rollback != null
                            ? rollback.ToDiagnosticString()
                            : $"Candidate rollback returned no result for Slot '{slot.Slot.PlayerSlotId.StableText}'.");
                }
                else
                {
                    slot.Released = true;
                }
            }

            if (removeTargetScope &&
                record.TargetOwner.IsValid)
            {
                RuntimeRootRegistryOperationResult removeRoot =
                    runtimeContentRuntime.RemoveScopeRoot(
                        record.TargetOwner,
                        source,
                        reason);
                if (removeRoot != null && removeRoot.Rejected)
                {
                    failures.Add(removeRoot.ToDiagnosticString());
                }
            }

            if (failures.Count > 0)
            {
                record.State =
                    ActivityPlayerLifecycleAdmissionState.Failed;
                record.LastStatus =
                    ActivityPlayerLifecycleAdmissionStatus
                        .FailedRollback;
                record.Message = string.Join(" | ", failures);
                issue = record.Message;
                return false;
            }

            record.State =
                ActivityPlayerLifecycleAdmissionState.RolledBack;
            record.LastStatus =
                ActivityPlayerLifecycleAdmissionStatus
                    .SucceededRolledBack;
            record.Message =
                "All target candidates and the reversible handoff group rolled back.";
            issue = string.Empty;
            return true;
        }

        private void TryCompleteLifecycle(
            TransactionRecord record)
        {
            if (!record.PreviousExitAcknowledged ||
                !record.TargetEnterAdopted)
            {
                lastSnapshot = Snapshot(record);
                return;
            }

            record.State = record.CommitCleanupPending
                ? ActivityPlayerLifecycleAdmissionState
                    .CommitCleanupPending
                : ActivityPlayerLifecycleAdmissionState.Completed;
            record.LastStatus = record.CommitCleanupPending
                ? ActivityPlayerLifecycleAdmissionStatus
                    .SucceededCommitCleanupPending
                : ActivityPlayerLifecycleAdmissionStatus
                    .SucceededLifecycleCompleted;
            record.Message = record.CommitCleanupPending
                ? "Target Activity lifecycle adopted all Players; previous Actor cleanup remains pending."
                : record.FlowKind ==
                    ActivityPlayerLifecycleAdmissionFlowKind
                        .RouteStartupActivitySwitch
                    ? "Route Startup GameplayReady Activity Player lifecycle admission completed."
                    : "Same-Route GameplayReady Activity Player lifecycle admission completed.";
            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                Snapshot(record);
            lastSnapshot = snapshot;
            if (!record.CommitCleanupPending)
            {
                completed = snapshot;
                active = null;
            }
        }

        private bool TryResolveActive(
            ActivityPlayerLifecycleAdmissionToken expected,
            out TransactionRecord record,
            out string issue)
        {
            record = active;
            if (record == null ||
                !expected.IsValid ||
                record.Token != expected)
            {
                issue =
                    "Activity Player lifecycle admission token is foreign, stale or no longer active.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private ActivityPlayerLifecycleAdmissionResult Reject(
            ActivityPlayerLifecycleAdmissionStatus status,
            string operation,
            string source,
            string reason,
            string message)
        {
            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                ActivityPlayerLifecycleAdmissionSnapshot.Empty(
                    status,
                    source,
                    reason,
                    message);
            lastSnapshot = snapshot;
            return Result(
                status,
                operation,
                snapshot,
                snapshot,
                message);
        }

        private ActivityPlayerLifecycleAdmissionResult RejectCurrent(
            ActivityPlayerLifecycleAdmissionStatus status,
            string operation,
            string source,
            string reason,
            string message)
        {
            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                CreateSnapshot();
            if (snapshot == null)
            {
                snapshot = ActivityPlayerLifecycleAdmissionSnapshot.Empty(
                    status,
                    source,
                    reason,
                    message);
            }
            return Result(
                status,
                operation,
                snapshot,
                snapshot,
                message);
        }

        private static ActivityPlayerLifecycleAdmissionResult Result(
            ActivityPlayerLifecycleAdmissionStatus status,
            string operation,
            ActivityPlayerLifecycleAdmissionSnapshot previous,
            ActivityPlayerLifecycleAdmissionSnapshot current,
            string message) =>
            new ActivityPlayerLifecycleAdmissionResult(
                status,
                operation,
                previous,
                current,
                message);

        private static RuntimeContentOwner CreateActivityOwner(
            ActivityAsset activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (!activity.HasValidActivityId)
            {
                throw new ArgumentException(
                    "Activity Player lifecycle owner requires a valid ActivityId.",
                    nameof(activity));
            }

            return RuntimeContentOwner.Activity(
                activity.ActivityId.StableText,
                activity.ActivityName,
                RuntimeDefinitionToken.FromUnityObject(activity));
        }

        private static ActivityPlayerLifecycleAdmissionSnapshot Snapshot(
            TransactionRecord record)
        {
            if (record == null)
            {
                return null;
            }

            var slots =
                new ActivityPlayerLifecycleAdmissionSlotSnapshot[
                    record.Slots.Count];
            for (int index = 0;
                 index < record.Slots.Count;
                 index++)
            {
                SlotRecord slot = record.Slots[index];
                slots[index] =
                    new ActivityPlayerLifecycleAdmissionSlotSnapshot(
                        slot.Slot.PlayerSlotId,
                        slot.PreviousAdmissionToken,
                        slot.CandidateToken,
                        slot.TargetPreparationToken,
                        slot.TargetAdmissionToken,
                        slot.Staged,
                        slot.GroupBegan,
                        slot.Committed,
                        slot.Adopted,
                        slot.Released,
                        slot.Message);
            }

            return new ActivityPlayerLifecycleAdmissionSnapshot(
                record.Token,
                record.State,
                record.LastStatus,
                record.FlowKind,
                record.PreviousRoute != null
                    ? record.PreviousRoute.RouteName
                    : string.Empty,
                record.TargetRoute != null
                    ? record.TargetRoute.RouteName
                    : string.Empty,
                record.PreviousActivity != null
                    ? record.PreviousActivity.ActivityName
                    : string.Empty,
                record.TargetActivity != null
                    ? record.TargetActivity.ActivityName
                    : string.Empty,
                record.PreviousOwner,
                record.TargetOwner,
                record.RequirementLevel,
                record.GroupSnapshot,
                slots,
                record.TransitionAuthorized,
                record.PreviousExitAcknowledged,
                record.PreviousExitDisposition,
                record.TargetEnterAdopted,
                record.CommitCleanupPending,
                record.Source,
                record.Reason,
                record.Message);
        }


        private static bool HasCommittedGroupSlot(
            ActivityPlayerHandoffGroupSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].Committed)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Join(
            string left,
            string right)
        {
            string a = left.NormalizeText();
            string b = right.NormalizeText();
            if (string.IsNullOrEmpty(a))
            {
                return b;
            }

            if (string.IsNullOrEmpty(b))
            {
                return a;
            }

            return a + " " + b;
        }
    }
}
