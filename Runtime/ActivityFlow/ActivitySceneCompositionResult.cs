using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Result produced from an Activity scene composition plan.
    /// F25C can represent both planning-only evidence and additive load execution evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25C Activity scene composition result; additive scene execution, release deferred.")]
    internal readonly struct ActivitySceneCompositionResult
    {
        private readonly ActivitySceneCompositionResultEntry[] _entries;

        public ActivitySceneCompositionResult(
            ActivitySceneCompositionPlan plan,
            IReadOnlyList<ActivitySceneCompositionResultEntry> entries,
            ActivitySceneCompositionStatus status,
            bool sideEffectsExecuted,
            string source,
            string reason,
            string message)
        {
            Plan = plan;
            Status = status;
            SideEffectsExecuted = sideEffectsExecuted;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<ActivitySceneCompositionResultEntry>();
            }
            else
            {
                _entries = new ActivitySceneCompositionResultEntry[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    _entries[i] = entries[i];
                }
            }
        }

        public ActivitySceneCompositionPlan Plan { get; }

        public ActivityAsset Activity => Plan.Activity;

        public ActivityContentProfileAsset Profile => Plan.Profile;

        public string ProfileId => Plan.ProfileId;

        public string ActivityOwnerId => Plan.ActivityOwnerId;

        public string ActivityOwnerName => Plan.ActivityOwnerName;

        public ActivitySceneCompositionStatus Status { get; }

        public bool SideEffectsExecuted { get; }

        public IReadOnlyList<ActivitySceneCompositionResultEntry> Entries => _entries ?? Array.Empty<ActivitySceneCompositionResultEntry>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Executed => Status != ActivitySceneCompositionStatus.Unknown;

        public bool HasProfile => Plan.HasProfile;

        public bool HasScenes => SceneCount > 0;

        public int SceneCount => Entries.Count;

        public int RequiredSceneCount => Plan.RequiredSceneCount;

        public int OptionalSceneCount => Plan.OptionalSceneCount;

        public int ExecutionReadySceneCount => Plan.ExecutionReadySceneCount;

        public int LoadedSceneCount => CountByStatus(ActivitySceneCompositionEntryStatus.Loaded);

        public int AlreadyLoadedSceneCount => CountByStatus(ActivitySceneCompositionEntryStatus.AlreadyLoaded);

        public int FailedSceneCount => CountByStatus(ActivitySceneCompositionEntryStatus.Failed);

        public int SkippedSceneCount => CountByStatus(ActivitySceneCompositionEntryStatus.Skipped)
            + CountByStatus(ActivitySceneCompositionEntryStatus.NotExecutionReady);

        public int BlockingIssueCount => CountBlockingIssues();

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool HasSceneLoadExecution => SideEffectsExecuted && LoadedSceneCount > 0;

        public string DiagnosticStatus => Status.ToString();

        public static ActivitySceneCompositionResult FromPlan(ActivitySceneCompositionPlan plan, string source, string reason)
        {
            if (!plan.HasActivity)
            {
                return NotRequested(
                    plan,
                    source,
                    reason,
                    "Activity scene composition planning was not requested because Activity is missing.");
            }

            if (!plan.HasProfile)
            {
                return NotRequested(
                    plan,
                    source,
                    reason,
                    "Activity scene composition planning skipped because Activity has no Activity Content Profile.");
            }

            if (!plan.HasScenes)
            {
                return NotRequested(
                    plan,
                    source,
                    reason,
                    "Activity scene composition planning skipped because Activity Content Profile has no scenes.");
            }

            var entries = new List<ActivitySceneCompositionResultEntry>(plan.SceneCount);
            for (int i = 0; i < plan.Scenes.Count; i++)
            {
                var entry = plan.Scenes[i];
                entries.Add(entry.IsExecutionReady
                    ? ActivitySceneCompositionResultEntry.PlannedEntry(entry)
                    : ActivitySceneCompositionResultEntry.NotExecutionReadyEntry(entry));
            }

            var status = EntriesHaveBlockingIssues(entries)
                ? ActivitySceneCompositionStatus.PlannedWithIssues
                : ActivitySceneCompositionStatus.Planned;
            string message = status == ActivitySceneCompositionStatus.Planned
                ? "Activity scene composition plan recorded. Scene loading has not executed yet."
                : "Activity scene composition plan recorded with blocking declaration issues. Scene loading has not executed yet.";

            return new ActivitySceneCompositionResult(
                plan,
                entries,
                status,
                false,
                source,
                reason,
                message);
        }

        public static ActivitySceneCompositionResult ExecutedResult(
            ActivitySceneCompositionPlan plan,
            IReadOnlyList<ActivitySceneCompositionResultEntry> entries,
            string source,
            string reason)
        {
            var status = DetermineExecutedStatus(entries);
            bool sideEffectsExecuted = CountByStatus(entries, ActivitySceneCompositionEntryStatus.Loaded) > 0;
            return new ActivitySceneCompositionResult(
                plan,
                entries,
                status,
                sideEffectsExecuted,
                source,
                reason,
                BuildExecutedMessage(plan, entries, status));
        }

        public string ToDiagnosticString()
        {
            string activityName = ActivityOwnerName.ToDiagnosticText("<missing>");
            var builder = new StringBuilder();
            builder.Append($"Activity Scene Composition Result activity='{activityName}' profile='{ProfileId}' status='{Status}' scenes='{SceneCount}' required='{RequiredSceneCount}' optional='{OptionalSceneCount}' executionReady='{ExecutionReadySceneCount}' loaded='{LoadedSceneCount}' alreadyLoaded='{AlreadyLoadedSceneCount}' failed='{FailedSceneCount}' skipped='{SkippedSceneCount}' blockingIssues='{BlockingIssueCount}' sideEffects='{SideEffectsExecuted}' message='{Message}' details=[");

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

        private static ActivitySceneCompositionResult NotRequested(
            ActivitySceneCompositionPlan plan,
            string source,
            string reason,
            string message)
        {
            return new ActivitySceneCompositionResult(
                plan,
                Array.Empty<ActivitySceneCompositionResultEntry>(),
                ActivitySceneCompositionStatus.NotRequested,
                false,
                source,
                reason,
                message);
        }

        private static ActivitySceneCompositionStatus DetermineExecutedStatus(IReadOnlyList<ActivitySceneCompositionResultEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return ActivitySceneCompositionStatus.NotRequested;
            }

            bool hasIssue = false;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.BlocksComposition)
                {
                    return ActivitySceneCompositionStatus.Failed;
                }

                if (entry.HasIssue)
                {
                    hasIssue = true;
                }
            }

            return hasIssue
                ? ActivitySceneCompositionStatus.SucceededWithIssues
                : ActivitySceneCompositionStatus.Succeeded;
        }

        private static string BuildExecutedMessage(
            ActivitySceneCompositionPlan plan,
            IReadOnlyList<ActivitySceneCompositionResultEntry> entries,
            ActivitySceneCompositionStatus status)
        {
            string activityName = plan.ActivityOwnerName.ToDiagnosticText("<missing>");
            int entryCount = entries?.Count ?? 0;
            int loaded = CountByStatus(entries, ActivitySceneCompositionEntryStatus.Loaded);
            int alreadyLoaded = CountByStatus(entries, ActivitySceneCompositionEntryStatus.AlreadyLoaded);
            int failed = CountByStatus(entries, ActivitySceneCompositionEntryStatus.Failed);
            int skipped = CountByStatus(entries, ActivitySceneCompositionEntryStatus.Skipped)
                + CountByStatus(entries, ActivitySceneCompositionEntryStatus.NotExecutionReady);
            int blockingIssues = CountBlockingIssues(entries);
            return $"Activity scene composition executed for Activity '{activityName}'. status='{status}' scenes='{entryCount}' loaded='{loaded}' alreadyLoaded='{alreadyLoaded}' failed='{failed}' skipped='{skipped}' blockingIssues='{blockingIssues}'.";
        }

        private static bool EntriesHaveBlockingIssues(IReadOnlyList<ActivitySceneCompositionResultEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].BlocksComposition)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountByStatus(ActivitySceneCompositionEntryStatus status)
        {
            return CountByStatus(Entries, status);
        }

        private static int CountByStatus(
            IReadOnlyList<ActivitySceneCompositionResultEntry> entries,
            ActivitySceneCompositionEntryStatus status)
        {
            if (entries == null || entries.Count == 0)
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
            return CountBlockingIssues(Entries);
        }

        private static int CountBlockingIssues(IReadOnlyList<ActivitySceneCompositionResultEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].BlocksComposition)
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
