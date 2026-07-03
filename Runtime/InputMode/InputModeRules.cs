using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Pure InputMode validation helpers.
    /// These helpers do not read input or mutate Unity Input System state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A pure InputMode validation helpers.")]
    public static class InputModeRules
    {
        public static bool IsValidKind(InputModeKind kind)
        {
            return Enum.IsDefined(typeof(InputModeKind), kind) && kind != InputModeKind.Unknown;
        }
    }
}
