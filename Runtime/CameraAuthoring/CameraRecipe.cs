using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// API status: Experimental. Reusable designer intent for CameraComposer defaults.
    /// This asset does not create runtime authority, does not arbitrate cameras and does not search the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraRecipe", menuName = "Immersive Framework/Camera/Camera Recipe")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Product surface MVP for reusable Cinemachine CameraComposer defaults.")]
    public sealed class CameraRecipe : ScriptableObject
    {
        [Header("Designer Defaults")]
        [SerializeField] private CameraMode mode = CameraMode.SinglePlayerFollowCamera;
        [SerializeField] private CameraOwnershipScope ownershipScope = CameraOwnershipScope.SinglePlayer;
        [SerializeField] private CameraTargetSourceKind targetSourceKind = CameraTargetSourceKind.PlayerComposer;
        [SerializeField] private CameraTargetRequirement followRequirement = CameraTargetRequirement.Required;
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private int priority = 10;

        [Header("Materialization Defaults")]
        [SerializeField] private bool createUnityCameraIfMissing = true;
        [SerializeField] private bool createCinemachineCameraIfMissing = true;
        [SerializeField] private string unityCameraObjectName = "Unity Camera";
        [SerializeField] private string cinemachineCameraObjectName = "Cinemachine Camera";
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        public CameraMode Mode => mode;

        public CameraOwnershipScope OwnershipScope => ownershipScope;

        public CameraTargetSourceKind TargetSourceKind => targetSourceKind;

        public CameraTargetRequirement FollowRequirement => followRequirement;

        public CameraTargetRequirement LookAtRequirement => lookAtRequirement;

        public int Priority => priority;

        public bool CreateUnityCameraIfMissing => createUnityCameraIfMissing;

        public bool CreateCinemachineCameraIfMissing => createCinemachineCameraIfMissing;

        public string UnityCameraObjectName => unityCameraObjectName.NormalizeTextOrFallback("Unity Camera");

        public string CinemachineCameraObjectName => cinemachineCameraObjectName.NormalizeTextOrFallback("Cinemachine Camera");

        public bool LogApplyRebuildDiagnostics => logApplyRebuildDiagnostics;

        public CameraProductIntent CreateIntent(Object sourceObject, string logicalSourceId, string diagnosticLabel)
        {
            var source = new CameraTargetSourceDescriptor(
                targetSourceKind,
                sourceObject,
                logicalSourceId,
                diagnosticLabel);

            return new CameraProductIntent(
                mode,
                ownershipScope,
                source,
                followRequirement,
                lookAtRequirement,
                priority);
        }
    }
}
