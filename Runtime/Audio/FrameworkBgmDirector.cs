using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.ApiStatus;
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
        private const string LogPrefix = "[FRAMEWORK_BGM]";

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

            Log($"Route BGM set. routeBgm='{FormatCue(cue)}' retainedActivityBgm='<cleared>' deferRefreshForStartupActivity='{deferRefreshForStartupActivity}'.");

            if (!deferRefreshForStartupActivity)
            {
                Refresh();
            }
        }

        public void ClearRouteBgm(AudioBgmCueAsset cue)
        {
            if (cue != null && !ReferenceEquals(currentRouteBgm, cue))
            {
                Log($"Route BGM clear ignored as stale. requested='{FormatCue(cue)}' currentRouteBgm='{FormatCue(currentRouteBgm)}'.");
                return;
            }

            currentRouteBgm = null;
            currentActivityBgm = null;
            retainedActivityBgmForCurrentRoute = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

            Log("Route BGM cleared. Activity retention cleared with Route scope.");
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
                Log("Activity BGM policy Silence applied. Activity retention cleared.");
                Refresh();
                return;
            }

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.UseRoute)
            {
                currentActivityBgm = null;
                Log($"Activity BGM policy UseRoute applied. retainedActivityBgm='{FormatCue(retainedActivityBgmForCurrentRoute)}'.");
                Refresh();
                return;
            }

            currentActivityBgm = cue;

            if (currentActivityPolicy == FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit && cue != null)
            {
                retainedActivityBgmForCurrentRoute = cue;
            }

            Log($"Activity BGM set. activityBgm='{FormatCue(cue)}' policy='{currentActivityPolicy}' retainedActivityBgm='{FormatCue(retainedActivityBgmForCurrentRoute)}'.");
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
                Log($"Activity BGM clear ignored as stale. requested='{FormatCue(cue)}' currentActivityBgm='{FormatCue(currentActivityBgm)}'.");
                return;
            }

            currentActivityBgm = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

            Log($"Activity BGM cleared. retainedActivityBgm='{FormatCue(retainedActivityBgmForCurrentRoute)}' deferRefresh='{deferRefreshForActivityTransition}'.");

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
                Log($"BGM refresh skipped. effectiveBgm='{FormatCue(next.Cue)}' reason='{next.Reason}'.");
                return lastPlaybackResult;
            }

            currentEffectiveBgm = next.Cue;
            currentEffectiveIsExplicitSilence = next.IsExplicitSilence;
            hasAppliedBgm = true;

            if (audioRuntimeHost == null)
            {
                Debug.LogError($"{LogPrefix} AudioRuntimeHost is missing. effectiveBgm='{FormatCue(next.Cue)}' reason='{next.Reason}'.", this);
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

            Log($"BGM applied. effectiveBgm='{FormatCue(next.Cue)}' reason='{next.Reason}' status='{lastPlaybackResult.Status}'.");
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

        private void Log(string message)
        {
            if (logTransitions)
            {
                Debug.Log($"{LogPrefix} {message}", this);
            }
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
