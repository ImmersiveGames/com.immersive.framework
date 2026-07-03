using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic result for explicit composite lifecycle release.
    /// It reports physical Unity release requests, logical RuntimeContent release, ContentAnchor binding cleanup and lifecycle registry state updates.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-S explicit composite lifecycle release result; no Route/Activity auto-release wiring.")]
    internal readonly struct UnityContentAnchorCompositeLifecycleReleaseResult : IEquatable<UnityContentAnchorCompositeLifecycleReleaseResult>
    {
        internal UnityContentAnchorCompositeLifecycleReleaseResult(
            LifecycleMaterializationReleasePlan plan,
            UnityContentAnchorCompositeLifecycleReleaseStatus status,
            int physicalReleaseRequests,
            int logicalRuntimeReleaseResults,
            int bindingCleanupResults,
            int bindingRemoved,
            int lifecycleReleaseRequested,
            int lifecycleReleased,
            int lifecycleReleaseFailed,
            int missingEntries,
            int registryEntries,
            int registryActive,
            int registryReleaseRequested,
            int registryReleased,
            int registryReleaseFailed,
            int physicalRegistryEntries,
            int physicalRegistryActive,
            int physicalRegistryReleaseRequested,
            int runtimeHandles,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(UnityContentAnchorCompositeLifecycleReleaseStatus), status)
                || status == UnityContentAnchorCompositeLifecycleReleaseStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Composite lifecycle release status must be explicit.");
            }

            if (physicalReleaseRequests < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalReleaseRequests), physicalReleaseRequests, "Physical release request count cannot be negative.");
            }

            if (logicalRuntimeReleaseResults < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalRuntimeReleaseResults), logicalRuntimeReleaseResults, "Logical RuntimeContent release result count cannot be negative.");
            }

            if (bindingCleanupResults < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingCleanupResults), bindingCleanupResults, "Binding cleanup result count cannot be negative.");
            }

            if (bindingRemoved < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingRemoved), bindingRemoved, "Binding removed count cannot be negative.");
            }

            if (lifecycleReleaseRequested < 0 || lifecycleReleased < 0 || lifecycleReleaseFailed < 0 || missingEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lifecycleReleaseRequested), lifecycleReleaseRequested, "Lifecycle release counters cannot be negative.");
            }

            Plan = plan;
            Status = status;
            PhysicalReleaseRequests = physicalReleaseRequests;
            LogicalRuntimeReleaseResults = logicalRuntimeReleaseResults;
            BindingCleanupResults = bindingCleanupResults;
            BindingRemoved = bindingRemoved;
            LifecycleReleaseRequested = lifecycleReleaseRequested;
            LifecycleReleased = lifecycleReleased;
            LifecycleReleaseFailed = lifecycleReleaseFailed;
            MissingEntries = missingEntries;
            RegistryEntries = registryEntries;
            RegistryActive = registryActive;
            RegistryReleaseRequested = registryReleaseRequested;
            RegistryReleased = registryReleased;
            RegistryReleaseFailed = registryReleaseFailed;
            PhysicalRegistryEntries = physicalRegistryEntries;
            PhysicalRegistryActive = physicalRegistryActive;
            PhysicalRegistryReleaseRequested = physicalRegistryReleaseRequested;
            RuntimeHandles = runtimeHandles;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public LifecycleMaterializationReleasePlan Plan { get; }

        public UnityContentAnchorCompositeLifecycleReleaseStatus Status { get; }

        public int RequestCount => Plan.RequestCount;

        public int PhysicalReleaseRequests { get; }

        public int LogicalRuntimeReleaseResults { get; }

        public int BindingCleanupResults { get; }

        public int BindingRemoved { get; }

        public int LifecycleReleaseRequested { get; }

        public int LifecycleReleased { get; }

        public int LifecycleReleaseFailed { get; }

        public int MissingEntries { get; }

        public int RegistryEntries { get; }

        public int RegistryActive { get; }

        public int RegistryReleaseRequested { get; }

        public int RegistryReleased { get; }

        public int RegistryReleaseFailed { get; }

        public int PhysicalRegistryEntries { get; }

        public int PhysicalRegistryActive { get; }

        public int PhysicalRegistryReleaseRequested { get; }

        public int RuntimeHandles { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededReleasedAll
            or UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededNoRequests;

        public bool Failed => !Succeeded;

        public bool ExecutesRelease => Status != UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededNoRequests;

        public bool PerformsPhysicalRelease => PhysicalReleaseRequests > 0;

        public bool PerformsLogicalRuntimeContentRelease => LogicalRuntimeReleaseResults > 0;

        public bool PerformsContentAnchorBindingCleanup => BindingCleanupResults > 0;

        public bool Equals(UnityContentAnchorCompositeLifecycleReleaseResult other)
        {
            return Plan.Equals(other.Plan)
                && Status == other.Status
                && PhysicalReleaseRequests == other.PhysicalReleaseRequests
                && LogicalRuntimeReleaseResults == other.LogicalRuntimeReleaseResults
                && BindingCleanupResults == other.BindingCleanupResults
                && BindingRemoved == other.BindingRemoved
                && LifecycleReleaseRequested == other.LifecycleReleaseRequested
                && LifecycleReleased == other.LifecycleReleased
                && LifecycleReleaseFailed == other.LifecycleReleaseFailed
                && MissingEntries == other.MissingEntries
                && RegistryEntries == other.RegistryEntries
                && RegistryActive == other.RegistryActive
                && RegistryReleaseRequested == other.RegistryReleaseRequested
                && RegistryReleased == other.RegistryReleased
                && RegistryReleaseFailed == other.RegistryReleaseFailed
                && PhysicalRegistryEntries == other.PhysicalRegistryEntries
                && PhysicalRegistryActive == other.PhysicalRegistryActive
                && PhysicalRegistryReleaseRequested == other.PhysicalRegistryReleaseRequested
                && RuntimeHandles == other.RuntimeHandles
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityContentAnchorCompositeLifecycleReleaseResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Plan.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ PhysicalReleaseRequests;
                hashCode = hashCode * 397 ^ LogicalRuntimeReleaseResults;
                hashCode = hashCode * 397 ^ BindingCleanupResults;
                hashCode = hashCode * 397 ^ BindingRemoved;
                hashCode = hashCode * 397 ^ LifecycleReleaseRequested;
                hashCode = hashCode * 397 ^ LifecycleReleased;
                hashCode = hashCode * 397 ^ LifecycleReleaseFailed;
                hashCode = hashCode * 397 ^ MissingEntries;
                hashCode = hashCode * 397 ^ RegistryEntries;
                hashCode = hashCode * 397 ^ RegistryActive;
                hashCode = hashCode * 397 ^ RegistryReleaseRequested;
                hashCode = hashCode * 397 ^ RegistryReleased;
                hashCode = hashCode * 397 ^ RegistryReleaseFailed;
                hashCode = hashCode * 397 ^ PhysicalRegistryEntries;
                hashCode = hashCode * 397 ^ PhysicalRegistryActive;
                hashCode = hashCode * 397 ^ PhysicalRegistryReleaseRequested;
                hashCode = hashCode * 397 ^ RuntimeHandles;
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
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"status='{Status}' succeeded='{Succeeded}' requests='{RequestCount}' physicalReleaseRequests='{PhysicalReleaseRequests}' logicalRuntimeReleaseResults='{LogicalRuntimeReleaseResults}' bindingCleanupResults='{BindingCleanupResults}' bindingRemoved='{BindingRemoved}' lifecycleReleaseRequested='{LifecycleReleaseRequested}' lifecycleReleased='{LifecycleReleased}' lifecycleReleaseFailed='{LifecycleReleaseFailed}' missingEntries='{MissingEntries}' registryEntries='{RegistryEntries}' registryActive='{RegistryActive}' registryReleaseRequested='{RegistryReleaseRequested}' registryReleased='{RegistryReleased}' registryReleaseFailed='{RegistryReleaseFailed}' physicalRegistryEntries='{PhysicalRegistryEntries}' physicalRegistryActive='{PhysicalRegistryActive}' physicalRegistryReleaseRequested='{PhysicalRegistryReleaseRequested}' runtimeHandles='{RuntimeHandles}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
