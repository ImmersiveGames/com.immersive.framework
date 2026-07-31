
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Stable. Canonical InputMode definition factory.
    /// Definitions are logical framework postures, not Unity action-map names.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public static class InputModeDefinitions
    {
        public static InputModeDefinition Gameplay(string source, string reason)
        {
            return Create(InputModeKind.Gameplay, "gameplay", "Gameplay", source, reason);
        }

        public static InputModeDefinition PauseOverlay(string source, string reason)
        {
            return Create(InputModeKind.PauseOverlay, "pause-overlay", "Pause Overlay", source, reason);
        }

        public static InputModeDefinition FrontendMenu(string source, string reason)
        {
            return Create(InputModeKind.FrontendMenu, "frontend-menu", "Frontend Menu", source, reason);
        }

        public static InputModeDefinition InputLocked(string source, string reason)
        {
            return Create(InputModeKind.InputLocked, "input-locked", "Input Locked", source, reason);
        }

        public static InputModeDefinition FromKind(InputModeKind kind, string source, string reason)
        {
            switch (kind)
            {
                case InputModeKind.Gameplay:
                    return Gameplay(source, reason);
                case InputModeKind.PauseOverlay:
                    return PauseOverlay(source, reason);
                case InputModeKind.FrontendMenu:
                    return FrontendMenu(source, reason);
                case InputModeKind.InputLocked:
                    return InputLocked(source, reason);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "InputMode kind must be explicit.");
            }
        }

        private static InputModeDefinition Create(InputModeKind kind, string id, string displayName, string source, string reason)
        {
            return new InputModeDefinition(kind, InputModeId.From(id), displayName, source, reason);
        }
    }
}
