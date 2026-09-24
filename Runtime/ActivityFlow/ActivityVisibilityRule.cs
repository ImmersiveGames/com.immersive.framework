using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored visibility rule for one GameObject.
    /// Activity Flow evaluates this rule against the canonical active Activity; it does not declare Activity ownership or local content identity.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Visibility Rule")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity visibility rule surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityVisibilityRule : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit Activities evaluated by the current visibility rule. Order is authored and preserved.")]
        private ActivityAsset[] activities = Array.Empty<ActivityAsset>();

        [SerializeField]
        [Tooltip("Defines whether a listed Activity makes this GameObject visible or hidden.")]
        private ActivityVisibilityMatchMode matchMode =
            ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive;

        [SerializeField]
        [Tooltip("Defines this GameObject visibility when no Activity is active.")]
        private ActivityVisibilityNoActivePolicy noActiveActivityPolicy =
            ActivityVisibilityNoActivePolicy.Hidden;

        public IReadOnlyList<ActivityAsset> Activities => activities ?? Array.Empty<ActivityAsset>();

        public ActivityVisibilityMatchMode MatchMode => matchMode;

        public ActivityVisibilityNoActivePolicy NoActiveActivityPolicy => noActiveActivityPolicy;

        internal bool IsSceneBinding => gameObject.scene.IsValid() && gameObject.scene.isLoaded;

        internal string ObjectName => gameObject.ToDiagnosticText(x => x.name, "<missing>");

        internal string SceneName => gameObject != null && gameObject.scene.IsValid()
            ? gameObject.scene.name
            : "<no-scene>";

        internal bool SetContentActive(bool active)
        {
            if (gameObject.activeSelf == active)
            {
                return false;
            }

            gameObject.SetActive(active);
            return true;
        }

        public ActivityVisibilityEvaluation EvaluateVisibility(ActivityAsset activeActivity)
        {
            if (activities == null || activities.Length == 0)
            {
                return Invalid(activeActivity, "CurrentActivitiesEmpty");
            }

            if (!IsSupported(matchMode))
            {
                return Invalid(activeActivity, "UnsupportedMatchMode");
            }

            if (!IsSupported(noActiveActivityPolicy))
            {
                return Invalid(activeActivity, "UnsupportedNoActiveActivityPolicy");
            }

            ActivityAsset matchedActivity = null;
            for (int index = 0; index < activities.Length; index++)
            {
                ActivityAsset listedActivity = activities[index];
                if (listedActivity == null)
                {
                    return Invalid(activeActivity, $"CurrentActivityNullAtIndex{index}");
                }

                if (!listedActivity.HasValidActivityId)
                {
                    return Invalid(activeActivity, $"CurrentActivityIdentityInvalidAtIndex{index}");
                }

                for (int priorIndex = 0; priorIndex < index; priorIndex++)
                {
                    if (activities[priorIndex] != null &&
                        ReferenceEquals(listedActivity, activities[priorIndex]))
                    {
                        return Invalid(activeActivity, $"CurrentActivityDuplicateAtIndex{index}");
                    }

                    if (activities[priorIndex] != null &&
                        listedActivity.ActivityId == activities[priorIndex].ActivityId)
                    {
                        return Invalid(activeActivity, $"CurrentActivityStableIdCollisionAtIndex{index}");
                    }
                }

                if (activeActivity != null && ReferenceEquals(listedActivity, activeActivity))
                {
                    matchedActivity = listedActivity;
                }
            }

            bool hasMatch = matchedActivity != null;
            if (activeActivity == null)
            {
                bool visible = noActiveActivityPolicy == ActivityVisibilityNoActivePolicy.Visible;
                return new ActivityVisibilityEvaluation(
                    true, null, null, false, visible,
                    visible ? "NoActiveActivityVisible" : "NoActiveActivityHidden");
            }

            bool desiredVisibility = matchMode == ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive
                ? hasMatch
                : !hasMatch;
            return new ActivityVisibilityEvaluation(
                true, activeActivity, matchedActivity, hasMatch,
                desiredVisibility,
                hasMatch ? "MatchedListedActivity" : "NoListedActivityMatch");
        }

        internal static bool IsSupported(ActivityVisibilityMatchMode value)
        {
            return value == ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive ||
                   value == ActivityVisibilityMatchMode.HiddenWhenAnyListedActivityIsActive;
        }

        internal static bool IsSupported(ActivityVisibilityNoActivePolicy value)
        {
            return value == ActivityVisibilityNoActivePolicy.Hidden ||
                   value == ActivityVisibilityNoActivePolicy.Visible;
        }

        private static ActivityVisibilityEvaluation Invalid(ActivityAsset activeActivity, string reason)
        {
            return new ActivityVisibilityEvaluation(false, activeActivity, null, false, false, reason);
        }
    }
}
