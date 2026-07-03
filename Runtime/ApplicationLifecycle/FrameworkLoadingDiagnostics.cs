using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Loading;
using Immersive.Framework.Common;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// API status: Internal. Route/Activity request-level loading diagnostics for logging and smoke validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24D request-level Loading diagnostics.")]
    internal readonly struct FrameworkLoadingDiagnostics
    {
        private const string AlreadyLoadedPhase = "AlreadyLoaded";
        private const string NoSceneLoadPhase = "NoSceneLoad";
        private const string ActivityPolicyPhase = "ActivityPolicy";
        private const string NoOpPhase = "NoOp";
        private const string RequiredSurfacePhase = "RequiredSurface";
        private const string UnitySurfaceText = "UnitySurface";
        private const string NoneText = "None";
        private const string SkippedText = "Skipped";

        public FrameworkLoadingDiagnostics(
            string loadingText,
            string visualText,
            string beforeText,
            string afterText,
            FrameworkLoadingProgress progress,
            int blockingIssueCount,
            int adapterCount,
            int adapterEvidenceCount = 0,
            int appliedAdapterEvidenceCount = 0,
            int skippedAdapterEvidenceCount = 0,
            int failedAdapterEvidenceCount = 0,
            int adapterEvidenceIssueCount = 0,
            int adapterEvidenceBlockingIssueCount = 0,
            string adapterEvidenceNamesText = NoneText,
            string adapterEvidenceStatusesText = NoneText)
        {
            LoadingText = Normalize(loadingText);
            VisualText = Normalize(visualText);
            BeforeText = Normalize(beforeText);
            AfterText = Normalize(afterText);
            Progress = progress;
            BlockingIssueCount = blockingIssueCount < 0 ? 0 : blockingIssueCount;
            AdapterCount = adapterCount < 0 ? 0 : adapterCount;
            AdapterEvidenceCount = adapterEvidenceCount < 0 ? 0 : adapterEvidenceCount;
            AppliedAdapterEvidenceCount = appliedAdapterEvidenceCount < 0 ? 0 : appliedAdapterEvidenceCount;
            SkippedAdapterEvidenceCount = skippedAdapterEvidenceCount < 0 ? 0 : skippedAdapterEvidenceCount;
            FailedAdapterEvidenceCount = failedAdapterEvidenceCount < 0 ? 0 : failedAdapterEvidenceCount;
            AdapterEvidenceIssueCount = adapterEvidenceIssueCount < 0 ? 0 : adapterEvidenceIssueCount;
            AdapterEvidenceBlockingIssueCount = adapterEvidenceBlockingIssueCount < 0 ? 0 : adapterEvidenceBlockingIssueCount;
            AdapterEvidenceNamesText = Normalize(adapterEvidenceNamesText).NormalizeTextOrFallback(NoneText);
            AdapterEvidenceStatusesText = Normalize(adapterEvidenceStatusesText).NormalizeTextOrFallback(NoneText);
        }

        public string LoadingText { get; }

        public string VisualText { get; }

        public string BeforeText { get; }

        public string AfterText { get; }

        public FrameworkLoadingProgress Progress { get; }

        public int BlockingIssueCount { get; }

        public int AdapterCount { get; }

        public int AdapterEvidenceCount { get; }

        public int AppliedAdapterEvidenceCount { get; }

        public int SkippedAdapterEvidenceCount { get; }

        public int FailedAdapterEvidenceCount { get; }

        public int AdapterEvidenceIssueCount { get; }

        public int AdapterEvidenceBlockingIssueCount { get; }

        public string AdapterEvidenceNamesText { get; }

        public string AdapterEvidenceStatusesText { get; }

        public bool HasAdapterEvidence => AdapterEvidenceCount > 0;

        public bool ProgressSupported => Progress.Supported;

        public string ProgressModeText => Progress.ModeText;

        public string ProgressValueText => Progress.ValueText;

        public string ProgressPercentText => Progress.PercentText;

        public string ProgressPhaseText => Progress.PhaseText;

        public string ProgressMessageText => Progress.MessageText;

        public string ProgressText => ProgressModeText;

        public bool HasDiagnostics => !string.IsNullOrWhiteSpace(LoadingText);

        public static FrameworkLoadingDiagnostics SkippedAlreadyLoaded()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedAlreadyLoaded",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    AlreadyLoadedPhase,
                    "Loading was skipped because the target is already active."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics SkippedNoSceneLoad()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedNoSceneLoad",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    NoSceneLoadPhase,
                    "No loading progress source was available because no scene load or release side-effect occurred."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics SkippedByActivityPolicy()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedByActivityPolicy",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    ActivityPolicyPhase,
                    "Activity loading presentation was skipped by the authored visual policy."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics SucceededWithNoOp()
        {
            return new FrameworkLoadingDiagnostics(
                "SucceededWithNoOp",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    NoOpPhase,
                    "Loading completed without an executable loading surface."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics FailedRequiredUnitySurfaceMissing()
        {
            return new FrameworkLoadingDiagnostics(
                "FailedRequiredUnitySurfaceMissing",
                NoneText,
                "Failed",
                "Failed",
                FrameworkLoadingProgress.Unsupported(
                    RequiredSurfacePhase,
                    "Required loading surface configuration is missing."),
                1,
                0);
        }

        public static FrameworkLoadingDiagnostics FromUnitySurface(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult,
            int adapterCount,
            bool progressSupported)
        {
            return FromUnitySurface(
                beforeResult,
                afterResult,
                adapterCount,
                progressSupported,
                FrameworkLoadingProgress.Indeterminate(
                    progressSupported,
                    UnitySurfaceText,
                    "Loading progress support is available but no determinate source is wired for this operation."));
        }

        public static FrameworkLoadingDiagnostics FromUnitySurface(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult,
            int adapterCount,
            bool progressSupported,
            FrameworkLoadingProgress observedProgress)
        {
            int blockingIssueCount = beforeResult.BlockingIssueCount + afterResult.BlockingIssueCount;
            string beforeText = StatusText(beforeResult);
            string afterText = StatusText(afterResult);
            string loadingText = DetermineSurfaceLoadingText(beforeResult, afterResult);
            var progress = ResolveObservedProgress(progressSupported, observedProgress);
            return new FrameworkLoadingDiagnostics(
                loadingText,
                UnitySurfaceText,
                beforeText,
                afterText,
                progress,
                blockingIssueCount,
                adapterCount,
                beforeResult.AdapterEvidenceCount + afterResult.AdapterEvidenceCount,
                beforeResult.AppliedAdapterEvidenceCount + afterResult.AppliedAdapterEvidenceCount,
                beforeResult.SkippedAdapterEvidenceCount + afterResult.SkippedAdapterEvidenceCount,
                beforeResult.FailedAdapterEvidenceCount + afterResult.FailedAdapterEvidenceCount,
                beforeResult.AdapterEvidenceIssueCount + afterResult.AdapterEvidenceIssueCount,
                beforeResult.AdapterEvidenceBlockingIssueCount + afterResult.AdapterEvidenceBlockingIssueCount,
                BuildAdapterEvidenceNamesText(beforeResult, afterResult),
                BuildAdapterEvidenceStatusesText(beforeResult, afterResult));
        }

        private static string DetermineSurfaceLoadingText(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult)
        {
            if (beforeResult.Failed || beforeResult.Rejected || afterResult.Failed || afterResult.Rejected)
            {
                return "FailedUnitySurface";
            }

            if (beforeResult.SucceededWithWarnings || afterResult.SucceededWithWarnings)
            {
                return "CompletedWithUnitySurfaceWarnings";
            }

            if (beforeResult.Skipped && afterResult.Skipped)
            {
                return "SucceededWithUnitySurface";
            }

            return "SucceededWithUnitySurface";
        }

        private static string BuildAdapterEvidenceNamesText(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult)
        {
            var builder = new StringBuilder();
            AppendAdapterEvidenceNames(builder, beforeResult);
            AppendAdapterEvidenceNames(builder, afterResult);
            return builder.Length > 0 ? builder.ToString() : NoneText;
        }

        private static string BuildAdapterEvidenceStatusesText(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult)
        {
            var builder = new StringBuilder();
            AppendAdapterEvidenceStatuses(builder, beforeResult);
            AppendAdapterEvidenceStatuses(builder, afterResult);
            return builder.Length > 0 ? builder.ToString() : NoneText;
        }

        private static void AppendAdapterEvidenceNames(StringBuilder builder, LoadingSurfaceResult result)
        {
            for (int i = 0; i < result.AdapterEvidence.Count; i++)
            {
                AppendSeparator(builder);
                builder.Append(result.AdapterEvidence[i].AdapterName.ToDiagnosticText());
            }
        }

        private static void AppendAdapterEvidenceStatuses(StringBuilder builder, LoadingSurfaceResult result)
        {
            for (int i = 0; i < result.AdapterEvidence.Count; i++)
            {
                LoadingSurfaceAdapterEvidence evidence = result.AdapterEvidence[i];
                AppendSeparator(builder);
                builder.Append(evidence.AdapterName.ToDiagnosticText());
                builder.Append(':');
                builder.Append(evidence.Status);
            }
        }

        private static void AppendSeparator(StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                builder.Append("; ");
            }
        }

        private static FrameworkLoadingProgress ResolveObservedProgress(
            bool progressSupported,
            FrameworkLoadingProgress observedProgress)
        {
            if (!progressSupported)
            {
                return FrameworkLoadingProgress.Unsupported(
                    UnitySurfaceText,
                    "Loading surface adapters do not expose progress for this operation.");
            }

            if (observedProgress is { Supported: true, IsDeterminate: true })
            {
                return observedProgress;
            }

            return FrameworkLoadingProgress.Indeterminate(
                true,
                UnitySurfaceText,
                "Loading progress support is available but no determinate source reported progress for this operation.");
        }

        private static string StatusText(LoadingSurfaceResult result)
        {
            if (result.Succeeded)
            {
                return "Succeeded";
            }

            if (result.SucceededWithWarnings)
            {
                return "SucceededWithWarnings";
            }

            if (result.Skipped)
            {
                return "Skipped";
            }

            if (result.Rejected)
            {
                return "Rejected";
            }

            if (result.Failed)
            {
                return "Failed";
            }

            return "Unknown";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
