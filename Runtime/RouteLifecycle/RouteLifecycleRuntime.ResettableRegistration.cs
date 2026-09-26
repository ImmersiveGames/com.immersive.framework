using Immersive.Framework.Authoring;
using Immersive.Framework.Reset.Unity;

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

        internal void SetResettableOwnerRegistration(
            ResettableOwnerRegistrationRuntime registration)
        {
            _resettableOwnerRegistration = registration;
            _activityFlowRuntime.SetResettableOwnerRegistration(registration);
        }

        private bool TryPrepareRouteResettableRegistration(
            RouteAsset route,
            RouteSceneCompositionResult compositionResult,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (_resettableOwnerRegistration == null || route == null)
            {
                return true;
            }

            if (_resettableOwnerRegistration.TryRegisterOwnerContent(
                    CreateRouteOwner(route),
                    ResolveMaterializedRouteSceneRoots(compositionResult),
                    source,
                    reason,
                    out string registrationDiagnostic))
            {
                return true;
            }

            diagnostic = "Resettable registration blocked Route admission. " + registrationDiagnostic;
            return false;
        }

        private void RollbackRouteResettableRegistration(
            RouteAsset route,
            string source,
            string reason)
        {
            if (_resettableOwnerRegistration == null || route == null)
            {
                return;
            }

            _resettableOwnerRegistration.TryRollbackOwner(
                CreateRouteOwner(route),
                source,
                reason,
                out _);
        }

        private bool TryReleasePreviousRouteResettableRegistration(
            RouteAsset previousRoute,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (_resettableOwnerRegistration == null || previousRoute == null)
            {
                return true;
            }

            if (_resettableOwnerRegistration.TryReleaseOwner(
                    CreateRouteOwner(previousRoute),
                    source,
                    reason,
                    out string releaseDiagnostic))
            {
                return true;
            }

            diagnostic = "Resettable registration release failed for the exiting Route. " + releaseDiagnostic;
            return false;
        }
    }
}
