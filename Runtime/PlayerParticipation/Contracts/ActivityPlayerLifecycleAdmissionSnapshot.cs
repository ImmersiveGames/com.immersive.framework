using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Immutable Activity Player lifecycle admission evidence for same-Route and Route Startup flows.")]
    public sealed class ActivityPlayerLifecycleAdmissionSnapshot
    {
        private readonly ActivityPlayerLifecycleAdmissionSlotSnapshot[] slots;

        internal ActivityPlayerLifecycleAdmissionSnapshot(
            ActivityPlayerLifecycleAdmissionToken token,
            ActivityPlayerLifecycleAdmissionState state,
            ActivityPlayerLifecycleAdmissionStatus lastStatus,
            ActivityPlayerLifecycleAdmissionFlowKind flowKind,
            string previousRouteName,
            string targetRouteName,
            string previousActivityName,
            string targetActivityName,
            RuntimeContentOwner previousOwner,
            RuntimeContentOwner targetOwner,
            PlayerParticipationRequirementLevel requirementLevel,
            ActivityPlayerHandoffGroupSnapshot handoffGroup,
            ActivityPlayerLifecycleAdmissionSlotSnapshot[] slots,
            bool transitionAuthorized,
            bool previousExitAcknowledged,
            ActivityPlayerPreviousExitDisposition previousExitDisposition,
            bool targetEnterAdopted,
            bool commitCleanupPending,
            string source,
            string reason,
            string message)
        {
            Token = token;
            State = state;
            LastStatus = lastStatus;
            FlowKind = flowKind;
            PreviousRouteName = previousRouteName.NormalizeText();
            TargetRouteName = targetRouteName.NormalizeText();
            PreviousActivityName = previousActivityName.NormalizeText();
            TargetActivityName = targetActivityName.NormalizeText();
            PreviousOwner = previousOwner;
            TargetOwner = targetOwner;
            RequirementLevel = requirementLevel;
            HandoffGroup = handoffGroup;
            this.slots = slots != null
                ? (ActivityPlayerLifecycleAdmissionSlotSnapshot[])slots.Clone()
                : Array.Empty<ActivityPlayerLifecycleAdmissionSlotSnapshot>();
            TransitionAuthorized = transitionAuthorized;
            PreviousExitAcknowledged = previousExitAcknowledged;
            PreviousExitDisposition = previousExitDisposition;
            TargetEnterAdopted = targetEnterAdopted;
            CommitCleanupPending = commitCleanupPending;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ActivityPlayerLifecycleAdmissionToken Token { get; }
        public ActivityPlayerLifecycleAdmissionState State { get; }
        public ActivityPlayerLifecycleAdmissionStatus LastStatus { get; }
        public ActivityPlayerLifecycleAdmissionFlowKind FlowKind { get; }
        public string PreviousRouteName { get; }
        public string TargetRouteName { get; }
        public string PreviousActivityName { get; }
        public string TargetActivityName { get; }
        public RuntimeContentOwner PreviousOwner { get; }
        public RuntimeContentOwner TargetOwner { get; }
        public PlayerParticipationRequirementLevel RequirementLevel { get; }
        public ActivityPlayerHandoffGroupSnapshot HandoffGroup { get; }
        public IReadOnlyList<ActivityPlayerLifecycleAdmissionSlotSnapshot> Slots => slots;
        public bool TransitionAuthorized { get; }
        public bool PreviousExitAcknowledged { get; }
        public ActivityPlayerPreviousExitDisposition PreviousExitDisposition { get; }
        public bool TargetEnterAdopted { get; }
        public bool CommitCleanupPending { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public int SlotCount => slots.Length;
        public bool IsRouteStartupFlow =>
            FlowKind == ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch;
        public bool IsNotRequired => State == ActivityPlayerLifecycleAdmissionState.NotRequired;
        public bool IsReadyToCommit => State == ActivityPlayerLifecycleAdmissionState.ReadyToCommit;
        public bool IsTransitionAuthorized =>
            State == ActivityPlayerLifecycleAdmissionState.TransitionAuthorized;
        public bool IsCommitted =>
            State is ActivityPlayerLifecycleAdmissionState.CommittedAwaitingLifecycle or
                ActivityPlayerLifecycleAdmissionState.CommitCleanupPending or
                ActivityPlayerLifecycleAdmissionState.Completed;
        public bool IsCompleted => State == ActivityPlayerLifecycleAdmissionState.Completed;
        public bool IsRollbackAvailable =>
            State is ActivityPlayerLifecycleAdmissionState.Preparing or
                ActivityPlayerLifecycleAdmissionState.ReadyToCommit or
                ActivityPlayerLifecycleAdmissionState.TransitionAuthorized;
        public bool Failed => State == ActivityPlayerLifecycleAdmissionState.Failed;

        public string ToDiagnosticString() =>
            $"transaction='{Token.StableText}' state='{State}' status='{LastStatus}' flow='{FlowKind}' " +
            $"previousRoute='{PreviousRouteName}' targetRoute='{TargetRouteName}' " +
            $"previousActivity='{PreviousActivityName}' targetActivity='{TargetActivityName}' " +
            $"previousOwner='{PreviousOwner.StableText}' targetOwner='{TargetOwner.StableText}' " +
            $"requirement='{RequirementLevel}' slots='{SlotCount}' " +
            $"transitionAuthorized='{TransitionAuthorized}' previousExit='{PreviousExitAcknowledged}' " +
            $"previousExitDisposition='{PreviousExitDisposition}' " +
            $"targetAdopted='{TargetEnterAdopted}' cleanupPending='{CommitCleanupPending}' " +
            $"source='{Source}' reason='{Reason}' message='{Message}'";

        internal static ActivityPlayerLifecycleAdmissionSnapshot NotRequired(
            string source,
            string reason,
            string message) =>
            new ActivityPlayerLifecycleAdmissionSnapshot(
                default,
                ActivityPlayerLifecycleAdmissionState.NotRequired,
                ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                ActivityPlayerLifecycleAdmissionFlowKind.None,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                default,
                default,
                PlayerParticipationRequirementLevel.None,
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

        internal static ActivityPlayerLifecycleAdmissionSnapshot Empty(
            ActivityPlayerLifecycleAdmissionStatus status,
            string source,
            string reason,
            string message) =>
            new ActivityPlayerLifecycleAdmissionSnapshot(
                default,
                ActivityPlayerLifecycleAdmissionState.None,
                status,
                ActivityPlayerLifecycleAdmissionFlowKind.None,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                default,
                default,
                PlayerParticipationRequirementLevel.None,
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
    }
}
