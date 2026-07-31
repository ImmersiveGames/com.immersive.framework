
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Logical kind of Pause request.
    /// Request kind does not encode an input binding, UI action or lifecycle transition.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum PauseRequestKind
    {
        /// <summary>Invalid default value. Do not use for canonical Pause requests.</summary>
        Unknown = 0,

        Pause = 10,
        Resume = 20,
        Toggle = 30
    }
}
