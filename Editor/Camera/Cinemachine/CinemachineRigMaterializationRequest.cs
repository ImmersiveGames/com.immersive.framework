using Immersive.Framework.Camera;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Editor-only request for creating or repairing one supported Cinemachine rig.
    /// Target resolution is completed by the caller; this request never authorizes
    /// scene/global lookup or Camera output arbitration.
    /// </summary>
    public sealed class CinemachineRigMaterializationRequest
    {
        public Transform RigRoot { get; set; }

        public CameraRigPresentationIntent PresentationIntent { get; set; } =
            CameraRigPresentationIntent.Follow;

        /// <summary>
        /// When true, materialization also requires or creates one Unity Camera
        /// and CinemachineBrain. CameraRigComposer always sets this to false.
        /// Output-authoring tools may set it to true explicitly.
        /// </summary>
        public bool MaterializeUnityOutput { get; set; }

        public UnityEngine.Camera UnityCamera { get; set; }

        public CinemachineCamera CinemachineCamera { get; set; }

        public Transform FollowTarget { get; set; }

        public Transform LookAtTarget { get; set; }

        public bool RequireFollowTarget { get; set; } = true;

        public bool RequireLookAtTarget { get; set; }

        public bool CreateUnityCameraIfMissing { get; set; }

        public bool CreateCinemachineCameraIfMissing { get; set; } = true;

        /// <summary>
        /// Retained for the existing Follow materialization contract. C3 dispatches
        /// explicitly by PresentationIntent and never uses this as model fallback.
        /// </summary>
        public bool CreateCinemachineFollowIfMissing { get; set; } = true;

        public Vector3 FollowOffset { get; set; } =
            new Vector3(0f, 5f, -8f);

        public float MountedPositionDamping { get; set; }

        public float MountedRotationDamping { get; set; }

        public Vector3 ThirdPersonShoulderOffset { get; set; } =
            new Vector3(0.5f, -0.4f, 0f);

        public float ThirdPersonVerticalArmLength { get; set; } = 0.4f;

        public float ThirdPersonCameraSide { get; set; } = 1f;

        public float ThirdPersonCameraDistance { get; set; } = 2f;

        public Vector3 ThirdPersonDamping { get; set; } =
            new Vector3(0.1f, 0.5f, 0.3f);

        /// <summary>
        /// Durable provenance supplied by the owning Composer. These references are
        /// evidence only: a pre-existing component that is not exactly one of these
        /// references is ExternalOrUnknown and must never be adopted implicitly.
        /// </summary>
        public CinemachineCamera FrameworkOwnedCinemachineCamera { get; set; }

        public Component FrameworkOwnedPositionControl { get; set; }

        public Component FrameworkOwnedRotationControl { get; set; }

        public int PreviousMaterializationRevision { get; set; }

        public bool UseUndo { get; set; } = true;

        public string UnityCameraObjectName { get; set; } = "Unity Camera";

        public string CinemachineCameraObjectName { get; set; } =
            "Cinemachine Camera";
    }
}
