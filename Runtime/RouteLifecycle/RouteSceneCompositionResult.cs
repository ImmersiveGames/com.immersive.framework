using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Immutable evidence record produced after a Route scene composition attempt.
    /// F6E records evidence produced by primary/additive route scene composition. It does not authorize release/unload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6E Route scene composition result; additive execution evidence only, no release/unload side effects.")]
    internal readonly struct RouteSceneCompositionResult
    {
        private readonly RouteSceneCompositionResultEntry[] _entries;

        public RouteSceneCompositionResult(
            RouteSceneCompositionPlan plan,
            IReadOnlyList<RouteSceneCompositionResultEntry> entries,
            RouteSceneCompositionStatus status,
            SceneLifecycleLoadResult primarySceneLoadResult,
            string activeSceneName,
            string activeScenePath,
            string source,
            string reason,
            string message)
        {
            Plan = plan;
            Status = status;
            PrimarySceneLoadResult = primarySceneLoadResult;
            ActiveSceneName = Normalize(activeSceneName);
            ActiveScenePath = Normalize(activeScenePath);
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<RouteSceneCompositionResultEntry>();
            }
            else
            {
                _entries = new RouteSceneCompositionResultEntry[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    _entries[i] = entries[i];
                }
            }
        }

        public RouteSceneCompositionPlan Plan { get; }

        public RouteAsset Route => Plan.Route;

        public string RouteOwnerId => Plan.RouteOwnerId;

        public string RouteOwnerName => Plan.RouteOwnerName;

        public RouteSceneCompositionStatus Status { get; }

        public SceneLifecycleLoadResult PrimarySceneLoadResult { get; }

        public IReadOnlyList<RouteSceneCompositionResultEntry> Entries => _entries ?? Array.Empty<RouteSceneCompositionResultEntry>();

        public RouteSceneActiveScenePolicy ActiveScenePolicy => Plan.ActiveScenePolicy;

        public string ActiveSceneName { get; }

        public string ActiveScenePath { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasRoute => Plan.HasRoute;

        public bool Succeeded => Status is RouteSceneCompositionStatus.Succeeded or RouteSceneCompositionStatus.SucceededWithIssues;

        public bool Failed => Status == RouteSceneCompositionStatus.Failed;

        public bool NotExecuted => Status == RouteSceneCompositionStatus.NotExecuted;

        public bool HasActiveScene => !string.IsNullOrWhiteSpace(ActiveSceneName)
            || !string.IsNullOrWhiteSpace(ActiveScenePath);

        public int EntryCount => _entries?.Length ?? 0;

        public int LoadedCount => CountByStatus(RouteSceneCompositionEntryStatus.Loaded)
            + CountByStatus(RouteSceneCompositionEntryStatus.AlreadyLoaded);

        public int AlreadyLoadedCount => CountByStatus(RouteSceneCompositionEntryStatus.AlreadyLoaded);

        public int FailedCount => CountByStatus(RouteSceneCompositionEntryStatus.Failed);

        public int SkippedCount => CountByStatus(RouteSceneCompositionEntryStatus.Skipped);

        public int NotExecutedCount => CountByStatus(RouteSceneCompositionEntryStatus.NotExecuted);

        public int IssueCount => CountIssues(false);

        public int BlockingIssueCount => CountIssues(true);

        public bool HasIssues => IssueCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public int OwnedLoadedCount => CountOwnedLoaded();

        public string ToDiagnosticString()
        {
            string routeName = RouteOwnerName.ToDiagnosticText("<missing>");
            string activeScene = !string.IsNullOrWhiteSpace(ActiveSceneName) ? ActiveSceneName : ActiveScenePath;
            if (string.IsNullOrWhiteSpace(activeScene))
            {
                activeScene = "<none>";
            }

            var builder = new StringBuilder();
            builder.Append($"Route Scene Composition Result route='{routeName}' owner='{RouteOwnerId}' status='{Status}' entries='{EntryCount}' loaded='{LoadedCount}' alreadyLoaded='{AlreadyLoadedCount}' failed='{FailedCount}' skipped='{SkippedCount}' notExecuted='{NotExecutedCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' ownedLoaded='{OwnedLoadedCount}' activeScenePolicy='{ActiveScenePolicy}' activeScene='{activeScene}' source='{Source}' reason='{Reason}' message='{Message}' details=[");
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

        public static RouteSceneCompositionResult NotExecutedResult(
            RouteSceneCompositionPlan plan,
            string source,
            string reason)
        {
            IReadOnlyList<RouteSceneCompositionResultEntry> entries = CreateNotExecutedEntries(plan, "Route scene composition execution has not started.");
            return new RouteSceneCompositionResult(
                plan,
                entries,
                RouteSceneCompositionStatus.NotExecuted,
                default,
                string.Empty,
                string.Empty,
                source,
                reason,
                "Route scene composition result was created without execution.");
        }

        public static RouteSceneCompositionResult FromPrimarySceneLoadResult(
            RouteSceneCompositionPlan plan,
            SceneLifecycleLoadResult primarySceneLoadResult,
            string source,
            string reason)
        {
            var entries = new List<RouteSceneCompositionResultEntry>(plan.EntryCount);
            if (primarySceneLoadResult.Loaded)
            {
                entries.Add(RouteSceneCompositionResultEntry.LoadedEntry(
                    plan.PrimaryScene,
                    primarySceneLoadResult.AlreadyLoaded,
                    plan.ActiveScenePolicy == RouteSceneActiveScenePolicy.PrimarySceneActive,
                    primarySceneLoadResult.Message));
            }
            else
            {
                entries.Add(RouteSceneCompositionResultEntry.FailedEntry(
                    plan.PrimaryScene,
                    true,
                    primarySceneLoadResult.Message));
            }

            for (int i = 0; i < plan.AdditionalScenes.Count; i++)
            {
                entries.Add(RouteSceneCompositionResultEntry.NotExecutedEntry(
                    plan.AdditionalScenes[i],
                    "Additive scene execution was not requested by this primary-scene-only result."));
            }

            var status = primarySceneLoadResult.Loaded
                ? plan.HasAdditionalScenes ? RouteSceneCompositionStatus.SucceededWithIssues : RouteSceneCompositionStatus.Succeeded
                : RouteSceneCompositionStatus.Failed;
            string message = primarySceneLoadResult.Loaded
                ? $"Route scene composition recorded primary scene result. additiveExecution='NotRequested' additionalScenes='{plan.AdditionalSceneCount}'."
                : "Route scene composition failed while resolving the primary scene.";

            return new RouteSceneCompositionResult(
                plan,
                entries,
                status,
                primarySceneLoadResult,
                primarySceneLoadResult.Loaded ? primarySceneLoadResult.SceneName : string.Empty,
                primarySceneLoadResult.Loaded ? primarySceneLoadResult.ScenePath : string.Empty,
                source,
                reason,
                message);
        }


        public static RouteSceneCompositionResult ExecutedResult(
            RouteSceneCompositionPlan plan,
            IReadOnlyList<RouteSceneCompositionResultEntry> entries,
            SceneLifecycleLoadResult primarySceneLoadResult,
            string source,
            string reason)
        {
            var status = DetermineStatus(entries);
            string activeSceneName = primarySceneLoadResult.Loaded ? primarySceneLoadResult.SceneName : string.Empty;
            string activeScenePath = primarySceneLoadResult.Loaded ? primarySceneLoadResult.ScenePath : string.Empty;
            string message = BuildExecutedMessage(plan, entries, status);

            return new RouteSceneCompositionResult(
                plan,
                entries,
                status,
                primarySceneLoadResult,
                activeSceneName,
                activeScenePath,
                source,
                reason,
                message);
        }

        private static RouteSceneCompositionStatus DetermineStatus(IReadOnlyList<RouteSceneCompositionResultEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return RouteSceneCompositionStatus.NotExecuted;
            }

            bool hasIssue = false;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.BlocksComposition)
                {
                    return RouteSceneCompositionStatus.Failed;
                }

                if (entry.HasIssue)
                {
                    hasIssue = true;
                }
            }

            return hasIssue
                ? RouteSceneCompositionStatus.SucceededWithIssues
                : RouteSceneCompositionStatus.Succeeded;
        }

        private static string BuildExecutedMessage(
            RouteSceneCompositionPlan plan,
            IReadOnlyList<RouteSceneCompositionResultEntry> entries,
            RouteSceneCompositionStatus status)
        {
            int entryCount = entries?.Count ?? 0;
            int loaded = CountByStatus(entries, RouteSceneCompositionEntryStatus.Loaded)
                + CountByStatus(entries, RouteSceneCompositionEntryStatus.AlreadyLoaded);
            int failed = CountByStatus(entries, RouteSceneCompositionEntryStatus.Failed);
            int skipped = CountByStatus(entries, RouteSceneCompositionEntryStatus.Skipped);
            int notExecuted = CountByStatus(entries, RouteSceneCompositionEntryStatus.NotExecuted);
            int issues = CountIssues(entries, false);
            int blockingIssues = CountIssues(entries, true);
            string routeName = plan.RouteOwnerName.ToDiagnosticText("<missing>");

            return $"Route scene composition executed for Route '{routeName}'. status='{status}' entries='{entryCount}' loaded='{loaded}' failed='{failed}' skipped='{skipped}' notExecuted='{notExecuted}' issues='{issues}' blockingIssues='{blockingIssues}' additiveScenes='{plan.AdditionalSceneCount}'.";
        }

        private static int CountByStatus(
            IReadOnlyList<RouteSceneCompositionResultEntry> entries,
            RouteSceneCompositionEntryStatus status)
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

        private static int CountIssues(
            IReadOnlyList<RouteSceneCompositionResultEntry> entries,
            bool blockingOnly)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (blockingOnly)
                {
                    if (entry.BlocksComposition)
                    {
                        count++;
                    }

                    continue;
                }

                if (entry.HasIssue)
                {
                    count++;
                }
            }

            return count;
        }

        private static IReadOnlyList<RouteSceneCompositionResultEntry> CreateNotExecutedEntries(
            RouteSceneCompositionPlan plan,
            string message)
        {
            var entries = new List<RouteSceneCompositionResultEntry>(plan.EntryCount)
            {
                RouteSceneCompositionResultEntry.NotExecutedEntry(plan.PrimaryScene, message)
            };

            for (int i = 0; i < plan.AdditionalScenes.Count; i++)
            {
                entries.Add(RouteSceneCompositionResultEntry.NotExecutedEntry(plan.AdditionalScenes[i], message));
            }

            return entries;
        }

        private int CountByStatus(RouteSceneCompositionEntryStatus status)
        {
            if (_entries == null || _entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountIssues(bool blockingOnly)
        {
            if (_entries == null || _entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (blockingOnly)
                {
                    if (entry.BlocksComposition)
                    {
                        count++;
                    }

                    continue;
                }

                if (entry.HasIssue)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountOwnedLoaded()
        {
            if (_entries == null || _entries.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].IsOwnedLoaded)
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
