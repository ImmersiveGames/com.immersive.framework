using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit Unity pipeline for releasing adapter-materialized ContentAnchor content by runtime owner context.
    /// This is not automatic Route/Activity lifecycle wiring and does not select consumers; it composes physical release, logical release and logical binding cleanup.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-D explicit ContentAnchor materialization scope release proof; no automatic lifecycle or consumer wiring.")]
    internal sealed class UnityContentAnchorMaterializationScopeReleasePipeline
    {
        private readonly UnityObjectRuntimeReleaseAdapter _releaseAdapter;
        private readonly string _source;

        internal UnityContentAnchorMaterializationScopeReleasePipeline(
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            string source)
        {
            _releaseAdapter = releaseAdapter ?? throw new ArgumentNullException(nameof(releaseAdapter));
            _source = source.NormalizeTextOrFallback(nameof(UnityContentAnchorMaterializationScopeReleasePipeline));
        }

        internal UnityRuntimeMaterializedObjectRegistry Registry => _releaseAdapter.Registry;

        internal UnityContentAnchorMaterializationScopeReleasePipelineResult ReleaseScope(
            FrameworkRuntimeHost runtimeHost,
            RuntimeScopeContext context,
            RuntimeReleasePolicy releasePolicy,
            string reason)
        {
            string normalizedReason = reason.NormalizeText();
            int registryCountBefore = Registry.Count;
            int registryActiveBefore = Registry.ActiveCount;
            int matchedPhysicalEntries = CountMatchingEntries(context);
            var bindingCleanupResult = default(ContentAnchorBindingLifecycleResult);
            var lastPhysicalReleaseResult = default(RuntimeReleaseResult);
            var lastLogicalReleaseResult = default(RuntimeReleaseResult);

            if (runtimeHost == null)
            {
                return Failure(
                    UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedMissingRuntimeHost,
                    context,
                    releasePolicy,
                    matchedPhysicalEntries,
                    0,
                    0,
                    registryCountBefore,
                    registryActiveBefore,
                    bindingCleanupResult,
                    lastPhysicalReleaseResult,
                    lastLogicalReleaseResult,
                    normalizedReason,
                    "ContentAnchor materialization scope release requires an explicit FrameworkRuntimeHost.");
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return Failure(
                    UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedMissingRuntimeContentRuntime,
                    context,
                    releasePolicy,
                    matchedPhysicalEntries,
                    0,
                    0,
                    registryCountBefore,
                    registryActiveBefore,
                    bindingCleanupResult,
                    lastPhysicalReleaseResult,
                    lastLogicalReleaseResult,
                    normalizedReason,
                    "ContentAnchor materialization scope release requires RuntimeContentRuntime.");
            }

            if (!context.IsValid)
            {
                return Failure(
                    UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedInvalidContext,
                    context,
                    releasePolicy,
                    matchedPhysicalEntries,
                    0,
                    0,
                    registryCountBefore,
                    registryActiveBefore,
                    bindingCleanupResult,
                    lastPhysicalReleaseResult,
                    lastLogicalReleaseResult,
                    normalizedReason,
                    "ContentAnchor materialization scope release requires a valid runtime scope context.");
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy)
                || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(releasePolicy), releasePolicy, "Runtime release policy must be explicit.");
            }

            int physicalReleaseRequests = 0;
            int logicalReleaseResults = 0;
            foreach (var evidence in Registry.Snapshot())
            {
                if (evidence == null || evidence.Owner != context.Owner || evidence.PhysicalReleaseRequested)
                {
                    continue;
                }

                var releaseExecution = ContentAnchorReleaseExecution.Execute(
                    runtimeContentRuntime,
                    _releaseAdapter,
                    context,
                    evidence.Identity,
                    releasePolicy,
                    _source,
                    normalizedReason);

                lastPhysicalReleaseResult = releaseExecution.PhysicalReleaseResult;
                if (!lastPhysicalReleaseResult.Succeeded)
                {
                    return Failure(
                        UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedPhysicalRelease,
                        context,
                        releasePolicy,
                        matchedPhysicalEntries,
                        physicalReleaseRequests,
                        logicalReleaseResults,
                        registryCountBefore,
                        registryActiveBefore,
                        bindingCleanupResult,
                        lastPhysicalReleaseResult,
                        lastLogicalReleaseResult,
                        normalizedReason,
                        lastPhysicalReleaseResult.Message);
                }

                physicalReleaseRequests++;

                lastLogicalReleaseResult = releaseExecution.LogicalReleaseResult;
                if (!lastLogicalReleaseResult.Succeeded)
                {
                    return Failure(
                        UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedLogicalRelease,
                        context,
                        releasePolicy,
                        matchedPhysicalEntries,
                        physicalReleaseRequests,
                        logicalReleaseResults,
                        registryCountBefore,
                        registryActiveBefore,
                        bindingCleanupResult,
                        lastPhysicalReleaseResult,
                        lastLogicalReleaseResult,
                        normalizedReason,
                        lastLogicalReleaseResult.Message);
                }

                logicalReleaseResults++;
            }

            bindingCleanupResult = runtimeHost.UnbindContentAnchorRuntimeOwner(
                context.Owner,
                _source,
                normalizedReason);
            if (!bindingCleanupResult.Succeeded)
            {
                return Failure(
                    UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedBindingCleanup,
                    context,
                    releasePolicy,
                    matchedPhysicalEntries,
                    physicalReleaseRequests,
                    logicalReleaseResults,
                    registryCountBefore,
                    registryActiveBefore,
                    bindingCleanupResult,
                    lastPhysicalReleaseResult,
                    lastLogicalReleaseResult,
                    normalizedReason,
                    bindingCleanupResult.Message);
            }

            return UnityContentAnchorMaterializationScopeReleasePipelineResult.Success(
                context,
                releasePolicy,
                matchedPhysicalEntries,
                physicalReleaseRequests,
                logicalReleaseResults,
                registryCountBefore,
                registryActiveBefore,
                Registry.Count,
                Registry.ActiveCount,
                bindingCleanupResult,
                lastPhysicalReleaseResult,
                lastLogicalReleaseResult,
                _source,
                normalizedReason,
                physicalReleaseRequests > 0
                    ? "ContentAnchor materialization scope release physically and logically released adapter-created content."
                    : "ContentAnchor materialization scope release completed with no active adapter-created content.");
        }

        private int CountMatchingEntries(RuntimeScopeContext context)
        {
            if (!context.IsValid)
            {
                return 0;
            }

            int count = 0;
            foreach (var evidence in Registry.Snapshot())
            {
                if (evidence != null && evidence.Owner == context.Owner)
                {
                    count++;
                }
            }

            return count;
        }

        private UnityContentAnchorMaterializationScopeReleasePipelineResult Failure(
            UnityContentAnchorMaterializationScopeReleasePipelineStatus status,
            RuntimeScopeContext context,
            RuntimeReleasePolicy releasePolicy,
            int matchedPhysicalEntries,
            int physicalReleaseRequests,
            int logicalReleaseResults,
            int registryCountBefore,
            int registryActiveBefore,
            ContentAnchorBindingLifecycleResult bindingCleanupResult,
            RuntimeReleaseResult lastPhysicalReleaseResult,
            RuntimeReleaseResult lastLogicalReleaseResult,
            string reason,
            string message)
        {
            return UnityContentAnchorMaterializationScopeReleasePipelineResult.Failure(
                status,
                context,
                releasePolicy,
                matchedPhysicalEntries,
                physicalReleaseRequests,
                logicalReleaseResults,
                registryCountBefore,
                registryActiveBefore,
                Registry.Count,
                Registry.ActiveCount,
                bindingCleanupResult,
                lastPhysicalReleaseResult,
                lastLogicalReleaseResult,
                _source,
                reason,
                message);
        }
    }
}
