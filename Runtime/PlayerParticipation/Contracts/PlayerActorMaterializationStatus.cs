using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Outcome vocabulary for attached Logical Player Actor materialization.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.3 attached Logical Player Actor materialization result status.")]
    public enum PlayerActorMaterializationStatus
    {
        None = 0,

        SucceededStaged = 10,

        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedScopeTransition = 120,
        RejectedScopeCancellation = 130,
        RejectedStaleScope = 140,
        RejectedHostUnavailable = 150,
        RejectedHostNotJoined = 160,
        RejectedSlotMismatch = 170,
        RejectedProfileUnavailable = 180,
        RejectedInvalidProfile = 190,
        RejectedMissingLogicalActorPrefab = 200,
        RejectedInvalidLogicalActorPrefab = 210,

        FailedInstantiate = 300,
        FailedMissingPlayerActorDeclaration = 310,
        FailedMultiplePlayerActorDeclarations = 320,
        FailedUnexpectedActorDeclaration = 330,
        FailedUnexpectedPlayerInput = 340,
        FailedActorIdentity = 350,
        FailedRuntimeContentRegistration = 360,
        FailedRollback = 370
    }
}
