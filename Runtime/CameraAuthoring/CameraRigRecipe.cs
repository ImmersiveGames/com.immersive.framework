using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "CameraRigRecipe", menuName = "Immersive Framework/Camera/Camera Rig Recipe")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Reusable presentation intent for a materialized Cinemachine camera rig.")]
    public sealed class CameraRigRecipe : ScriptableObject
    {
        [Header("Designer Defaults")]
        [SerializeField] private CameraRigPresentationIntent presentationIntent = CameraRigPresentationIntent.Follow;
        [SerializeField] private CameraTargetSourceKind targetSourceKind = CameraTargetSourceKind.PlayerComposer;
        [SerializeField] private CameraTargetRequirement followRequirement = CameraTargetRequirement.Required;
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;

        [Header("Materialization Defaults")]
        [SerializeField] private bool createUnityCameraIfMissing = true;
        [SerializeField] private bool createCinemachineCameraIfMissing = true;
        [SerializeField] private string unityCameraObjectName = "Unity Camera";
        [SerializeField] private string cinemachineCameraObjectName = "Cinemachine Camera";
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        public CameraRigPresentationIntent PresentationIntent => presentationIntent;
        public CameraTargetSourceKind TargetSourceKind => targetSourceKind;
        public CameraTargetRequirement FollowRequirement => followRequirement;
        public CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public bool CreateUnityCameraIfMissing => createUnityCameraIfMissing;
        public bool CreateCinemachineCameraIfMissing => createCinemachineCameraIfMissing;
        public string UnityCameraObjectName => unityCameraObjectName.NormalizeTextOrFallback("Unity Camera");
        public string CinemachineCameraObjectName => cinemachineCameraObjectName.NormalizeTextOrFallback("Cinemachine Camera");
        public bool LogApplyRebuildDiagnostics => logApplyRebuildDiagnostics;
    }
}
