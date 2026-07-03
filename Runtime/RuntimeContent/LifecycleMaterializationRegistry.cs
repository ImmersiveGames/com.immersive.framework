using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Explicit lifecycle-owned registry for materialized runtime content evidence.
    /// It tracks ownership and release evidence only; it does not instantiate prefabs, destroy objects, pool, unload scenes, bind anchors or wire Route/Activity lifecycle automatically.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-N minimal lifecycle-owned materialization registry contract; no auto-materialization or auto-release wiring.")]
    public sealed class LifecycleMaterializationRegistry
    {
        private readonly Dictionary<RuntimeContentIdentity, LifecycleMaterializedEntry> _entries;

        public LifecycleMaterializationRegistry()
        {
            _entries = new Dictionary<RuntimeContentIdentity, LifecycleMaterializedEntry>();
        }

        public int Count => _entries.Count;

        public int ActiveCount => CountByState(LifecycleMaterializationEntryState.Active);

        public int ReleaseRequestedCount => CountByState(LifecycleMaterializationEntryState.ReleaseRequested);

        public int ReleasedCount => CountByState(LifecycleMaterializationEntryState.Released);

        public int ReleaseFailedCount => CountByState(LifecycleMaterializationEntryState.ReleaseFailed);

        public bool HasEntries => _entries.Count > 0;

        public LifecycleMaterializationRegistryOperationResult Register(
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (!handle.IsMaterialized)
            {
                return LifecycleMaterializationRegistryOperationResult.Failure(
                    handle.Identity,
                    LifecycleMaterializationRegistryOperationStatus.RejectedInvalidTransition,
                    null,
                    source,
                    reason,
                    $"Lifecycle materialization registry can only register materialized handles. Current handle state: '{handle.State}'.");
            }

            if (_entries.TryGetValue(handle.Identity, out var existingEntry))
            {
                if (ReferenceEquals(existingEntry.Handle, handle))
                {
                    return LifecycleMaterializationRegistryOperationResult.Success(
                        handle.Identity,
                        LifecycleMaterializationRegistryOperationStatus.SucceededAlreadyRegistered,
                        existingEntry,
                        source,
                        reason,
                        "Lifecycle materialization entry was already registered for this handle.");
                }

                return LifecycleMaterializationRegistryOperationResult.Failure(
                    handle.Identity,
                    LifecycleMaterializationRegistryOperationStatus.RejectedDuplicateEntry,
                    existingEntry,
                    source,
                    reason,
                    "Lifecycle materialization registry rejected a duplicate entry for the same runtime content identity.");
            }

            var entry = new LifecycleMaterializedEntry(handle, source, reason);
            _entries.Add(handle.Identity, entry);

            return LifecycleMaterializationRegistryOperationResult.Success(
                handle.Identity,
                LifecycleMaterializationRegistryOperationStatus.SucceededRegistered,
                entry,
                source,
                reason,
                "Lifecycle materialization entry registered.");
        }

        public LifecycleMaterializationRegistryOperationResult RequestRelease(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            if (!TryGetEntry(identity, source, reason, out var entry, out var missingResult))
            {
                return missingResult;
            }

            return entry.RequestRelease(source, reason);
        }

        public LifecycleMaterializationRegistryOperationResult MarkReleased(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            if (!TryGetEntry(identity, source, reason, out var entry, out var missingResult))
            {
                return missingResult;
            }

            return entry.MarkReleased(source, reason);
        }

        public LifecycleMaterializationRegistryOperationResult MarkReleaseFailed(
            RuntimeContentIdentity identity,
            string source,
            string reason,
            string message)
        {
            if (!TryGetEntry(identity, source, reason, out var entry, out var missingResult))
            {
                return missingResult;
            }

            return entry.MarkReleaseFailed(source, reason, message);
        }


        public LifecycleMaterializationReleasePlan CreateReleasePlan(
            RuntimeContentOwner owner,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            ValidateOwner(owner);
            ValidateReleasePolicy(policy);

            return CreateReleasePlanCore(
                LifecycleMaterializationReleasePlanTargetKind.Owner,
                owner.Scope,
                owner,
                policy,
                source,
                reason,
                entry => entry.Owner == owner);
        }

        public LifecycleMaterializationReleasePlan CreateReleasePlan(
            RuntimeContentScope scope,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            ValidateScope(scope);
            ValidateReleasePolicy(policy);

            return CreateReleasePlanCore(
                LifecycleMaterializationReleasePlanTargetKind.Scope,
                scope,
                default(RuntimeContentOwner),
                policy,
                source,
                reason,
                entry => entry.Scope == scope);
        }


        public LifecycleMaterializationReleaseExecutionResult ExecuteReleasePlan(
            LifecycleMaterializationReleasePlan plan,
            Func<RuntimeReleaseRequest, RuntimeReleaseResult> releaseExecutor,
            string source,
            string reason)
        {
            if (!IsReleasePlanValid(plan))
            {
                return LifecycleMaterializationReleaseExecutionResult.InvalidPlan(
                    plan,
                    source,
                    reason,
                    "Lifecycle materialization release execution rejected an invalid or non-successful release plan.");
            }

            if (releaseExecutor == null)
            {
                throw new ArgumentNullException(nameof(releaseExecutor));
            }

            if (!plan.HasRequests)
            {
                return new LifecycleMaterializationReleaseExecutionResult(
                    plan,
                    LifecycleMaterializationReleaseExecutionStatus.SucceededNoRequests,
                    Array.Empty<RuntimeReleaseResult>(),
                    Array.Empty<LifecycleMaterializationRegistryOperationResult>(),
                    0,
                    0,
                    0,
                    0,
                    source,
                    reason,
                    "Lifecycle materialization release execution skipped an empty release plan.");
            }

            var releaseResults = new List<RuntimeReleaseResult>();
            var registryResults = new List<LifecycleMaterializationRegistryOperationResult>();
            int releaseRequested = 0;
            int released = 0;
            int releaseFailed = 0;
            int missingEntries = 0;

            RuntimeReleaseRequest[] requests = plan.SnapshotRequests();
            for (int i = 0; i < requests.Length; i++)
            {
                RuntimeReleaseRequest request = requests[i];
                var requestResult = RequestRelease(
                    request.Identity,
                    source,
                    reason);
                registryResults.Add(requestResult);

                if (requestResult.Failed)
                {
                    if (requestResult.Status == LifecycleMaterializationRegistryOperationStatus.RejectedMissingEntry)
                    {
                        missingEntries++;
                    }

                    releaseFailed++;
                    continue;
                }

                releaseRequested++;

                RuntimeReleaseResult releaseResult = ExecuteReleaseRequest(
                    request,
                    releaseExecutor,
                    source,
                    reason);
                releaseResults.Add(releaseResult);

                LifecycleMaterializationRegistryOperationResult registryResult;
                if (releaseResult.Succeeded)
                {
                    registryResult = MarkReleased(
                        request.Identity,
                        source,
                        reason);
                    if (registryResult.Succeeded)
                    {
                        released++;
                    }
                    else
                    {
                        releaseFailed++;
                    }
                }
                else
                {
                    registryResult = MarkReleaseFailed(
                        request.Identity,
                        source,
                        reason,
                        releaseResult.Message);
                    releaseFailed++;
                }

                registryResults.Add(registryResult);
            }

            var status = releaseFailed == 0
                && missingEntries == 0
                && released == requests.Length
                ? LifecycleMaterializationReleaseExecutionStatus.SucceededReleasedAll
                : LifecycleMaterializationReleaseExecutionStatus.FailedPartialRelease;

            string message = status == LifecycleMaterializationReleaseExecutionStatus.SucceededReleasedAll
                ? "Lifecycle materialization release plan executed explicitly and all entries were marked released."
                : "Lifecycle materialization release plan execution completed with one or more failures.";

            return new LifecycleMaterializationReleaseExecutionResult(
                plan,
                status,
                releaseResults.ToArray(),
                registryResults.ToArray(),
                releaseRequested,
                released,
                releaseFailed,
                missingEntries,
                source,
                reason,
                message);
        }

        public bool TryGet(RuntimeContentIdentity identity, out LifecycleMaterializedEntry entry)
        {
            ValidateIdentity(identity);
            return _entries.TryGetValue(identity, out entry);
        }

        public bool Contains(RuntimeContentIdentity identity)
        {
            ValidateIdentity(identity);
            return _entries.ContainsKey(identity);
        }

        public LifecycleMaterializedEntry[] Snapshot()
        {
            var snapshot = new LifecycleMaterializedEntry[_entries.Count];
            _entries.Values.CopyTo(snapshot, 0);
            return snapshot;
        }

        public LifecycleMaterializedEntry[] Snapshot(RuntimeContentOwner owner)
        {
            ValidateOwner(owner);

            var results = new List<LifecycleMaterializedEntry>();
            foreach (var entry in _entries.Values)
            {
                if (entry.Owner == owner)
                {
                    results.Add(entry);
                }
            }

            return results.ToArray();
        }

        public LifecycleMaterializedEntry[] Snapshot(RuntimeContentScope scope)
        {
            ValidateScope(scope);

            var results = new List<LifecycleMaterializedEntry>();
            foreach (var entry in _entries.Values)
            {
                if (entry.Scope == scope)
                {
                    results.Add(entry);
                }
            }

            return results.ToArray();
        }

        public string ToDiagnosticString()
        {
            return $"entries='{Count}' active='{ActiveCount}' releaseRequested='{ReleaseRequestedCount}' released='{ReleasedCount}' releaseFailed='{ReleaseFailedCount}'";
        }


        private LifecycleMaterializationReleasePlan CreateReleasePlanCore(
            LifecycleMaterializationReleasePlanTargetKind targetKind,
            RuntimeContentScope scope,
            RuntimeContentOwner owner,
            RuntimeReleasePolicy policy,
            string source,
            string reason,
            Func<LifecycleMaterializedEntry, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            int totalEntries = 0;
            int activeCandidates = 0;
            int releaseFailedCandidates = 0;
            int skippedReleaseRequested = 0;
            int skippedReleased = 0;
            var requests = new List<RuntimeReleaseRequest>();

            foreach (var entry in _entries.Values)
            {
                if (entry == null || !predicate(entry))
                {
                    continue;
                }

                totalEntries++;

                if (entry.IsActive)
                {
                    activeCandidates++;
                    requests.Add(CreateReleaseRequest(entry, policy, source, reason));
                    continue;
                }

                if (entry.IsReleaseFailed)
                {
                    releaseFailedCandidates++;
                    requests.Add(CreateReleaseRequest(entry, policy, source, reason));
                    continue;
                }

                if (entry.IsReleaseRequested)
                {
                    skippedReleaseRequested++;
                    continue;
                }

                if (entry.IsReleased)
                {
                    skippedReleased++;
                }
            }

            string message = requests.Count > 0
                ? "Lifecycle materialization release plan created."
                : "Lifecycle materialization release plan query found no release candidates.";

            return new LifecycleMaterializationReleasePlan(
                targetKind,
                scope,
                owner,
                policy,
                requests.ToArray(),
                totalEntries,
                activeCandidates,
                releaseFailedCandidates,
                skippedReleaseRequested,
                skippedReleased,
                source,
                reason,
                message);
        }

        private static RuntimeReleaseRequest CreateReleaseRequest(
            LifecycleMaterializedEntry entry,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            var context = new RuntimeScopeContext(entry.Owner, source, reason);
            return new RuntimeReleaseRequest(
                context,
                entry.Identity,
                policy,
                source,
                reason);
        }


        private static RuntimeReleaseResult ExecuteReleaseRequest(
            RuntimeReleaseRequest request,
            Func<RuntimeReleaseRequest, RuntimeReleaseResult> releaseExecutor,
            string source,
            string reason)
        {
            try
            {
                var releaseResult = releaseExecutor(request);
                if (!releaseResult.Request.IsValid || releaseResult.Identity != request.Identity)
                {
                    return RuntimeReleaseResult.Failure(
                        request,
                        RuntimeReleaseStatus.FailedInvalidRequest,
                        null,
                        RuntimeContentState.Unknown,
                        RuntimeContentState.Unknown,
                        source,
                        reason,
                        "Lifecycle materialization release executor returned an invalid or mismatched runtime release result.");
                }

                return releaseResult;
            }
            catch (Exception exception)
            {
                return RuntimeReleaseResult.AdapterFailure(
                    request,
                    source,
                    reason,
                    $"Lifecycle materialization release executor threw exception='{exception.GetType().Name}' message='{exception.Message}'.");
            }
        }

        private static bool IsReleasePlanValid(LifecycleMaterializationReleasePlan plan)
        {
            return plan.Succeeded
                && Enum.IsDefined(typeof(LifecycleMaterializationReleasePlanTargetKind), plan.TargetKind)
                && plan.TargetKind != LifecycleMaterializationReleasePlanTargetKind.Unknown
                && Enum.IsDefined(typeof(RuntimeContentScope), plan.Scope)
                && plan.Scope != RuntimeContentScope.Unknown
                && Enum.IsDefined(typeof(RuntimeReleasePolicy), plan.Policy)
                && plan.Policy != RuntimeReleasePolicy.Unknown
                && plan.RequestCount == plan.ActiveCandidates + plan.ReleaseFailedCandidates;
        }

        private int CountByState(LifecycleMaterializationEntryState state)
        {
            int count = 0;
            foreach (var entry in _entries.Values)
            {
                if (entry != null && entry.State == state)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetEntry(
            RuntimeContentIdentity identity,
            string source,
            string reason,
            out LifecycleMaterializedEntry entry,
            out LifecycleMaterializationRegistryOperationResult missingResult)
        {
            ValidateIdentity(identity);

            if (_entries.TryGetValue(identity, out entry))
            {
                missingResult = default(LifecycleMaterializationRegistryOperationResult);
                return true;
            }

            missingResult = LifecycleMaterializationRegistryOperationResult.Failure(
                identity,
                LifecycleMaterializationRegistryOperationStatus.RejectedMissingEntry,
                null,
                source,
                reason,
                "Lifecycle materialization registry operation rejected a missing entry.");
            return false;
        }


        private static void ValidateReleasePolicy(RuntimeReleasePolicy policy)
        {
            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), policy) || policy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(policy), policy, "Runtime release policy must be explicit.");
            }
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateScope(RuntimeContentScope scope)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentScope), scope) || scope == RuntimeContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Runtime content scope must be explicit.");
            }
        }
    }
}
