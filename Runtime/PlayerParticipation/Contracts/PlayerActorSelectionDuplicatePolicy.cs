using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session policy for comparing selected ActorProfile identities across Joined Player Slots.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-003 Session Actor-selection duplicate policy; GameApplicationAsset serialization remains Stable.")]
    public enum PlayerActorSelectionDuplicatePolicy
    {
        Unspecified = 0,
        AllowDuplicates = 10,
        UniqueAcrossJoinedSlots = 20
    }

    internal static class PlayerActorSelectionDuplicatePolicyExtensions
    {
        internal static bool IsDefinedPolicy(this PlayerActorSelectionDuplicatePolicy policy)
        {
            return Enum.IsDefined(typeof(PlayerActorSelectionDuplicatePolicy), policy) &&
                policy != PlayerActorSelectionDuplicatePolicy.Unspecified;
        }

        internal static bool RequiresUniqueActors(this PlayerActorSelectionDuplicatePolicy policy)
        {
            return policy == PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots;
        }
    }
}
