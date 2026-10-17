using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Actors;
using UnityEngine;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// IF-ADR-035 RESET-035-B: owner-aware Resettable registration inside the official Activity
    /// transaction. Target registrations use the target Activity owner and the materialized Activity
    /// scene roots before commit; pre-commit failure rolls them back; Activity exit releases them.
    /// Mirrors the Pause Activity Binding transaction pattern.
    /// </summary>
    internal sealed partial class ActivityFlowRuntime
    {
        private ResettableOwnerRegistrationRuntime _resettableOwnerRegistration;
        private StableObjectBindingRegistry _stableObjectBindingRegistry;
        private ActorOccurrenceReferenceRuntime _actorOccurrenceReferenceRuntime;

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
                throw new InvalidOperationException("Stable Object Binding commit failed for Activity content. " + diagnostic);
            if (_actorOccurrenceReferenceRuntime != null &&
                !_actorOccurrenceReferenceRuntime.TryCommitOwner(owner, out string actorDiagnostic))
                throw new InvalidOperationException("Actor occurrence publication commit failed for Activity content. " + actorDiagnostic);
        }

        private void ReleaseStableObjectBindingsForPreviousActivity(ActivityAsset previousActivity)
        {
            if (previousActivity == null) return;
            RuntimeContentOwner owner = CreateActivityOwner(previousActivity);
            if (_stableObjectBindingRegistry != null &&
                !_stableObjectBindingRegistry.TryReleaseOwner(owner, out string diagnostic))
                throw new InvalidOperationException("Stable Object Binding release failed for the exiting Activity. " + diagnostic);
            if (_actorOccurrenceReferenceRuntime != null &&
                !_actorOccurrenceReferenceRuntime.TryReleaseOwner(owner, out string actorDiagnostic))
                throw new InvalidOperationException("Actor occurrence publication release failed for the exiting Activity. " + actorDiagnostic);
        }
    }
}
