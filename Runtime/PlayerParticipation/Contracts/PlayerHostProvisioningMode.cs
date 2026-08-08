using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Initial provisioning intent for one Player Host. This is configuration
    /// evidence only; it does not create, admit or assign a host.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 effective Player Session host provisioning intent.")]
    public enum PlayerHostProvisioningMode
    {
        Unspecified = 0,
        ManagerProvisioned = 10,
        SceneProvided = 20
    }

    internal static class PlayerHostProvisioningModeExtensions
    {
        internal static bool IsDefinedMode(
            this PlayerHostProvisioningMode mode)
        {
            return FrameworkEnumValidation.IsDefinedAndNot(
                mode,
                PlayerHostProvisioningMode.Unspecified);
        }

        internal static void ThrowIfInvalid(
            this PlayerHostProvisioningMode mode,
            string paramName)
        {
            FrameworkEnumValidation.ThrowIfUndefinedOr(
                mode,
                PlayerHostProvisioningMode.Unspecified,
                paramName,
                "Player Host provisioning mode must be specified.");
        }
    }
}
