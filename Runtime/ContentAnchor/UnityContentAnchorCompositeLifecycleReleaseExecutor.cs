using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit composite release executor for lifecycle-registered Unity ContentAnchor materializations.
    /// It composes physical Unity release, logical RuntimeContent release, ContentAnchor binding cleanup and LifecycleMaterializationRegistry state updates.
    /// It is not Route/Activity exit wiring and it does not materialize content automatically.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-S explicit composite lifecycle release executor proof; no Route/Activity auto-release wiring.")]
    internal sealed class UnityContentAnchorCompositeLifecycleReleaseExecutor
    {
        private readonly UnityObjectRuntimeReleaseAdapter _releaseAdapter;
        private readonly string _source;

        internal UnityContentAnchorCompositeLifecycleReleaseExecutor(
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            string source)
        {
            _releaseAdapter = releaseAdapter ?? throw new ArgumentNullException(nameof(releaseAdapter));
            _source = source.NormalizeTextOrFallback(nameof(UnityContentAnchorCompositeLifecycleReleaseExecutor));
        }

        internal UnityRuntimeMaterializedObjectRegistry PhysicalRegistry => _releaseAdapter.Registry;

        internal UnityContentAnchorCompositeLifecycleReleaseResult Execute(
            FrameworkRuntimeHost runtimeHost,
            LifecycleMaterializationRegistry lifecycleRegistry,
            LifecycleMaterializationReleasePlan plan,
            string reason)
        {
            string resolvedReason = reason.NormalizeText();
            if (runtimeHost == null)
            {
                return CreateResult(
                    plan,
                    UnityContentAnchorCompositeLifecycleReleaseStatus.FailedMissingRuntimeHost,
                    lifecycleRegistry,
                    null,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    resolvedReason,
                    "Composite lifecycle release requires an explicit FrameworkRuntimeHost.");
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return CreateResult(
                    plan,
                    UnityContentAnchorCompositeLifecycleReleaseStatus.FailedMissingRuntimeContentRuntime,
                    lifecycleRegistry,
                    runtimeHost,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    resolvedReason,
                    "Composite lifecycle release requires RuntimeContentRuntime.");
            }

            if (lifecycleRegistry == null)
            {
                throw new ArgumentNullException(nameof(lifecycleRegistry));
            }

            if (!IsReleasePlanValid(plan))
            {
                return CreateResult(
                    plan,
                    UnityContentAnchorCompositeLifecycleReleaseStatus.FailedInvalidPlan,
                    lifecycleRegistry,
                    runtimeHost,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    resolvedReason,
                    "Composite lifecycle release rejected an invalid or non-successful release plan.");
            }

            if (!plan.HasRequests)
            {
                return CreateResult(
                    plan,
                    UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededNoRequests,
                    lifecycleRegistry,
                    runtimeHost,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    resolvedReason,
                    "Composite lifecycle release skipped an empty release plan.");
            }

            int physicalReleaseRequests = 0;
            int logicalRuntimeReleaseResults = 0;
            int bindingCleanupResults = 0;
            int bindingRemoved = 0;
            int lifecycleReleaseRequested = 0;
            int lifecycleReleased = 0;
            int lifecycleReleaseFailed = 0;
            int missingEntries = 0;
            var releasedIdentities = new List<RuntimeContentIdentity>();
            var cleanupOwners = new List<RuntimeContentOwner>();

            RuntimeReleaseRequest[] requests = plan.SnapshotRequests();
            for (int i = 0; i < requests.Length; i++)
            {
                RuntimeReleaseRequest request = requests[i];
                var lifecycleRequest = lifecycleRegistry.RequestRelease(
                    request.Identity,
                    _source,
                    resolvedReason + ".lifecycle-request." + i);
                if (lifecycleRequest.Failed)
                {
                    if (lifecycleRequest.Status == LifecycleMaterializationRegistryOperationStatus.RejectedMissingEntry)
                    {
                        missingEntries++;
                    }

                    lifecycleReleaseFailed++;
                    return CreateResult(
                        plan,
                        lifecycleRequest.Status == LifecycleMaterializationRegistryOperationStatus.RejectedMissingEntry
                            ? UnityContentAnchorCompositeLifecycleReleaseStatus.FailedLifecycleRequestRelease
                            : UnityContentAnchorCompositeLifecycleReleaseStatus.FailedPartialRelease,
                        lifecycleRegistry,
                        runtimeHost,
                        physicalReleaseRequests,
                        logicalRuntimeReleaseResults,
                        bindingCleanupResults,
                        bindingRemoved,
                        lifecycleReleaseRequested,
                        lifecycleReleased,
                        lifecycleReleaseFailed,
                        missingEntries,
                        resolvedReason,
                        lifecycleRequest.Message);
                }

                lifecycleReleaseRequested++;

                var releaseExecution = ContentAnchorReleaseExecution.Execute(
                    runtimeContentRuntime,
                    _releaseAdapter,
                    request,
                    _source,
                    resolvedReason + ".logical-release." + i);
                RuntimeReleaseResult physicalRelease = releaseExecution.PhysicalReleaseResult;
                if (!physicalRelease.Succeeded)
                {
                    lifecycleRegistry.MarkReleaseFailed(
                        request.Identity,
                        _source,
                        resolvedReason + ".physical-failed." + i,
                        physicalRelease.Message);
                    lifecycleReleaseFailed++;
                    return CreateResult(
                        plan,
                        UnityContentAnchorCompositeLifecycleReleaseStatus.FailedPhysicalRelease,
                        lifecycleRegistry,
                        runtimeHost,
                        physicalReleaseRequests,
                        logicalRuntimeReleaseResults,
                        bindingCleanupResults,
                        bindingRemoved,
                        lifecycleReleaseRequested,
                        lifecycleReleased,
                        lifecycleReleaseFailed,
                        missingEntries,
                        resolvedReason,
                        physicalRelease.Message);
                }

                physicalReleaseRequests++;

                RuntimeReleaseResult logicalRelease = releaseExecution.LogicalReleaseResult;
                if (!logicalRelease.Succeeded)
                {
                    lifecycleRegistry.MarkReleaseFailed(
                        request.Identity,
                        _source,
                        resolvedReason + ".logical-failed." + i,
                        logicalRelease.Message);
                    lifecycleReleaseFailed++;
                    return CreateResult(
                        plan,
                        UnityContentAnchorCompositeLifecycleReleaseStatus.FailedLogicalRelease,
                        lifecycleRegistry,
                        runtimeHost,
                        physicalReleaseRequests,
                        logicalRuntimeReleaseResults,
                        bindingCleanupResults,
                        bindingRemoved,
                        lifecycleReleaseRequested,
                        lifecycleReleased,
                        lifecycleReleaseFailed,
                        missingEntries,
                        resolvedReason,
                        logicalRelease.Message);
                }

                logicalRuntimeReleaseResults++;
                releasedIdentities.Add(request.Identity);
                AddOwnerIfMissing(cleanupOwners, request.Owner);
            }

            for (int i = 0; i < cleanupOwners.Count; i++)
            {
                var cleanup = runtimeHost.UnbindContentAnchorRuntimeOwner(
                    cleanupOwners[i],
                    _source,
                    resolvedReason + ".binding-cleanup." + i);
                bindingCleanupResults++;
                bindingRemoved += cleanup.RemovedCount;
                if (!cleanup.Succeeded)
                {
                    for (int j = 0; j < releasedIdentities.Count; j++)
                    {
                        lifecycleRegistry.MarkReleaseFailed(
                            releasedIdentities[j],
                            _source,
                            resolvedReason + ".binding-cleanup-failed." + j,
                            cleanup.Message);
                    }

                    lifecycleReleaseFailed += releasedIdentities.Count;
                    return CreateResult(
                        plan,
                        UnityContentAnchorCompositeLifecycleReleaseStatus.FailedBindingCleanup,
                        lifecycleRegistry,
                        runtimeHost,
                        physicalReleaseRequests,
                        logicalRuntimeReleaseResults,
                        bindingCleanupResults,
                        bindingRemoved,
                        lifecycleReleaseRequested,
                        lifecycleReleased,
                        lifecycleReleaseFailed,
                        missingEntries,
                        resolvedReason,
                        cleanup.Message);
                }
            }

            for (int i = 0; i < releasedIdentities.Count; i++)
            {
                var markReleased = lifecycleRegistry.MarkReleased(
                    releasedIdentities[i],
                    _source,
                    resolvedReason + ".mark-released." + i);
                if (markReleased.Succeeded)
                {
                    lifecycleReleased++;
                    continue;
                }

                lifecycleReleaseFailed++;
                return CreateResult(
                    plan,
                    UnityContentAnchorCompositeLifecycleReleaseStatus.FailedLifecycleMarkReleased,
                    lifecycleRegistry,
                    runtimeHost,
                    physicalReleaseRequests,
                    logicalRuntimeReleaseResults,
                    bindingCleanupResults,
                    bindingRemoved,
                    lifecycleReleaseRequested,
                    lifecycleReleased,
                    lifecycleReleaseFailed,
                    missingEntries,
                    resolvedReason,
                    markReleased.Message);
            }

            var status = lifecycleReleaseFailed == 0
                && missingEntries == 0
                && lifecycleReleased == requests.Length
                && physicalReleaseRequests == requests.Length
                && logicalRuntimeReleaseResults == requests.Length
                ? UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededReleasedAll
                : UnityContentAnchorCompositeLifecycleReleaseStatus.FailedPartialRelease;

            return CreateResult(
                plan,
                status,
                lifecycleRegistry,
                runtimeHost,
                physicalReleaseRequests,
                logicalRuntimeReleaseResults,
                bindingCleanupResults,
                bindingRemoved,
                lifecycleReleaseRequested,
                lifecycleReleased,
                lifecycleReleaseFailed,
                missingEntries,
                resolvedReason,
                status == UnityContentAnchorCompositeLifecycleReleaseStatus.SucceededReleasedAll
                    ? "Composite lifecycle release physically released Unity content, logically released RuntimeContent, cleaned ContentAnchor binding and updated lifecycle registry state."
                    : "Composite lifecycle release completed with one or more failures.");
        }

        private UnityContentAnchorCompositeLifecycleReleaseResult CreateResult(
            LifecycleMaterializationReleasePlan plan,
            UnityContentAnchorCompositeLifecycleReleaseStatus status,
            LifecycleMaterializationRegistry lifecycleRegistry,
            FrameworkRuntimeHost runtimeHost,
            int physicalReleaseRequests,
            int logicalRuntimeReleaseResults,
            int bindingCleanupResults,
            int bindingRemoved,
            int lifecycleReleaseRequested,
            int lifecycleReleased,
            int lifecycleReleaseFailed,
            int missingEntries,
            string reason,
            string message)
        {
            int runtimeHandles = CountRuntimeHandles(runtimeHost, plan);
            return new UnityContentAnchorCompositeLifecycleReleaseResult(
                plan,
                status,
                physicalReleaseRequests,
                logicalRuntimeReleaseResults,
                bindingCleanupResults,
                bindingRemoved,
                lifecycleReleaseRequested,
                lifecycleReleased,
                lifecycleReleaseFailed,
                missingEntries,
                lifecycleRegistry?.Count ?? 0,
                lifecycleRegistry?.ActiveCount ?? 0,
                lifecycleRegistry?.ReleaseRequestedCount ?? 0,
                lifecycleRegistry?.ReleasedCount ?? 0,
                lifecycleRegistry?.ReleaseFailedCount ?? 0,
                PhysicalRegistry.Count,
                PhysicalRegistry.ActiveCount,
                PhysicalRegistry.PhysicalReleaseRequestedCount,
                runtimeHandles,
                _source,
                reason,
                message);
        }

        private static int CountRuntimeHandles(
            FrameworkRuntimeHost runtimeHost,
            LifecycleMaterializationReleasePlan plan)
        {
            var runtimeContentRuntime = runtimeHost?.RuntimeContentRuntime;
            if (runtimeContentRuntime == null || !plan.Succeeded)
            {
                return 0;
            }

            int count = 0;
            var owners = new List<RuntimeContentOwner>();
            RuntimeReleaseRequest[] requests = plan.SnapshotRequests();
            for (int i = 0; i < requests.Length; i++)
            {
                AddOwnerIfMissing(owners, requests[i].Owner);
            }

            for (int i = 0; i < owners.Count; i++)
            {
                if (runtimeContentRuntime.TryCreateScopeContext(
                        owners[i],
                        nameof(UnityContentAnchorCompositeLifecycleReleaseExecutor),
                        "count-runtime-handles",
                        out var context))
                {
                    count += runtimeContentRuntime.SnapshotHandles(context).Length;
                }
            }

            return count;
        }

        private static bool IsReleasePlanValid(LifecycleMaterializationReleasePlan plan)
        {
            return plan.Succeeded
                && plan.TargetKind != LifecycleMaterializationReleasePlanTargetKind.Unknown
                && plan.Scope != RuntimeContentScope.Unknown
                && plan.Policy != RuntimeReleasePolicy.Unknown
                && plan.RequestCount == plan.ActiveCandidates + plan.ReleaseFailedCandidates;
        }

        private static void AddOwnerIfMissing(List<RuntimeContentOwner> owners, RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                return;
            }

            for (int i = 0; i < owners.Count; i++)
            {
                if (owners[i] == owner)
                {
                    return;
                }
            }

            owners.Add(owner);
        }
    }
}
