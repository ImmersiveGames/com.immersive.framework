using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable result of explicitly executing a lifecycle-owned release plan through a caller-provided release executor.
    /// It records delegated runtime release results and lifecycle registry state updates. It does not instantiate content, destroy Unity objects, remove ContentAnchor bindings or wire Route/Activity lifecycle automatically.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-Q explicit lifecycle materialization release execution result; delegated executor only, no auto-release wiring.")]
    public readonly struct LifecycleMaterializationReleaseExecutionResult : IEquatable<LifecycleMaterializationReleaseExecutionResult>
    {
        public LifecycleMaterializationReleaseExecutionResult(
            LifecycleMaterializationReleasePlan plan,
            LifecycleMaterializationReleaseExecutionStatus status,
            RuntimeReleaseResult[] releaseResults,
            LifecycleMaterializationRegistryOperationResult[] registryResults,
            int releaseRequested,
            int released,
            int releaseFailed,
            int missingEntries,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(LifecycleMaterializationReleaseExecutionStatus), status) || status == LifecycleMaterializationReleaseExecutionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Lifecycle materialization release execution status must be explicit.");
            }

            if (releaseResults == null)
            {
                throw new ArgumentNullException(nameof(releaseResults));
            }

            if (registryResults == null)
            {
                throw new ArgumentNullException(nameof(registryResults));
            }

            if (releaseRequested < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(releaseRequested), releaseRequested, "Release requested count cannot be negative.");
            }

            if (released < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(released), released, "Released count cannot be negative.");
            }

            if (releaseFailed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(releaseFailed), releaseFailed, "Release failed count cannot be negative.");
            }

            if (missingEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(missingEntries), missingEntries, "Missing entry count cannot be negative.");
            }

            Plan = plan;
            Status = status;
            ReleaseResults = CopyReleaseResults(releaseResults);
            RegistryResults = CopyRegistryResults(registryResults);
            ReleaseRequested = releaseRequested;
            Released = released;
            ReleaseFailed = releaseFailed;
            MissingEntries = missingEntries;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public LifecycleMaterializationReleasePlan Plan { get; }

        public LifecycleMaterializationReleaseExecutionStatus Status { get; }

        public RuntimeReleaseResult[] ReleaseResults { get; }

        public LifecycleMaterializationRegistryOperationResult[] RegistryResults { get; }

        public int RequestCount => Plan.RequestCount;

        public int ReleaseResultCount => ReleaseResults?.Length ?? 0;

        public int RegistryResultCount => RegistryResults?.Length ?? 0;

        public int ReleaseRequested { get; }

        public int Released { get; }

        public int ReleaseFailed { get; }

        public int MissingEntries { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is LifecycleMaterializationReleaseExecutionStatus.SucceededNoRequests
            or LifecycleMaterializationReleaseExecutionStatus.SucceededReleasedAll;

        public bool Failed => !Succeeded;

        public bool ExecutesRelease => Status != LifecycleMaterializationReleaseExecutionStatus.SucceededNoRequests;

        public bool PerformsPhysicalRelease => false;

        public bool PerformsLogicalRuntimeContentRelease => ExecutesRelease;

        public bool PerformsContentAnchorBindingCleanup => false;

        public RuntimeReleaseResult[] SnapshotReleaseResults()
        {
            return CopyReleaseResults(ReleaseResults ?? Array.Empty<RuntimeReleaseResult>());
        }

        public LifecycleMaterializationRegistryOperationResult[] SnapshotRegistryResults()
        {
            return CopyRegistryResults(RegistryResults ?? Array.Empty<LifecycleMaterializationRegistryOperationResult>());
        }

        public bool Equals(LifecycleMaterializationReleaseExecutionResult other)
        {
            return Plan.Equals(other.Plan)
                && Status == other.Status
                && ReleaseResultsEqual(ReleaseResults, other.ReleaseResults)
                && RegistryResultsEqual(RegistryResults, other.RegistryResults)
                && ReleaseRequested == other.ReleaseRequested
                && Released == other.Released
                && ReleaseFailed == other.ReleaseFailed
                && MissingEntries == other.MissingEntries
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LifecycleMaterializationReleaseExecutionResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Plan.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ ReleaseRequested;
                hashCode = hashCode * 397 ^ Released;
                hashCode = hashCode * 397 ^ ReleaseFailed;
                hashCode = hashCode * 397 ^ MissingEntries;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                if (ReleaseResults != null)
                {
                    for (int i = 0; i < ReleaseResults.Length; i++)
                    {
                        hashCode = hashCode * 397 ^ ReleaseResults[i].GetHashCode();
                    }
                }

                if (RegistryResults != null)
                {
                    for (int i = 0; i < RegistryResults.Length; i++)
                    {
                        hashCode = hashCode * 397 ^ RegistryResults[i].GetHashCode();
                    }
                }

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
            return $"status='{Status}' succeeded='{Succeeded}' requests='{RequestCount}' releaseResults='{ReleaseResultCount}' registryResults='{RegistryResultCount}' releaseRequested='{ReleaseRequested}' released='{Released}' releaseFailed='{ReleaseFailed}' missingEntries='{MissingEntries}' executesRelease='{ExecutesRelease}' physicalRelease='{PerformsPhysicalRelease}' logicalRuntimeContentRelease='{PerformsLogicalRuntimeContentRelease}' contentAnchorBindingCleanup='{PerformsContentAnchorBindingCleanup}' source='{sourceText}' reason='{reasonText}' message='{messageText}' plan={Plan.ToDiagnosticString()}";
        }

        public static LifecycleMaterializationReleaseExecutionResult InvalidPlan(
            LifecycleMaterializationReleasePlan plan,
            string source,
            string reason,
            string message)
        {
            return new LifecycleMaterializationReleaseExecutionResult(
                plan,
                LifecycleMaterializationReleaseExecutionStatus.FailedInvalidPlan,
                Array.Empty<RuntimeReleaseResult>(),
                Array.Empty<LifecycleMaterializationRegistryOperationResult>(),
                0,
                0,
                0,
                0,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Lifecycle materialization release execution rejected an invalid plan."
                    : message);
        }

        public static bool operator ==(LifecycleMaterializationReleaseExecutionResult left, LifecycleMaterializationReleaseExecutionResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LifecycleMaterializationReleaseExecutionResult left, LifecycleMaterializationReleaseExecutionResult right)
        {
            return !left.Equals(right);
        }

        private static RuntimeReleaseResult[] CopyReleaseResults(RuntimeReleaseResult[] results)
        {
            if (results == null || results.Length == 0)
            {
                return Array.Empty<RuntimeReleaseResult>();
            }

            var copy = new RuntimeReleaseResult[results.Length];
            Array.Copy(results, copy, results.Length);
            return copy;
        }

        private static LifecycleMaterializationRegistryOperationResult[] CopyRegistryResults(LifecycleMaterializationRegistryOperationResult[] results)
        {
            if (results == null || results.Length == 0)
            {
                return Array.Empty<LifecycleMaterializationRegistryOperationResult>();
            }

            var copy = new LifecycleMaterializationRegistryOperationResult[results.Length];
            Array.Copy(results, copy, results.Length);
            return copy;
        }

        private static bool ReleaseResultsEqual(RuntimeReleaseResult[] left, RuntimeReleaseResult[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RegistryResultsEqual(LifecycleMaterializationRegistryOperationResult[] left, LifecycleMaterializationRegistryOperationResult[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
