using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Broad gameplay role vocabulary for actor identity diagnostics.
    /// This is not a prefab type, enemy class, inventory category, movement controller or spawn policy.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A actor role identity vocabulary.")]
    public enum ActorRole
    {
        Unknown = 0,
        Protagonist = 10,
        Enemy = 20,
        Boss = 30,
        Ally = 40,
        Neutral = 50,
        Objective = 60,
        Interactable = 70
    }
}
