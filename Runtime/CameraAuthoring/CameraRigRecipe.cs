using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(
        fileName = "CameraRigRecipe",
        menuName = "Immersive Framework/Camera/Camera Rig Recipe")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Reusable presentation intent for a materialized Cinemachine Camera rig.")]
    public sealed class CameraRigRecipe : ScriptableObject
    {
        [Header("Camera Behavior Defaults")]

        [Tooltip(
            "Reusable camera presentation behavior. " +
            "The current Camera Rig Composer supports Follow only.")]
        [SerializeField]
        private CameraRigPresentationIntent presentationIntent =
            CameraRigPresentationIntent.Follow;

        [Tooltip(
            "Declares the kind of target source expected by the Camera Rig Composer. " +
            "The Recipe does not assign a target-source component or scene Transform.")]
        [SerializeField]
        private CameraTargetSourceKind targetSourceKind =
            CameraTargetSourceKind.ExplicitTransform;

        [Tooltip(
            "Controls whether a Follow target is required, optional or unused. " +
            "Follow presentation currently requires Follow to participate.")]
        [SerializeField]
        private CameraTargetRequirement followRequirement =
            CameraTargetRequirement.Required;

        [Tooltip(
            "Controls whether a Look At target is required, optional or unused.")]
        [SerializeField]
        private CameraTargetRequirement lookAtRequirement =
            CameraTargetRequirement.Optional;

        [Tooltip(
            "Default local offset applied to Cinemachine Follow when the Composer runs Apply / Rebuild.")]
        [SerializeField]
        private Vector3 followOffset =
            new Vector3(0f, 5f, -8f);

        [Header("Advanced Materialization Defaults")]

        [Tooltip(
            "Allows Apply / Rebuild to create a local Cinemachine Camera when the Composer has no assigned camera.")]
        [SerializeField]
        private bool createCinemachineCameraIfMissing = true;

        [Tooltip(
            "Name used only when Apply / Rebuild creates a Cinemachine Camera. " +
            "An empty value falls back to 'Cinemachine Camera'.")]
        [SerializeField]
        private string cinemachineCameraObjectName =
            "Cinemachine Camera";

        [Tooltip(
            "Writes detailed Apply / Rebuild diagnostics through the framework logging system.")]
        [SerializeField]
        private bool logApplyRebuildDiagnostics = true;

        public CameraRigPresentationIntent PresentationIntent =>
            presentationIntent;

        public CameraTargetSourceKind TargetSourceKind =>
            targetSourceKind;

        public CameraTargetRequirement FollowRequirement =>
            followRequirement;

        public CameraTargetRequirement LookAtRequirement =>
            lookAtRequirement;

        public Vector3 FollowOffset =>
            followOffset;

        public bool CreateCinemachineCameraIfMissing =>
            createCinemachineCameraIfMissing;

        public string CinemachineCameraObjectName =>
            cinemachineCameraObjectName.NormalizeTextOrFallback(
                "Cinemachine Camera");

        public bool LogApplyRebuildDiagnostics =>
            logApplyRebuildDiagnostics;
    }
}
