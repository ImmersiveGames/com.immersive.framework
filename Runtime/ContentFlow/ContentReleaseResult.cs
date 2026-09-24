using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Immutable release result for one content release plan.
    /// F6F introduces structured result evidence without executing physical release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release result model; execution starts in a later cut.")]
    internal readonly struct ContentReleaseResult
    {
        private readonly ContentReleaseResultEntry[] _entries;

        public ContentReleaseResult(
            ContentReleasePlan plan,
            IReadOnlyList<ContentReleaseResultEntry> entries,
            ContentReleaseStatus status,
            bool completed,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ContentReleaseStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Release result status must be explicit.");
            }

            Plan = plan;
            Status = status;
            Completed = completed;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<ContentReleaseResultEntry>();
            }
            else
            {
                _entries = new ContentReleaseResultEntry[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    _entries[i] = entries[i];
                }
            }
        }

        public ContentReleasePlan Plan { get; }

        public ContentReleaseStatus Status { get; }

        public bool Completed { get; }

        public FrameworkContentScope Scope => Plan.Scope;

        public string OwnerId => Plan.OwnerId;

        public string OwnerName => Plan.OwnerName;

        public IReadOnlyList<ContentReleaseResultEntry> Entries => _entries ?? Array.Empty<ContentReleaseResultEntry>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int PlannedCount => Plan.EntryCount;

        public int ResultCount => _entries?.Length ?? 0;

        public int ReleasedCount => CountByStatus(ContentReleaseEntryStatus.Released);

        public int SkippedCount => CountByStatus(ContentReleaseEntryStatus.Skipped);

        public int FailedCount => CountByStatus(ContentReleaseEntryStatus.Failed);

        public int NotExecutedCount => CountByStatus(ContentReleaseEntryStatus.NotExecuted);

        public int IssueCount => CountIssues(false);

        public int BlockingIssueCount => CountIssues(true);

        public bool HasIssues => IssueCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool Succeeded => Status is ContentReleaseStatus.Succeeded or ContentReleaseStatus.SucceededWithIssues;

        public bool Failed => Status == ContentReleaseStatus.Failed;

        public bool NotExecuted => Status == ContentReleaseStatus.NotExecuted;

        public static ContentReleaseResult NotExecutedResult(
            ContentReleasePlan plan,
            string source,
            string reason)
        {
            var resultEntries = new List<ContentReleaseResultEntry>(plan.EntryCount);
            for (int i = 0; i < plan.Entries.Count; i++)
            {
                resultEntries.Add(ContentReleaseResultEntry.NotExecutedEntry(
                    plan.Entries[i],
                    "Content release execution has not started."));
            }

            return new ContentReleaseResult(
                plan,
                resultEntries,
                ContentReleaseStatus.NotExecuted,
                false,
                source,
                reason,
                "Content release result was created without execution.");
        }

        public string ToDiagnosticString()
        {
            string owner = !string.IsNullOrWhiteSpace(OwnerName) ? OwnerName : OwnerId;
            if (string.IsNullOrWhiteSpace(owner))
            {
                owner = "<missing>";
            }

            string message = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"Content Release Result scope='{Scope}' owner='{owner}' ownerId='{OwnerId}' status='{Status}' completed='{Completed}' planned='{PlannedCount}' results='{ResultCount}' released='{ReleasedCount}' skipped='{SkippedCount}' failed='{FailedCount}' notExecuted='{NotExecutedCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' source='{Source}' reason='{Reason}' message='{message}' details=[");
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

        private int CountByStatus(ContentReleaseEntryStatus status)
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
                    if (entry.BlockingIssue)
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

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
