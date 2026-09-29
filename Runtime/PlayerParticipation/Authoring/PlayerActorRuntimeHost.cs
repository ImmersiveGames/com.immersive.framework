using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Generic Framework-owned runtime composition for one Player Actor.
    /// It defines one Actor occurrence root and owns neither PlayerInput nor Actor-specific gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Actor Runtime Host")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-038 Actor occurrence runtime host; optional visual content is subordinate.")]
    public sealed class PlayerActorRuntimeHost : MonoBehaviour
    {
        [Header("Framework Actor Runtime")]
        [SerializeField]
        [Tooltip("Canonical Framework Player Actor declaration owned by this generic runtime host.")]
        private PlayerActorDeclaration playerActorDeclaration;

        [Header("Optional Actor Visual Content")]
        [SerializeField]
        [Tooltip("Optional explicit child mount for visual content. The Actor root remains the physical and spatial authority.")]
        private Transform presentationMount;

        public PlayerActorDeclaration PlayerActorDeclaration => playerActorDeclaration;
        public Transform VisualContentMount => presentationMount;
        public bool HasPlayerActorDeclaration => playerActorDeclaration != null;
        public bool HasVisualContentMount => presentationMount != null;

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

            if (GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Player Actor Runtime Host must not contain PlayerInput. PlayerInput belongs to the Local Player Host.";
                return false;
            }

            if (presentationMount != null &&
                (presentationMount == transform || !presentationMount.IsChildOf(transform)))
            {
                issue = "Optional Player Actor Runtime Host visual-content mount must be a child of the Actor occurrence root.";
                return false;
            }

            if (presentationMount != null && presentationMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Optional visual-content mount must not contain PlayerInput.";
                return false;
            }

            if (presentationMount != null && presentationMount.GetComponentInChildren<ActorDeclaration>(true) != null)
            {
                issue = "Optional visual-content mount must not contain Framework Actor declarations.";
                return false;
            }

            return true;
        }
    }
}
