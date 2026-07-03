using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Internal. Runtime bridge used by lifecycle owners to publish determinate progress
    /// into the configured loading surface. It does not execute scene operations and does not own UI state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26D determinate loading progress reporter bridge.")]
    internal interface IFrameworkLoadingProgressReporter
    {
        bool IsEnabled { get; }

        bool HasReportedProgress { get; }

        FrameworkLoadingProgress LastProgress { get; }

        Awaitable ReportAsync(FrameworkLoadingProgress progress);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26D no-op loading progress reporter.")]
    internal sealed class NoOpFrameworkLoadingProgressReporter : IFrameworkLoadingProgressReporter
    {
        internal static readonly NoOpFrameworkLoadingProgressReporter Instance = new NoOpFrameworkLoadingProgressReporter();

        private static readonly FrameworkLoadingProgress NoProgress = FrameworkLoadingProgress.Unsupported(
            "NoOp",
            "No loading progress reporter is active for this operation.");

        private NoOpFrameworkLoadingProgressReporter()
        {
        }

        public bool IsEnabled => false;

        public bool HasReportedProgress => false;

        public FrameworkLoadingProgress LastProgress => NoProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            await Awaitable.NextFrameAsync();
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26D loading surface progress reporter.")]
    internal sealed class LoadingSurfaceProgressReporter : IFrameworkLoadingProgressReporter
    {
        private readonly LoadingSurfaceRuntime _loadingSurfaceRuntime;
        private readonly string _title;
        private readonly string _detail;
        private readonly string _source;
        private readonly string _reason;
        private FrameworkLoadingProgress _lastProgress;
        private bool _hasReportedProgress;

        internal LoadingSurfaceProgressReporter(
            LoadingSurfaceRuntime loadingSurfaceRuntime,
            LoadingSurfaceRequest baseRequest)
        {
            _loadingSurfaceRuntime = loadingSurfaceRuntime;
            _title = baseRequest.Title;
            _detail = baseRequest.Detail;
            _source = baseRequest.Source;
            _reason = baseRequest.Reason;
            _lastProgress = FrameworkLoadingProgress.Indeterminate(
                supported: true,
                phase: "UnitySurface",
                message: "Loading surface progress reporter is ready and waiting for a determinate source.");
        }

        public bool IsEnabled => _loadingSurfaceRuntime is { HasVisibleSurface: true, ProgressSupported: true };

        public bool HasReportedProgress => _hasReportedProgress;

        public FrameworkLoadingProgress LastProgress => _lastProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            if (!IsEnabled)
            {
                return;
            }

            _lastProgress = progress;
            _hasReportedProgress = true;

            bool progressSupported = progress is { Supported: true, IsDeterminate: true };
            var surfaceProgress = progressSupported
                ? LoadingProgress.FromNormalized(progress.Value01)
                : LoadingProgress.Zero;

            var request = LoadingSurfaceRequest.Update(
                _title,
                _detail,
                _source,
                _reason,
                surfaceProgress,
                progressSupported);

            await _loadingSurfaceRuntime.UpdateAsync(request);
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26E weighted loading progress reporter utilities.")]
    internal static class FrameworkLoadingProgressReporterUtility
    {
        internal static IFrameworkLoadingProgressReporter CreateWeightedStepReporter(
            IFrameworkLoadingProgressReporter parent,
            int stepIndex,
            int stepCount,
            string aggregatePhase,
            string aggregateMessage)
        {
            return CreateWeightedRangeReporter(
                parent,
                stepIndex,
                1,
                stepCount,
                aggregatePhase,
                aggregateMessage);
        }

        internal static IFrameworkLoadingProgressReporter CreateWeightedRangeReporter(
            IFrameworkLoadingProgressReporter parent,
            int startStepIndex,
            int rangeStepCount,
            int totalStepCount,
            string aggregatePhase,
            string aggregateMessage)
        {
            if (parent == null || !parent.IsEnabled || rangeStepCount <= 0 || totalStepCount <= 0)
            {
                return NoOpFrameworkLoadingProgressReporter.Instance;
            }

            if (rangeStepCount >= totalStepCount && startStepIndex <= 0)
            {
                return string.IsNullOrWhiteSpace(aggregatePhase)
                    ? parent
                    : new FrameworkLoadingProgressPhaseReporter(parent, aggregatePhase, aggregateMessage);
            }

            float start01 = Clamp01((float)Math.Max(0, startStepIndex) / totalStepCount);
            float weight01 = Clamp01((float)rangeStepCount / totalStepCount);
            if (weight01 <= 0f)
            {
                return NoOpFrameworkLoadingProgressReporter.Instance;
            }

            return new FrameworkLoadingProgressWeightedReporter(
                parent,
                start01,
                weight01,
                aggregatePhase,
                aggregateMessage);
        }

        internal static async Awaitable ReportCompletedIfAnyAsync(
            IFrameworkLoadingProgressReporter reporter,
            string phase,
            string message)
        {
            if (reporter == null || !reporter.IsEnabled || !reporter.HasReportedProgress)
            {
                return;
            }

            await reporter.ReportAsync(FrameworkLoadingProgress.Determinate(1f, phase, message));
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26E phase-remapping loading progress reporter.")]
    internal sealed class FrameworkLoadingProgressPhaseReporter : IFrameworkLoadingProgressReporter
    {
        private readonly IFrameworkLoadingProgressReporter _parent;
        private readonly string _phase;
        private readonly string _messagePrefix;
        private FrameworkLoadingProgress _lastProgress;
        private bool _hasReportedProgress;

        internal FrameworkLoadingProgressPhaseReporter(
            IFrameworkLoadingProgressReporter parent,
            string phase,
            string messagePrefix)
        {
            _parent = parent ?? NoOpFrameworkLoadingProgressReporter.Instance;
            _phase = NormalizePhase(phase);
            _messagePrefix = Normalize(messagePrefix);
            _lastProgress = parent?.LastProgress ?? FrameworkLoadingProgress.Unsupported("NoOp", "No loading progress reporter is active for this operation.");
        }

        public bool IsEnabled => _parent.IsEnabled;

        public bool HasReportedProgress => _hasReportedProgress || _parent.HasReportedProgress;

        public FrameworkLoadingProgress LastProgress => _hasReportedProgress ? _lastProgress : _parent.LastProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            if (!IsEnabled)
            {
                return;
            }

            var remapped = progress is { Supported: true, IsDeterminate: true }
                ? FrameworkLoadingProgress.Determinate(
                    progress.Value01,
                    _phase,
                    BuildMessage(progress.Message))
                : FrameworkLoadingProgress.Indeterminate(
                    progress.Supported,
                    _phase,
                    BuildMessage(progress.Message));

            _lastProgress = remapped;
            _hasReportedProgress = true;
            await _parent.ReportAsync(remapped);
        }

        private string BuildMessage(string childMessage)
        {
            childMessage = Normalize(childMessage);
            if (string.IsNullOrWhiteSpace(_messagePrefix))
            {
                return childMessage;
            }

            return string.IsNullOrWhiteSpace(childMessage)
                ? _messagePrefix
                : $"{_messagePrefix} {childMessage}";
        }

        private static string NormalizePhase(string value)
        {
            return value.NormalizeTextOrFallback("Loading");
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26E weighted loading progress reporter.")]
    internal sealed class FrameworkLoadingProgressWeightedReporter : IFrameworkLoadingProgressReporter
    {
        private readonly IFrameworkLoadingProgressReporter _parent;
        private readonly float _start01;
        private readonly float _weight01;
        private readonly string _aggregatePhase;
        private readonly string _aggregateMessagePrefix;
        private FrameworkLoadingProgress _lastProgress;
        private bool _hasReportedProgress;

        internal FrameworkLoadingProgressWeightedReporter(
            IFrameworkLoadingProgressReporter parent,
            float start01,
            float weight01,
            string aggregatePhase,
            string aggregateMessagePrefix)
        {
            _parent = parent ?? NoOpFrameworkLoadingProgressReporter.Instance;
            _start01 = Clamp01(start01);
            _weight01 = Clamp01(weight01);
            _aggregatePhase = aggregatePhase.NormalizeTextOrFallback("Loading");
            _aggregateMessagePrefix = Normalize(aggregateMessagePrefix);
            _lastProgress = parent?.LastProgress ?? FrameworkLoadingProgress.Unsupported("NoOp", "No loading progress reporter is active for this operation.");
        }

        public bool IsEnabled => _parent.IsEnabled && _weight01 > 0f;

        public bool HasReportedProgress => _hasReportedProgress || _parent.HasReportedProgress;

        public FrameworkLoadingProgress LastProgress => _hasReportedProgress ? _lastProgress : _parent.LastProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            if (!IsEnabled)
            {
                return;
            }

            var mapped = MapProgress(progress);
            _lastProgress = mapped;
            _hasReportedProgress = true;
            await _parent.ReportAsync(mapped);
        }

        private FrameworkLoadingProgress MapProgress(FrameworkLoadingProgress progress)
        {
            if (!progress.Supported || !progress.IsDeterminate)
            {
                return FrameworkLoadingProgress.Indeterminate(
                    progress.Supported,
                    _aggregatePhase,
                    BuildMessage(progress.Message));
            }

            float mappedValue = _start01 + Clamp01(progress.Value01) * _weight01;
            return FrameworkLoadingProgress.Determinate(
                Clamp01(mappedValue),
                _aggregatePhase,
                BuildMessage(progress.Message));
        }

        private string BuildMessage(string childMessage)
        {
            childMessage = Normalize(childMessage);
            if (string.IsNullOrWhiteSpace(_aggregateMessagePrefix))
            {
                return childMessage;
            }

            return string.IsNullOrWhiteSpace(childMessage)
                ? _aggregateMessagePrefix
                : $"{_aggregateMessagePrefix} {childMessage}";
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
