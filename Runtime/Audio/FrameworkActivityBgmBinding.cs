using Immersive.Audio.Authoring;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Activity content binding that supplies BGM to a FrameworkBgmDirector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/Activity BGM Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F47C optional framework-owned BGM adapter.")]
    public sealed class FrameworkActivityBgmBinding : ActivityContentBehaviour
    {
        [SerializeField] private ActivityAsset assignedActivity;
        [SerializeField] private AudioBgmCueAsset activityBgm;
        [SerializeField] private FrameworkBgmActivityPolicy policy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
        [SerializeField] private FrameworkBgmDirector director;
        private FrameworkLogger logger;

        public ActivityAsset AssignedActivity => assignedActivity;

        public AudioBgmCueAsset ActivityBgm => activityBgm;

        public FrameworkBgmActivityPolicy Policy => policy;

        public FrameworkBgmDirector Director => director;

        public bool TryApplyStartupActivityBgm(
            FrameworkBgmDirector expectedDirector,
            ActivityAsset expectedActivity,
            string routeName)
        {
            if (director == null)
            {
                Error("Activity BGM binding requires a FrameworkBgmDirector.");
                return false;
            }

            if (expectedDirector != null && !ReferenceEquals(director, expectedDirector))
            {
                Warning(
                    "Startup Activity BGM binding ignored because it targets a different director.",
                    LogFields.Of(
                        LogFields.Field("route", routeName),
                        LogFields.Field("activityBgm", FormatCue(activityBgm))));
                return false;
            }

            if (!MatchesExpectedActivity(expectedActivity, out string assignedActivityName, out bool hasActivityEvidence))
            {
                Warning(
                    "Startup Activity BGM binding ignored because it does not match the Route Startup Activity.",
                    LogFields.Of(
                        LogFields.Field("route", routeName),
                        LogFields.Field("expectedActivity", FormatActivity(expectedActivity)),
                        LogFields.Field("assignedActivity", assignedActivityName),
                        LogFields.Field("activityBgm", FormatCue(activityBgm))));
                return false;
            }

            if (!hasActivityEvidence)
            {
                Warning(
                    "Startup Activity BGM binding has no assigned Activity evidence. Explicit Route reference will be used.",
                    LogFields.Of(
                        LogFields.Field("route", routeName),
                        LogFields.Field("expectedActivity", FormatActivity(expectedActivity)),
                        LogFields.Field("activityBgm", FormatCue(activityBgm))));
            }

            Debug(
                "Startup Activity BGM pre-applied from explicit Route binding.",
                LogFields.Of(
                    LogFields.Field("route", routeName),
                    LogFields.Field("activity", FormatActivity(expectedActivity)),
                    LogFields.Field("activityBgm", FormatCue(activityBgm)),
                    LogFields.Field("policy", policy)));
            director.SetActivityBgm(activityBgm, policy);
            return true;
        }

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Activity BGM binding requires a FrameworkBgmDirector.");
                return;
            }

            director.SetActivityBgm(activityBgm, policy);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Activity BGM binding requires a FrameworkBgmDirector.");
                return;
            }

            bool deferRefreshForActivityTransition = context.NextActivity != null
                && (context.Activity == null || !context.NextActivity.HasSameIdentity(context.Activity));

            director.ClearActivityBgm(activityBgm, deferRefreshForActivityTransition);
        }

        private bool MatchesExpectedActivity(
            ActivityAsset expectedActivity,
            out string assignedActivityName,
            out bool hasActivityEvidence)
        {
            ActivityAsset resolvedActivity = ResolveAssignedActivity();
            assignedActivityName = FormatActivity(resolvedActivity);
            hasActivityEvidence = resolvedActivity != null;

            return expectedActivity == null
                || resolvedActivity == null
                || resolvedActivity.HasSameIdentity(expectedActivity);
        }

        private ActivityAsset ResolveAssignedActivity()
        {
            if (assignedActivity != null)
            {
                return assignedActivity;
            }

            ActivityLocalVisibilityAdapter adapter = GetComponent<ActivityLocalVisibilityAdapter>();
            if (adapter == null)
            {
                adapter = GetComponentInParent<ActivityLocalVisibilityAdapter>();
            }

            return adapter != null ? adapter.Activity : null;
        }

        private static string FormatCue(AudioBgmCueAsset cue)
        {
            return cue != null ? cue.name : "<silence>";
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }

        private void Debug(string message, params LogField[] fields)
        {
            EnsureLogger();
            logger.Debug(message, fields);
        }

        private void Warning(string message, params LogField[] fields)
        {
            EnsureLogger();
            logger.Warning(message, fields);
        }

        private void Error(string message, params LogField[] fields)
        {
            EnsureLogger();
            logger.Error(message, fields);
        }

        private void EnsureLogger()
        {
            logger ??= FrameworkLogger.Create<FrameworkActivityBgmBinding>();
        }
    }
}
