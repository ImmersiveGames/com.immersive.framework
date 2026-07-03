using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Result of local contribution validation.
    /// Validation is authoring/policy only and does not perform materialization or lifecycle work.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Local contribution validation result introduced by F5G.")]
    internal readonly struct LocalContributionValidationResult
    {
        private readonly LocalContributionValidationIssue[] _issues;

        public LocalContributionValidationResult(
            LocalContributionSet contributionSet,
            IReadOnlyList<LocalContributionValidationIssue> issues)
        {
            ContributionSet = contributionSet;

            if (issues == null || issues.Count == 0)
            {
                _issues = Array.Empty<LocalContributionValidationIssue>();
                return;
            }

            _issues = new LocalContributionValidationIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++)
            {
                _issues[i] = issues[i];
            }
        }

        public LocalContributionSet ContributionSet { get; }

        public IReadOnlyList<LocalContributionValidationIssue> Issues => _issues ?? Array.Empty<LocalContributionValidationIssue>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public int BlockingIssueCount => CountIssues(blocking: true);

        public int NonBlockingDiagnosticCount => IssueCount - BlockingIssueCount;

        public int OptionalSkipCount => CountByKind(LocalContributionValidationIssueKind.OptionalContributionSkipped);

        public bool Succeeded => BlockingIssueCount == 0;

        public int ContributionCount => ContributionSet.Count;

        public string ToDiagnosticString(int maxIssues = 8, int maxHandles = 8)
        {
            var builder = new StringBuilder();
            builder.Append($"localContributions='{ContributionCount}' validationIssues='{IssueCount}' blockingIssues='{BlockingIssueCount}' optionalSkips='{OptionalSkipCount}'");
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
            builder.Append(" validationDetails=[");
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
                builder.Append($" omittedValidationIssues='{IssueCount - shown}'");
            }

            return builder.ToString();
        }

        private int CountIssues(bool blocking)
        {
            if (!HasIssues)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<LocalContributionValidationIssue> items = Issues;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Blocking == blocking)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountByKind(LocalContributionValidationIssueKind kind)
        {
            if (!HasIssues)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<LocalContributionValidationIssue> items = Issues;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public static LocalContributionValidationResult Empty()
        {
            return new LocalContributionValidationResult(LocalContributionSet.Empty(), Array.Empty<LocalContributionValidationIssue>());
        }
    }
}
