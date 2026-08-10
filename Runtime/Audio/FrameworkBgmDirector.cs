using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Framework-owned Route/Activity BGM precedence director.
    /// It selects BGM cues and delegates playback to the optional Immersive Audio package.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/BGM Director")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F47C optional framework-owned BGM adapter.")]
    public sealed class FrameworkBgmDirector : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioRuntimeHost audioRuntimeHost;

        [Header("Diagnostics")]
        [SerializeField] private bool logTransitions = true;

        private AudioBgmCueAsset currentRouteBgm;
        private AudioBgmCueAsset currentActivityBgm;
        private AudioBgmCueAsset retainedActivityBgmForCurrentRoute;
        private AudioBgmCueAsset currentEffectiveBgm;
        private AudioBgmCueAsset confirmedBgm;
        private FrameworkBgmActivityPolicy currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
        private bool hasActiveActivityBgmBinding;
        private bool currentEffectiveIsExplicitSilence;
        private FrameworkBgmOperationResult lastOperationResult;
        private FrameworkLogger logger;

        public AudioBgmCueAsset CurrentRouteBgm => currentRouteBgm;

        public AudioBgmCueAsset CurrentActivityBgm => currentActivityBgm;

        public AudioBgmCueAsset RetainedActivityBgmForCurrentRoute => retainedActivityBgmForCurrentRoute;

        public AudioBgmCueAsset CurrentEffectiveBgm => currentEffectiveBgm;

        public AudioBgmCueAsset ConfirmedBgm => confirmedBgm;

        public FrameworkBgmActivityPolicy CurrentActivityPolicy => currentActivityPolicy;

        public bool HasActiveActivityBgmBinding => hasActiveActivityBgmBinding;

        public bool CurrentEffectiveIsExplicitSilence => currentEffectiveIsExplicitSilence;

        public FrameworkBgmOperationResult LastOperationResult => lastOperationResult;

        public FrameworkBgmOperationResult SetRouteBgm(AudioBgmCueAsset cue)
        {
            return SetRouteBgm(cue, false);
        }

        public FrameworkBgmOperationResult SetRouteBgm(AudioBgmCueAsset cue, bool deferRefreshForStartupActivity)
        {
            currentRouteBgm = cue;
            currentActivityBgm = null;
            retainedActivityBgmForCurrentRoute = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

            Trace(
                "Route BGM set.",
                LogFields.Of(
                    LogFields.Field("routeBgm", FormatCue(cue)),
                    LogFields.Field("retainedActivityBgm", "<cleared>"),
                    LogFields.Field("deferRefreshForStartupActivity", deferRefreshForStartupActivity)));

            if (!deferRefreshForStartupActivity)
            {
                return Refresh();
            }

            return RecordNoChange(FrameworkBgmOperation.Apply, cue, false, "BGM refresh deferred for startup Activity.");
        }

        public FrameworkBgmOperationResult ClearRouteBgm(AudioBgmCueAsset cue)
        {
            if (cue != null && !ReferenceEquals(currentRouteBgm, cue))
            {
                Trace(
                    "Route BGM clear ignored as stale.",
                    LogFields.Of(
                        LogFields.Field("requested", FormatCue(cue)),
                        LogFields.Field("currentRouteBgm", FormatCue(currentRouteBgm))));
                return RecordNoChange(FrameworkBgmOperation.Release, null, false, "Stale Route BGM clear ignored.");
            }

            currentRouteBgm = null;
            currentActivityBgm = null;
            retainedActivityBgmForCurrentRoute = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

            Trace("Route BGM cleared. Activity retention cleared with Route scope.");
            return Refresh();
        }

        public FrameworkBgmOperationResult SetActivityBgm(AudioBgmCueAsset cue, FrameworkBgmActivityPolicy policy)
        {
            hasActiveActivityBgmBinding = true;
            currentActivityPolicy = NormalizeActivityPolicy(policy);

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.Silence)
            {
                currentActivityBgm = null;
                retainedActivityBgmForCurrentRoute = null;
                Trace("Activity BGM policy Silence applied. Activity retention cleared.");
                return Refresh();
            }

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.UseRoute)
            {
                currentActivityBgm = null;
                Trace(
                    "Activity BGM policy UseRoute applied.",
                    LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute)));
                return Refresh();
            }

            currentActivityBgm = cue;

            Trace(
                "Activity BGM set.",
                LogFields.Of(
                    LogFields.Field("activityBgm", FormatCue(cue)),
                    LogFields.Field("policy", currentActivityPolicy),
                    LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute))));
            return Refresh();
        }

        public FrameworkBgmOperationResult ClearActivityBgm(AudioBgmCueAsset cue)
        {
            return ClearActivityBgm(cue, false);
        }

        public FrameworkBgmOperationResult ClearActivityBgm(AudioBgmCueAsset cue, bool deferRefreshForActivityTransition)
        {
            if (currentActivityBgm != null && cue != null && !ReferenceEquals(currentActivityBgm, cue))
            {
                Trace(
                    "Activity BGM clear ignored as stale.",
                    LogFields.Of(
                        LogFields.Field("requested", FormatCue(cue)),
                        LogFields.Field("currentActivityBgm", FormatCue(currentActivityBgm))));
                return RecordNoChange(FrameworkBgmOperation.Release, null, false, "Stale Activity BGM clear ignored.");
            }

            currentActivityBgm = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

            Trace(
                "Activity BGM cleared.",
                LogFields.Of(
                    LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute)),
                    LogFields.Field("deferRefresh", deferRefreshForActivityTransition)));

            if (!deferRefreshForActivityTransition)
            {
                return Refresh();
            }

            return RecordNoChange(FrameworkBgmOperation.Release, null, false, "BGM refresh deferred for Activity transition.");
        }

        public FrameworkBgmOperationResult Refresh()
        {
            BgmResolution next = ResolveEffectiveBgm();
            currentEffectiveBgm = next.Cue;
            currentEffectiveIsExplicitSilence = next.IsExplicitSilence;

            if (ReferenceEquals(confirmedBgm, next.Cue))
            {
                RetainConfirmedActivityCueWhenApplicable(next.Cue);
                Trace(
                    "BGM refresh skipped.",
                    LogFields.Of(
                        LogFields.Field("effectiveBgm", FormatCue(next.Cue)),
                        LogFields.Field("reason", next.Reason)));
                return RecordNoChange(
                    next.Cue != null ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                    next.Cue,
                    next.IsExplicitSilence,
                    next.Reason);
            }

            if (audioRuntimeHost == null)
            {
                EnsureLogger();
                logger.Error(
                    "BGM could not be applied because AudioRuntimeHost is missing.",
                    LogFields.Of(
                        LogFields.Field("effectiveBgm", FormatCue(next.Cue)),
                        LogFields.Field("reason", next.Reason)));
                return Record(
                    next.Cue != null ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                    FrameworkBgmOperationOutcome.OptionalAuthorityUnavailable,
                    next,
                    "framework_bgm_audio_runtime_host_missing",
                    confirmedBgm);
            }

            AudioBgmCueAsset previousConfirmedBgm = confirmedBgm;
            AudioPlaybackResult providerResult = next.Cue != null
                ? audioRuntimeHost.PlayBgm(next.Cue)
                : audioRuntimeHost.StopBgm();

            bool succeeded = next.Cue != null
                ? providerResult.Succeeded
                : providerResult.Status == AudioPlaybackStatus.Stopped;

            if (succeeded)
            {
                confirmedBgm = next.Cue;

                if (next.Cue != null
                    && currentActivityPolicy == FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit
                    && ReferenceEquals(currentActivityBgm, next.Cue))
                {
                    retainedActivityBgmForCurrentRoute = next.Cue;
                }

                return Record(
                    next.Cue != null ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                    next.Cue != null ? FrameworkBgmOperationOutcome.Applied : FrameworkBgmOperationOutcome.Released,
                    next,
                    FormatProviderReason(providerResult),
                    previousConfirmedBgm);
            }

            return Record(
                next.Cue != null ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                FrameworkBgmOperationOutcome.Rejected,
                next,
                FormatProviderReason(providerResult),
                confirmedBgm);
        }

        private FrameworkBgmOperationResult RecordNoChange(
            FrameworkBgmOperation operation,
            AudioBgmCueAsset requestedCue,
            bool requestedExplicitSilence,
            string reason)
        {
            return Record(
                operation,
                FrameworkBgmOperationOutcome.NoChange,
                new BgmResolution(requestedCue, requestedExplicitSilence, reason),
                reason,
                confirmedBgm);
        }

        private FrameworkBgmOperationResult Record(
            FrameworkBgmOperation operation,
            FrameworkBgmOperationOutcome outcome,
            BgmResolution requested,
            string reason,
            AudioBgmCueAsset previousConfirmed)
        {
            lastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                outcome,
                previousConfirmed,
                requested.Cue,
                confirmedBgm,
                requested.IsExplicitSilence,
                false,
                reason);

            Debug(
                "BGM operation completed.",
                LogFields.Of(
                    LogFields.Field("operation", operation),
                    LogFields.Field("outcome", outcome),
                    LogFields.Field("requestedBgm", FormatCue(requested.Cue)),
                    LogFields.Field("previousConfirmedBgm", FormatCue(previousConfirmed)),
                    LogFields.Field("confirmedBgm", FormatCue(confirmedBgm)),
                    LogFields.Field("reason", reason)));
            return lastOperationResult;
        }

        private void RetainConfirmedActivityCueWhenApplicable(AudioBgmCueAsset confirmedCue)
        {
            if (confirmedCue != null
                && currentActivityPolicy == FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit
                && ReferenceEquals(currentActivityBgm, confirmedCue))
            {
                retainedActivityBgmForCurrentRoute = confirmedCue;
            }
        }

        private static string FormatProviderReason(AudioPlaybackResult providerResult)
        {
            if (providerResult.Issues == null || providerResult.Issues.Count == 0)
            {
                return providerResult.Status.ToString();
            }

            return providerResult.Status + ": " + providerResult.Issues[0].Code;
        }

        private BgmResolution ResolveEffectiveBgm()
        {
            if (hasActiveActivityBgmBinding)
            {
                switch (currentActivityPolicy)
                {
                    case FrameworkBgmActivityPolicy.Silence:
                        return BgmResolution.Silence("activity-policy-silence");

                    case FrameworkBgmActivityPolicy.UseRoute:
                        return BgmResolution.FromCue(currentRouteBgm, "activity-policy-use-route");

                    case FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit:
                        if (currentActivityBgm != null)
                        {
                            return BgmResolution.FromCue(currentActivityBgm, "activity-own");
                        }

                        if (retainedActivityBgmForCurrentRoute != null)
                        {
                            return BgmResolution.FromCue(retainedActivityBgmForCurrentRoute, "activity-retained-until-route-exit");
                        }

                        return BgmResolution.FromCue(currentRouteBgm, "activity-fallback-route");

                    default:
                        return BgmResolution.FromCue(
                            currentActivityBgm != null ? currentActivityBgm : currentRouteBgm,
                            currentActivityBgm != null ? "activity-own" : "activity-fallback-route");
                }
            }

            if (retainedActivityBgmForCurrentRoute != null)
            {
                return BgmResolution.FromCue(retainedActivityBgmForCurrentRoute, "activity-retained-until-route-exit");
            }

            return BgmResolution.FromCue(currentRouteBgm, "route");
        }

        private void Trace(string message, params LogField[] fields)
        {
            if (logTransitions)
            {
                EnsureLogger();
                logger.Trace(message, fields);
            }
        }

        private void Debug(string message, params LogField[] fields)
        {
            if (logTransitions)
            {
                EnsureLogger();
                logger.Debug(message, fields);
            }
        }

        private void EnsureLogger()
        {
            logger ??= FrameworkLogger.Create<FrameworkBgmDirector>();
        }

        private static FrameworkBgmActivityPolicy NormalizeActivityPolicy(FrameworkBgmActivityPolicy policy)
        {
            return policy == FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit
                || policy == FrameworkBgmActivityPolicy.UseRoute
                || policy == FrameworkBgmActivityPolicy.Silence
                ? policy
                : FrameworkBgmActivityPolicy.UseOwnOrRoute;
        }

        private static string FormatCue(AudioBgmCueAsset cue)
        {
            return cue != null ? cue.name : "<silence>";
        }

        private readonly struct BgmResolution
        {
            internal BgmResolution(AudioBgmCueAsset cue, bool isExplicitSilence, string reason)
            {
                Cue = cue;
                IsExplicitSilence = isExplicitSilence;
                Reason = reason;
            }

            public AudioBgmCueAsset Cue { get; }

            public bool IsExplicitSilence { get; }

            public string Reason { get; }

            public static BgmResolution FromCue(AudioBgmCueAsset cue, string reason)
            {
                return new BgmResolution(cue, false, reason);
            }

            public static BgmResolution Silence(string reason)
            {
                return new BgmResolution(null, true, reason);
            }
        }
    }
}
