using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Common;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Immutable result for a completed runtime activity request.
    /// This is diagnostics data and does not expose a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct FrameworkActivityRequestResult
    {
        public FrameworkActivityRequestResult(
            FrameworkActivityRequestKind kind,
            string message,
            ActivityAsset targetActivity,
            string source,
            string reason,
            ActivityFlowStartResult activityFlowResult,
            FrameworkTransitionDiagnostics transitionDiagnostics = default,
            ActivityVisualTransitionMode activityTransitionMode = ActivityVisualTransitionMode.Seamless,
            GameFlowRequestOperationKind operationKind = GameFlowRequestOperationKind.Activity)
        {
            Kind = kind;
            Message = message.NormalizeText();
            TargetActivity = targetActivity;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
            ActivityFlowResult = activityFlowResult;
            TransitionDiagnostics = transitionDiagnostics;
            ActivityTransitionMode = NormalizeActivityTransitionMode(activityTransitionMode);
            ActivityLoadingMode = DetermineActivityLoadingMode(ActivityFlowResult, ActivityTransitionMode);
            OperationKind = NormalizeOperationKind(operationKind);
        }

        public FrameworkActivityRequestKind Kind { get; }

        public string Message { get; }

        public ActivityAsset TargetActivity { get; }

        public string Source { get; }

        public string Reason { get; }

        internal ActivityFlowStartResult ActivityFlowResult { get; }

        internal FrameworkTransitionDiagnostics TransitionDiagnostics { get; }

        internal ActivityVisualTransitionMode ActivityTransitionMode { get; }

        internal string ActivityLoadingMode { get; }

        internal GameFlowRequestOperationKind OperationKind { get; }

        public bool Succeeded => Kind == FrameworkActivityRequestKind.Succeeded;

        public static FrameworkActivityRequestResult FailedInvalidConfig(
            string message,
            ActivityAsset targetActivity = null,
            string source = null,
            string reason = null,
            ActivityVisualTransitionMode activityTransitionMode = ActivityVisualTransitionMode.Seamless,
            GameFlowRequestOperationKind operationKind = GameFlowRequestOperationKind.Activity)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.FailedInvalidConfig,
                message,
                targetActivity,
                source,
                reason,
                default,
                default,
                activityTransitionMode,
                operationKind);
        }

        public static FrameworkActivityRequestResult FailedRuntimeUnavailable(
            string message,
            ActivityAsset targetActivity = null,
            string source = null,
            string reason = null)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.FailedRuntimeUnavailable,
                message,
                targetActivity,
                source,
                reason,
                default);
        }

        public static FrameworkActivityRequestResult IgnoredAlreadyActive(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredAlreadyActive,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} Activity '{targetActivity.ActivityName}' is already active.",
                targetActivity,
                source,
                reason,
                default);
        }

        public static FrameworkActivityRequestResult IgnoredAlreadyInFlight(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            string activityName = targetActivity.ToDiagnosticText(x => x.ActivityName);
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredAlreadyInFlight,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} Another activity or route request is already in flight. targetActivity='{activityName}'.",
                targetActivity,
                source,
                reason,
                default);
        }

        internal static FrameworkActivityRequestResult IgnoredBlockedByGate(
            ActivityAsset targetActivity,
            string source,
            string reason,
            GateEvaluationResult gateEvaluation,
            GameFlowRequestOperationKind operationKind = GameFlowRequestOperationKind.Activity)
        {
            string activityName = targetActivity.ToDiagnosticText(x => x.ActivityName);
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredAlreadyInFlight,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} targetActivity='{activityName}'. {GateRequestAdmission.FormatBlockedMessage("Activity Request", gateEvaluation)}",
                targetActivity,
                source,
                reason,
                default,
                default,
                ActivityVisualTransitionMode.Seamless,
                operationKind);
        }

        public static FrameworkActivityRequestResult IgnoredNoActiveActivity(
            string source,
            string reason)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredNoActiveActivity,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} No Activity is active.",
                null,
                source.NormalizeTextOrFallback("Unknown"),
                reason.NormalizeTextOrFallback("None"),
                default,
                default,
                ActivityVisualTransitionMode.Seamless,
                GameFlowRequestOperationKind.ActivityClear);
        }

        internal static FrameworkActivityRequestResult SucceededWith(
            ActivityAsset targetActivity,
            string source,
            string reason,
            ActivityFlowStartResult activityFlowResult,
            FrameworkTransitionDiagnostics transitionDiagnostics = default,
            ActivityVisualTransitionMode activityTransitionMode = ActivityVisualTransitionMode.Seamless)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.Succeeded,
                $"Activity Request completed. {FormatRequestContext(source, reason)} {activityFlowResult.Message}",
                targetActivity,
                source,
                reason,
                activityFlowResult,
                transitionDiagnostics,
                activityTransitionMode,
                targetActivity == null ? GameFlowRequestOperationKind.ActivityClear : GameFlowRequestOperationKind.Activity);
        }

        private static GameFlowRequestOperationKind NormalizeOperationKind(GameFlowRequestOperationKind operationKind)
        {
            if (operationKind == GameFlowRequestOperationKind.ActivityClear)
            {
                return GameFlowRequestOperationKind.ActivityClear;
            }

            return GameFlowRequestOperationKind.Activity;
        }

        private static ActivityVisualTransitionMode NormalizeActivityTransitionMode(ActivityVisualTransitionMode mode)
        {
            return System.Enum.IsDefined(typeof(ActivityVisualTransitionMode), mode)
                ? mode
                : ActivityVisualTransitionMode.Seamless;
        }

        private static string DetermineActivityLoadingMode(
            ActivityFlowStartResult activityFlowResult,
            ActivityVisualTransitionMode mode)
        {
            bool hasSceneLoad = activityFlowResult.ActivitySceneCompositionResult.HasSceneLoadExecution;
            bool hasSceneRelease = activityFlowResult.ActivitySceneReleaseResult.HasSceneReleaseExecution;

            if (hasSceneLoad && hasSceneRelease)
            {
                return "ActivitySceneCompositionAndRelease";
            }

            if (hasSceneLoad)
            {
                return "ActivitySceneComposition";
            }

            if (hasSceneRelease)
            {
                return "ActivitySceneRelease";
            }

            return "SkippedNoSceneLoad";
        }

        private static string FormatRequestContext(string source, string reason)
        {
            return $"source='{source.NormalizeTextOrFallback("Unknown")}' reason='{reason.NormalizeTextOrFallback("None")}'.";
        }
    }
}
