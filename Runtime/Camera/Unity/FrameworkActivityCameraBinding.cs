using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Activity content binding that supplies a camera rig to a FrameworkCameraDirector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Activity Camera Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public sealed class FrameworkActivityCameraBinding : ActivityContentBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_CAMERA]";

        [SerializeField] private ActivityAsset assignedActivity;
        [SerializeField] private GameObject activityCameraRig;
        [SerializeField] private FrameworkCameraActivityPolicy policy = FrameworkCameraActivityPolicy.UseOwnOrRoute;
        [SerializeField] private FrameworkCameraAnchorHost anchors;
        [SerializeField] private FrameworkCameraDirector director;

        public ActivityAsset AssignedActivity => assignedActivity;

        public GameObject ActivityCameraRig => activityCameraRig;

        public FrameworkCameraActivityPolicy Policy => policy;

        public FrameworkCameraAnchorHost Anchors => anchors;

        public FrameworkCameraDirector Director => director;

        public bool TryApplyStartupActivityCamera(
            FrameworkCameraDirector expectedDirector,
            ActivityAsset expectedActivity,
            string routeName)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Activity Camera binding requires a FrameworkCameraDirector.", this);
                return false;
            }

            if (expectedDirector != null && !ReferenceEquals(director, expectedDirector))
            {
                Debug.LogWarning(
                    $"{LogPrefix} Startup Activity Camera binding ignored because it targets a different director. route='{routeName}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
                return false;
            }

            if (!MatchesExpectedActivity(expectedActivity, out string assignedActivityName, out bool hasActivityEvidence))
            {
                Debug.LogWarning(
                    $"{LogPrefix} Startup Activity Camera binding ignored because it does not match the Route Startup Activity. route='{routeName}' expectedActivity='{FormatActivity(expectedActivity)}' assignedActivity='{assignedActivityName}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
                return false;
            }

            if (!hasActivityEvidence)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Startup Activity Camera binding has no assigned Activity evidence. Explicit Route reference will be used. route='{routeName}' expectedActivity='{FormatActivity(expectedActivity)}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
            }

            Debug.Log(
                $"{LogPrefix} Startup Activity Camera pre-applied from explicit Route binding. route='{routeName}' activity='{FormatActivity(expectedActivity)}' activityRig='{FormatRig(activityCameraRig)}' policy='{policy}'.",
                this);
            director.SetActivityCamera(activityCameraRig, policy, anchors);
            return true;
        }

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Activity Camera binding requires a FrameworkCameraDirector.", this);
                return;
            }

            director.SetActivityCamera(activityCameraRig, policy, anchors);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError($"{LogPrefix} Activity Camera binding requires a FrameworkCameraDirector.", this);
                return;
            }

            bool deferRefreshForActivityTransition = context.NextActivity != null
                && !ReferenceEquals(context.NextActivity, context.Activity);

            director.ClearActivityCamera(activityCameraRig, deferRefreshForActivityTransition);
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

        private static string FormatRig(GameObject rig)
        {
            return rig != null ? rig.name : "<none>";
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }
    }
}
