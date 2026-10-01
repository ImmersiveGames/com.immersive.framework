using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private ResettableOwnerRegistrationRuntime _resettableOwnerRegistration;
        private StableObjectBindingRegistry _stableObjectBindings;

        /// <summary>
        /// IF-ADR-035 RESET-035-B host-owned Resettable registration over the existing ResetRegistry.
        /// No static access: the instance is injected into Route/Activity transactions only.
        /// </summary>
        internal ResettableOwnerRegistrationRuntime ResettableOwnerRegistration =>
            _resettableOwnerRegistration ??= new ResettableOwnerRegistrationRuntime(ResetRegistry);

        internal StableObjectBindingRegistry StableObjectBindings =>
            _stableObjectBindings ??= new StableObjectBindingRegistry();

        private void ApplyResettableOwnerRegistration()
        {
            _gameFlowRuntime?.SetResettableOwnerRegistration(
                ResettableOwnerRegistration);
        }

        private void ApplyStableObjectBindings()
        {
            _gameFlowRuntime?.SetStableObjectBindingRegistry(StableObjectBindings);
        }
    }
}
