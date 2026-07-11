using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerAuthoring;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Designer-facing authoring surface that resolves explicit targets and materializes a Cinemachine rig.
    /// It does not select an active camera, own runtime output or arbitrate requests.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Rig Composer")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Camera rig authoring and idempotent materialization surface.")]
    public sealed class CameraRigComposer : MonoBehaviour
    {
        [Header("Designer")]
        [SerializeField] private CameraRigRecipe recipe;
        [SerializeField] private CameraRigPresentationIntent presentationIntent = CameraRigPresentationIntent.Follow;
        [SerializeField] private CameraTargetSourceKind targetSourceKind = CameraTargetSourceKind.PlayerComposer;
        [SerializeField] private PlayerComposer playerComposer;
        [SerializeField] private Transform explicitFollowTarget;
        [SerializeField] private Transform explicitLookAtTarget;
        [SerializeField] private CameraTargetRequirement followRequirement = CameraTargetRequirement.Required;
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;

        [Header("Advanced / Technical Materialization")]
        [SerializeField] private UnityEngine.Camera unityCamera;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private bool createUnityCameraIfMissing = true;
        [SerializeField] private bool createCinemachineCameraIfMissing = true;
        [SerializeField] private string unityCameraObjectName = "Unity Camera";
        [SerializeField] private string cinemachineCameraObjectName = "Cinemachine Camera";
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        [Header("Debug")]
        [SerializeField] private string lastApplyRebuildStatus;
        [SerializeField] private string lastBlockingIssue;
        [SerializeField] private string lastTargetResolutionSummary;
        [SerializeField] private string lastMaterializationSummary;
        [SerializeField] private Transform lastResolvedFollowTarget;
        [SerializeField] private Transform lastResolvedLookAtTarget;

        public CameraRigRecipe Recipe => recipe;
        public CameraRigPresentationIntent PresentationIntent => presentationIntent;
        public CameraTargetSourceKind TargetSourceKind => targetSourceKind;
        public PlayerComposer PlayerComposer => playerComposer;
        public Transform ExplicitFollowTarget => explicitFollowTarget;
        public Transform ExplicitLookAtTarget => explicitLookAtTarget;
        public CameraTargetRequirement FollowRequirement => followRequirement;
        public CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public UnityEngine.Camera UnityCamera => unityCamera;
        public CinemachineCamera CinemachineCamera => cinemachineCamera;
        public bool CreateUnityCameraIfMissing => createUnityCameraIfMissing;
        public bool CreateCinemachineCameraIfMissing => createCinemachineCameraIfMissing;
        public string UnityCameraObjectName => unityCameraObjectName.NormalizeTextOrFallback("Unity Camera");
        public string CinemachineCameraObjectName => cinemachineCameraObjectName.NormalizeTextOrFallback("Cinemachine Camera");
        public bool LogApplyRebuildDiagnostics => logApplyRebuildDiagnostics;
        public string LastApplyRebuildStatus => lastApplyRebuildStatus.NormalizeText();
        public string LastBlockingIssue => lastBlockingIssue.NormalizeText();
        public string LastTargetResolutionSummary => lastTargetResolutionSummary.NormalizeText();
        public string LastMaterializationSummary => lastMaterializationSummary.NormalizeText();
        public Transform LastResolvedFollowTarget => lastResolvedFollowTarget;
        public Transform LastResolvedLookAtTarget => lastResolvedLookAtTarget;

        public bool TryValidateForApply(out string issue)
        {
            issue = string.Empty;
            if (presentationIntent != CameraRigPresentationIntent.Follow)
            {
                issue = $"CameraRigComposer supports only Follow presentation intent. Current intent: '{presentationIntent}'.";
                return false;
            }

            if (targetSourceKind != CameraTargetSourceKind.PlayerComposer &&
                targetSourceKind != CameraTargetSourceKind.ExplicitTransform)
            {
                issue = $"CameraRigComposer supports only PlayerComposer or ExplicitTransform target sources. Current source: '{targetSourceKind}'.";
                return false;
            }

            if (targetSourceKind == CameraTargetSourceKind.PlayerComposer && playerComposer == null)
            {
                issue = "CameraRigComposer requires an explicit PlayerComposer target source.";
                return false;
            }

            if (followRequirement == CameraTargetRequirement.NotUsed)
            {
                issue = "Follow presentation requires Follow to participate.";
                return false;
            }

            return true;
        }

        public CameraTargetResolveResult ResolveCameraTargets(
            CameraTargetRequirement requestedFollowRequirement,
            CameraTargetRequirement requestedLookAtRequirement)
        {
            CameraTargetSourceDescriptor source = CreateTargetSourceDescriptor();
            CameraResolvedTargets targets;
            switch (targetSourceKind)
            {
                case CameraTargetSourceKind.PlayerComposer:
                    if (playerComposer == null)
                    {
                        return CameraTargetResolveResult.Blocked(
                            source,
                            "PlayerComposer target source is missing.",
                            "Camera rig target resolution was blocked because PlayerComposer is not assigned.",
                            CameraIssue.Blocking("camera.target-source.player-composer.missing", "PlayerComposer target source is missing."));
                    }

                    targets = CameraResolvedTargets.FromFollowAndLookAt(
                        playerComposer.CameraTarget,
                        requestedLookAtRequirement == CameraTargetRequirement.NotUsed ? null : playerComposer.LookAtTarget);
                    break;
                case CameraTargetSourceKind.ExplicitTransform:
                    targets = new CameraResolvedTargets(
                        requestedFollowRequirement == CameraTargetRequirement.NotUsed ? null : explicitFollowTarget,
                        requestedLookAtRequirement == CameraTargetRequirement.NotUsed ? null : explicitLookAtTarget);
                    break;
                default:
                    return CameraTargetResolveResult.Blocked(
                        source,
                        $"Unsupported CameraRigComposer target source: '{targetSourceKind}'.",
                        "Camera rig target resolution was blocked by an unsupported source kind.");
            }

            return CameraTargetResolveResult.ValidateRequirements(
                source,
                targets,
                requestedFollowRequirement,
                requestedLookAtRequirement);
        }

        public CameraRigComposerDebugSnapshot CreateDebugSnapshot()
        {
            CameraTargetSourceDescriptor source = CreateTargetSourceDescriptor();
            return new CameraRigComposerDebugSnapshot(
                presentationIntent,
                targetSourceKind,
                source.LogicalSourceId,
                source.DiagnosticLabel,
                unityCamera != null ? unityCamera.name.NormalizeText() : string.Empty,
                cinemachineCamera != null ? cinemachineCamera.name.NormalizeText() : string.Empty,
                lastResolvedFollowTarget != null ? lastResolvedFollowTarget.name.NormalizeText() : string.Empty,
                lastResolvedLookAtTarget != null ? lastResolvedLookAtTarget.name.NormalizeText() : string.Empty,
                lastApplyRebuildStatus.NormalizeText(),
                lastBlockingIssue.NormalizeText(),
                lastTargetResolutionSummary.NormalizeText(),
                lastMaterializationSummary.NormalizeText());
        }

        private CameraTargetSourceDescriptor CreateTargetSourceDescriptor()
        {
            return targetSourceKind == CameraTargetSourceKind.PlayerComposer
                ? new CameraTargetSourceDescriptor(CameraTargetSourceKind.PlayerComposer, playerComposer, playerComposer != null ? playerComposer.ActorId : string.Empty, playerComposer != null ? $"PlayerComposer:{playerComposer.ActorId}" : "PlayerComposer:missing")
                : CameraTargetSourceDescriptor.ExplicitTransform(explicitFollowTarget, explicitFollowTarget != null ? "ExplicitTransform" : "ExplicitTransform:missing");
        }

#if UNITY_EDITOR
        public bool EditorApplyRecipeDefaults(bool overwriteExisting, out string issue)
        {
            issue = string.Empty;
            if (recipe == null)
            {
                issue = "CameraRigComposer requires a CameraRigRecipe before recipe defaults can be applied.";
                return false;
            }

            if (overwriteExisting || presentationIntent == CameraRigPresentationIntent.Undefined)
                presentationIntent = recipe.PresentationIntent;
            if (overwriteExisting || targetSourceKind == CameraTargetSourceKind.None)
                targetSourceKind = recipe.TargetSourceKind;
            followRequirement = recipe.FollowRequirement;
            lookAtRequirement = recipe.LookAtRequirement;
            createUnityCameraIfMissing = recipe.CreateUnityCameraIfMissing;
            createCinemachineCameraIfMissing = recipe.CreateCinemachineCameraIfMissing;
            unityCameraObjectName = recipe.UnityCameraObjectName;
            cinemachineCameraObjectName = recipe.CinemachineCameraObjectName;
            logApplyRebuildDiagnostics = recipe.LogApplyRebuildDiagnostics;
            return true;
        }

        public void EditorSetGeneratedReferences(UnityEngine.Camera generatedUnityCamera, CinemachineCamera generatedCinemachineCamera)
        {
            if (unityCamera == null) unityCamera = generatedUnityCamera;
            if (cinemachineCamera == null) cinemachineCamera = generatedCinemachineCamera;
        }

        public void EditorSetApplyRebuildResult(string status, string blockingIssue, string targetResolutionSummary, string materializationSummary, Transform resolvedFollowTarget, Transform resolvedLookAtTarget)
        {
            lastApplyRebuildStatus = status.NormalizeText();
            lastBlockingIssue = blockingIssue.NormalizeText();
            lastTargetResolutionSummary = targetResolutionSummary.NormalizeText();
            lastMaterializationSummary = materializationSummary.NormalizeText();
            lastResolvedFollowTarget = resolvedFollowTarget;
            lastResolvedLookAtTarget = resolvedLookAtTarget;
        }

        private void Reset()
        {
            presentationIntent = CameraRigPresentationIntent.Follow;
            targetSourceKind = CameraTargetSourceKind.PlayerComposer;
            followRequirement = CameraTargetRequirement.Required;
            lookAtRequirement = CameraTargetRequirement.Optional;
            unityCamera = GetComponentInChildren<UnityEngine.Camera>(true);
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }
#endif
    }
}
