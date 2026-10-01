using Immersive.Framework.Authoring;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// IF-ADR-035 RESET-035-B: owner-aware Resettable registration inside the Route start transaction.
    /// Route content is registered with the target Route owner over the materialized Route scene roots,
    /// rolled back when Route start fails before the Route becomes current, and released when the
    /// previous Route content exits.
    /// </summary>
    internal sealed partial class RouteLifecycleRuntime
    {
        private ResettableOwnerRegistrationRuntime _resettableOwnerRegistration;
        private StableObjectBindingRegistry _stableObjectBindingRegistry;

        internal void SetResettableOwnerRegistration(
            ResettableOwnerRegistrationRuntime registration)
        {
            _resettableOwnerRegistration = registration;
            _activityFlowRuntime.SetResettableOwnerRegistration(registration);
        }

        internal void SetStableObjectBindingRegistry(StableObjectBindingRegistry registry)
        {
            _stableObjectBindingRegistry = registry;
            _activityFlowRuntime.SetStableObjectBindingRegistry(registry);
        }

        private bool TryPrepareRouteResettableRegistration(
            RouteAsset route,
            RouteSceneCompositionResult compositionResult,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (route == null)
            {
                return TryPrepareRouteStableObjectBindings(route, compositionResult, out diagnostic);
            }

            if (_resettableOwnerRegistration != null && !_resettableOwnerRegistration.TryRegisterOwnerContent(
                    CreateRouteOwner(route),
                    ResolveMaterializedRouteSceneRoots(compositionResult),
                    source,
                    reason,
                    out string registrationDiagnostic))
            {
                diagnostic = "Resettable registration blocked Route admission. " + registrationDiagnostic;
                return false;
            }

            if (TryPrepareRouteStableObjectBindings(route, compositionResult, out diagnostic)) return true;
            RollbackRouteResettableRegistration(route, source, "stable-object-binding-admission-failed");
            return false;
        }

        private bool TryPrepareRouteStableObjectBindings(
            RouteAsset route,
            RouteSceneCompositionResult compositionResult,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (_stableObjectBindingRegistry == null || route == null) return true;
            if (_stableObjectBindingRegistry.TryRegisterOwnerContent(
                    CreateRouteOwner(route),
                    ResolveMaterializedRouteSceneRoots(compositionResult),
                    out diagnostic)) return true;
            diagnostic = "Stable Object Binding admission failed for Route content. " + diagnostic;
            return false;
        }

        private void CommitRouteStableObjectBindings(RouteAsset route)
        {
            if (_stableObjectBindingRegistry == null || route == null) return;
            if (!_stableObjectBindingRegistry.TryCommitOwner(CreateRouteOwner(route), out string diagnostic))
                throw new System.InvalidOperationException("Stable Object Binding commit failed for Route content. " + diagnostic);
        }

        private void RollbackRouteResettableRegistration(
            RouteAsset route,
            string source,
            string reason)
        {
            if (route == null) return;
            RuntimeContentOwner owner = CreateRouteOwner(route);
            _resettableOwnerRegistration?.TryRollbackOwner(owner, source, reason, out _);
            _stableObjectBindingRegistry?.TryRollbackOwner(owner, out _);
        }

        private bool TryReleasePreviousRouteResettableRegistration(
            RouteAsset previousRoute,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (previousRoute == null)
            {
                return true;
            }

            string releaseDiagnostic = string.Empty;
            bool resettableReleased = _resettableOwnerRegistration == null || _resettableOwnerRegistration.TryReleaseOwner(
                    CreateRouteOwner(previousRoute),
                    source,
                    reason,
                    out releaseDiagnostic);
            if (!resettableReleased)
            {
                diagnostic = "Resettable registration release failed for the exiting Route. " + releaseDiagnostic;
                return false;
            }

            if (_stableObjectBindingRegistry == null || _stableObjectBindingRegistry.TryReleaseOwner(
                    CreateRouteOwner(previousRoute), out string bindingDiagnostic))
            {
                return true;
            }

            diagnostic = "Stable Object Binding release failed for the exiting Route. " + bindingDiagnostic;
            return false;
        }
    }
}
