using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Loading;
using UnityEngine;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private FrameworkLoadingDiagnostics
            _lastStartupActivityEntryLoadingDiagnostics;

        internal FrameworkLoadingDiagnostics
            LastStartupActivityEntryLoadingDiagnostics =>
                _lastStartupActivityEntryLoadingDiagnostics;

        private async Task<FrameworkGameFlowStartResult>
            StartGameFlowWithActivityEntryLoadingProgressAsync()
        {
            RouteAsset startupRoute =
                _gameApplication != null
                    ? _gameApplication.StartupRoute
                    : null;
            bool showLoadingSurface =
                ShouldShowLoadingSurface(startupRoute);
            bool loadingProgressSupported =
                showLoadingSurface &&
                _loadingSurfaceRuntime.ProgressSupported;
            LoadingSurfaceRequest loadingShowRequest =
                CreateLoadingSurfaceRequest(
                    startupRoute,
                    "GameApplication",
                    "startup",
                    true,
                    LoadingProgress.Zero,
                    loadingProgressSupported);
            IFrameworkLoadingProgressReporter progressReporter =
                CreateLoadingProgressReporter(
                    loadingShowRequest,
                    showLoadingSurface);
            LoadingSurfaceResult loadingBeforeResult = default;
            LoadingSurfaceResult loadingAfterResult = default;
            ActivityEntryLoadingProgressDiagnostics
                activityEntryProgressDiagnostics = default;

            async Awaitable ShowLoadingAfterTransitionGate()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                loadingBeforeResult =
                    await _loadingSurfaceRuntime.ShowAsync(
                        loadingShowRequest);
            }

            async Awaitable HideLoadingBeforeTransitionRelease()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                LoadingSurfaceRequest loadingHideRequest =
                    CreateLoadingSurfaceRequest(
                        startupRoute,
                        "GameApplication",
                        "startup",
                        false,
                        ToSurfaceProgress(
                            progressReporter.LastProgress),
                        progressReporter.HasReportedProgress &&
                        progressReporter.LastProgress is
                        {
                            Supported: true,
                            IsDeterminate: true
                        });
                loadingAfterResult =
                    await _loadingSurfaceRuntime.HideAsync(
                        loadingHideRequest);
            }

            FrameworkGameFlowStartResult result =
                await _gameFlowRuntime
                    .StartWithActivityEntryLoadingProgressAsync(
                        _gameApplication,
                        ShowLoadingAfterTransitionGate,
                        HideLoadingBeforeTransitionRelease,
                        progressReporter,
                        diagnostics =>
                            activityEntryProgressDiagnostics =
                                diagnostics);

            if (showLoadingSurface &&
                activityEntryProgressDiagnostics.IsValid)
            {
                _lastStartupActivityEntryLoadingDiagnostics =
                    loadingAfterResult.Status !=
                    LoadingSurfaceResultStatus.Unknown
                        ? FrameworkLoadingDiagnostics.FromUnitySurface(
                            loadingBeforeResult,
                            loadingAfterResult,
                            _loadingSurfaceRuntime.AdapterCount,
                            _loadingSurfaceRuntime.ProgressSupported,
                            progressReporter.LastProgress)
                        : FrameworkLoadingDiagnostics.FromUnitySurface(
                            loadingBeforeResult,
                            loadingAfterResult,
                            _loadingSurfaceRuntime.AdapterCount,
                            _loadingSurfaceRuntime.ProgressSupported);
            }
            else
            {
                _lastStartupActivityEntryLoadingDiagnostics =
                    FrameworkLoadingDiagnostics.SucceededWithNoOp();
            }
            return result;
        }
    }
}
