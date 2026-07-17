using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free plan for scenes declared by an Activity Content Profile.
    /// F25B records planning evidence only; later F25 cuts consume this plan for execution and release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25B Activity scene composition plan; side-effect-free, execution and release deferred.")]
    internal readonly struct ActivitySceneCompositionPlan
    {
        private readonly ActivitySceneCompositionPlanEntry[] _scenes;

        public ActivitySceneCompositionPlan(
            ActivityAsset activity,
            ActivityContentProfileAsset profile,
            string activityOwnerId,
            string activityOwnerName,
            IReadOnlyList<ActivitySceneCompositionPlanEntry> scenes,
            string source,
            string reason)
        {
            Activity = activity;
            Profile = profile;
            ActivityOwnerId = Normalize(activityOwnerId);
            ActivityOwnerName = Normalize(activityOwnerName);
            Source = Normalize(source);
            Reason = Normalize(reason);

            if (scenes == null || scenes.Count == 0)
            {
                _scenes = Array.Empty<ActivitySceneCompositionPlanEntry>();
            }
            else
            {
                _scenes = new ActivitySceneCompositionPlanEntry[scenes.Count];
                for (int i = 0; i < scenes.Count; i++)
                {
                    _scenes[i] = scenes[i];
                }
            }
        }

        public ActivityAsset Activity { get; }

        public ActivityContentProfileAsset Profile { get; }

        public bool HasActivity => Activity != null;

        public bool HasProfile => Profile != null;

        public string ProfileId => Profile != null ? Profile.ProfileId : string.Empty;

        public string ActivityOwnerId { get; }

        public string ActivityOwnerName { get; }

        public FrameworkContentScope Scope => FrameworkContentScope.Activity;

        public IReadOnlyList<ActivitySceneCompositionPlanEntry> Scenes => _scenes ?? Array.Empty<ActivitySceneCompositionPlanEntry>();

        public string Source { get; }

        public string Reason { get; }

        public int SceneCount => _scenes?.Length ?? 0;

        public bool HasScenes => SceneCount > 0;

        public int RequiredSceneCount => CountRequiredness(FrameworkContentRequiredness.Required);

        public int OptionalSceneCount => CountRequiredness(FrameworkContentRequiredness.Optional);

        public int ExecutionReadySceneCount => CountExecutionReady();

        public int BlockingIssueCount => CountBlockingIssues();

        public bool HasExecutionBlockingPlanIssue => BlockingIssueCount > 0;

        public static ActivitySceneCompositionPlan FromActivity(ActivityAsset activity, string source, string reason)
        {
            string ownerId = CreateActivityOwnerId(activity);
            string ownerName = activity != null ? activity.ActivityName : string.Empty;
            var profile = activity != null ? activity.ActivityContentProfile : null;
            if (profile == null || profile.Scenes == null || profile.Scenes.Count == 0)
            {
                return new ActivitySceneCompositionPlan(
                    activity,
                    profile,
                    ownerId,
                    ownerName,
                    Array.Empty<ActivitySceneCompositionPlanEntry>(),
                    source,
                    reason);
            }

            var plannedScenes = new List<ActivitySceneCompositionPlanEntry>(profile.Scenes.Count);
            for (int i = 0; i < profile.Scenes.Count; i++)
            {
                plannedScenes.Add(ActivitySceneCompositionPlanEntry.FromEntry(profile.Scenes[i], i, ownerId));
            }

            return new ActivitySceneCompositionPlan(
                activity,
                profile,
                ownerId,
                ownerName,
                plannedScenes,
                source,
                reason);
        }

        public string ToDiagnosticString()
        {
            string activityName = ActivityOwnerName.ToDiagnosticText("<missing>");
            var builder = new StringBuilder();
            builder.Append($"Activity Scene Composition Plan activity='{activityName}' owner='{ActivityOwnerId}' scenes='{SceneCount}' required='{RequiredSceneCount}' optional='{OptionalSceneCount}' executionReady='{ExecutionReadySceneCount}' blockingIssues='{BlockingIssueCount}' sideEffects='False' profile='{ProfileId}' details=[");

            for (int i = 0; i < Scenes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Scenes[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static string CreateActivityOwnerId(ActivityAsset activity)
        {
            if (activity == null)
            {
                return string.Empty;
            }

            if (!activity.HasValidActivityId)
            {
                throw new ArgumentException("Activity scene ownership requires a valid ActivityId.", nameof(activity));
            }

            return activity.ActivityId.StableText;
        }

        private int CountRequiredness(FrameworkContentRequiredness requiredness)
        {
            int count = 0;
            for (int i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountExecutionReady()
        {
            int count = 0;
            for (int i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].IsExecutionReady)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountBlockingIssues()
        {
            int count = 0;
            for (int i = 0; i < Scenes.Count; i++)
            {
                var entry = Scenes[i];
                if (entry is { Requiredness: FrameworkContentRequiredness.Required, IsExecutionReady: false })
                {
                    count++;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
