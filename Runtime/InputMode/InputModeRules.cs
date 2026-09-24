using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Stable. Pure InputMode validation helpers.
    /// These helpers do not read input or mutate Unity Input System state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public static class InputModeRules
    {
        public static bool IsValidKind(InputModeKind kind)
        {
            return Enum.IsDefined(typeof(InputModeKind), kind) && kind != InputModeKind.Unknown;
        }
    }
}
