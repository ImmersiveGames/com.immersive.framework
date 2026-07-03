using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Immutable release result entry for one planned content item.
    /// F6F only creates NotExecuted evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release result entry; physical execution deferred.")]
    internal readonly struct ContentReleaseResultEntry
    {
        public ContentReleaseResultEntry(
            ContentReleasePlanEntry planEntry,
            ContentReleaseEntryStatus status,
            bool blockingIssue,
            string message)
        {
            if (!Enum.IsDefined(typeof(ContentReleaseEntryStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Release result entry status must be explicit.");
            }

            PlanEntry = planEntry;
            Status = status;
            BlockingIssue = blockingIssue;
            Message = Normalize(message);
        }

        public ContentReleasePlanEntry PlanEntry { get; }

        public ContentReleaseEntryStatus Status { get; }

        public bool BlockingIssue { get; }

        public string Message { get; }

        public FrameworkContentIdentity Identity => PlanEntry.Identity;

        public string ContentId => PlanEntry.ContentId;

        public ContentReleaseAction Action => PlanEntry.Action;

        public ContentReleaseOwnership Ownership => PlanEntry.Ownership;

        public FrameworkContentScope Scope => PlanEntry.Scope;

        public FrameworkContentKind Kind => PlanEntry.Kind;

        public FrameworkContentRequiredness Requiredness => PlanEntry.Requiredness;

        public bool Released => Status == ContentReleaseEntryStatus.Released;

        public bool Skipped => Status == ContentReleaseEntryStatus.Skipped;

        public bool Failed => Status == ContentReleaseEntryStatus.Failed;

        public bool NotExecuted => Status == ContentReleaseEntryStatus.NotExecuted;

        public bool HasIssue => Failed || BlockingIssue;

        public string ToDiagnosticString()
        {
            string message = Message.ToDiagnosticText();
            return $"status='{Status}' blockingIssue='{BlockingIssue}' message='{message}' {PlanEntry.ToDiagnosticString()}";
        }

        public static ContentReleaseResultEntry NotExecutedEntry(
            ContentReleasePlanEntry planEntry,
            string message)
        {
            return new ContentReleaseResultEntry(
                planEntry,
                ContentReleaseEntryStatus.NotExecuted,
                false,
                message);
        }

        public static ContentReleaseResultEntry ReleasedEntry(
            ContentReleasePlanEntry planEntry,
            string message)
        {
            return new ContentReleaseResultEntry(
                planEntry,
                ContentReleaseEntryStatus.Released,
                false,
                message);
        }

        public static ContentReleaseResultEntry SkippedEntry(
            ContentReleasePlanEntry planEntry,
            string message)
        {
            return new ContentReleaseResultEntry(
                planEntry,
                ContentReleaseEntryStatus.Skipped,
                false,
                message);
        }

        public static ContentReleaseResultEntry FailedEntry(
            ContentReleasePlanEntry planEntry,
            bool blockingIssue,
            string message)
        {
            return new ContentReleaseResultEntry(
                planEntry,
                ContentReleaseEntryStatus.Failed,
                blockingIssue,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
