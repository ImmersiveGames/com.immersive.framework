using UnityEngine;
using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.LocalContribution;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Scene-authored local visibility rule for one GameObject.
    /// Activity Flow evaluates this component against the canonical active Activity.
    /// It is not canonical Activity materialization.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Local Visibility Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityLocalVisibilityAdapter : MonoBehaviour
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

        [SerializeField]
        [Tooltip("Explicit local content id for this scene-authored Activity contribution. Required for F5 local identity. GameObject names and hierarchy paths are diagnostics only and are not used as fallback.")]
        private string localContentId = string.Empty;

        [SerializeField]
        [Tooltip("Declares whether this local Activity contribution should be treated as required by future contribution consumers. F5F records this policy but does not validate absence yet.")]
        private FrameworkContentRequiredness requiredness = FrameworkContentRequiredness.Required;

        public IReadOnlyList<ActivityAsset> Activities => activities ?? Array.Empty<ActivityAsset>();

        public ActivityVisibilityMatchMode MatchMode => matchMode;

        public ActivityVisibilityNoActivePolicy NoActiveActivityPolicy => noActiveActivityPolicy;

        public FrameworkContentRequiredness Requiredness => requiredness;

        public LocalContentScopeKind LocalScopeKind => LocalContentScopeKind.SceneAuthored;

        public string LocalContentIdText => !string.IsNullOrWhiteSpace(localContentId)
            ? localContentId.NormalizeText()
            : string.Empty;

        public bool HasExplicitLocalContentId => !string.IsNullOrWhiteSpace(localContentId);

        public bool TryGetSingleActivityOwner(out ActivityAsset singleActivity)
        {
            singleActivity = null;
            ActivityVisibilityEvaluation evaluation = EvaluateVisibility(null);
            if (!evaluation.IsValid ||
                matchMode != ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive ||
                noActiveActivityPolicy != ActivityVisibilityNoActivePolicy.Hidden ||
                activities == null || activities.Length != 1 || activities[0] == null)
            {
                return false;
            }

            singleActivity = activities[0];
            return true;
        }

        public bool TryGetLocalContentId(out LocalContentId localId)
        {
            if (!HasExplicitLocalContentId)
            {
                localId = default;
                return false;
            }

            localId = LocalContentId.From(localContentId);
            return true;
        }

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
            if (!HasExplicitLocalContentId)
            {
                return Invalid(activeActivity, "MissingLocalContentId");
            }

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
                        listedActivity.HasSameIdentity(activities[priorIndex]))
                    {
                        return Invalid(activeActivity, $"CurrentActivityDuplicateAtIndex{index}");
                    }
                }

                if (activeActivity != null && listedActivity.HasSameIdentity(activeActivity))
                {
                    matchedActivity = listedActivity;
                }
            }

            bool hasMatch = matchedActivity != null;
            if (activeActivity == null)
            {
                return new ActivityVisibilityEvaluation(
                    true, null, null, false,
                    noActiveActivityPolicy == ActivityVisibilityNoActivePolicy.Visible,
                    noActiveActivityPolicy == ActivityVisibilityNoActivePolicy.Visible
                        ? "NoActiveActivityVisible"
                        : "NoActiveActivityHidden");
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

        private ActivityVisibilityEvaluation Invalid(ActivityAsset activeActivity, string reason)
        {
            return new ActivityVisibilityEvaluation(
                false, activeActivity, null, false, false, reason);
        }
    }
}
