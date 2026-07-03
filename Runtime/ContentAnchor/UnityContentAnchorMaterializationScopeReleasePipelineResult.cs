using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic result for explicit Unity ContentAnchor materialization scope release proof.
    /// It summarizes physical release, logical RuntimeContent release and logical ContentAnchor unbinding for one runtime owner context.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-D ContentAnchor materialization scope release pipeline result; explicit cleanup proof only.")]
    internal readonly struct UnityContentAnchorMaterializationScopeReleasePipelineResult : IEquatable<UnityContentAnchorMaterializationScopeReleasePipelineResult>
    {
        internal UnityContentAnchorMaterializationScopeReleasePipelineResult(
            UnityContentAnchorMaterializationScopeReleasePipelineStatus status,
            RuntimeScopeContext context,
            RuntimeReleasePolicy releasePolicy,
            int matchedPhysicalEntries,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int registryCountBefore,
            int registryActiveBefore,
            int registryCountAfter,
            int registryActiveAfter,
            ContentAnchorBindingLifecycleResult bindingCleanupResult,
            RuntimeReleaseResult lastPhysicalReleaseResult,
            RuntimeReleaseResult lastLogicalReleaseResult,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(UnityContentAnchorMaterializationScopeReleasePipelineStatus), status)
                || status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "ContentAnchor materialization scope release status must be explicit.");
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy)
                || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(releasePolicy), releasePolicy, "ContentAnchor materialization scope release policy must be explicit.");
            }

            if (matchedPhysicalEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchedPhysicalEntries), matchedPhysicalEntries, "Matched physical entry count cannot be negative.");
            }

            if (physicalReleaseRequests < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalReleaseRequests), physicalReleaseRequests, "Physical release request count cannot be negative.");
            }

            if (logicalReleaseResults < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalReleaseResults), logicalReleaseResults, "Logical release result count cannot be negative.");
            }

            Status = status;
            Context = context;
            ReleasePolicy = releasePolicy;
            MatchedPhysicalEntries = matchedPhysicalEntries;
            PhysicalReleaseRequests = physicalReleaseRequests;
            LogicalReleaseResults = logicalReleaseResults;
            RegistryCountBefore = registryCountBefore;
            RegistryActiveBefore = registryActiveBefore;
            RegistryCountAfter = registryCountAfter;
            RegistryActiveAfter = registryActiveAfter;
            BindingCleanupResult = bindingCleanupResult;
            LastPhysicalReleaseResult = lastPhysicalReleaseResult;
            LastLogicalReleaseResult = lastLogicalReleaseResult;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public UnityContentAnchorMaterializationScopeReleasePipelineStatus Status { get; }

        public RuntimeScopeContext Context { get; }

        public RuntimeReleasePolicy ReleasePolicy { get; }

        public int MatchedPhysicalEntries { get; }

        public int PhysicalReleaseRequests { get; }

        public int LogicalReleaseResults { get; }

        public int RegistryCountBefore { get; }

        public int RegistryActiveBefore { get; }

        public int RegistryCountAfter { get; }

        public int RegistryActiveAfter { get; }

        public ContentAnchorBindingLifecycleResult BindingCleanupResult { get; }

        public RuntimeReleaseResult LastPhysicalReleaseResult { get; }

        public RuntimeReleaseResult LastLogicalReleaseResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is UnityContentAnchorMaterializationScopeReleasePipelineStatus.Succeeded or UnityContentAnchorMaterializationScopeReleasePipelineStatus.SucceededNoContent;

        public bool Failed => !Succeeded;

        public bool HasBindingCleanup => BindingCleanupResult.Executed;

        public int BindingRemovedCount => HasBindingCleanup ? BindingCleanupResult.RemovedCount : 0;

        public bool Equals(UnityContentAnchorMaterializationScopeReleasePipelineResult other)
        {
            return Status == other.Status
                && Context.Equals(other.Context)
                && ReleasePolicy == other.ReleasePolicy
                && MatchedPhysicalEntries == other.MatchedPhysicalEntries
                && PhysicalReleaseRequests == other.PhysicalReleaseRequests
                && LogicalReleaseResults == other.LogicalReleaseResults
                && RegistryCountBefore == other.RegistryCountBefore
                && RegistryActiveBefore == other.RegistryActiveBefore
                && RegistryCountAfter == other.RegistryCountAfter
                && RegistryActiveAfter == other.RegistryActiveAfter
                && BindingCleanupResult.Equals(other.BindingCleanupResult)
                && LastPhysicalReleaseResult.Equals(other.LastPhysicalReleaseResult)
                && LastLogicalReleaseResult.Equals(other.LastLogicalReleaseResult)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityContentAnchorMaterializationScopeReleasePipelineResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ Context.GetHashCode();
                hashCode = hashCode * 397 ^ (int)ReleasePolicy;
                hashCode = hashCode * 397 ^ MatchedPhysicalEntries;
                hashCode = hashCode * 397 ^ PhysicalReleaseRequests;
                hashCode = hashCode * 397 ^ LogicalReleaseResults;
                hashCode = hashCode * 397 ^ RegistryCountBefore;
                hashCode = hashCode * 397 ^ RegistryActiveBefore;
                hashCode = hashCode * 397 ^ RegistryCountAfter;
                hashCode = hashCode * 397 ^ RegistryActiveAfter;
                hashCode = hashCode * 397 ^ BindingCleanupResult.GetHashCode();
                hashCode = hashCode * 397 ^ LastPhysicalReleaseResult.GetHashCode();
                hashCode = hashCode * 397 ^ LastLogicalReleaseResult.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string contextText = Context.IsValid ? Context.ToDiagnosticString() : "<invalid>";
            string bindingText = HasBindingCleanup ? BindingCleanupResult.ToDiagnosticString() : "<none>";
            string physicalText = LastPhysicalReleaseResult.Request.IsValid ? LastPhysicalReleaseResult.ToDiagnosticString() : "<none>";
            string logicalText = LastLogicalReleaseResult.Request.IsValid ? LastLogicalReleaseResult.ToDiagnosticString() : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();

            return $"status='{Status}' succeeded='{Succeeded}' policy='{ReleasePolicy}' matched='{MatchedPhysicalEntries}' physicalReleaseRequests='{PhysicalReleaseRequests}' logicalReleaseResults='{LogicalReleaseResults}' bindingRemoved='{BindingRemovedCount}' registryBefore='{RegistryCountBefore}' activeBefore='{RegistryActiveBefore}' registryAfter='{RegistryCountAfter}' activeAfter='{RegistryActiveAfter}' context={contextText} bindingCleanup={bindingText} lastPhysicalRelease={physicalText} lastLogicalRelease={logicalText} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        internal static UnityContentAnchorMaterializationScopeReleasePipelineResult Success(
            RuntimeScopeContext context,
            RuntimeReleasePolicy releasePolicy,
            int matchedPhysicalEntries,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int registryCountBefore,
            int registryActiveBefore,
            int registryCountAfter,
            int registryActiveAfter,
            ContentAnchorBindingLifecycleResult bindingCleanupResult,
            RuntimeReleaseResult lastPhysicalReleaseResult,
            RuntimeReleaseResult lastLogicalReleaseResult,
            string source,
            string reason,
            string message)
        {
            var status = physicalReleaseRequests > 0 || (bindingCleanupResult.Executed && bindingCleanupResult.RemovedAny)
                ? UnityContentAnchorMaterializationScopeReleasePipelineStatus.Succeeded
                : UnityContentAnchorMaterializationScopeReleasePipelineStatus.SucceededNoContent;

            return new UnityContentAnchorMaterializationScopeReleasePipelineResult(
                status,
                context,
                releasePolicy,
                matchedPhysicalEntries,
                physicalReleaseRequests,
                logicalReleaseResults,
                registryCountBefore,
                registryActiveBefore,
                registryCountAfter,
                registryActiveAfter,
                bindingCleanupResult,
                lastPhysicalReleaseResult,
                lastLogicalReleaseResult,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Unity ContentAnchor materialization scope release completed."
                    : message);
        }

        internal static UnityContentAnchorMaterializationScopeReleasePipelineResult Failure(
            UnityContentAnchorMaterializationScopeReleasePipelineStatus status,
            RuntimeScopeContext context,
            RuntimeReleasePolicy releasePolicy,
            int matchedPhysicalEntries,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int registryCountBefore,
            int registryActiveBefore,
            int registryCountAfter,
            int registryActiveAfter,
            ContentAnchorBindingLifecycleResult bindingCleanupResult,
            RuntimeReleaseResult lastPhysicalReleaseResult,
            RuntimeReleaseResult lastLogicalReleaseResult,
            string source,
            string reason,
            string message)
        {
            if (status is UnityContentAnchorMaterializationScopeReleasePipelineStatus.Succeeded or UnityContentAnchorMaterializationScopeReleasePipelineStatus.SucceededNoContent)
            {
                throw new ArgumentException("Use Success for successful ContentAnchor materialization scope release results.", nameof(status));
            }

            return new UnityContentAnchorMaterializationScopeReleasePipelineResult(
                status,
                context,
                releasePolicy,
                matchedPhysicalEntries,
                physicalReleaseRequests,
                logicalReleaseResults,
                registryCountBefore,
                registryActiveBefore,
                registryCountAfter,
                registryActiveAfter,
                bindingCleanupResult,
                lastPhysicalReleaseResult,
                lastLogicalReleaseResult,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Unity ContentAnchor materialization scope release failed."
                    : message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
