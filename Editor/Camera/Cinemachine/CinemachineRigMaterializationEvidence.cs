using Immersive.Framework.Camera;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Editor.Camera.Cinemachine
{
    /// <summary>
    /// Provenance classification for one materialized Cinemachine technical component.
    /// ExternalOrUnknown is intentionally conservative: compatible pre-existing content
    /// can be used, but it is never silently adopted as Framework-owned.
    /// </summary>
    public enum CinemachineRigMaterializationOwnership
    {
        None = 0,
        FrameworkOwned = 10,
        ExternalOrUnknown = 20
    }

    /// <summary>
    /// Technical evidence produced by Cinemachine rig materialization.
    /// It is intended for inspector/debug output, QA and later Composer diagnostics.
    /// </summary>
    public sealed class CinemachineRigMaterializationEvidence
    {
        public CameraRigPresentationIntent PresentationIntent { get; internal set; }

        public int MaterializationRevision { get; internal set; }

        public UnityEngine.Camera UnityCamera { get; internal set; }

        public CinemachineBrain Brain { get; internal set; }

        public CinemachineCamera CinemachineCamera { get; internal set; }

        public CinemachineRigMaterializationOwnership CinemachineCameraOwnership { get; internal set; }

        public CinemachineComponentBase PositionControl { get; internal set; }

        public CinemachineRigMaterializationOwnership PositionControlOwnership { get; internal set; }

        public CinemachineComponentBase RotationControl { get; internal set; }

        public CinemachineRigMaterializationOwnership RotationControlOwnership { get; internal set; }

        public CinemachineFollow CinemachineFollow { get; internal set; }

        public Transform FollowTarget { get; internal set; }

        public Transform LookAtTarget { get; internal set; }

        public CinemachineCamera FrameworkOwnedCinemachineCamera =>
            CinemachineCameraOwnership ==
            CinemachineRigMaterializationOwnership.FrameworkOwned
                ? CinemachineCamera
                : null;

        public Component FrameworkOwnedPositionControl =>
            PositionControlOwnership ==
            CinemachineRigMaterializationOwnership.FrameworkOwned
                ? PositionControl
                : null;

        public Component FrameworkOwnedRotationControl =>
            RotationControlOwnership ==
            CinemachineRigMaterializationOwnership.FrameworkOwned
                ? RotationControl
                : null;
    }
}
