using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable lifecycle-owned release plan built from registered materialization evidence.
    /// The plan contains RuntimeReleaseRequest values only. It does not request release, execute physical cleanup, unregister RuntimeContent handles or remove ContentAnchor bindings.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-P lifecycle-owned materialization release plan; query/planning only, no release execution.")]
    public readonly struct LifecycleMaterializationReleasePlan : IEquatable<LifecycleMaterializationReleasePlan>
    {
        public LifecycleMaterializationReleasePlan(
            LifecycleMaterializationReleasePlanTargetKind targetKind,
            RuntimeContentScope scope,
            RuntimeContentOwner owner,
            RuntimeReleasePolicy policy,
            RuntimeReleaseRequest[] requests,
            int totalEntries,
            int activeCandidates,
            int releaseFailedCandidates,
            int skippedReleaseRequested,
            int skippedReleased,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(LifecycleMaterializationReleasePlanTargetKind), targetKind)
                || targetKind == LifecycleMaterializationReleasePlanTargetKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Lifecycle materialization release plan target kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(RuntimeContentScope), scope) || scope == RuntimeContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Lifecycle materialization release plan scope must be explicit.");
            }

            if (targetKind == LifecycleMaterializationReleasePlanTargetKind.Owner)
            {
                if (!owner.IsValid)
                {
                    throw new ArgumentException("Lifecycle materialization release plan owner must be valid for owner-targeted plans.", nameof(owner));
                }

                if (owner.Scope != scope)
                {
                    throw new ArgumentException("Lifecycle materialization release plan owner scope must match the plan scope.", nameof(owner));
                }
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), policy) || policy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(policy), policy, "Lifecycle materialization release plan policy must be explicit.");
            }

            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            if (totalEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalEntries), totalEntries, "Lifecycle materialization release plan total entry count cannot be negative.");
            }

            if (activeCandidates < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activeCandidates), activeCandidates, "Lifecycle materialization release plan active candidate count cannot be negative.");
            }

            if (releaseFailedCandidates < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(releaseFailedCandidates), releaseFailedCandidates, "Lifecycle materialization release plan release-failed candidate count cannot be negative.");
            }

            if (skippedReleaseRequested < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedReleaseRequested), skippedReleaseRequested, "Lifecycle materialization release plan skipped release-requested count cannot be negative.");
            }

            if (skippedReleased < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedReleased), skippedReleased, "Lifecycle materialization release plan skipped released count cannot be negative.");
            }

            if (requests.Length != activeCandidates + releaseFailedCandidates)
            {
                throw new ArgumentException("Lifecycle materialization release plan request count must match active plus release-failed candidates.", nameof(requests));
            }

            for (int i = 0; i < requests.Length; i++)
            {
                if (!requests[i].IsValid)
                {
                    throw new ArgumentException($"Lifecycle materialization release plan request at index '{i}' must be valid.", nameof(requests));
                }

                if (requests[i].Scope != scope)
                {
                    throw new ArgumentException($"Lifecycle materialization release plan request at index '{i}' must match the plan scope.", nameof(requests));
                }

                if (targetKind == LifecycleMaterializationReleasePlanTargetKind.Owner && requests[i].Owner != owner)
                {
                    throw new ArgumentException($"Lifecycle materialization release plan request at index '{i}' must match the plan owner.", nameof(requests));
                }

                if (requests[i].Policy != policy)
                {
                    throw new ArgumentException($"Lifecycle materialization release plan request at index '{i}' must match the plan policy.", nameof(requests));
                }
            }

            TargetKind = targetKind;
            Scope = scope;
            Owner = owner;
            Policy = policy;
            Requests = CopyRequests(requests);
            TotalEntries = totalEntries;
            ActiveCandidates = activeCandidates;
            ReleaseFailedCandidates = releaseFailedCandidates;
            SkippedReleaseRequested = skippedReleaseRequested;
            SkippedReleased = skippedReleased;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            Status = requests.Length > 0
                ? LifecycleMaterializationReleasePlanStatus.SucceededPlanned
                : LifecycleMaterializationReleasePlanStatus.SucceededEmpty;
        }

        public LifecycleMaterializationReleasePlanTargetKind TargetKind { get; }

        public RuntimeContentScope Scope { get; }

        public RuntimeContentOwner Owner { get; }

        public RuntimeReleasePolicy Policy { get; }

        public LifecycleMaterializationReleasePlanStatus Status { get; }

        public RuntimeReleaseRequest[] Requests { get; }

        public int TotalEntries { get; }

        public int RequestCount => Requests?.Length ?? 0;

        public int ActiveCandidates { get; }

        public int ReleaseFailedCandidates { get; }

        public int SkippedReleaseRequested { get; }

        public int SkippedReleased { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is LifecycleMaterializationReleasePlanStatus.SucceededPlanned
            or LifecycleMaterializationReleasePlanStatus.SucceededEmpty;

        public bool HasRequests => RequestCount > 0;

        public bool IsOwnerTargeted => TargetKind == LifecycleMaterializationReleasePlanTargetKind.Owner;

        public bool IsScopeTargeted => TargetKind == LifecycleMaterializationReleasePlanTargetKind.Scope;

        public bool ExecutesRelease => false;

        public bool PerformsPhysicalRelease => false;

        public bool PerformsLogicalRuntimeContentRelease => false;

        public bool PerformsContentAnchorBindingCleanup => false;

        public RuntimeReleaseRequest[] SnapshotRequests()
        {
            return CopyRequests(Requests ?? Array.Empty<RuntimeReleaseRequest>());
        }

        public bool Equals(LifecycleMaterializationReleasePlan other)
        {
            return TargetKind == other.TargetKind
                && Scope == other.Scope
                && Owner.Equals(other.Owner)
                && Policy == other.Policy
                && Status == other.Status
                && RequestsEqual(Requests, other.Requests)
                && TotalEntries == other.TotalEntries
                && ActiveCandidates == other.ActiveCandidates
                && ReleaseFailedCandidates == other.ReleaseFailedCandidates
                && SkippedReleaseRequested == other.SkippedReleaseRequested
                && SkippedReleased == other.SkippedReleased
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LifecycleMaterializationReleasePlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)TargetKind;
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Policy;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ TotalEntries;
                hashCode = hashCode * 397 ^ ActiveCandidates;
                hashCode = hashCode * 397 ^ ReleaseFailedCandidates;
                hashCode = hashCode * 397 ^ SkippedReleaseRequested;
                hashCode = hashCode * 397 ^ SkippedReleased;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                if (Requests != null)
                {
                    for (int i = 0; i < Requests.Length; i++)
                    {
                        hashCode = hashCode * 397 ^ Requests[i].GetHashCode();
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
            string targetText = IsOwnerTargeted ? Owner.StableText : Scope.ToString();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"targetKind='{TargetKind}' target='{targetText}' scope='{Scope}' policy='{Policy}' status='{Status}' succeeded='{Succeeded}' totalEntries='{TotalEntries}' requests='{RequestCount}' activeCandidates='{ActiveCandidates}' releaseFailedCandidates='{ReleaseFailedCandidates}' skippedReleaseRequested='{SkippedReleaseRequested}' skippedReleased='{SkippedReleased}' executesRelease='{ExecutesRelease}' physicalRelease='{PerformsPhysicalRelease}' logicalRuntimeContentRelease='{PerformsLogicalRuntimeContentRelease}' contentAnchorBindingCleanup='{PerformsContentAnchorBindingCleanup}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static bool operator ==(LifecycleMaterializationReleasePlan left, LifecycleMaterializationReleasePlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LifecycleMaterializationReleasePlan left, LifecycleMaterializationReleasePlan right)
        {
            return !left.Equals(right);
        }

        private static RuntimeReleaseRequest[] CopyRequests(RuntimeReleaseRequest[] requests)
        {
            if (requests == null || requests.Length == 0)
            {
                return Array.Empty<RuntimeReleaseRequest>();
            }

            var copy = new RuntimeReleaseRequest[requests.Length];
            Array.Copy(requests, copy, requests.Length);
            return copy;
        }

        private static bool RequestsEqual(RuntimeReleaseRequest[] left, RuntimeReleaseRequest[] right)
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
