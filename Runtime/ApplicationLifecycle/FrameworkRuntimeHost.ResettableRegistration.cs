using Immersive.Framework.Reset.Unity;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private ResettableOwnerRegistrationRuntime _resettableOwnerRegistration;

        /// <summary>
        /// IF-ADR-035 RESET-035-B host-owned Resettable registration over the existing ResetRegistry.
        /// No static access: the instance is injected into Route/Activity transactions only.
        /// </summary>
        internal ResettableOwnerRegistrationRuntime ResettableOwnerRegistration =>
            _resettableOwnerRegistration ??= new ResettableOwnerRegistrationRuntime(ResetRegistry);

        private void ApplyResettableOwnerRegistration()
        {
            _gameFlowRuntime?.SetResettableOwnerRegistration(
                ResettableOwnerRegistration);
        }
    }
}
