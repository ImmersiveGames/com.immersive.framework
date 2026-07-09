using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Editor-only request for creating or repairing the technical Cinemachine rig used by a Camera Product Surface.
    /// This type is intentionally not a runtime authority and does not perform lookup outside the supplied rig root.
    /// </summary>
    public sealed class CinemachineRigMaterializationRequest
    {
        public Transform RigRoot { get; set; }

        public UnityEngine.Camera UnityCamera { get; set; }

        public CinemachineCamera CinemachineCamera { get; set; }

        public Transform FollowTarget { get; set; }

        public Transform LookAtTarget { get; set; }

        public int Priority { get; set; } = 10;

        public bool RequireFollowTarget { get; set; } = true;

        public bool RequireLookAtTarget { get; set; }

        public bool CreateUnityCameraIfMissing { get; set; } = true;

        public bool CreateCinemachineCameraIfMissing { get; set; } = true;

        public bool UseUndo { get; set; } = true;

        public string UnityCameraObjectName { get; set; } = "Unity Camera";

        public string CinemachineCameraObjectName { get; set; } = "Cinemachine Camera";
    }
}
