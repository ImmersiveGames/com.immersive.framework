using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable aggregate result for releasing Activity-owned scenes on Activity change.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25D Activity scene release result; unloads ReleaseOnActivityChange scenes.")]
    internal readonly struct ActivitySceneReleaseResult
    {
        private readonly ActivitySceneReleaseResultEntry[] _entries;

        public ActivitySceneReleaseResult(
            ActivityAsset activity,
            IReadOnlyList<ActivitySceneReleaseResultEntry> entries,
            ActivitySceneReleaseStatus status,
            bool sideEffectsExecuted,
            string source,
            string reason,
            string message)
        {
            Activity = activity;
            Status = status;
            SideEffectsExecuted = sideEffectsExecuted;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<ActivitySceneReleaseResultEntry>();
            }
            else
            {
                _entries = new ActivitySceneReleaseResultEntry[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    _entries[i] = entries[i];
                }
            }
        }

        public ActivityAsset Activity { get; }

        public ActivitySceneReleaseStatus Status { get; }

        public IReadOnlyList<ActivitySceneReleaseResultEntry> Entries => _entries ?? Array.Empty<ActivitySceneReleaseResultEntry>();

        public bool SideEffectsExecuted { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Executed => Status != ActivitySceneReleaseStatus.Unknown;

        public int SceneCount => Entries.Count;

        public int ReleasedSceneCount => CountByStatus(ActivitySceneReleaseEntryStatus.Unloaded);

        public int SkippedSceneCount => CountByStatus(ActivitySceneReleaseEntryStatus.SkippedKeepOnActivityChange)
            + CountByStatus(ActivitySceneReleaseEntryStatus.SkippedNotLoaded);

        public int FailedSceneCount => CountByStatus(ActivitySceneReleaseEntryStatus.Failed);

        public int BlockingIssueCount => CountBlockingIssues();

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool HasSceneReleaseExecution => SideEffectsExecuted && ReleasedSceneCount > 0;

        public string DiagnosticStatus => Status.ToString();

        public static ActivitySceneReleaseResult NotRequested(
            ActivityAsset activity,
            string source,
            string reason,
            string message)
        {
            return new ActivitySceneReleaseResult(
                activity,
                Array.Empty<ActivitySceneReleaseResultEntry>(),
                ActivitySceneReleaseStatus.NotRequested,
                false,
                source,
                reason,
                message);
        }

        public static ActivitySceneReleaseResult FromEntries(
            ActivityAsset activity,
            IReadOnlyList<ActivitySceneReleaseResultEntry> entries,
            string source,
            string reason)
        {
            var status = DetermineStatus(entries);
            int released = CountByStatus(entries, ActivitySceneReleaseEntryStatus.Unloaded);
            int failed = CountByStatus(entries, ActivitySceneReleaseEntryStatus.Failed);
            int skipped = CountByStatus(entries, ActivitySceneReleaseEntryStatus.SkippedKeepOnActivityChange)
                + CountByStatus(entries, ActivitySceneReleaseEntryStatus.SkippedNotLoaded);
            string activityName = activity.ToDiagnosticText(x => x.ActivityName);
            return new ActivitySceneReleaseResult(
                activity,
                entries,
                status,
                released > 0,
                source,
                reason,
                $"Activity scene release executed for Activity '{activityName}'. status='{status}' scenes='{entries?.Count ?? 0}' released='{released}' failed='{failed}' skipped='{skipped}'.");
        }

        public string ToDiagnosticString()
        {
            string activityName = Activity.ToDiagnosticText(x => x.ActivityName);
            var builder = new StringBuilder();
            builder.Append($"Activity Scene Release Result activity='{activityName}' status='{Status}' scenes='{SceneCount}' released='{ReleasedSceneCount}' failed='{FailedSceneCount}' skipped='{SkippedSceneCount}' blockingIssues='{BlockingIssueCount}' sideEffects='{SideEffectsExecuted}' message='{Message}' details=[");

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

        private static ActivitySceneReleaseStatus DetermineStatus(IReadOnlyList<ActivitySceneReleaseResultEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return ActivitySceneReleaseStatus.NotRequested;
            }

            bool hasIssue = false;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.BlocksRelease)
                {
                    return ActivitySceneReleaseStatus.Failed;
                }

                if (entry.HasIssue)
                {
                    hasIssue = true;
                }
            }

            return hasIssue
                ? ActivitySceneReleaseStatus.SucceededWithIssues
                : ActivitySceneReleaseStatus.Succeeded;
        }

        private int CountByStatus(ActivitySceneReleaseEntryStatus status)
        {
            return CountByStatus(Entries, status);
        }

        private static int CountByStatus(IReadOnlyList<ActivitySceneReleaseResultEntry> entries, ActivitySceneReleaseEntryStatus status)
        {
            if (entries == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountBlockingIssues()
        {
            int count = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].BlocksRelease)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
