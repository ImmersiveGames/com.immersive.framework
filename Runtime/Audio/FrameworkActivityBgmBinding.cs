using Immersive.Audio.Authoring;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
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
        private const string LogPrefix = "[FRAMEWORK_BGM]";

        [SerializeField] private ActivityAsset assignedActivity;
        [SerializeField] private AudioBgmCueAsset activityBgm;
        [SerializeField] private FrameworkBgmActivityPolicy policy = FrameworkBgmActivityPolicy.UseOwnOrRoute;
        [SerializeField] private FrameworkBgmDirector director;

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
                Debug.LogError($"{LogPrefix} Activity BGM binding requires a FrameworkBgmDirector.", this);
                return false;
            }

            if (expectedDirector != null && !ReferenceEquals(director, expectedDirector))
            {
                Debug.LogWarning(
                    $"{LogPrefix} Startup Activity BGM binding ignored because it targets a different director. route='{routeName}' activityBgm='{FormatCue(activityBgm)}'.",
                    this);
                return false;
            }

            if (!MatchesExpectedActivity(expectedActivity, out string assignedActivityName, out bool hasActivityEvidence))
            {
                Debug.LogWarning(
                    $"{LogPrefix} Startup Activity BGM binding ignored because it does not match the Route Startup Activity. route='{routeName}' expectedActivity='{FormatActivity(expectedActivity)}' assignedActivity='{assignedActivityName}' activityBgm='{FormatCue(activityBgm)}'.",
                    this);
                return false;
            }

            if (!hasActivityEvidence)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Startup Activity BGM binding has no assigned Activity evidence. Explicit Route reference will be used. route='{routeName}' expectedActivity='{FormatActivity(expectedActivity)}' activityBgm='{FormatCue(activityBgm)}'.",
                    this);
            }

            Debug.Log(
                $"{LogPrefix} Startup Activity BGM pre-applied from explicit Route binding. route='{routeName}' activity='{FormatActivity(expectedActivity)}' activityBgm='{FormatCue(activityBgm)}' policy='{policy}'.",
                this);
            director.SetActivityBgm(activityBgm, policy);
            return true;
        }

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Activity BGM binding requires a FrameworkBgmDirector.", this);
                return;
            }

            director.SetActivityBgm(activityBgm, policy);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Activity BGM binding requires a FrameworkBgmDirector.", this);
                return;
            }

            bool deferRefreshForActivityTransition = context.NextActivity != null
                && !ReferenceEquals(context.NextActivity, context.Activity);

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
                || ReferenceEquals(resolvedActivity, expectedActivity);
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
    }
}
