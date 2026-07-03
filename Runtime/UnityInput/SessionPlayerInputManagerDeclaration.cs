using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Unity-facing declaration that marks Unity's official PlayerInputManager as Session-scoped.
    /// The framework validates this component as evidence only. It does not own joining, instantiate players, switch action maps,
    /// replace PlayerInputManager or create a custom input manager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputManager))]
    [AddComponentMenu("Immersive Framework/Unity Input/Session Player Input Manager Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31B Session-scoped Unity PlayerInputManager declaration.")]
    public sealed class SessionPlayerInputManagerDeclaration : MonoBehaviour
    {
        [Tooltip("Stable diagnostic id for this session-level Unity PlayerInputManager integration point. Do not use GameObject names or scene paths as functional keys.")]
        [SerializeField] private string managerId = "session.player-input-manager";

        [Tooltip("Human-readable diagnostic label only.")]
        [SerializeField] private string displayName = "Session PlayerInputManager";

        [Tooltip("Optional explicit PlayerInputManager evidence. If empty, the declaration checks PlayerInputManager on the same GameObject.")]
        [SerializeField] private PlayerInputManager playerInputManager;

        [Tooltip("Diagnostic reason/source for this declaration.")]
        [SerializeField] private string reason = "session.player-input-manager.declaration";

        public UnityInputPlayerInputManagerScope Scope => UnityInputPlayerInputManagerScope.Session;

        public string ManagerId => managerId.NormalizeTextOrFallback("session.player-input-manager");

        public string DisplayName => displayName.NormalizeTextOrFallback(name);

        public PlayerInputManager PlayerInputManager => playerInputManager != null ? playerInputManager : GetComponent<PlayerInputManager>();

        public bool HasPlayerInputManagerEvidence => PlayerInputManager != null;

        public string Reason => reason.NormalizeText();

        internal void ConfigureForDiagnostics(
            string id,
            string label,
            PlayerInputManager managerReference,
            string declarationReason)
        {
            managerId = id.NormalizeText();
            displayName = label.NormalizeTextOrFallback(name);
            playerInputManager = managerReference;
            reason = declarationReason.NormalizeText();
        }

        private void Reset()
        {
            playerInputManager = GetComponent<PlayerInputManager>();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
        }
    }
}
