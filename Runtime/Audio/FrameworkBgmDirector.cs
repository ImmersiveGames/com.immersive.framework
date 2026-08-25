using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.ActivityFlow;
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
    public sealed class FrameworkBgmDirector : MonoBehaviour, IActivityContentEntryCompletionReceiver
    {
        [Header("Audio")]
        [SerializeField] private AudioRuntimeHost audioRuntimeHost;

        [Header("Diagnostics")]
        [SerializeField] private bool logTransitions = true;

        private FrameworkLogger _logger;
        private FrameworkBgmDirectorInjectionRuntime _injectionRuntime;
        private BgmIntent _pendingIntent;
        private BgmIntent _currentRouteIntent = BgmIntent.None("no-active-route");
        private bool _awaitingStartupActivityEntry;

        public AudioBgmCueAsset CurrentRouteBgm { get; private set; }

        /// <summary>
        /// The authored policy of the current Route. Together with CurrentRouteBgm, this is the
        /// complete Route intent; a cue is meaningful only for PlayOwn.
        /// </summary>
        public FrameworkBgmRoutePolicy CurrentRoutePolicy { get; private set; } = FrameworkBgmRoutePolicy.PreserveCurrent;

        public AudioBgmCueAsset CurrentActivityBgm { get; private set; }

        /// <summary>
        /// Diagnostic evidence for the last Activity cue confirmed under UseOwnOrPreserveCurrent.
        /// It does not own playback continuity; confirmed presentation remains sticky independently of Route scope.
        /// </summary>
        public AudioBgmCueAsset RetainedActivityBgmForCurrentRoute { get; private set; }

        public AudioBgmCueAsset CurrentEffectiveBgm { get; private set; }

        public AudioBgmCueAsset ConfirmedBgm { get; private set; }

        public bool ConfirmedExplicitSilence { get; private set; }

        public FrameworkBgmActivityPolicy CurrentActivityPolicy { get; private set; } = FrameworkBgmActivityPolicy.UseOwnOrRoute;

        public bool HasActiveActivityBgmBinding { get; private set; }

        public bool CurrentEffectiveIsExplicitSilence { get; private set; }

        public FrameworkBgmOperationResult LastOperationResult { get; private set; }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _injectionRuntime ??= new FrameworkBgmDirectorInjectionRuntime(this);
        }

        private void OnDisable()
        {
            _injectionRuntime?.Dispose();
            _injectionRuntime = null;
        }

        public FrameworkBgmOperationResult SetRouteBgm(AudioBgmCueAsset cue)
        {
            return SetRouteBgm(
                cue,
                cue != null
                    ? FrameworkBgmRoutePolicy.PlayOwn
                    : FrameworkBgmRoutePolicy.PreserveCurrent,
                false);
        }

        public FrameworkBgmOperationResult SetRouteBgm(AudioBgmCueAsset cue, bool deferRefreshForStartupActivity)
        {
            return SetRouteBgm(
                cue,
                cue != null
                    ? FrameworkBgmRoutePolicy.PlayOwn
                    : FrameworkBgmRoutePolicy.PreserveCurrent,
                deferRefreshForStartupActivity);
        }

        public FrameworkBgmOperationResult SetRouteBgm(
            AudioBgmCueAsset cue,
            FrameworkBgmRoutePolicy policy)
        {
            return SetRouteBgm(cue, policy, false);
        }

        public FrameworkBgmOperationResult SetRouteBgm(
            AudioBgmCueAsset cue,
            FrameworkBgmRoutePolicy policy,
            bool deferRefreshForStartupActivity)
        {
            CurrentRoutePolicy = NormalizeRoutePolicy(policy);
            CurrentRouteBgm = CurrentRoutePolicy == FrameworkBgmRoutePolicy.PlayOwn
                ? cue
                : null;
            CurrentActivityBgm = null;
            RetainedActivityBgmForCurrentRoute = null;
            HasActiveActivityBgmBinding = false;
            CurrentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
            _currentRouteIntent = ResolveRouteIntent(CurrentRouteBgm, CurrentRoutePolicy);
            _pendingIntent = _currentRouteIntent;
            _awaitingStartupActivityEntry = deferRefreshForStartupActivity;

            Trace(
                "Route BGM intent set.",
                LogFields.Of(
                    LogFields.Field("routePolicy", CurrentRoutePolicy),
                    LogFields.Field("routeBgm", FormatCue(CurrentRouteBgm)),
                    LogFields.Field("intent", _pendingIntent.Kind),
                    LogFields.Field("deferRefreshForStartupActivity", deferRefreshForStartupActivity)));

            if (deferRefreshForStartupActivity)
            {
                return RecordNoChange(
                    OperationForIntent(_currentRouteIntent),
                    _currentRouteIntent.Cue,
                    _currentRouteIntent.Kind == BgmIntentKind.Silence,
                    "BGM intent refresh deferred for Startup Activity.");
            }

            return Refresh();
        }

        public FrameworkBgmOperationResult ClearRouteBgm(AudioBgmCueAsset cue)
        {
            return ClearRouteBgm(
                cue,
                cue != null
                    ? FrameworkBgmRoutePolicy.PlayOwn
                    : FrameworkBgmRoutePolicy.PreserveCurrent);
        }

        public FrameworkBgmOperationResult ClearRouteBgm(
            AudioBgmCueAsset cue,
            FrameworkBgmRoutePolicy policy)
        {
            FrameworkBgmRoutePolicy normalizedPolicy = NormalizeRoutePolicy(policy);
            if (normalizedPolicy != CurrentRoutePolicy
                || (normalizedPolicy == FrameworkBgmRoutePolicy.PlayOwn
                    && !ReferenceEquals(CurrentRouteBgm, cue)))
            {
                Trace(
                    "Route BGM clear ignored as stale.",
                    LogFields.Of(
                        LogFields.Field("requestedPolicy", normalizedPolicy),
                        LogFields.Field("currentRoutePolicy", CurrentRoutePolicy),
                        LogFields.Field("requested", FormatCue(cue)),
                        LogFields.Field("currentRouteBgm", FormatCue(CurrentRouteBgm))));
                return RecordPreservedPresentation("Stale Route BGM clear ignored.");
            }

            CurrentRouteBgm = null;
            CurrentRoutePolicy = FrameworkBgmRoutePolicy.PreserveCurrent;
            _currentRouteIntent = BgmIntent.None("route-owner-exit-no-request");
            CurrentActivityBgm = null;
            RetainedActivityBgmForCurrentRoute = null;
            HasActiveActivityBgmBinding = false;
            CurrentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
            _pendingIntent = _currentRouteIntent;
            _awaitingStartupActivityEntry = false;

            Trace("Route BGM owner cleared. Confirmed BGM presentation is preserved.");
            return RecordPreservedPresentation("Route owner exit does not mutate confirmed BGM.");
        }

        public FrameworkBgmOperationResult SetActivityBgm(AudioBgmCueAsset cue, FrameworkBgmActivityPolicy policy)
        {
            _awaitingStartupActivityEntry = false;
            HasActiveActivityBgmBinding = true;
            CurrentActivityPolicy = NormalizeActivityPolicy(policy);
            CurrentActivityBgm = cue;

            switch (CurrentActivityPolicy)
            {
                case FrameworkBgmActivityPolicy.Silence:
                    CurrentActivityBgm = null;
                    RetainedActivityBgmForCurrentRoute = null;
                    _pendingIntent = BgmIntent.Silence("activity-policy-silence");
                    Trace("Activity BGM explicit Silence intent applied.");
                    return Refresh();

                case FrameworkBgmActivityPolicy.UseRoute:
                    _pendingIntent = _currentRouteIntent.WithReason("activity-policy-use-route");
                    Trace(
                        "Activity BGM policy UseRoute evaluated.",
                        LogFields.Of(
                            LogFields.Field("routePolicy", CurrentRoutePolicy),
                            LogFields.Field("routeBgm", FormatCue(CurrentRouteBgm)),
                            LogFields.Field("intent", _pendingIntent.Kind)));
                    return Refresh();

                case FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent:
                    _pendingIntent = cue != null
                        ? BgmIntent.Play(cue, "activity-own")
                        : BgmIntent.None("activity-preserve-current");
                    Trace(
                        "Activity BGM preserve-current policy evaluated.",
                        LogFields.Of(
                            LogFields.Field("activityBgm", FormatCue(cue)),
                            LogFields.Field("retainedActivityBgm", FormatCue(RetainedActivityBgmForCurrentRoute))));
                    return Refresh();

                default:
                    if (cue != null)
                    {
                        _pendingIntent = BgmIntent.Play(cue, "activity-own");
                    }
                    else
                    {
                        _pendingIntent = _currentRouteIntent.WithReason("activity-fallback-route");
                    }

                    Trace(
                        "Activity BGM intent evaluated.",
                        LogFields.Of(
                            LogFields.Field("activityBgm", FormatCue(cue)),
                            LogFields.Field("policy", CurrentActivityPolicy),
                            LogFields.Field("routePolicy", CurrentRoutePolicy),
                            LogFields.Field("routeBgm", FormatCue(CurrentRouteBgm)),
                            LogFields.Field("intent", _pendingIntent.Kind)));
                    return Refresh();
            }
        }

        public FrameworkBgmOperationResult ClearActivityBgm(AudioBgmCueAsset cue)
        {
            return ClearActivityBgm(cue, false);
        }

        public FrameworkBgmOperationResult ClearActivityBgm(AudioBgmCueAsset cue, bool deferRefreshForActivityTransition)
        {
            if (CurrentActivityBgm != null && cue != null && !ReferenceEquals(CurrentActivityBgm, cue))
            {
                Trace(
                    "Activity BGM clear ignored as stale.",
                    LogFields.Of(
                        LogFields.Field("requested", FormatCue(cue)),
                        LogFields.Field("currentActivityBgm", FormatCue(CurrentActivityBgm))));
                return RecordPreservedPresentation("Stale Activity BGM clear ignored.");
            }

            CurrentActivityBgm = null;
            HasActiveActivityBgmBinding = false;
            CurrentActivityPolicy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
            _pendingIntent = BgmIntent.None("activity-owner-exit-no-request");

            Trace(
                "Activity BGM owner cleared. Confirmed BGM presentation is preserved.",
                LogFields.Of(
                    LogFields.Field("retainedActivityBgm", FormatCue(RetainedActivityBgmForCurrentRoute)),
                    LogFields.Field("deferRefresh", deferRefreshForActivityTransition)));

            return RecordPreservedPresentation("Activity owner exit does not mutate confirmed BGM.");
        }

        void IActivityContentEntryCompletionReceiver.OnActivityContentEntryCompleted()
        {
            if (!_awaitingStartupActivityEntry)
            {
                return;
            }

            _awaitingStartupActivityEntry = false;
            if (HasActiveActivityBgmBinding)
            {
                return;
            }

            Trace("Startup Activity published no BGM intent; resolving pending Route BGM intent.");
            LastOperationResult = Refresh();
        }

        /// <summary>
        /// Applies only a pending explicit Play/Silence intent. When no explicit intent exists,
        /// Refresh is provider-idempotent and preserves the confirmed presentation.
        /// </summary>
        public FrameworkBgmOperationResult Refresh()
        {
            if (_pendingIntent.Kind == BgmIntentKind.None)
            {
                return RecordPreservedPresentation(_pendingIntent.Reason ?? "No explicit BGM request.");
            }

            CurrentEffectiveBgm = _pendingIntent.Cue;
            CurrentEffectiveIsExplicitSilence = _pendingIntent.Kind == BgmIntentKind.Silence;

            if (_pendingIntent.Kind == BgmIntentKind.Play
                && !ConfirmedExplicitSilence
                && ReferenceEquals(ConfirmedBgm, _pendingIntent.Cue))
            {
                RetainConfirmedActivityCueWhenApplicable(_pendingIntent.Cue);
                string reason = "Requested BGM is already the confirmed presentation.";
                _pendingIntent = BgmIntent.None("same-confirmed-cue");
                return RecordNoChange(FrameworkBgmOperation.Apply, ConfirmedBgm, false, reason);
            }

            if (_pendingIntent.Kind == BgmIntentKind.Silence && ConfirmedExplicitSilence)
            {
                string reason = "Explicit silence is already the confirmed presentation.";
                _pendingIntent = BgmIntent.None("already-confirmed-silence");
                return RecordNoChange(FrameworkBgmOperation.Release, null, true, reason);
            }

            if (audioRuntimeHost == null)
            {
                EnsureLogger();
                _logger.Error(
                    "BGM intent could not be applied because AudioRuntimeHost is missing.",
                    LogFields.Of(
                        LogFields.Field("requestedBgm", FormatCue(_pendingIntent.Cue)),
                        LogFields.Field("explicitSilence", _pendingIntent.Kind == BgmIntentKind.Silence),
                        LogFields.Field("reason", _pendingIntent.Reason)));

                return Record(
                    _pendingIntent.Kind == BgmIntentKind.Play ? FrameworkBgmOperation.Apply : FrameworkBgmOperation.Release,
                    FrameworkBgmOperationOutcome.OptionalAuthorityUnavailable,
                    _pendingIntent,
                    "framework_bgm_audio_runtime_host_missing",
                    ConfirmedBgm,
                    ConfirmedExplicitSilence);
            }

            BgmIntent requested = _pendingIntent;
            AudioBgmCueAsset previousConfirmedBgm = ConfirmedBgm;
            bool previousConfirmedSilence = ConfirmedExplicitSilence;
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
                    ConfirmedBgm = requested.Cue;
                    ConfirmedExplicitSilence = false;
                    RetainConfirmedActivityCueWhenApplicable(requested.Cue);
                }
                else
                {
                    ConfirmedBgm = null;
                    ConfirmedExplicitSilence = true;
                    RetainedActivityBgmForCurrentRoute = null;
                }

                _pendingIntent = BgmIntent.None("provider-confirmed");
                CurrentEffectiveBgm = ConfirmedBgm;
                CurrentEffectiveIsExplicitSilence = ConfirmedExplicitSilence;

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
            CurrentEffectiveBgm = ConfirmedBgm;
            CurrentEffectiveIsExplicitSilence = ConfirmedExplicitSilence;
            const FrameworkBgmOperation operation = FrameworkBgmOperation.Preserve;

            LastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                FrameworkBgmOperationOutcome.NoChange,
                ConfirmedBgm,
                null,
                ConfirmedBgm,
                false,
                ConfirmedExplicitSilence,
                reason);

            Debug(
                "BGM presentation preserved because no explicit provider intent exists.",
                LogFields.Of(
                    LogFields.Field("confirmedBgm", FormatCue(ConfirmedBgm)),
                    LogFields.Field("confirmedExplicitSilence", ConfirmedExplicitSilence),
                    LogFields.Field("reason", reason)));
            return LastOperationResult;
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

            LastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                FrameworkBgmOperationOutcome.NoChange,
                ConfirmedBgm,
                requestedCue,
                ConfirmedBgm,
                requestedExplicitSilence,
                ConfirmedExplicitSilence,
                reason);

            Debug(
                "BGM operation completed without provider mutation.",
                LogFields.Of(
                    LogFields.Field("operation", operation),
                    LogFields.Field("requestedBgm", FormatCue(requested.Cue)),
                    LogFields.Field("requestedExplicitSilence", requestedExplicitSilence),
                    LogFields.Field("confirmedBgm", FormatCue(ConfirmedBgm)),
                    LogFields.Field("confirmedExplicitSilence", ConfirmedExplicitSilence),
                    LogFields.Field("reason", reason)));
            return LastOperationResult;
        }

        private FrameworkBgmOperationResult Record(
            FrameworkBgmOperation operation,
            FrameworkBgmOperationOutcome outcome,
            BgmIntent requested,
            string reason,
            AudioBgmCueAsset previousConfirmed,
            bool previousConfirmedSilence)
        {
            LastOperationResult = FrameworkBgmOperationResult.Create(
                operation,
                outcome,
                previousConfirmed,
                requested.Cue,
                ConfirmedBgm,
                requested.Kind == BgmIntentKind.Silence,
                ConfirmedExplicitSilence,
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
                    LogFields.Field("confirmedBgm", FormatCue(ConfirmedBgm)),
                    LogFields.Field("confirmedExplicitSilence", ConfirmedExplicitSilence),
                    LogFields.Field("reason", reason)));
            return LastOperationResult;
        }

        private void RetainConfirmedActivityCueWhenApplicable(AudioBgmCueAsset confirmedCue)
        {
            if (confirmedCue != null
                && CurrentActivityPolicy == FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent
                && ReferenceEquals(CurrentActivityBgm, confirmedCue))
            {
                RetainedActivityBgmForCurrentRoute = confirmedCue;
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
                _logger.Trace(message, fields);
            }
        }

        private void Debug(string message, params LogField[] fields)
        {
            if (logTransitions)
            {
                EnsureLogger();
                _logger.Debug(message, fields);
            }
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<FrameworkBgmDirector>();
        }

        private static FrameworkBgmActivityPolicy NormalizeActivityPolicy(FrameworkBgmActivityPolicy policy)
        {
            return policy == FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent
                || policy == FrameworkBgmActivityPolicy.UseRoute
                || policy == FrameworkBgmActivityPolicy.Silence
                ? policy
                : FrameworkBgmActivityPolicy.UseOwnOrRoute;
        }

        private static FrameworkBgmRoutePolicy NormalizeRoutePolicy(FrameworkBgmRoutePolicy policy)
        {
            return policy == FrameworkBgmRoutePolicy.PreserveCurrent
                || policy == FrameworkBgmRoutePolicy.Silence
                ? policy
                : FrameworkBgmRoutePolicy.PlayOwn;
        }

        private static BgmIntent ResolveRouteIntent(
            AudioBgmCueAsset cue,
            FrameworkBgmRoutePolicy policy)
        {
            switch (policy)
            {
                case FrameworkBgmRoutePolicy.PreserveCurrent:
                    return BgmIntent.None("route-policy-preserve-current");

                case FrameworkBgmRoutePolicy.Silence:
                    return BgmIntent.Silence("route-policy-silence");

                default:
                    return BgmIntent.Play(cue, "route-policy-play-own");
            }
        }

        private static FrameworkBgmOperation OperationForIntent(BgmIntent intent)
        {
            return intent.Kind == BgmIntentKind.Play
                ? FrameworkBgmOperation.Apply
                : intent.Kind == BgmIntentKind.Silence
                    ? FrameworkBgmOperation.Release
                    : FrameworkBgmOperation.Preserve;
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

            internal BgmIntent WithReason(string reason)
            {
                return new BgmIntent(Kind, Cue, reason);
            }
        }
    }
}
