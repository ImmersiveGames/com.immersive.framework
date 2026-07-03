using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. F5G validator for discovered local contributions and expected local contribution policy.
    /// It emits structured authoring failures and optional skips. It does not materialize, load, unload, release or mutate scene objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Local contribution validators introduced by F5G; expected contribution consumers come later.")]
    internal static class LocalContributionValidator
    {
        public static LocalContributionValidationResult ValidateLoadedSceneAuthored()
        {
            return Validate(LocalContributionDiscovery.DiscoverLoadedSceneAuthored(), Array.Empty<LocalContributionRequirement>());
        }

        public static LocalContributionValidationResult Validate(
            LocalContributionDiscoveryResult discoveryResult,
            IReadOnlyList<LocalContributionRequirement> expectedContributions)
        {
            var validationIssues = new List<LocalContributionValidationIssue>();

            AddDiscoveryIssues(discoveryResult, validationIssues);
            AddExpectedContributionIssues(discoveryResult.ContributionSet, expectedContributions, validationIssues);

            return new LocalContributionValidationResult(discoveryResult.ContributionSet, validationIssues);
        }

        private static void AddDiscoveryIssues(
            LocalContributionDiscoveryResult discoveryResult,
            List<LocalContributionValidationIssue> validationIssues)
        {
            if (!discoveryResult.HasIssues)
            {
                return;
            }

            IReadOnlyList<LocalContributionDiscoveryIssue> issues = discoveryResult.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                validationIssues.Add(new LocalContributionValidationIssue(
                    LocalContributionValidationIssueKind.DiscoveryIssue,
                    issue.ToDiagnosticString(),
                    blocking: true,
                    identityText: issue.IdentityText));
            }
        }

        private static void AddExpectedContributionIssues(
            LocalContributionSet contributionSet,
            IReadOnlyList<LocalContributionRequirement> expectedContributions,
            List<LocalContributionValidationIssue> validationIssues)
        {
            if (expectedContributions == null || expectedContributions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < expectedContributions.Count; i++)
            {
                var expected = expectedContributions[i];
                if (!expected.IsValid)
                {
                    validationIssues.Add(new LocalContributionValidationIssue(
                        LocalContributionValidationIssueKind.InvalidExpectedContribution,
                        "Expected local contribution requires a valid LocalContentIdentity.",
                        blocking: true,
                        diagnosticLabel: expected.DiagnosticLabel));
                    continue;
                }

                if (contributionSet.Contains(expected.Identity))
                {
                    continue;
                }

                if (expected.Requiredness == FrameworkContentRequiredness.Required)
                {
                    validationIssues.Add(new LocalContributionValidationIssue(
                        LocalContributionValidationIssueKind.MissingRequiredContribution,
                        "Required local contribution was not discovered in the active local contribution set.",
                        blocking: true,
                        identityText: expected.Identity.StableText,
                        diagnosticLabel: expected.DiagnosticLabel));
                    continue;
                }

                validationIssues.Add(new LocalContributionValidationIssue(
                    LocalContributionValidationIssueKind.OptionalContributionSkipped,
                    "Optional local contribution was not discovered in the active local contribution set.",
                    blocking: false,
                    identityText: expected.Identity.StableText,
                    diagnosticLabel: expected.DiagnosticLabel));
            }
        }
    }
}
