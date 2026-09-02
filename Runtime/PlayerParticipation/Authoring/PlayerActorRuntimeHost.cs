using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Generic Framework-owned runtime composition for one Player Actor.
    /// It owns neither the physical PlayerInput boundary nor Actor-specific presentation or gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Actor Runtime Host")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-023 generic Player Actor Runtime Host composition for selected Actor Profile Presentation.")]
    public sealed class PlayerActorRuntimeHost : MonoBehaviour
    {
        [Header("Framework Actor Runtime")]
        [SerializeField]
        [Tooltip("Canonical Framework Player Actor declaration owned by this generic runtime host.")]
        private PlayerActorDeclaration playerActorDeclaration;

        [Header("Actor Presentation")]
        [SerializeField]
        [Tooltip("Explicit child mount for Actor-specific presentation. It does not own Framework Actor identity or PlayerInput.")]
        private Transform presentationMount;

        public PlayerActorDeclaration PlayerActorDeclaration => playerActorDeclaration;
        public Transform PresentationMount => presentationMount;
        public bool HasPlayerActorDeclaration => playerActorDeclaration != null;
        public bool HasPresentationMount => presentationMount != null;

        /// <summary>
        /// Validates only this generic runtime-host structure without materializing or binding runtime state.
        /// </summary>
        public bool TryValidateConfiguration(out string issue)
        {
            issue = string.Empty;

            if (playerActorDeclaration == null)
            {
                issue = "Player Actor Runtime Host requires an explicit PlayerActorDeclaration.";
                return false;
            }

            if (playerActorDeclaration.gameObject != gameObject)
            {
                issue = "Player Actor Runtime Host PlayerActorDeclaration must exist on the canonical Player Actor root.";
                return false;
            }

            PlayerActorDeclaration[] playerActorDeclarations =
                GetComponentsInChildren<PlayerActorDeclaration>(true);
            if (playerActorDeclarations.Length != 1 ||
                playerActorDeclarations[0] != playerActorDeclaration)
            {
                issue = $"Player Actor Runtime Host requires exactly one canonical PlayerActorDeclaration. Found '{playerActorDeclarations.Length}'.";
                return false;
            }

            ActorDeclaration[] actorDeclarations =
                GetComponentsInChildren<ActorDeclaration>(true);
            if (actorDeclarations.Length != 1 ||
                actorDeclarations[0] != playerActorDeclaration)
            {
                issue = $"Player Actor Runtime Host requires one canonical PlayerActorDeclaration and no additional ActorDeclaration. Found '{actorDeclarations.Length}'.";
                return false;
            }

            if (GetComponent<CharacterController>() == null)
            {
                issue = "Player Actor Runtime Host requires a CharacterController on the canonical Player Actor root.";
                return false;
            }

            if (GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Player Actor Runtime Host must not contain PlayerInput. PlayerInput belongs to the Local Player Host.";
                return false;
            }

            if (presentationMount == null)
            {
                issue = "Player Actor Runtime Host requires an explicit Presentation Mount child transform.";
                return false;
            }

            if (presentationMount == transform ||
                !presentationMount.IsChildOf(transform))
            {
                issue = "Player Actor Runtime Host Presentation Mount must be a child of the runtime host root.";
                return false;
            }

            if (presentationMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Player Actor Runtime Host Presentation Mount must not contain PlayerInput.";
                return false;
            }

            if (presentationMount.GetComponentInChildren<ActorDeclaration>(true) != null)
            {
                issue = "Player Actor Runtime Host Presentation Mount must not contain Framework Actor declarations.";
                return false;
            }

            return true;
        }
    }
}
