using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. Executes logical Pause snapshot presentation against adapters collected from UIGlobal.
    /// It is a presentation bridge only; PauseRuntime remains the owner of logical state and Gate snapshot production.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F27A Pause UIGlobal surface runtime.")]
    internal sealed class PauseSurfaceRuntime
    {
        private readonly IPauseSurfaceAdapter[] _adapters;
        private readonly string _surfaceLabel;

        private PauseSurfaceRuntime(string surfaceLabel, IReadOnlyList<IPauseSurfaceAdapter> adapters)
        {
            _surfaceLabel = surfaceLabel.NormalizeTextOrFallback("Pause Surface");
            _adapters = CopyAdapters(adapters);
        }

        public int AdapterCount => _adapters.Length;

        public bool HasVisibleSurface => AdapterCount > 0;

        public string SurfaceLabel => _surfaceLabel;

        public string VisualText => HasVisibleSurface ? "UnitySurface" : "None";

        public PauseSurfaceApplicationResult LastApplicationResult { get; private set; }

        internal static PauseSurfaceRuntime Create(
            FrameworkLogger logger,
            IReadOnlyList<IPauseSurfaceAdapter> sceneAdapters,
            string sceneLabel)
        {
            logger ??= FrameworkLogger.Create<PauseSurfaceRuntime>();
            string resolvedSceneLabel = sceneLabel.NormalizeTextOrFallback("UIGlobal Pause Surface");
            bool hasSceneAdapters = sceneAdapters is { Count: > 0 };

            if (!hasSceneAdapters)
            {
                logger.Debug("Pause surface is not configured. Logical Pause will remain available without visual Pause presentation.");
                return new PauseSurfaceRuntime("Pause Surface", Array.Empty<IPauseSurfaceAdapter>());
            }

            logger.Debug("Pause surface resolved.", LogFields.Field("scene", resolvedSceneLabel));
            logger.Debug(
                "Pause surface diagnostics.",
                LogFields.Of(
                    LogFields.Field("scene", resolvedSceneLabel),
                    LogFields.Field("adapterCount", sceneAdapters.Count)));
            return new PauseSurfaceRuntime(resolvedSceneLabel, sceneAdapters);
        }

        public PauseSurfaceApplicationResult ApplySnapshot(PauseSnapshot snapshot, string source, string reason)
        {
            if (!snapshot.IsValid)
            {
                throw new ArgumentException("Pause surface runtime requires a valid Pause snapshot.", nameof(snapshot));
            }

            if (!HasVisibleSurface)
            {
                LastApplicationResult = PauseSurfaceApplicationResult.NoSurface(snapshot, _surfaceLabel, source, reason);
                return LastApplicationResult;
            }

            int supportedCount = 0;
            int appliedCount = 0;
            int failedCount = 0;
            var issues = new List<string>();

            for (int i = 0; i < _adapters.Length; i++)
            {
                var adapter = _adapters[i];
                if (adapter == null)
                {
                    continue;
                }

                if (!adapter.Supports(snapshot))
                {
                    continue;
                }

                supportedCount++;
                try
                {
                    adapter.Apply(snapshot);
                    appliedCount++;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    issues.Add($"{adapter.AdapterName}: {exception.GetType().Name}: {exception.Message}");
                }
            }

            LastApplicationResult = PauseSurfaceApplicationResult.FromExecution(
                snapshot,
                _surfaceLabel,
                source,
                reason,
                _adapters.Length,
                supportedCount,
                appliedCount,
                failedCount,
                issues);
            return LastApplicationResult;
        }

        private static IPauseSurfaceAdapter[] CopyAdapters(IReadOnlyList<IPauseSurfaceAdapter> adapters)
        {
            if (adapters == null || adapters.Count == 0)
            {
                return Array.Empty<IPauseSurfaceAdapter>();
            }

            var copy = new IPauseSurfaceAdapter[adapters.Count];
            for (int i = 0; i < adapters.Count; i++)
            {
                copy[i] = adapters[i];
            }

            return copy;
        }
    }

    /// <summary>
    /// Passive result for one Pause surface snapshot application.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F27A Pause surface application diagnostics.")]
    internal readonly struct PauseSurfaceApplicationResult
    {
        private readonly string[] _issues;

        private PauseSurfaceApplicationResult(
            PauseSnapshot snapshot,
            string surfaceLabel,
            string source,
            string reason,
            int adapterCount,
            int supportedAdapterCount,
            int appliedAdapterCount,
            int failedAdapterCount,
            IReadOnlyList<string> issues)
        {
            Snapshot = snapshot;
            SurfaceLabel = Normalize(surfaceLabel);
            Source = Normalize(source);
            Reason = Normalize(reason);
            AdapterCount = Math.Max(0, adapterCount);
            SupportedAdapterCount = Math.Max(0, supportedAdapterCount);
            AppliedAdapterCount = Math.Max(0, appliedAdapterCount);
            FailedAdapterCount = Math.Max(0, failedAdapterCount);
            _issues = CopyIssues(issues);
        }

        public PauseSnapshot Snapshot { get; }

        public string SurfaceLabel { get; }

        public string Source { get; }

        public string Reason { get; }

        public int AdapterCount { get; }

        public int SupportedAdapterCount { get; }

        public int AppliedAdapterCount { get; }

        public int FailedAdapterCount { get; }

        public IReadOnlyList<string> Issues => _issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasSurface => AdapterCount > 0;

        public bool HasSupportedAdapter => SupportedAdapterCount > 0;

        public bool Succeeded => HasSurface && AppliedAdapterCount > 0 && FailedAdapterCount == 0;

        public bool SkippedNoSurface => !HasSurface;

        public bool SkippedNoSupportingAdapter => HasSurface && SupportedAdapterCount == 0;

        public bool Failed => FailedAdapterCount > 0;

        public string VisualText => HasSurface ? "UnitySurface" : "None";

        public string StatusText
        {
            get
            {
                if (Failed)
                {
                    return "Failed";
                }

                if (Succeeded)
                {
                    return "Succeeded";
                }

                if (SkippedNoSurface)
                {
                    return "SkippedNoSurface";
                }

                if (SkippedNoSupportingAdapter)
                {
                    return "SkippedNoSupportingAdapter";
                }

                return "Unknown";
            }
        }

        public static PauseSurfaceApplicationResult NoSurface(
            PauseSnapshot snapshot,
            string surfaceLabel,
            string source,
            string reason)
        {
            return new PauseSurfaceApplicationResult(
                snapshot,
                surfaceLabel,
                source,
                reason,
                0,
                0,
                0,
                0,
                Array.Empty<string>());
        }

        public static PauseSurfaceApplicationResult FromExecution(
            PauseSnapshot snapshot,
            string surfaceLabel,
            string source,
            string reason,
            int adapterCount,
            int supportedAdapterCount,
            int appliedAdapterCount,
            int failedAdapterCount,
            IReadOnlyList<string> issues)
        {
            return new PauseSurfaceApplicationResult(
                snapshot,
                surfaceLabel,
                source,
                reason,
                adapterCount,
                supportedAdapterCount,
                appliedAdapterCount,
                failedAdapterCount,
                issues);
        }

        public LogField[] ToLogFields()
        {
            return LogFields.Of(
                LogFields.Field("pauseSurface", StatusText),
                LogFields.Field("pauseSurfaceVisual", VisualText),
                LogFields.Field("pauseSurfaceAdapterCount", AdapterCount),
                LogFields.Field("pauseSurfaceSupportedAdapters", SupportedAdapterCount),
                LogFields.Field("pauseSurfaceAppliedAdapters", AppliedAdapterCount),
                LogFields.Field("pauseSurfaceFailedAdapters", FailedAdapterCount),
                LogFields.Field("pauseSurfaceIssues", IssueCount),
                LogFields.Field("pauseSurfaceState", Snapshot.State.ToString()),
                LogFields.Field("pauseSurfacePaused", Snapshot.IsPaused));
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = Normalize(source[i]);
            }

            return copy;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
