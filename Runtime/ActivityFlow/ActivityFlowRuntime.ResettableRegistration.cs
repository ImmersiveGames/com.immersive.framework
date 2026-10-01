using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;

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

        internal void SetResettableOwnerRegistration(
            ResettableOwnerRegistrationRuntime registration)
        {
            _resettableOwnerRegistration = registration;
        }

        internal void SetStableObjectBindingRegistry(StableObjectBindingRegistry registry)
        {
            _stableObjectBindingRegistry = registry;
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
            if (_stableObjectBindingRegistry == null) return;
            if (!_stableObjectBindingRegistry.TryRegisterOwnerContent(
                    owner,
                    ResolveMaterializedActivitySceneRoots(compositionResult),
                    out string diagnostic))
            {
                throw new InvalidOperationException(
                    "Stable Object Binding admission failed for Activity content. " + diagnostic);
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
            if (_stableObjectBindingRegistry == null || targetActivity == null) return string.Empty;
            return _stableObjectBindingRegistry.TryRollbackOwner(CreateActivityOwner(targetActivity), out string diagnostic)
                ? string.Empty
                : " Stable Object Binding rollback failed. " + diagnostic;
        }

        private void CommitStableObjectBindings(RuntimeContentOwner owner)
        {
            if (_stableObjectBindingRegistry == null) return;
            if (!_stableObjectBindingRegistry.TryCommitOwner(owner, out string diagnostic))
                throw new InvalidOperationException("Stable Object Binding commit failed for Activity content. " + diagnostic);
        }

        private void ReleaseStableObjectBindingsForPreviousActivity(ActivityAsset previousActivity)
        {
            if (_stableObjectBindingRegistry == null || previousActivity == null) return;
            if (!_stableObjectBindingRegistry.TryReleaseOwner(CreateActivityOwner(previousActivity), out string diagnostic))
                throw new InvalidOperationException("Stable Object Binding release failed for the exiting Activity. " + diagnostic);
        }
    }
}
