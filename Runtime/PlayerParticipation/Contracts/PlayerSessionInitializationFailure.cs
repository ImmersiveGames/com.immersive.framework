using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Typed reason why required Player Session initial configuration could
    /// not be resolved. This does not describe a mutable runtime operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 Player Session initialization configuration failure vocabulary.")]
    public enum PlayerSessionInitializationFailure
    {
        None = 0,
        MissingRequiredConfiguration = 10,
        InvalidPlayerSessionProfile = 20,
        InvalidEffectiveConfiguration = 30
    }
}
