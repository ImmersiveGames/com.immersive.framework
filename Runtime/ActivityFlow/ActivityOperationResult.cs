using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free result wrapper for an Activity operation plan.
    /// F25E does not execute the operation; it only records whether the operation can be planned or is blocked.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation result baseline; execution deferred to a later cut.")]
    internal readonly struct ActivityOperationResult
    {
        public ActivityOperationResult(
            ActivityOperationPlan plan,
            ActivityOperationResultStatus status,
            string message)
        {
            Plan = plan;
            Status = status;
            Message = Normalize(message);
        }

        public ActivityOperationPlan Plan { get; }

        public ActivityOperationResultStatus Status { get; }

        public string Message { get; }

        public ActivityOperationKind OperationKind => Plan.OperationKind;

        public ActivityVisualTransitionMode VisualMode => Plan.VisualMode;

        public bool IsValid => Plan.IsValid && Status != ActivityOperationResultStatus.Blocked;

        public bool IsBlocked => Status == ActivityOperationResultStatus.Blocked || Plan.HasBlockingIssues;

        public bool HasSceneSideEffects => Plan.HasSceneSideEffects;

        public bool RequiresVisualOcclusion => Plan.RequiresVisualOcclusion;

        public bool RequiresLoadingSurface => Plan.RequiresLoadingSurface;

        public bool SideEffectsExecuted => false;

        public int SceneCount => Plan.SceneCount;

        public int ScenesToLoadCount => Plan.ScenesToLoadCount;

        public int ScenesToReleaseCount => Plan.ScenesToReleaseCount;

        public int SceneSideEffectCount => Plan.SceneSideEffectCount;

        public int BlockingIssueCount => Plan.BlockingIssueCount;

        public int WarningIssueCount => Plan.WarningIssueCount;

        public string DiagnosticStatus => Status.ToString();

        public static ActivityOperationResult FromPlan(ActivityOperationPlan plan)
        {
            var status = DetermineStatus(plan);
            return new ActivityOperationResult(plan, status, BuildMessage(plan, status));
        }

        public static ActivityOperationResult NotRequested(string source, string reason)
        {
            var plan = ActivityOperationPlan.NotRequested(source, reason);
            return new ActivityOperationResult(
                plan,
                ActivityOperationResultStatus.NotRequested,
                "Activity operation was not requested.");
        }

        public string ToDiagnosticString()
        {
            return $"Activity Operation Result status='{Status}' kind='{OperationKind}' visualMode='{VisualMode}' valid='{IsValid}' blocked='{IsBlocked}' scenes='{SceneCount}' load='{ScenesToLoadCount}' release='{ScenesToReleaseCount}' sceneSideEffects='{SceneSideEffectCount}' requiresVisualOcclusion='{RequiresVisualOcclusion}' requiresLoadingSurface='{RequiresLoadingSurface}' sideEffectsExecuted='{SideEffectsExecuted}' blockingIssues='{BlockingIssueCount}' warnings='{WarningIssueCount}' message='{Message}' plan=({Plan.ToDiagnosticString()})";
        }

        private static ActivityOperationResultStatus DetermineStatus(ActivityOperationPlan plan)
        {
            if (plan.Status == ActivityOperationPlanStatus.NotRequested)
            {
                return ActivityOperationResultStatus.NotRequested;
            }

            return plan.HasBlockingIssues
                ? ActivityOperationResultStatus.Blocked
                : ActivityOperationResultStatus.Planned;
        }

        private static string BuildMessage(ActivityOperationPlan plan, ActivityOperationResultStatus status)
        {
            return $"Activity operation planning completed. status='{status}' kind='{plan.OperationKind}' visualMode='{plan.VisualMode}' scenes='{plan.SceneCount}' load='{plan.ScenesToLoadCount}' release='{plan.ScenesToReleaseCount}' sceneSideEffects='{plan.SceneSideEffectCount}' blockingIssues='{plan.BlockingIssueCount}' warnings='{plan.WarningIssueCount}'.";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
