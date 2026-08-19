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
    /// API status: Experimental. Framework-owned Route/Activity BGM intent director.
    ///
    /// Confirmed BGM presentation is sticky: removing Route/Activity ownership or having no new
    /// request does not mutate provider playback. Only an explicit Play cue or explicit Silence
    /// intent is sent to the optional Immersive Audio provider.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/BGM Director")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "BGM-CONTINUITY-1 persistent BGM intent authority.")]
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
        private bool confirmedExplicitSilence;
        private FrameworkBgmOperationResult lastOperationResult;
        private FrameworkLogger logger;
        private FrameworkBgmDirectorInjectionRuntime injectionRuntime;
        private BgmIntent pendingIntent;

        public AudioBgmCueAsset CurrentRouteBgm => currentRouteBgm;

        public AudioBgmCueAsset CurrentActivityBgm => currentActivityBgm;

        /// <summary>
        /// Diagnostic evidence for the last Activity cue confirmed under UseOwnOrPreserveCurrent.
        /// It does not own playback continuity; confirmed presentation remains sticky independently of Route scope.
        /// </summary>
        public AudioBgmCueAsset RetainedActivityBgmForCurrentRoute => retainedActivityBgmForCurrentRoute;

        public AudioBgmCueAsset CurrentEffectiveBgm => currentEffectiveBgm;

        public AudioBgmCueAsset ConfirmedBgm => confirmedBgm;

        public bool ConfirmedExplicitSilence => confirmedExplicitSilence;

        public FrameworkBgmActivityPolicy CurrentActivityPolicy => currentActivityPolicy;

        public bool HasActiveActivityBgmBinding => hasActiveActivityBgmBinding;

        public bool CurrentEffectiveIsExplicitSilence => currentEffectiveIsExplicitSilence;

        public FrameworkBgmOperationResult LastOperationResult => lastOperationResult;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            injectionRuntime ??= new FrameworkBgmDirectorInjectionRuntime(this);
        }

        private void OnDisable()
        {
            injectionRuntime?.Dispose();
            injectionRuntime = null;
        }

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

            pendingIntent = cue != null ? BgmIntent.Play(cue, "route-play") : BgmIntent.None("route-no-request");

            Trace(
                "Route BGM intent set.",
                LogFields.Of(
                    LogFields.Field("routeBgm", FormatCue(cue)),
                    LogFields.Field("intent", pendingIntent.Kind),
                    LogFields.Field("deferRefreshForStartupActivity", deferRefreshForStartupActivity)));

            if (deferRefreshForStartupActivity)
            {
                return RecordNoChange(
                    cue != null ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Preserve,
                    cue,
                    false,
                    "BGM intent refresh deferred for Startup Activity.");
            }

            return Refresh();
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
                return RecordPreservedPresentation("Stale Route BGM clear ignored.");
            }

            currentRouteBgm = null;
            currentActivityBgm = null;
            retainedActivityBgmForCurrentRoute = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
            pendingIntent = BgmIntent.None("route-owner-exit-no-request");

            Trace("Route BGM owner cleared. Confirmed BGM presentation is preserved.");
            return RecordPreservedPresentation("Route owner exit does not mutate confirmed BGM.");
        }

        public FrameworkBgmOperationResult SetActivityBgm(AudioBgmCueAsset cue, FrameworkBgmActivityPolicy policy)
        {
            hasActiveActivityBgmBinding = true;
            currentActivityPolicy = NormalizeActivityPolicy(policy);
            currentActivityBgm = cue;

            switch (currentActivityPolicy)
            {
                case FrameworkBgmActivityPolicy.Silence:
                    currentActivityBgm = null;
                    retainedActivityBgmForCurrentRoute = null;
                    pendingIntent = BgmIntent.Silence("activity-policy-silence");
                    Trace("Activity BGM explicit Silence intent applied.");
                    return Refresh();

                case FrameworkBgmActivityPolicy.UseRoute:
                    pendingIntent = currentRouteBgm != null
                        ? BgmIntent.Play(currentRouteBgm, "activity-policy-use-route")
                        : BgmIntent.None("activity-policy-use-route-no-route-request");
                    Trace(
                        "Activity BGM policy UseRoute evaluated.",
                        LogFields.Field("routeBgm", FormatCue(currentRouteBgm)));
                    return Refresh();

                case FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent:
                    pendingIntent = cue != null
                        ? BgmIntent.Play(cue, "activity-own")
                        : BgmIntent.None("activity-preserve-current");
                    Trace(
                        "Activity BGM preserve-current policy evaluated.",
                        LogFields.Of(
                            LogFields.Field("activityBgm", FormatCue(cue)),
                            LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute))));
                    return Refresh();

                default:
                    if (cue != null)
                    {
                        pendingIntent = BgmIntent.Play(cue, "activity-own");
                    }
                    else if (currentRouteBgm != null)
                    {
                        pendingIntent = BgmIntent.Play(currentRouteBgm, "activity-fallback-route");
                    }
                    else
                    {
                        pendingIntent = BgmIntent.None("activity-no-request");
                    }

                    Trace(
                        "Activity BGM intent evaluated.",
                        LogFields.Of(
                            LogFields.Field("activityBgm", FormatCue(cue)),
                            LogFields.Field("policy", currentActivityPolicy),
                            LogFields.Field("routeBgm", FormatCue(currentRouteBgm)),
                            LogFields.Field("intent", pendingIntent.Kind)));
                    return Refresh();
            }
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
                return RecordPreservedPresentation("Stale Activity BGM clear ignored.");
            }

            currentActivityBgm = null;
            hasActiveActivityBgmBinding = false;
            currentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
            pendingIntent = BgmIntent.None("activity-owner-exit-no-request");

            Trace(
                "Activity BGM owner cleared. Confirmed BGM presentation is preserved.",
                LogFields.Of(
                    LogFields.Field("retainedActivityBgm", FormatCue(retainedActivityBgmForCurrentRoute)),
                    LogFields.Field("deferRefresh", deferRefreshForActivityTransition)));

            return RecordPreservedPresentation("Activity owner exit does not mutate confirmed BGM.");
        }

        /// <summary>
        /// Applies only a pending explicit Play/Silence intent. When no explicit intent exists,
        /// Refresh is provider-idempotent and preserves the confirmed presentation.
        /// </summary>
        public FrameworkBgmOperationResult Refresh()
        {
            if (pendingIntent.Kind == BgmIntentKind.None)
            {
                return RecordPreservedPresentation(pendingIntent.Reason ?? "No explicit BGM request.");
            }

            currentEffectiveBgm = pendingIntent.Cue;
            currentEffectiveIsExplicitSilence = pendingIntent.Kind == BgmIntentKind.Silence;

            if (pendingIntent.Kind == BgmIntentKind.Play
                && !confirmedExplicitSilence
                && ReferenceEquals(confirmedBgm, pendingIntent.Cue))
            {
                RetainConfirmedActivityCueWhenApplicable(pendingIntent.Cue);
                string reason = "Requested BGM is already the confirmed presentation.";
                pendingIntent = BgmIntent.None("same-confirmed-cue");
                return RecordNoChange(FrameworkBgmOperation.Apply, confirmedBgm, false, reason);
            }

            if (pendingIntent.Kind == BgmIntentKind.Silence && confirmedExplicitSilence)
            {
                string reason = "Explicit silence is already the confirmed presentation.";
                pendingIntent = BgmIntent.None("already-confirmed-silence");
                return RecordNoChange(FrameworkBgmOperation.Release, null, true, reason);
            }

            if (audioRuntimeHost == null)
            {
                EnsureLogger();
                logger.Error(
                    "BGM intent could not be applied because AudioRuntimeHost is missing.",
                    LogFields.Of(
                        LogFields.Field("requestedBgm", FormatCue(pendingIntent.Cue)),
                        LogFields.Field("explicitSilence", pendingIntent.Kind == BgmIntentKind.Silence),
                        LogFields.Field("reason", pendingIntent.Reason)));

                return Record(
                    pendingIntent.Kind == BgmIntentKind.Play ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                    FrameworkBgmOperationOutcome.OptionalAuthorityUnavailable,
                    pendingIntent,
                    "framework_bgm_audio_runtime_host_missing",
                    confirmedBgm,
                    confirmedExplicitSilence);
            }

            BgmIntent requested = pendingIntent;
            AudioBgmCueAsset previousConfirmedBgm = confirmedBgm;
            bool previousConfirmedSilence = confirmedExplicitSilence;
            AudioPlaybackResult providerResult = requested.Kind == BgmIntentKind.Play
                ? audioRuntimeHost.PlayBgm(requested.Cue)
                : audioRuntimeHost.StopBgm();

            bool succeeded = requested.Kind == BgmIntentKind.Play
                ? providerResult.Succeeded
                : providerResult.Status == AudioPlaybackStatus.Stopped;

            if (succeeded)
            {
                if (requested.Kind == BgmIntentKind.Play)
                {
                    confirmedBgm = requested.Cue;
                    confirmedExplicitSilence = false;
                    RetainConfirmedActivityCueWhenApplicable(requested.Cue);
                }
                else
                {
                    confirmedBgm = null;
                    confirmedExplicitSilence = true;
                    retainedActivityBgmForCurrentRoute = null;
                }

                pendingIntent = BgmIntent.None("provider-confirmed");
                currentEffectiveBgm = confirmedBgm;
                currentEffectiveIsExplicitSilence = confirmedExplicitSilence;

                return Record(
                    requested.Kind == BgmIntentKind.Play ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                    requested.Kind == BgmIntentKind.Play ? FrameworkBgmOperationOutcome.Applied : FrameworkBgmOperationOutcome.Released,
                    requested,
                    FormatProviderReason(providerResult),
                    previousConfirmedBgm,
                    previousConfirmedSilence);
            }

            return Record(
                requested.Kind == BgmIntentKind.Play ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                FrameworkBgmOperationOutcome.Rejected,
                requested,
                FormatProviderReason(providerResult),
                previousConfirmedBgm,
                previousConfirmedSilence);
        }

        private FrameworkBgmOperationResult RecordPreservedPresentation(string reason)
        {
            currentEffectiveBgm = confirmedBgm;
            currentEffectiveIsExplicitSilence = confirmedExplicitSilence;
            const FrameworkBgmOperation operation = FrameworkBgmOperation.Preserve;

            lastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                FrameworkBgmOperationOutcome.NoChange,
                confirmedBgm,
                null,
                confirmedBgm,
                false,
                confirmedExplicitSilence,
                reason);

            Debug(
                "BGM presentation preserved because no explicit provider intent exists.",
                LogFields.Of(
                    LogFields.Field("confirmedBgm", FormatCue(confirmedBgm)),
                    LogFields.Field("confirmedExplicitSilence", confirmedExplicitSilence),
                    LogFields.Field("reason", reason)));
            return lastOperationResult;
        }

        private FrameworkBgmOperationResult RecordNoChange(
            FrameworkBgmOperation operation,
            AudioBgmCueAsset requestedCue,
            bool requestedExplicitSilence,
            string reason)
        {
            BgmIntent requested = requestedExplicitSilence
                ? BgmIntent.Silence(reason)
                : requestedCue != null
                    ? BgmIntent.Play(requestedCue, reason)
                    : BgmIntent.None(reason);

            lastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                FrameworkBgmOperationOutcome.NoChange,
                confirmedBgm,
                requestedCue,
                confirmedBgm,
                requestedExplicitSilence,
                confirmedExplicitSilence,
                reason);

            Debug(
                "BGM operation completed without provider mutation.",
                LogFields.Of(
                    LogFields.Field("operation", operation),
                    LogFields.Field("requestedBgm", FormatCue(requested.Cue)),
                    LogFields.Field("requestedExplicitSilence", requestedExplicitSilence),
                    LogFields.Field("confirmedBgm", FormatCue(confirmedBgm)),
                    LogFields.Field("confirmedExplicitSilence", confirmedExplicitSilence),
                    LogFields.Field("reason", reason)));
            return lastOperationResult;
        }

        private FrameworkBgmOperationResult Record(
            FrameworkBgmOperation operation,
            FrameworkBgmOperationOutcome outcome,
            BgmIntent requested,
            string reason,
            AudioBgmCueAsset previousConfirmed,
            bool previousConfirmedSilence)
        {
            lastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                outcome,
                previousConfirmed,
                requested.Cue,
                confirmedBgm,
                requested.Kind == BgmIntentKind.Silence,
                confirmedExplicitSilence,
                reason);

            Debug(
                "BGM operation completed.",
                LogFields.Of(
                    LogFields.Field("operation", operation),
                    LogFields.Field("outcome", outcome),
                    LogFields.Field("requestedBgm", FormatCue(requested.Cue)),
                    LogFields.Field("requestedExplicitSilence", requested.Kind == BgmIntentKind.Silence),
                    LogFields.Field("previousConfirmedBgm", FormatCue(previousConfirmed)),
                    LogFields.Field("previousConfirmedExplicitSilence", previousConfirmedSilence),
                    LogFields.Field("confirmedBgm", FormatCue(confirmedBgm)),
                    LogFields.Field("confirmedExplicitSilence", confirmedExplicitSilence),
                    LogFields.Field("reason", reason)));
            return lastOperationResult;
        }

        private void RetainConfirmedActivityCueWhenApplicable(AudioBgmCueAsset confirmedCue)
        {
            if (confirmedCue != null
                && currentActivityPolicy == FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent
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
            return policy == FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent
                || policy == FrameworkBgmActivityPolicy.UseRoute
                || policy == FrameworkBgmActivityPolicy.Silence
                ? policy
                : FrameworkBgmActivityPolicy.UseOwnOrRoute;
        }

        private static string FormatCue(AudioBgmCueAsset cue)
        {
            return cue != null ? cue.name : "<none>";
        }

        private enum BgmIntentKind
        {
            None = 0,
            Play = 1,
            Silence = 2
        }

        private readonly struct BgmIntent
        {
            private BgmIntent(BgmIntentKind kind, AudioBgmCueAsset cue, string reason)
            {
                Kind = kind;
                Cue = cue;
                Reason = reason;
            }

            internal BgmIntentKind Kind { get; }

            internal AudioBgmCueAsset Cue { get; }

            internal string Reason { get; }

            internal static BgmIntent None(string reason)
            {
                return new BgmIntent(BgmIntentKind.None, null, reason);
            }

            internal static BgmIntent Play(AudioBgmCueAsset cue, string reason)
            {
                return cue != null
                    ? new BgmIntent(BgmIntentKind.Play, cue, reason)
                    : None(reason);
            }

            internal static BgmIntent Silence(string reason)
            {
                return new BgmIntent(BgmIntentKind.Silence, null, reason);
            }
        }
    }
}
