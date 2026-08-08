using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Initial Actor selection intent for one effective Player Session
    /// configuration. Actor lifecycle remains owned by its existing runtime.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 effective Player Session Actor resolution intent.")]
    public enum PlayerActorResolutionPolicy
    {
        Unspecified = 0,
        ResolveConfiguredDefault = 10,
        LeaveUnresolved = 20
    }

    internal static class PlayerActorResolutionPolicyExtensions
    {
        internal static bool IsDefinedPolicy(
            this PlayerActorResolutionPolicy policy)
        {
            return FrameworkEnumValidation.IsDefinedAndNot(
                policy,
                PlayerActorResolutionPolicy.Unspecified);
        }

        internal static void ThrowIfInvalid(
            this PlayerActorResolutionPolicy policy,
            string paramName)
        {
            FrameworkEnumValidation.ThrowIfUndefinedOr(
                policy,
                PlayerActorResolutionPolicy.Unspecified,
                paramName,
                "Player Actor resolution policy must be specified.");
        }
    }
}
