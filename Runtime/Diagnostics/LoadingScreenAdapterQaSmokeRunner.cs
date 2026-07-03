using Immersive.Framework.Common;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Loading;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F22E Loading Screen adapter boundary.
    /// It validates adapter-facing contracts only; it does not create UI, prefabs, scene objects, fade, curtain or TransitionEffects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F22E Loading Screen adapter boundary diagnostics smoke.")]
    internal static class LoadingScreenAdapterQaSmokeRunner
    {
        internal const string SmokeName = "Loading Screen Adapter Boundary Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(LoadingScreenAdapterQaSmokeRunner));

            try
            {
                bool contractsPassed = ValidateContracts(logger, normalizedSource);
                bool showUpdateHidePassed = ValidateShowUpdateHide(logger, normalizedSource);
                bool unsupportedPassed = ValidateUnsupportedOperation(logger, normalizedSource);
                bool failurePassed = ValidateAdapterFailure(logger, normalizedSource);
                bool boundaryPassed = ValidateBoundary(logger);

                return Task.FromResult(contractsPassed
                    && showUpdateHidePassed
                    && unsupportedPassed
                    && failurePassed
                    && boundaryPassed);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Loading Screen Adapter Boundary Smoke failed with exception.",
                    LogFields.Of(
                        LogFields.Field("source", normalizedSource),
                        LogFields.Field("exception", exception.GetType().Name),
                        LogFields.Field("message", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static bool ValidateContracts(FrameworkLogger logger, string source)
        {
            var operation = LoadingOperation.Running(
                "qa.loading.screen.operation.contracts",
                0.25f,
                "QA Loading Screen Contracts",
                source,
                "contracts");
            var presentation = LoadingScreenPresentation.FromOperation(
                operation,
                true,
                "Loading QA",
                "Contracts",
                source);
            var adapter = new SyntheticLoadingScreenAdapter("Synthetic Loading Screen Adapter", true, false);
            var result = adapter.Show(presentation);

            bool passed = adapter.AdapterName == "Synthetic Loading Screen Adapter"
                && adapter.Supports(operation)
                && presentation is { IsValid: true, ShouldBeVisible: true, Progress: { PercentRounded: 25 } }
                && result is { IsValid: true, Action: LoadingScreenAdapterAction.Show, Status: LoadingScreenAdapterStatus.Succeeded, Completed: true };

            LogStep(
                logger,
                "contracts",
                passed,
                LogFields.Of(
                    LogFields.Field("source", source),
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("adapter", adapter.AdapterName),
                    LogFields.Field("interface", nameof(ILoadingScreenAdapter)),
                    LogFields.Field("presentation", nameof(LoadingScreenPresentation)),
                    LogFields.Field("result", nameof(LoadingScreenAdapterResult)),
                    LogFields.Field("operation", operation.OperationId.StableText),
                    LogFields.Field("progress", presentation.Progress.NormalizedValue),
                    LogFields.Field("percent", presentation.Progress.PercentRounded),
                    LogFields.Field("action", result.Action.ToString()),
                    LogFields.Field("status", result.Status.ToString())));

            return passed;
        }

        private static bool ValidateShowUpdateHide(FrameworkLogger logger, string source)
        {
            var adapter = new SyntheticLoadingScreenAdapter("Synthetic Loading Screen Adapter", true, false);
            var showPresentation = LoadingScreenPresentation.FromOperation(
                LoadingOperation.Running("qa.loading.screen.operation.show-update-hide", 0.1f, "Show Update Hide", source, "show"),
                true,
                "Loading",
                "Starting",
                source);
            var updatePresentation = LoadingScreenPresentation.FromOperation(
                LoadingOperation.Running("qa.loading.screen.operation.show-update-hide", 0.75f, "Show Update Hide", source, "update"),
                true,
                "Loading",
                "Almost there",
                source);
            var hidePresentation = LoadingScreenPresentation.FromOperation(
                LoadingOperation.Completed("qa.loading.screen.operation.show-update-hide", "Show Update Hide", source, "hide"),
                false,
                "Loading",
                "Done",
                source);

            var showResult = adapter.Show(showPresentation);
            var updateResult = adapter.Update(updatePresentation);
            var hideResult = adapter.Hide(hidePresentation);

            bool passed = showResult.Succeeded
                && updateResult.Succeeded
                && hideResult.Succeeded
                && showResult.Action == LoadingScreenAdapterAction.Show
                && updateResult.Action == LoadingScreenAdapterAction.Update
                && hideResult.Action == LoadingScreenAdapterAction.Hide
                && adapter.ShowCount == 1
                && adapter.UpdateCount == 1
                && adapter.HideCount == 1
                && !adapter.Visible
                && adapter.LastProgress.PercentRounded == 100;

            LogStep(
                logger,
                "show-update-hide",
                passed,
                LogFields.Of(
                    LogFields.Field("adapter", adapter.AdapterName),
                    LogFields.Field("showStatus", showResult.Status.ToString()),
                    LogFields.Field("updateStatus", updateResult.Status.ToString()),
                    LogFields.Field("hideStatus", hideResult.Status.ToString()),
                    LogFields.Field("showCount", adapter.ShowCount),
                    LogFields.Field("updateCount", adapter.UpdateCount),
                    LogFields.Field("hideCount", adapter.HideCount),
                    LogFields.Field("visible", adapter.Visible),
                    LogFields.Field("lastProgress", adapter.LastProgress.NormalizedValue),
                    LogFields.Field("ui", "none"),
                    LogFields.Field("prefab", "none")));

            return passed;
        }

        private static bool ValidateUnsupportedOperation(FrameworkLogger logger, string source)
        {
            var adapter = new SyntheticLoadingScreenAdapter("Rejecting Loading Screen Adapter", false, false);
            var operation = LoadingOperation.Running(
                "qa.loading.screen.operation.unsupported",
                0.5f,
                "Unsupported",
                source,
                "unsupported");
            var presentation = LoadingScreenPresentation.FromOperation(
                operation,
                true,
                "Unsupported",
                "Unsupported",
                source);
            var result = adapter.Show(presentation);

            bool passed = !adapter.Supports(operation)
                && result is { Rejected: true, IssueCount: 1 }
                && !adapter.Visible
                && adapter.ShowCount == 0;

            LogStep(
                logger,
                "unsupported-operation",
                passed,
                LogFields.Of(
                    LogFields.Field("adapter", adapter.AdapterName),
                    LogFields.Field("supports", adapter.Supports(operation)),
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("visible", adapter.Visible),
                    LogFields.Field("fallback", "none")));

            return passed;
        }

        private static bool ValidateAdapterFailure(FrameworkLogger logger, string source)
        {
            var adapter = new SyntheticLoadingScreenAdapter("Failing Loading Screen Adapter", true, true);
            var presentation = LoadingScreenPresentation.FromOperation(
                LoadingOperation.Running("qa.loading.screen.operation.failed-update", 0.6f, "Failed Update", source, "failure"),
                true,
                "Loading",
                "Failure",
                source);
            var result = adapter.Update(presentation);

            bool passed = result is { Failed: true, IssueCount: 1 }
                && adapter.UpdateCount == 0
                && !adapter.Visible;

            LogStep(
                logger,
                "adapter-failure",
                passed,
                LogFields.Of(
                    LogFields.Field("adapter", adapter.AdapterName),
                    LogFields.Field("status", result.Status.ToString()),
                    LogFields.Field("issues", result.IssueCount),
                    LogFields.Field("updateCount", adapter.UpdateCount),
                    LogFields.Field("visible", adapter.Visible),
                    LogFields.Field("blocksLoadingOperation", "none")));

            return passed;
        }

        private static bool ValidateBoundary(FrameworkLogger logger)
        {
            bool passed = typeof(ILoadingScreenAdapter).Namespace == "Immersive.Framework.Loading"
                && typeof(LoadingScreenPresentation).Namespace == "Immersive.Framework.Loading"
                && typeof(LoadingScreenAdapterResult).Namespace == "Immersive.Framework.Loading";

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("namespace", "Immersive.Framework.Loading"),
                    LogFields.Field("adapter", nameof(ILoadingScreenAdapter)),
                    LogFields.Field("ui", "none"),
                    LogFields.Field("sceneObject", "none"),
                    LogFields.Field("prefab", "none"),
                    LogFields.Field("scriptableObject", "none"),
                    LogFields.Field("transitionEffects", "none"),
                    LogFields.Field("fade", "none"),
                    LogFields.Field("curtain", "none"),
                    LogFields.Field("sceneLifecycleExecution", "none"),
                    LogFields.Field("transitionExecution", "none"),
                    LogFields.Field("readinessMutation", "none")));

            return passed;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            LogField[] stepFields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed));

            if (passed)
            {
                logger.Info("QA Loading Screen Adapter Boundary Smoke step completed.", stepFields);
                logger.Debug("QA Loading Screen Adapter Boundary Smoke step diagnostics.", fields);
                return;
            }

            logger.Warning("QA Loading Screen Adapter Boundary Smoke step failed.", stepFields);
            logger.Debug("QA Loading Screen Adapter Boundary Smoke failure diagnostics.", fields);
        }

        private sealed class SyntheticLoadingScreenAdapter : ILoadingScreenAdapter
        {
            private const string UnsupportedIssue = "loading-screen-operation-unsupported";
            private const string SyntheticFailureIssue = "loading-screen-synthetic-failure";

            private readonly string _adapterName;
            private readonly bool _supports;
            private readonly bool _failUpdate;

            public SyntheticLoadingScreenAdapter(string adapterName, bool supports, bool failUpdate)
            {
                _adapterName = adapterName.NormalizeTextOrFallback(nameof(SyntheticLoadingScreenAdapter));
                _supports = supports;
                _failUpdate = failUpdate;
                LastProgress = LoadingProgress.Zero;
            }

            public string AdapterName => _adapterName;

            public bool Visible { get; private set; }

            public int ShowCount { get; private set; }

            public int UpdateCount { get; private set; }

            public int HideCount { get; private set; }

            public LoadingProgress LastProgress { get; private set; }

            public bool Supports(LoadingOperation operation)
            {
                return _supports && operation.IsValid;
            }

            public LoadingScreenAdapterResult Show(LoadingScreenPresentation presentation)
            {
                if (!Supports(presentation.Operation))
                {
                    return LoadingScreenAdapterResult.RejectedResult(
                        presentation,
                        LoadingScreenAdapterAction.Show,
                        AdapterName,
                        "Synthetic adapter rejected unsupported loading operation.",
                        new[] { UnsupportedIssue });
                }

                Visible = true;
                LastProgress = presentation.Progress;
                ShowCount++;
                return LoadingScreenAdapterResult.SucceededResult(
                    presentation,
                    LoadingScreenAdapterAction.Show,
                    AdapterName,
                    "Synthetic loading screen shown.");
            }

            public LoadingScreenAdapterResult Update(LoadingScreenPresentation presentation)
            {
                if (!Supports(presentation.Operation))
                {
                    return LoadingScreenAdapterResult.RejectedResult(
                        presentation,
                        LoadingScreenAdapterAction.Update,
                        AdapterName,
                        "Synthetic adapter rejected unsupported loading operation.",
                        new[] { UnsupportedIssue });
                }

                if (_failUpdate)
                {
                    return LoadingScreenAdapterResult.FailedResult(
                        presentation,
                        LoadingScreenAdapterAction.Update,
                        AdapterName,
                        "Synthetic update failure.",
                        new[] { SyntheticFailureIssue });
                }

                Visible = presentation.ShouldBeVisible;
                LastProgress = presentation.Progress;
                UpdateCount++;
                return LoadingScreenAdapterResult.SucceededResult(
                    presentation,
                    LoadingScreenAdapterAction.Update,
                    AdapterName,
                    "Synthetic loading screen updated.");
            }

            public LoadingScreenAdapterResult Hide(LoadingScreenPresentation presentation)
            {
                if (!Supports(presentation.Operation))
                {
                    return LoadingScreenAdapterResult.RejectedResult(
                        presentation,
                        LoadingScreenAdapterAction.Hide,
                        AdapterName,
                        "Synthetic adapter rejected unsupported loading operation.",
                        new[] { UnsupportedIssue });
                }

                Visible = false;
                LastProgress = presentation.Progress;
                HideCount++;
                return LoadingScreenAdapterResult.SucceededResult(
                    presentation,
                    LoadingScreenAdapterAction.Hide,
                    AdapterName,
                    "Synthetic loading screen hidden.");
            }
        }
    }
}
#endif
