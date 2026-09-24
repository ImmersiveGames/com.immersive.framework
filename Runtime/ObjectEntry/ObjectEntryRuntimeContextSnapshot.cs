using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Passive runtime snapshot of the current logical Object Entry context.
    /// It exposes diagnostics and immutable descriptors only; it is not a registry, binding table, reset inventory, lifecycle owner or service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry runtime context snapshot introduced by F13G and extended with F13J scoped-filter diagnostics; no physical binding or reset authority.")]
    public sealed class ObjectEntryRuntimeContextSnapshot
    {
        private ObjectEntryRuntimeContextSnapshot(
            string source,
            ObjectEntrySet objectEntries,
            ObjectEntryResultStatus status,
            int declarationCount,
            int candidateDescriptorCount,
            int acceptedDeclarationCount,
            int rejectedDeclarationCount,
            int filteredDeclarationCount,
            int issueCount,
            int blockingIssueCount,
            int nonBlockingIssueCount)
        {
            if (objectEntries == null)
            {
                throw new ArgumentNullException(nameof(objectEntries));
            }

            if (!Enum.IsDefined(typeof(ObjectEntryResultStatus), status) || status == ObjectEntryResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object Entry runtime context snapshot status must be explicit.");
            }

            Source = ResolveSource(source);
            ObjectEntries = objectEntries;
            Status = status;
            DeclarationCount = declarationCount;
            CandidateDescriptorCount = candidateDescriptorCount;
            AcceptedDeclarationCount = acceptedDeclarationCount;
            RejectedDeclarationCount = rejectedDeclarationCount;
            FilteredDeclarationCount = filteredDeclarationCount;
            IssueCount = issueCount;
            BlockingIssueCount = blockingIssueCount;
            NonBlockingIssueCount = nonBlockingIssueCount;
        }

        public string Source { get; }

        public ObjectEntrySet ObjectEntries { get; }

        public IReadOnlyList<ObjectEntryDescriptor> Entries => ObjectEntries.Entries;

        public ObjectEntryResultStatus Status { get; }

        public int DeclarationCount { get; }

        public int CandidateDescriptorCount { get; }

        public int AcceptedDeclarationCount { get; }

        public int RejectedDeclarationCount { get; }

        public int FilteredDeclarationCount { get; }

        public int IssueCount { get; }

        public int BlockingIssueCount { get; }

        public int NonBlockingIssueCount { get; }

        public int Count => ObjectEntries.Count;

        public int RequiredCount => ObjectEntries.RequiredCount;

        public int OptionalCount => ObjectEntries.OptionalCount;

        public bool HasEntries => !ObjectEntries.IsEmpty;

        public bool Succeeded => Status is ObjectEntryResultStatus.Accepted or ObjectEntryResultStatus.AcceptedWithWarnings;

        public bool Failed => Status == ObjectEntryResultStatus.Rejected;

        public bool IsAvailable => Succeeded && BlockingIssueCount == 0;

        public bool TryGet(ObjectEntryId id, out ObjectEntryDescriptor descriptor)
        {
            return ObjectEntries.TryGet(id, out descriptor);
        }

        public IReadOnlyList<ObjectEntryDescriptor> GetByScope(ObjectEntryScope scope)
        {
            return ObjectEntries.GetByScope(scope);
        }

        public string Summary => $"source='{Source}' resultStatus='{Status}' declarations='{DeclarationCount}' candidateDescriptors='{CandidateDescriptorCount}' acceptedDeclarations='{AcceptedDeclarationCount}' rejectedDeclarations='{RejectedDeclarationCount}' filteredDeclarations='{FilteredDeclarationCount}' objectEntries='{Count}' required='{RequiredCount}' optional='{OptionalCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}'";

        public static ObjectEntryRuntimeContextSnapshot From(
            ObjectEntryDeclarationSourceResult result,
            string source = null)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new ObjectEntryRuntimeContextSnapshot(
                source,
                result.ObjectEntries,
                result.Status,
                result.DeclarationCount,
                result.CandidateDescriptorCount,
                result.AcceptedDeclarationCount,
                result.RejectedDeclarationCount,
                result.FilteredDeclarationCount,
                result.IssueCount,
                result.BlockingIssueCount,
                result.NonBlockingIssueCount);
        }

        private static string ResolveSource(string source)
        {
            return source.NormalizeTextOrFallback(nameof(ObjectEntryRuntimeContextSnapshot));
        }
    }
}
