using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Actors;
using UnityEngine;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed class StableObjectCompositionCleanupException : InvalidOperationException
    {
        internal StableObjectCompositionCleanupException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// IF-ADR-035 RESET-035-B: owner-aware Resettable registration inside the official Activity
    /// transaction. Target registrations use the target Activity owner and the materialized Activity
    /// scene roots before commit; pre-commit failure rolls them back; Activity exit releases them.
    /// Mirrors the Pause Activity Binding transaction pattern.
    /// </summary>
    internal sealed partial class ActivityFlowRuntime
    {
        private sealed class DeferredCompositionSceneRelease
        {
            internal DeferredCompositionSceneRelease(
                ActivityAsset activity,
                RuntimeContentOwner owner,
                string routeInstanceId,
                string source,
                string reason)
            {
                Activity = activity;
                Owner = owner;
                RouteInstanceId = routeInstanceId ?? string.Empty;
                Source = source ?? string.Empty;
                Reason = reason ?? string.Empty;
            }

            internal ActivityAsset Activity { get; }
            internal RuntimeContentOwner Owner { get; }
            internal string RouteInstanceId { get; }
            internal string Source { get; }
            internal string Reason { get; }
        }

        private ResettableOwnerRegistrationRuntime _resettableOwnerRegistration;
        private StableObjectBindingRegistry _stableObjectBindingRegistry;
        private ActorOccurrenceReferenceRuntime _actorOccurrenceReferenceRuntime;
        private ActorGroupRuntime _actorGroupRuntime;
        private readonly List<DeferredCompositionSceneRelease> _deferredCompositionSceneReleases =
            new List<DeferredCompositionSceneRelease>();

        internal void SetResettableOwnerRegistration(
            ResettableOwnerRegistrationRuntime registration)
        {
            _resettableOwnerRegistration = registration;
        }

        internal void SetStableObjectBindingRegistry(StableObjectBindingRegistry registry)
        {
            _stableObjectBindingRegistry = registry;
        }

        internal void SetActorOccurrenceReferenceRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            _actorOccurrenceReferenceRuntime = runtime;
        }

        internal void SetActorGroupRuntime(ActorGroupRuntime runtime)
        {
            _actorGroupRuntime = runtime;
        }

        private void PrepareResettableRegistration(
            RuntimeContentOwner owner,
            ActivitySceneCompositionResult compositionResult,
            string source,
            string reason)
        {
            if (_resettableOwnerRegistration == null)
            {
                return;
            }

            if (!_resettableOwnerRegistration.TryRegisterOwnerContent(
                    owner,
                    ResolveMaterializedActivitySceneRoots(compositionResult),
                    source,
                    reason,
                    out string diagnostic))
            {
                throw new InvalidOperationException(
                    "Resettable registration blocked Activity admission. " +
                    diagnostic);
            }
        }

        private void PrepareStableObjectBindings(
            RuntimeContentOwner owner,
            ActivitySceneCompositionResult compositionResult)
        {
            IReadOnlyList<GameObject> roots = ResolveMaterializedActivitySceneRoots(compositionResult);
            if (_stableObjectBindingRegistry != null &&
                !_stableObjectBindingRegistry.TryRegisterOwnerContent(owner, roots, out string diagnostic))
            {
                throw new InvalidOperationException(
                    "Stable Object Binding admission failed for Activity content. " + diagnostic);
            }
            if (_actorOccurrenceReferenceRuntime != null &&
                !_actorOccurrenceReferenceRuntime.TryRegisterOwnerContent(owner, roots, out string actorDiagnostic))
            {
                _stableObjectBindingRegistry?.TryRollbackOwner(owner, out _);
                throw new InvalidOperationException(
                    "Actor occurrence publication admission failed for Activity content. " + actorDiagnostic);
            }
            if (_actorGroupRuntime != null &&
                !_actorGroupRuntime.TryRegisterOwnerContent(owner, roots, out string groupDiagnostic))
            {
                _actorOccurrenceReferenceRuntime?.TryRollbackOwner(owner, out _);
                _stableObjectBindingRegistry?.TryRollbackOwner(owner, out _);
                throw new InvalidOperationException(
                    "Actor Group access admission failed for Activity content. " + groupDiagnostic);
            }
        }

        private string RollbackTargetResettableRegistration(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            if (_resettableOwnerRegistration == null || targetActivity == null)
            {
                return string.Empty;
            }

            return _resettableOwnerRegistration.TryRollbackOwner(
                CreateActivityOwner(targetActivity),
                source,
                reason,
                out string diagnostic)
                ? string.Empty
                : " Resettable registration compensation failed. " + diagnostic;
        }

        private void ReleaseResettableRegistrationForPreviousActivity(
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            if (_resettableOwnerRegistration == null || previousActivity == null)
            {
                return;
            }

            if (!_resettableOwnerRegistration.TryReleaseOwner(
                    CreateActivityOwner(previousActivity),
                    source,
                    reason,
                    out string diagnostic))
            {
                throw new InvalidOperationException(
                    "Resettable registration release failed for the exiting Activity. " +
                    diagnostic);
            }
        }

        private string RollbackTargetStableObjectBindings(
            ActivityAsset targetActivity)
        {
            if (targetActivity == null) return string.Empty;
            RuntimeContentOwner owner = CreateActivityOwner(targetActivity);
            string diagnostic = string.Empty;
            if (_actorGroupRuntime != null &&
                !_actorGroupRuntime.TryRollbackOwner(owner, out diagnostic))
                return " Actor Group rollback failed. " + diagnostic;
            if (_stableObjectBindingRegistry != null &&
                !_stableObjectBindingRegistry.TryRollbackOwner(owner, out diagnostic))
                return " Stable Object Binding rollback failed. " + diagnostic;
            if (_actorOccurrenceReferenceRuntime != null &&
                !_actorOccurrenceReferenceRuntime.TryRollbackOwner(owner, out diagnostic))
                return " Actor occurrence publication rollback failed. " + diagnostic;
            return string.Empty;
        }

        private void CommitStableObjectBindings(RuntimeContentOwner owner)
        {
            if (_stableObjectBindingRegistry != null &&
                !_stableObjectBindingRegistry.TryCommitOwner(owner, out string diagnostic))
            {
                _actorOccurrenceReferenceRuntime?.TryRollbackOwner(owner, out _);
                _actorGroupRuntime?.TryRollbackOwner(owner, out _);
                _stableObjectBindingRegistry.TryRollbackOwner(owner, out _);
                throw new InvalidOperationException("Stable Object Binding commit failed for Activity content. " + diagnostic);
            }
            if (_actorOccurrenceReferenceRuntime != null &&
                !_actorOccurrenceReferenceRuntime.TryCommitOwner(owner, out string actorDiagnostic))
            {
                _stableObjectBindingRegistry?.TryRollbackOwner(owner, out _);
                _actorGroupRuntime?.TryRollbackOwner(owner, out _);
                _actorOccurrenceReferenceRuntime.TryRollbackOwner(owner, out _);
                throw new InvalidOperationException("Actor occurrence publication commit failed for Activity content. " + actorDiagnostic);
            }
            if (_actorGroupRuntime != null &&
                !_actorGroupRuntime.TryCommitOwner(owner, out string groupDiagnostic))
            {
                _actorOccurrenceReferenceRuntime?.TryRollbackOwner(owner, out _);
                _stableObjectBindingRegistry?.TryRollbackOwner(owner, out _);
                _actorGroupRuntime.TryRollbackOwner(owner, out _);
                throw new InvalidOperationException("Actor Group access commit failed for Activity content. " + groupDiagnostic);
            }
        }

        private Task<string> ActivateStableObjectCompositionsAsync(RuntimeContentOwner owner)
        {
            return _stableObjectBindingRegistry != null
                ? _stableObjectBindingRegistry.ActivateOwnerCompositionsAsync(owner)
                : Task.FromResult(string.Empty);
        }

        private async Task ReleaseStableObjectBindingsForPreviousActivityAsync(
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            if (previousActivity == null) return;
            RuntimeContentOwner owner = CreateActivityOwner(previousActivity);
            string bindingDiagnostic = _stableObjectBindingRegistry != null
                ? await _stableObjectBindingRegistry.ReleaseOwnerAsync(owner)
                : string.Empty;
            if (!string.IsNullOrEmpty(bindingDiagnostic))
            {
                RememberDeferredCompositionSceneRelease(previousActivity, owner, source, reason);
                throw new StableObjectCompositionCleanupException(
                    "Stable Object Binding composition release remains pending. " + bindingDiagnostic);
            }

            if (_actorOccurrenceReferenceRuntime != null &&
                !_actorOccurrenceReferenceRuntime.TryReleaseOwner(owner, out string actorDiagnostic))
            {
                throw new InvalidOperationException(
                    "Actor occurrence publication release failed. " + actorDiagnostic);
            }

            if (_actorGroupRuntime != null &&
                !_actorGroupRuntime.TryReleaseOwner(owner, out string groupDiagnostic))
            {
                throw new InvalidOperationException("Actor Group release failed. " + groupDiagnostic);
            }
        }

        internal async Task<string> ReleaseNonCurrentActivityObjectEntryBindingsAsync(
            ActivityAsset retainedActivity)
        {
            RuntimeContentOwner? retainedOwner = retainedActivity != null
                ? CreateActivityOwner(retainedActivity)
                : (RuntimeContentOwner?)null;

            for (int index = _deferredCompositionSceneReleases.Count - 1; index >= 0; index--)
            {
                DeferredCompositionSceneRelease receipt = _deferredCompositionSceneReleases[index];
                if (!string.Equals(receipt.RouteInstanceId, _currentRouteInstanceId, StringComparison.Ordinal))
                {
                    return "Deferred Object Entry composition cleanup belongs to a different Route instance; " +
                        "release is blocked to avoid unloading a scene from a newer occurrence by its reused path. " +
                        $"owner='{receipt.Owner.StableText}' receiptRouteInstance='{receipt.RouteInstanceId}' " +
                        $"currentRouteInstance='{_currentRouteInstanceId}'.";
                }

                if (retainedOwner.HasValue && receipt.Owner.Equals(retainedOwner.Value))
                {
                    return "Deferred Object Entry composition owner is still the retained current Activity. " +
                        $"owner='{receipt.Owner.StableText}' routeInstance='{receipt.RouteInstanceId}'.";
                }
            }

            string bindingIssue = _stableObjectBindingRegistry != null
                ? await _stableObjectBindingRegistry.ReleaseNonCurrentOwnerBindingsAsync(
                    RuntimeContentScope.Activity,
                    retainedOwner)
                : string.Empty;
            if (!string.IsNullOrEmpty(bindingIssue)) return bindingIssue;

            for (int index = _deferredCompositionSceneReleases.Count - 1; index >= 0; index--)
            {
                DeferredCompositionSceneRelease receipt = _deferredCompositionSceneReleases[index];
                if (_actorOccurrenceReferenceRuntime != null &&
                    !_actorOccurrenceReferenceRuntime.TryReleaseOwner(
                        receipt.Owner,
                        out string actorDiagnostic))
                {
                    return "Deferred Actor occurrence publication release failed. " + actorDiagnostic;
                }

                if (_actorGroupRuntime != null &&
                    !_actorGroupRuntime.TryReleaseOwner(
                        receipt.Owner,
                        out string groupDiagnostic))
                {
                    return "Deferred Actor Group release failed. " + groupDiagnostic;
                }

                ActivitySceneReleaseResult releaseResult =
                    await _activitySceneCompositionRuntime.ReleaseOnActivityChangeAsync(
                        receipt.Activity,
                        receipt.RouteInstanceId,
                        receipt.Source,
                        receipt.Reason);
                if (releaseResult.HasBlockingIssues)
                {
                    return "Deferred Activity scene release remains blocked until Object Entry composition cleanup completes. " +
                        releaseResult.ToDiagnosticString();
                }

                _deferredCompositionSceneReleases.RemoveAt(index);
            }

            return string.Empty;
        }

        private void RememberDeferredCompositionSceneRelease(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            for (int index = 0; index < _deferredCompositionSceneReleases.Count; index++)
            {
                DeferredCompositionSceneRelease existing = _deferredCompositionSceneReleases[index];
                if (existing.Owner.Equals(owner) &&
                    string.Equals(existing.RouteInstanceId, _currentRouteInstanceId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            _deferredCompositionSceneReleases.Add(new DeferredCompositionSceneRelease(
                activity,
                owner,
                _currentRouteInstanceId,
                source,
                reason));
        }
    }
}
