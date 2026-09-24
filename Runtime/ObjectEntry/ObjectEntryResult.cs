using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Result envelope for object entry contract operations.
    /// F13A uses it as a passive primitive; future cuts may return it from real entry runtimes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry result envelope introduced by F13A.")]
    public sealed class ObjectEntryResult
    {
        private static readonly IReadOnlyList<ObjectEntryIssue> EmptyIssues = Array.Empty<ObjectEntryIssue>();

        public ObjectEntryResult(ObjectEntryDescriptor descriptor, ObjectEntryResultStatus status, IEnumerable<ObjectEntryIssue> issues = null)
        {
            if (!Enum.IsDefined(typeof(ObjectEntryResultStatus), status) || status == ObjectEntryResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object entry result status must be explicit.");
            }

            Descriptor = descriptor;
            Status = status;
            Issues = issues?.ToArray() ?? EmptyIssues;
        }

        public ObjectEntryDescriptor Descriptor { get; }

        public ObjectEntryResultStatus Status { get; }

        public IReadOnlyList<ObjectEntryIssue> Issues { get; }

        public bool Succeeded => Status is ObjectEntryResultStatus.Accepted or ObjectEntryResultStatus.AcceptedWithWarnings;

        public bool Failed => Status == ObjectEntryResultStatus.Rejected;

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public string Summary => $"status='{Status}' objectEntry='{Descriptor.Id.StableText}' scope='{Descriptor.Scope}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}'";

        public static ObjectEntryResult Accepted(ObjectEntryDescriptor descriptor)
        {
            return new ObjectEntryResult(descriptor, ObjectEntryResultStatus.Accepted);
        }

        public static ObjectEntryResult AcceptedWithWarnings(ObjectEntryDescriptor descriptor, IEnumerable<ObjectEntryIssue> issues)
        {
            return new ObjectEntryResult(descriptor, ObjectEntryResultStatus.AcceptedWithWarnings, issues);
        }

        public static ObjectEntryResult Rejected(ObjectEntryDescriptor descriptor, IEnumerable<ObjectEntryIssue> issues)
        {
            return new ObjectEntryResult(descriptor, ObjectEntryResultStatus.Rejected, issues);
        }
    }
}
