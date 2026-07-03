using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Explicit typed snapshot for the current Activity scope.
    /// This is Activity Flow state data, not a service locator, loader, materializer, readiness gate or release executor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Activity runtime state boundary introduced by F4A; not game-facing API.")]
    internal readonly struct ActivityRuntimeState
    {
        public ActivityRuntimeState(
            ActivityRuntimeStatus status,
            ActivityAsset activity,
            FrameworkIdentityKey activityIdentity,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            Status = status;
            Activity = activity;
            ActivityIdentity = activityIdentity;
            PreviousActivity = previousActivity;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public ActivityRuntimeStatus Status { get; }

        public ActivityAsset Activity { get; }

        public FrameworkIdentityKey ActivityIdentity { get; }

        public ActivityAsset PreviousActivity { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsNone => Status == ActivityRuntimeStatus.None;

        public bool IsActive => Status == ActivityRuntimeStatus.Active && Activity != null;

        public bool IsTransitioning => Status == ActivityRuntimeStatus.Transitioning;

        public bool HasActivity => Activity != null;

        public bool HasPreviousActivity => PreviousActivity != null;

        public bool HasIdentity => ActivityIdentity.IsValid;

        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string DiagnosticIdentity => HasIdentity ? ActivityIdentity.StableText : string.Empty;

        public string DiagnosticStatus => Status.ToString();

        public static ActivityRuntimeState Empty()
        {
            return default;
        }

        public static ActivityRuntimeState None(ActivityAsset previousActivity, string source, string reason)
        {
            return new ActivityRuntimeState(
                ActivityRuntimeStatus.None,
                null,
                default,
                previousActivity,
                source,
                reason);
        }

        public static ActivityRuntimeState ActiveWith(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return new ActivityRuntimeState(
                ActivityRuntimeStatus.Active,
                activity,
                CreateActivityIdentity(activity),
                previousActivity,
                source,
                reason);
        }

        private static FrameworkIdentityKey CreateActivityIdentity(ActivityAsset activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (string.IsNullOrWhiteSpace(activity.ActivityName))
            {
                throw new ArgumentException("Activity identity requires a non-empty Activity name.", nameof(activity));
            }

            return FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, activity.ActivityName);
        }
    }
}
