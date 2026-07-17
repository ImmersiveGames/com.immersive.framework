using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable snapshot of scene-authored content currently registered for an Activity scope.
    /// F4B records local visibility adapter content only; it does not load scenes, materialize prefabs, own release, or discover local contributions.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal Activity content set introduced by F4B.")]
    internal readonly struct ActivityContentSet
    {
        private readonly ActivityContentEntry[] _entries;

        public ActivityContentSet(
            ActivityAsset activity,
            FrameworkContentSet contentSet,
            IReadOnlyList<ActivityContentEntry> entries)
        {
            Activity = activity;
            ContentSet = contentSet;

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<ActivityContentEntry>();
                return;
            }

            _entries = new ActivityContentEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                _entries[i] = entries[i];
            }
        }

        public ActivityAsset Activity { get; }

        public FrameworkContentSet ContentSet { get; }

        public IReadOnlyList<ActivityContentEntry> Entries => _entries ?? Array.Empty<ActivityContentEntry>();

        public bool HasContent => ContentSet.HasContent;

        public int Count => ContentSet.Count;

        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        public string DiagnosticMessage
        {
            get
            {
                if (!HasContent)
                {
                    return Activity != null
                        ? $"Activity Content Set is empty for Activity '{Activity.ActivityName}'."
                        : "Activity Content Set is empty.";
                }

                return $"Activity Content Set registered {Count} handle(s). {ContentSet.ToDiagnosticString()}";
            }
        }

        public string OwnershipDiagnosticMessage
        {
            get
            {
                if (!HasContent)
                {
                    return "Activity Content Set ownership is empty.";
                }

                var builder = new StringBuilder();
                builder.Append($"Activity Content Set details=[");
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append(Entries[i].ToDiagnosticString());
                }

                builder.Append("]");
                return builder.ToString();
            }
        }

        public static ActivityContentSet Empty(ActivityAsset activity = null)
        {
            string ownerId = CreateActivityOwnerId(activity);
            string ownerName = activity != null ? activity.ActivityName : string.Empty;
            return new ActivityContentSet(
                activity,
                FrameworkContentSet.Empty(FrameworkContentScope.Activity, ownerId, ownerName),
                Array.Empty<ActivityContentEntry>());
        }

        public static ActivityContentSet FromEntries(ActivityAsset activity, IReadOnlyList<ActivityContentEntry> entries)
        {
            if (activity == null || entries == null || entries.Count == 0)
            {
                return Empty(activity);
            }

            var handles = new FrameworkContentHandle[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                handles[i] = entries[i].Handle;
            }

            return new ActivityContentSet(
                activity,
                new FrameworkContentSet(FrameworkContentScope.Activity, CreateActivityOwnerId(activity), activity.ActivityName, handles),
                entries);
        }

        internal static string CreateActivityOwnerId(ActivityAsset activity)
        {
            if (activity == null)
            {
                return string.Empty;
            }

            if (!activity.HasValidActivityId)
            {
                throw new ArgumentException("Activity content ownership requires a valid ActivityId.", nameof(activity));
            }

            return activity.ActivityId.StableText;
        }
    }
}
