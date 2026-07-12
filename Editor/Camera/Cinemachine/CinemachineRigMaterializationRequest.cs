using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Editor-only request for creating or repairing a Cinemachine technical rig.
    /// It does not perform lookup outside the supplied rig root.
    /// </summary>
    public sealed class CinemachineRigMaterializationRequest
    {
        public Transform RigRoot { get; set; }

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
        /// Materializes the Cinemachine position pipeline required for Follow
        /// intent. A Follow target without a position control is incomplete.
        /// </summary>
        public bool CreateCinemachineFollowIfMissing { get; set; } = true;

        public bool UseUndo { get; set; } = true;

        public string UnityCameraObjectName { get; set; } = "Unity Camera";

        public string CinemachineCameraObjectName { get; set; } =
            "Cinemachine Camera";
    }
}
