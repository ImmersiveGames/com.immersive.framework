using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Declares that one contextual Logical Player Actor participates in the
    /// gameplay Camera product.
    ///
    /// The referenced CameraRigComposer is the single authority for targets,
    /// target requirements and framing. This component carries only participation,
    /// requiredness and arbitration intent.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Gameplay Camera")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 explicit contextual Player camera eligibility authoring.")]
    public sealed class PlayerGameplayCameraAuthoring :
        MonoBehaviour
    {
        [SerializeField]
        private PlayerGameplayCameraRequiredness requiredness =
            PlayerGameplayCameraRequiredness.Optional;
        
        [SerializeField]
        private CameraRigComposer cameraRig;
        
        [SerializeField]
        private int precedence = 50;

        public PlayerGameplayCameraRequiredness Requiredness =>
            requiredness;

        public CameraRigComposer CameraRig =>
            cameraRig;

        public int Precedence =>
            precedence;

        public Transform FollowTarget
        {
            get
            {
                return TryResolveCameraTargets(
                        out CameraResolvedTargets targets,
                        out _)
                    ? targets.FollowTarget
                    : null;
            }
        }

        public Transform LookAtTarget
        {
            get
            {
                return TryResolveCameraTargets(
                        out CameraResolvedTargets targets,
                        out _)
                    ? targets.LookAtTarget
                    : null;
            }
        }

        public bool HasExplicitCameraReferences =>
            cameraRig != null &&
            TryResolveCameraTargets(
                out CameraResolvedTargets targets,
                out _) &&
            targets.FollowTarget != null;

        public bool TryResolveCameraTargets(
            out CameraResolvedTargets targets,
            out string diagnostic)
        {
            targets = default;

            if (cameraRig == null)
            {
                diagnostic =
                    "Player Gameplay Camera requires an explicit Camera Rig Composer.";
                return false;
            }

            CameraTargetResolveResult resolution =
                cameraRig.ResolveConfiguredCameraTargets();

            if (!resolution.IsSucceeded)
            {
                diagnostic =
                    $"Player Gameplay Camera could not resolve its Camera Rig targets. {resolution.BlockingIssue}";
                return false;
            }

            targets =
                resolution.Targets;

            diagnostic =
                resolution.DiagnosticSummary;

            return true;
        }
    }
}
