using Immersive.Framework.InputMode;

namespace Immersive.Framework.GlobalUi
{
    internal readonly struct GlobalUiPauseRuntimeBindingResult
    {
        private GlobalUiPauseRuntimeBindingResult(
            bool succeeded,
            bool integrationConfigured,
            string status,
            string message,
            int registrationCount,
            PauseInputModeUnityPlayerInputRuntimeBridge bridge)
        {
            Succeeded = succeeded;
            IntegrationConfigured = integrationConfigured;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            RegistrationCount = registrationCount;
            Bridge = bridge;
        }

        internal bool Succeeded { get; }
        internal bool IntegrationConfigured { get; }
        internal string Status { get; }
        internal string Message { get; }
        internal int RegistrationCount { get; }
        internal PauseInputModeUnityPlayerInputRuntimeBridge Bridge { get; }

        internal static GlobalUiPauseRuntimeBindingResult OptionalAbsent()
        {
            return new GlobalUiPauseRuntimeBindingResult(
                true,
                false,
                "OptionalIntegrationAbsent",
                "UIGlobal has no Pause InputMode runtime bridge registration.",
                0,
                null);
        }

        internal static GlobalUiPauseRuntimeBindingResult Bound(
            PauseInputModeUnityPlayerInputRuntimeBridge bridge)
        {
            return new GlobalUiPauseRuntimeBindingResult(
                true,
                true,
                "Bound",
                "UIGlobal Pause InputMode runtime bridge is explicitly bound.",
                1,
                bridge);
        }

        internal static GlobalUiPauseRuntimeBindingResult Rejected(
            string status,
            string message,
            int registrationCount)
        {
            return new GlobalUiPauseRuntimeBindingResult(
                false,
                registrationCount > 0,
                status,
                message,
                registrationCount,
                null);
        }
    }
}
