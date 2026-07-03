using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Minimal actor category vocabulary.
    /// This is not a movement controller, prefab type, character class or spawn policy.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A minimal actor identity vocabulary.")]
    public enum ActorKind
    {
        Unknown = 0,
        Player = 10,
        NonPlayer = 20
    }
}
