using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Result produced by collecting passive ObjectEntryDeclaration components.
    /// It exposes descriptors and diagnostics only; it is not a runtime registry, binding table or reset inventory.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry declaration source result introduced by F13D; diagnostics clarified by F13E.")]
    public sealed class ObjectEntryDeclarationSourceResult
    {
        private static readonly IReadOnlyList<ObjectEntryIssue> EmptyIssues = Array.Empty<ObjectEntryIssue>();

        public ObjectEntryDeclarationSourceResult(
            ObjectEntrySet objectEntries,
            ObjectEntryResultStatus status,
            int declarationCount,
            int candidateDescriptorCount,
            int acceptedDeclarationCount,
            int rejectedDeclarationCount,
            IEnumerable<ObjectEntryIssue> issues = null)
            : this(
                objectEntries,
                status,
                declarationCount,
                candidateDescriptorCount,
                acceptedDeclarationCount,
                rejectedDeclarationCount,
                0,
                issues)
        {
        }

        public ObjectEntryDeclarationSourceResult(
            ObjectEntrySet objectEntries,
            ObjectEntryResultStatus status,
            int declarationCount,
            int candidateDescriptorCount,
            int acceptedDeclarationCount,
            int rejectedDeclarationCount,
            int filteredDeclarationCount,
            IEnumerable<ObjectEntryIssue> issues = null)
        {
            if (!Enum.IsDefined(typeof(ObjectEntryResultStatus), status) || status == ObjectEntryResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object Entry declaration source result status must be explicit.");
            }

            if (declarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(declarationCount), declarationCount, "Declaration count cannot be negative.");
            }

            if (candidateDescriptorCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(candidateDescriptorCount), candidateDescriptorCount, "Candidate descriptor count cannot be negative.");
            }

            if (acceptedDeclarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(acceptedDeclarationCount), acceptedDeclarationCount, "Accepted declaration count cannot be negative.");
            }

            if (rejectedDeclarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rejectedDeclarationCount), rejectedDeclarationCount, "Rejected declaration count cannot be negative.");
            }

            if (filteredDeclarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(filteredDeclarationCount), filteredDeclarationCount, "Filtered declaration count cannot be negative.");
            }

            if (acceptedDeclarationCount + rejectedDeclarationCount + filteredDeclarationCount != declarationCount)
            {
                throw new ArgumentException("Accepted, rejected and filtered declaration counts must match total declaration count.");
            }

            if (candidateDescriptorCount > declarationCount)
            {
                throw new ArgumentOutOfRangeException(nameof(candidateDescriptorCount), candidateDescriptorCount, "Candidate descriptor count cannot exceed declaration count.");
            }

            ObjectEntries = objectEntries ?? ObjectEntrySet.Empty();
            Status = status;
            DeclarationCount = declarationCount;
            CandidateDescriptorCount = candidateDescriptorCount;
            AcceptedDeclarationCount = acceptedDeclarationCount;
            RejectedDeclarationCount = rejectedDeclarationCount;
            FilteredDeclarationCount = filteredDeclarationCount;
            Issues = issues?.ToArray() ?? EmptyIssues;
        }

        public ObjectEntrySet ObjectEntries { get; }

        public ObjectEntryResultStatus Status { get; }

        public int DeclarationCount { get; }

        /// <summary>
        /// Number of declarations that could be converted to descriptors before aggregate validation.
        /// A failed aggregate result, such as duplicate identity, can still have candidate descriptors.
        /// </summary>
        public int CandidateDescriptorCount { get; }

        /// <summary>
        /// Number of declarations accepted into the final ObjectEntrySet.
        /// This is zero when aggregate validation rejects the set.
        /// </summary>
        public int AcceptedDeclarationCount { get; }

        /// <summary>
        /// Number of declarations not accepted into the final ObjectEntrySet.
        /// </summary>
        public int RejectedDeclarationCount { get; }

        /// <summary>
        /// Number of valid authored declarations intentionally excluded because their explicit owner is not active.
        /// Filtered declarations are neither accepted nor rejected.
        /// </summary>
        public int FilteredDeclarationCount { get; }

        public IReadOnlyList<ObjectEntryIssue> Issues { get; }

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public bool Succeeded => Status is ObjectEntryResultStatus.Accepted or ObjectEntryResultStatus.AcceptedWithWarnings;

        public bool Failed => Status == ObjectEntryResultStatus.Rejected;

        public string Summary => $"resultStatus='{Status}' declarations='{DeclarationCount}' candidateDescriptors='{CandidateDescriptorCount}' acceptedDeclarations='{AcceptedDeclarationCount}' rejectedDeclarations='{RejectedDeclarationCount}' filteredDeclarations='{FilteredDeclarationCount}' objectEntries='{ObjectEntries.Count}' required='{ObjectEntries.RequiredCount}' optional='{ObjectEntries.OptionalCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}'";

        public ObjectEntryRuntimeContextSnapshot ToRuntimeContextSnapshot(string source = null)
        {
            return ObjectEntryRuntimeContextSnapshot.From(this, source);
        }

        public string ToDiagnosticString()
        {
            if (Issues.Count == 0)
            {
                return Summary;
            }

            return $"{Summary} issueDetails=[{string.Join(" | ", Issues.Select(issue => issue.ToString()))}]";
        }
    }
}
