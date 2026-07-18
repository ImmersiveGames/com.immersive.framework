using Immersive.Framework.InputMode;
using UnityEngine;

namespace Immersive.Framework.GlobalUi
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Global UI/Pause InputMode Runtime Bridge Registration")]
    internal sealed class PauseInputModeRuntimeBridgeRegistration : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit canonical Pause/InputMode bridge for this UIGlobal composition.")]
        private PauseInputModeUnityPlayerInputRuntimeBridge bridge;

        internal PauseInputModeUnityPlayerInputRuntimeBridge PauseInputModeRuntimeBridge =>
            bridge;

        internal bool TryConfigureBridge(
            PauseInputModeUnityPlayerInputRuntimeBridge value,
            out string issue)
        {
            if (value == null)
            {
                issue = "Pause InputMode runtime bridge registration requires a bridge reference.";
                return false;
            }

            bridge = value;
            issue = string.Empty;
            return true;
        }
    }
}
