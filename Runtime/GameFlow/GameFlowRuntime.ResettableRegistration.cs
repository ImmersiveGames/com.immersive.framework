using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Actors;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        /// <summary>
        /// IF-ADR-035 RESET-035-B: forwards the host-owned Resettable registration runtime to the
        /// Route/Activity transactions that own Resettable registration.
        /// </summary>
        internal void SetResettableOwnerRegistration(
            ResettableOwnerRegistrationRuntime registration)
        {
            _routeLifecycleRuntime.SetResettableOwnerRegistration(registration);
        }

        internal void SetStableObjectBindingRegistry(StableObjectBindingRegistry registry)
        {
            _routeLifecycleRuntime.SetStableObjectBindingRegistry(registry);
        }

        internal void SetActorOccurrenceReferenceRuntime(ActorOccurrenceReferenceRuntime runtime)
        {
            _routeLifecycleRuntime.SetActorOccurrenceReferenceRuntime(runtime);
        }
    }
}
