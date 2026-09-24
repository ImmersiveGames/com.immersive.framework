using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Result for defensive stale-owner cleanup in ResetRegistry.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset registry stale cleanup result.")]
    public readonly struct ResetRegistryCleanupResult : IEquatable<ResetRegistryCleanupResult>
    {
        private readonly ResetIssue[] _issues;

        public ResetRegistryCleanupResult(
            int removedSubjects,
            int removedParticipants,
            IReadOnlyList<ResetIssue> issues)
        {
            RemovedSubjects = Math.Max(0, removedSubjects);
            RemovedParticipants = Math.Max(0, removedParticipants);
            _issues = issues == null ? Array.Empty<ResetIssue>() : issues.ToArray();
        }

        public int RemovedSubjects { get; }

        public int RemovedParticipants { get; }

        public IReadOnlyList<ResetIssue> Issues => _issues ?? Array.Empty<ResetIssue>();

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public bool RemovedAny => RemovedSubjects > 0 || RemovedParticipants > 0;

        public bool Equals(ResetRegistryCleanupResult other)
        {
            if (RemovedSubjects != other.RemovedSubjects
                || RemovedParticipants != other.RemovedParticipants
                || IssueCount != other.IssueCount)
            {
                return false;
            }

            for (int i = 0; i < IssueCount; i++)
            {
                if (!Issues[i].Equals(other.Issues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ResetRegistryCleanupResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RemovedSubjects;
                hashCode = hashCode * 397 ^ RemovedParticipants;
                for (int i = 0; i < IssueCount; i++)
                {
                    hashCode = hashCode * 397 ^ Issues[i].GetHashCode();
                }

                return hashCode;
            }
        }
    }
}
