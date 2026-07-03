using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Result of loaded local contribution discovery.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Local contribution discovery result introduced by F5D.")]
    internal readonly struct LocalContributionDiscoveryResult
    {
        private readonly LocalContributionDiscoveryIssue[] _issues;

        public LocalContributionDiscoveryResult(
            LocalContributionSet contributionSet,
            IReadOnlyList<LocalContributionDiscoveryIssue> issues)
        {
            ContributionSet = contributionSet;

            if (issues == null || issues.Count == 0)
            {
                _issues = Array.Empty<LocalContributionDiscoveryIssue>();
                return;
            }

            _issues = new LocalContributionDiscoveryIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++)
            {
                _issues[i] = issues[i];
            }
        }

        public LocalContributionSet ContributionSet { get; }

        public IReadOnlyList<LocalContributionDiscoveryIssue> Issues => _issues ?? Array.Empty<LocalContributionDiscoveryIssue>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool Succeeded => !HasIssues;

        public int ContributionCount => ContributionSet.Count;

        public string ToDiagnosticString(int maxIssues = 8, int maxHandles = 8)
        {
            var builder = new StringBuilder();
            builder.Append($"localContributions='{ContributionCount}' issues='{IssueCount}'");
            if (ContributionSet.HasContributions)
            {
                builder.Append(" ");
                builder.Append(ContributionSet.ToDiagnosticString(maxHandles));
            }

            if (!HasIssues)
            {
                return builder.ToString();
            }

            int limit = Math.Max(0, maxIssues);
            int shown = Math.Min(limit, IssueCount);
            builder.Append(" discoveryIssues=[");
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Issues[i].ToDiagnosticString());
            }

            builder.Append("]");
            if (IssueCount > shown)
            {
                builder.Append($" omittedIssues='{IssueCount - shown}'");
            }

            return builder.ToString();
        }

        public static LocalContributionDiscoveryResult Empty()
        {
            return new LocalContributionDiscoveryResult(LocalContributionSet.Empty(), Array.Empty<LocalContributionDiscoveryIssue>());
        }
    }
}
