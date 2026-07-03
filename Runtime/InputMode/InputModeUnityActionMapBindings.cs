
using Immersive.Framework.ApiStatus;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Canonical QA/default InputMode-to-action-map bindings.
    /// Projects may replace these names through later authoring; this type performs no Unity Input behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B canonical InputMode Unity action map bindings.")]
    public static class InputModeUnityActionMapBindings
    {
        public static InputModeUnityActionMapBinding Gameplay(string actionMapName)
        {
            return new InputModeUnityActionMapBinding(
                InputModeKind.Gameplay,
                UnityInputActionMapName.From(actionMapName),
                true);
        }

        public static InputModeUnityActionMapBinding PauseOverlay(string actionMapName)
        {
            return new InputModeUnityActionMapBinding(
                InputModeKind.PauseOverlay,
                UnityInputActionMapName.From(actionMapName),
                true);
        }

        public static InputModeUnityActionMapBinding FrontendMenu(string actionMapName)
        {
            return new InputModeUnityActionMapBinding(
                InputModeKind.FrontendMenu,
                UnityInputActionMapName.From(actionMapName),
                true);
        }

        public static InputModeUnityActionMapBinding InputLocked()
        {
            return new InputModeUnityActionMapBinding(
                InputModeKind.InputLocked,
                UnityInputActionMapName.From(string.Empty),
                false);
        }

        public static InputModeUnityActionMapBinding[] CanonicalPlayerUi()
        {
            return new[]
            {
                Gameplay("Player"),
                PauseOverlay("UI"),
                FrontendMenu("UI"),
                InputLocked()
            };
        }
    }
}
