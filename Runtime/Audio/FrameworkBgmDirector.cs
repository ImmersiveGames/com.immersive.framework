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
        private FrameworkBgmActivityPolicy currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
        private bool hasActiveActivityBgmBinding;
        private bool currentEffectiveIsExplicitSilence;
        private bool hasAppliedBgm;
        private AudioPlaybackResult lastPlaybackResult;
        private FrameworkLogger logger;

        public AudioBgmCueAsset CurrentRouteBgm => currentRouteBgm;

        public AudioBgmCueAsset CurrentActivityBgm => currentActivityBgm;

        public AudioBgmCueAsset RetainedActivityBgmForCurrentRoute => retainedActivityBgmForCurrentRoute;

        public AudioBgmCueAsset CurrentEffectiveBgm => currentEffectiveBgm;

        public FrameworkBgmActivityPolicy CurrentActivityPolicy => currentActivityPolicy;

        public bool HasActiveActivityBgmBinding => hasActiveActivityBgmBinding;

        public bool CurrentEffectiveIsExplicitSilence => currentEffectiveIsExplicitSilence;

        public AudioPlaybackResult LastPlaybackResult => lastPlaybackResult;

        public void SetRouteBgm(AudioBgmCueAsset cue)
        {
            SetRouteBgm(cue, false);
        }

        public void SetRouteBgm(AudioBgmCueAsset cue, bool deferRefreshForStartupActivity)
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
                Refresh();
            }
        }

        public void ClearRouteBgm(AudioBgmCueAsset cue)
        {
            if (cue != null && !ReferenceEquals(currentRouteBgm, cue))
            {
                Trace(
                    "Route BGM clear ignored as stale.",
                    LogFields.Of(
                        LogFields.Field("requested", FormatCue(cue)),
                        LogFields.Field("currentRouteBgm", FormatCue(currentRouteBgm))));
                return;
            }

            currentRouteBgm = null;
            currentActivityBgm = null;
            retainedActivityBgmForCurrentRoute = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

            Trace("Route BGM cleared. Activity retention cleared with Route scope.");
            Refresh();
        }

        public void SetActivityBgm(AudioBgmCueAsset cue, FrameworkBgmActivityPolicy policy)
        {
            hasActiveActivityBgmBinding = true;
            currentActivityPolicy = NormalizeActivityPolicy(policy);

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.Silence)
            {
                currentActivityBgm = null;
                retainedActivityBgmForCurrentRoute = null;
                Trace("Activity BGM policy Silence applied. Activity retention cleared.");
                Refresh();
                return;
            }

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.UseRoute)
            {
                currentActivityBgm = null;
                Trace(
                    "Activity BGM policy UseRoute applied.",
                    LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute)));
                Refresh();
                return;
            }

            currentActivityBgm = cue;

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit && cue != null)
            {
                retainedActivityBgmForCurrentRoute = cue;
            }

            Trace(
                "Activity BGM set.",
                LogFields.Of(
                    LogFields.Field("activityBgm", FormatCue(cue)),
                    LogFields.Field("policy", currentActivityPolicy),
                    LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute))));
            Refresh();
        }

        public void ClearActivityBgm(AudioBgmCueAsset cue)
        {
            ClearActivityBgm(cue, false);
        }

        public void ClearActivityBgm(AudioBgmCueAsset cue, bool deferRefreshForActivityTransition)
        {
            if (currentActivityBgm != null && cue != null && !ReferenceEquals(currentActivityBgm, cue))
            {
                Trace(
                    "Activity BGM clear ignored as stale.",
                    LogFields.Of(
                        LogFields.Field("requested", FormatCue(cue)),
                        LogFields.Field("currentActivityBgm", FormatCue(currentActivityBgm))));
                return;
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
                Refresh();
            }
        }

        public AudioPlaybackResult Refresh()
        {
            BgmResolution next = ResolveEffectiveBgm();
            if (hasAppliedBgm
                && ReferenceEquals(currentEffectiveBgm, next.Cue)
                && currentEffectiveIsExplicitSilence == next.IsExplicitSilence)
            {
                Trace(
                    "BGM refresh skipped.",
                    LogFields.Of(
                        LogFields.Field("effectiveBgm", FormatCue(next.Cue)),
                        LogFields.Field("reason", next.Reason)));
                return lastPlaybackResult;
            }

            currentEffectiveBgm = next.Cue;
            currentEffectiveIsExplicitSilence = next.IsExplicitSilence;
            hasAppliedBgm = true;

            if (audioRuntimeHost == null)
            {
                EnsureLogger();
                logger.Error(
                    "BGM could not be applied because AudioRuntimeHost is missing.",
                    LogFields.Of(
                        LogFields.Field("effectiveBgm", FormatCue(next.Cue)),
                        LogFields.Field("reason", next.Reason)));
                lastPlaybackResult = AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue(
                        "framework_bgm_audio_runtime_host_missing",
                        "FrameworkBgmDirector requires an AudioRuntimeHost reference.",
                        nameof(audioRuntimeHost)));
                return lastPlaybackResult;
            }

            lastPlaybackResult = next.Cue != null
                ? audioRuntimeHost.PlayBgm(next.Cue)
                : audioRuntimeHost.StopBgm();

            Debug(
                "BGM applied.",
                LogFields.Of(
                    LogFields.Field("effectiveBgm", FormatCue(next.Cue)),
                    LogFields.Field("reason", next.Reason),
                    LogFields.Field("status", lastPlaybackResult.Status)));
            return lastPlaybackResult;
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
            private BgmResolution(AudioBgmCueAsset cue, bool isExplicitSilence, string reason)
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
