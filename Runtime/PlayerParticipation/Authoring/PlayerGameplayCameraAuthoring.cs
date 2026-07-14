using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit camera endpoint on one contextual Logical Player Actor.
    /// It carries no PlayerInput, Slot/Actor string identity, request identity,
    /// winner policy or lifecycle behavior.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Player Gameplay Camera Authoring")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 explicit contextual Player camera eligibility authoring.")]
    public sealed class PlayerGameplayCameraAuthoring : MonoBehaviour
    {
        [Header("Policy")]
        [SerializeField]
        private PlayerGameplayCameraRequiredness requiredness =
            PlayerGameplayCameraRequiredness.Optional;

        [Header("Explicit Actor-Owned References")]
        [SerializeField] private CameraRigComposer cameraRig;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;

        [Header("Arbitration Intent")]
        [SerializeField] private int precedence = 50;

        public PlayerGameplayCameraRequiredness Requiredness => requiredness;
        public CameraRigComposer CameraRig => cameraRig;
        public Transform FollowTarget => followTarget;
        public Transform LookAtTarget => lookAtTarget;
        public int Precedence => precedence;

        public bool HasExplicitCameraReferences =>
            cameraRig != null &&
            followTarget != null;
    }
}
