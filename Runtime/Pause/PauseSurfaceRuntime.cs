using System;
using System.Collections.Generic;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. Executes logical Pause snapshot presentation against adapters
    /// currently valid for presentation. Presentation is composed from two sources: a fixed
    /// Persistent Content baseline (collected once at boot, unchanged from prior behavior) and
    /// zero or more lifecycle-scoped contributions (Route Primary, RouteContent, ActivityContent),
    /// each owned and replaced/released by the exact scene lifecycle that materializes/releases
    /// that content. This class does not decide by itself whether a scene/Route/Activity is
    /// currently valid; the lifecycle that owns that content is the one that calls
    /// SetSceneContribution/ReleaseSceneContribution. It is a presentation bridge only;
    /// PauseRuntime remains the owner of logical state and Gate snapshot production.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F27A/IF-ADR-005 Cut C Pause surface presentation runtime.")]
    internal sealed class PauseSurfaceRuntime
    {
        private readonly IPauseSurfaceAdapter[] _baselineAdapters;
        private readonly Dictionary<ulong, IPauseSurfaceAdapter[]> _sceneContributions =
            new Dictionary<ulong, IPauseSurfaceAdapter[]>();
        private readonly string _surfaceLabel;
        private readonly FrameworkLogger _logger;

        private PauseSurfaceRuntime(
            FrameworkLogger logger,
            string surfaceLabel,
            IReadOnlyList<IPauseSurfaceAdapter> baselineAdapters)
        {
            _logger = logger ?? FrameworkLogger.Create<PauseSurfaceRuntime>();
            _surfaceLabel = surfaceLabel.NormalizeTextOrFallback("Pause Surface");
            _baselineAdapters = CopyAdapters(baselineAdapters);
        }

        public int AdapterCount => CountAllAdapters();

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
                logger.Debug("Pause surface baseline is not configured. Logical Pause will remain available without Persistent Content presentation until lifecycle-scoped surfaces register.");
                return new PauseSurfaceRuntime(logger, "Pause Surface", Array.Empty<IPauseSurfaceAdapter>());
            }

            logger.Debug("Pause surface baseline resolved.", LogFields.Field("scene", resolvedSceneLabel));
            logger.Debug(
                "Pause surface baseline diagnostics.",
                LogFields.Of(
                    LogFields.Field("scene", resolvedSceneLabel),
                    LogFields.Field("adapterCount", sceneAdapters.Count)));
            return new PauseSurfaceRuntime(logger, resolvedSceneLabel, sceneAdapters);
        }

        /// <summary>
        /// Replaces the contribution of adapters owned by one lifecycle-scoped scene (Route
        /// Primary, RouteContent or ActivityContent), identified by its Unity scene handle. The
        /// caller (a Scene Lifecycle participant) is the only one that decides when this content
        /// is currently valid; this method does not re-derive that validity itself. If the scene
        /// is re-notified as available (e.g. an idempotent "already loaded" re-entry), the prior
        /// contribution for that exact scene handle is replaced, not duplicated. If a valid
        /// current PauseSnapshot is supplied, it is applied immediately to the newly registered
        /// adapters only, so an adapter that enters while Paused starts Paused and one that
        /// enters while Running starts Running, without waiting for the next Pause/Resume
        /// request.
        /// </summary>
        internal void SetSceneContribution(
            ulong sceneOwnerId,
            IReadOnlyList<IPauseSurfaceAdapter> adapters,
            PauseSnapshot currentSnapshot,
            string source,
            string reason)
        {
            IPauseSurfaceAdapter[] copy = CopyAdapters(adapters);
            if (copy.Length == 0)
            {
                _sceneContributions.Remove(sceneOwnerId);
                return;
            }

            _sceneContributions[sceneOwnerId] = copy;

            if (!currentSnapshot.IsValid)
            {
                return;
            }

            for (int i = 0; i < copy.Length; i++)
            {
                if (!TryApplyToAdapter(copy[i], currentSnapshot, out bool supported, out string issue) && supported)
                {
                    _logger.Warning(
                        "Pause surface scene contribution failed to synchronize a newly registered adapter with the current snapshot.",
                        LogFields.Of(
                            LogFields.Field("source", source.NormalizeTextOrFallback(nameof(PauseSurfaceRuntime))),
                            LogFields.Field("reason", reason.NormalizeTextOrFallback("scene-contribution-sync")),
                            LogFields.Field("issue", issue.NormalizeTextOrFallback("unknown"))));
                }
            }
        }

        /// <summary>
        /// Releases the contribution previously registered for one lifecycle-scoped scene. Safe
        /// to call even if no contribution is currently registered for that scene handle
        /// (idempotent). Never keeps a reference to adapters from a scene that has left its
        /// lifecycle.
        /// </summary>
        internal void ReleaseSceneContribution(ulong sceneOwnerId)
        {
            _sceneContributions.Remove(sceneOwnerId);
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

            int adapterCount = 0;
            int supportedCount = 0;
            int appliedCount = 0;
            int failedCount = 0;
            var issues = new List<string>();

            adapterCount += ApplyToList(_baselineAdapters, snapshot, ref supportedCount, ref appliedCount, ref failedCount, issues);
            foreach (IPauseSurfaceAdapter[] contribution in _sceneContributions.Values)
            {
                adapterCount += ApplyToList(contribution, snapshot, ref supportedCount, ref appliedCount, ref failedCount, issues);
            }

            LastApplicationResult = PauseSurfaceApplicationResult.FromExecution(
                snapshot,
                _surfaceLabel,
                source,
                reason,
                adapterCount,
                supportedCount,
                appliedCount,
                failedCount,
                issues);
            return LastApplicationResult;
        }

        private int CountAllAdapters()
        {
            int count = _baselineAdapters.Length;
            foreach (IPauseSurfaceAdapter[] contribution in _sceneContributions.Values)
            {
                count += contribution.Length;
            }

            return count;
        }

        private static int ApplyToList(
            IPauseSurfaceAdapter[] adapters,
            PauseSnapshot snapshot,
            ref int supportedCount,
            ref int appliedCount,
            ref int failedCount,
            List<string> issues)
        {
            if (adapters == null || adapters.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < adapters.Length; i++)
            {
                if (adapters[i] == null)
                {
                    continue;
                }

                bool applied = TryApplyToAdapter(adapters[i], snapshot, out bool supported, out string issue);
                if (!supported)
                {
                    continue;
                }

                supportedCount++;
                if (applied)
                {
                    appliedCount++;
                }
                else
                {
                    failedCount++;
                    issues.Add(issue);
                }
            }

            return adapters.Length;
        }

        private static bool TryApplyToAdapter(
            IPauseSurfaceAdapter adapter,
            PauseSnapshot snapshot,
            out bool supported,
            out string issue)
        {
            issue = null;
            supported = false;
            if (adapter == null || !adapter.Supports(snapshot))
            {
                return false;
            }

            supported = true;
            try
            {
                adapter.Apply(snapshot);
                return true;
            }
            catch (Exception exception)
            {
                issue = $"{adapter.AdapterName}: {exception.GetType().Name}: {exception.Message}";
                return false;
            }
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
