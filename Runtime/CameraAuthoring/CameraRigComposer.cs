using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Designer-facing authoring surface that resolves explicit camera targets and
    /// materializes one local Cinemachine Camera rig.
    ///
    /// A rig may use direct Transform references or one typed ICameraTargetSource.
    /// It does not create or own a Unity Camera, CinemachineBrain or runtime output.
    /// It does not select an active camera or arbitrate requests.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Rig Composer")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Camera rig authoring and idempotent Cinemachine Camera materialization surface.")]
    public sealed class CameraRigComposer : MonoBehaviour
    {
        [Header("Designer")]
        [SerializeField] private CameraRigRecipe recipe;
        [SerializeField] private CameraRigPresentationIntent presentationIntent =
            CameraRigPresentationIntent.Follow;
        [SerializeField] private CameraTargetSourceKind targetSourceKind =
            CameraTargetSourceKind.ExplicitTransform;
        [Tooltip("Optional explicit component implementing ICameraTargetSource. When assigned, it provides Follow and Look At evidence.")]
        [SerializeField] private MonoBehaviour targetSource;
        [SerializeField] private Transform explicitFollowTarget;
        [SerializeField] private Transform explicitLookAtTarget;
        [SerializeField] private CameraTargetRequirement followRequirement =
            CameraTargetRequirement.Required;
        [SerializeField] private CameraTargetRequirement lookAtRequirement =
            CameraTargetRequirement.Optional;
        [SerializeField] private Vector3 followOffset =
            new Vector3(0f, 5f, -8f);

        [Header("Advanced / Technical Materialization")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private bool createCinemachineCameraIfMissing = true;
        [SerializeField] private string cinemachineCameraObjectName =
            "Cinemachine Camera";
        [SerializeField] private bool logApplyRebuildDiagnostics = true;

        [Header("Debug")]
        [SerializeField] private string lastApplyRebuildStatus;
        [SerializeField] private string lastBlockingIssue;
        [SerializeField] private string lastTargetResolutionSummary;
        [SerializeField] private string lastMaterializationSummary;
        [SerializeField] private Transform lastResolvedFollowTarget;
        [SerializeField] private Transform lastResolvedLookAtTarget;

        public CameraRigRecipe Recipe => recipe;
        public CameraRigPresentationIntent PresentationIntent =>
            presentationIntent;
        public CameraTargetSourceKind TargetSourceKind => targetSourceKind;
        public MonoBehaviour TargetSourceBehaviour => targetSource;
        public ICameraTargetSource TargetSource =>
            targetSource as ICameraTargetSource;
        public Transform ExplicitFollowTarget => explicitFollowTarget;
        public Transform ExplicitLookAtTarget => explicitLookAtTarget;


        public CameraTargetRequirement FollowRequirement => followRequirement;
        public CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public Vector3 FollowOffset => followOffset;
        public CinemachineCamera CinemachineCamera => cinemachineCamera;
        public bool CreateCinemachineCameraIfMissing =>
            createCinemachineCameraIfMissing;
        public string CinemachineCameraObjectName =>
            cinemachineCameraObjectName.NormalizeTextOrFallback(
                "Cinemachine Camera");
        public bool LogApplyRebuildDiagnostics =>
            logApplyRebuildDiagnostics;
        public string LastApplyRebuildStatus =>
            lastApplyRebuildStatus.NormalizeText();
        public string LastBlockingIssue =>
            lastBlockingIssue.NormalizeText();
        public string LastTargetResolutionSummary =>
            lastTargetResolutionSummary.NormalizeText();
        public string LastMaterializationSummary =>
            lastMaterializationSummary.NormalizeText();
        public Transform LastResolvedFollowTarget =>
            lastResolvedFollowTarget;
        public Transform LastResolvedLookAtTarget =>
            lastResolvedLookAtTarget;

        public bool TryValidateForApply(out string issue)
        {
            issue = string.Empty;

            if (presentationIntent != CameraRigPresentationIntent.Follow)
            {
                issue =
                    $"CameraRigComposer supports only Follow presentation intent. Current intent: '{presentationIntent}'.";
                return false;
            }

            if (targetSource != null && TargetSource == null)
            {
                issue =
                    $"Assigned Camera Target Source '{targetSource.GetType().FullName}' does not implement ICameraTargetSource.";
                return false;
            }

            if (targetSource == null &&
                targetSourceKind != CameraTargetSourceKind.ExplicitTransform)
            {
                issue =
                    $"CameraRigComposer requires a typed target-source component for source kind '{targetSourceKind}'.";
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
            if (targetSource != null)
            {
                ICameraTargetSource provider = TargetSource;
                if (provider == null)
                {
                    return CameraTargetResolveResult.Blocked(
                        new CameraTargetSourceDescriptor(
                            CameraTargetSourceKind.None,
                            targetSource,
                            string.Empty,
                            $"InvalidTargetSource:{targetSource.GetType().FullName}"),
                        "Assigned component does not implement ICameraTargetSource.",
                        "Camera rig target resolution was blocked by invalid target-source authoring.");
                }

                try
                {
                    return provider.ResolveCameraTargets(
                        requestedFollowRequirement,
                        requestedLookAtRequirement);
                }
                catch (Exception exception)
                {
                    return CameraTargetResolveResult.Blocked(
                        new CameraTargetSourceDescriptor(
                            provider.TargetSourceKind,
                            targetSource,
                            string.Empty,
                            provider.GetType().FullName),
                        $"Camera target source threw during resolution. {exception.Message}",
                        "Camera rig target resolution failed explicitly.",
                        CameraIssue.Blocking(
                            "camera.target-source.resolve-failed",
                            exception.Message));
                }
            }

            if (targetSourceKind != CameraTargetSourceKind.ExplicitTransform)
            {
                return CameraTargetResolveResult.Blocked(
                    new CameraTargetSourceDescriptor(
                        targetSourceKind,
                        null,
                        string.Empty,
                        $"UnsupportedTargetSource:{targetSourceKind}"),
                    $"CameraRigComposer requires a typed provider for target source kind '{targetSourceKind}'.",
                    "Camera rig target resolution was blocked by unsupported source authoring.");
            }

            CameraTargetSourceDescriptor source =
                CameraTargetSourceDescriptor.ExplicitTransform(
                    explicitFollowTarget,
                    explicitFollowTarget != null
                        ? "ExplicitTransform"
                        : "ExplicitTransform:missing");
            var targets = new CameraResolvedTargets(
                requestedFollowRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : explicitFollowTarget,
                requestedLookAtRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : explicitLookAtTarget);

            return CameraTargetResolveResult.ValidateRequirements(
                source,
                targets,
                requestedFollowRequirement,
                requestedLookAtRequirement);
        }

        public CameraRigComposerDebugSnapshot CreateDebugSnapshot()
        {
            CameraTargetResolveResult resolution = ResolveCameraTargets(
                followRequirement,
                lookAtRequirement);
            CameraTargetSourceDescriptor source = resolution.Source;

            return new CameraRigComposerDebugSnapshot(
                presentationIntent,
                targetSource != null
                    ? resolution.Source.Kind
                    : targetSourceKind,
                source.LogicalSourceId,
                source.DiagnosticLabel,
                string.Empty,
                cinemachineCamera != null
                    ? cinemachineCamera.name.NormalizeText()
                    : string.Empty,
                lastResolvedFollowTarget != null
                    ? lastResolvedFollowTarget.name.NormalizeText()
                    : string.Empty,
                lastResolvedLookAtTarget != null
                    ? lastResolvedLookAtTarget.name.NormalizeText()
                    : string.Empty,
                lastApplyRebuildStatus.NormalizeText(),
                lastBlockingIssue.NormalizeText(),
                lastTargetResolutionSummary.NormalizeText(),
                lastMaterializationSummary.NormalizeText());
        }

#if UNITY_EDITOR
        public bool EditorApplyRecipeDefaults(
            bool overwriteExisting,
            out string issue)
        {
            issue = string.Empty;

            if (recipe == null)
            {
                issue =
                    "CameraRigComposer requires a CameraRigRecipe before recipe defaults can be applied.";
                return false;
            }

            if (overwriteExisting ||
                presentationIntent == CameraRigPresentationIntent.Undefined)
            {
                presentationIntent = recipe.PresentationIntent;
            }

            if (overwriteExisting ||
                targetSourceKind == CameraTargetSourceKind.None)
            {
                targetSourceKind = recipe.TargetSourceKind;
            }

            followRequirement = recipe.FollowRequirement;
            lookAtRequirement = recipe.LookAtRequirement;
            followOffset = recipe.FollowOffset;
            createCinemachineCameraIfMissing =
                recipe.CreateCinemachineCameraIfMissing;
            cinemachineCameraObjectName =
                recipe.CinemachineCameraObjectName;
            logApplyRebuildDiagnostics =
                recipe.LogApplyRebuildDiagnostics;

            return true;
        }

        public void EditorSetGeneratedReference(
            CinemachineCamera generatedCinemachineCamera)
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera = generatedCinemachineCamera;
            }
        }

        public void EditorSetApplyRebuildResult(
            string status,
            string blockingIssue,
            string targetResolutionSummary,
            string materializationSummary,
            Transform resolvedFollowTarget,
            Transform resolvedLookAtTarget)
        {
            lastApplyRebuildStatus = status.NormalizeText();
            lastBlockingIssue = blockingIssue.NormalizeText();
            lastTargetResolutionSummary =
                targetResolutionSummary.NormalizeText();
            lastMaterializationSummary =
                materializationSummary.NormalizeText();
            lastResolvedFollowTarget = resolvedFollowTarget;
            lastResolvedLookAtTarget = resolvedLookAtTarget;
        }

        private void Reset()
        {
            presentationIntent = CameraRigPresentationIntent.Follow;
            targetSourceKind = CameraTargetSourceKind.ExplicitTransform;
            followRequirement = CameraTargetRequirement.Required;
            lookAtRequirement = CameraTargetRequirement.Optional;
            cinemachineCamera =
                GetComponentInChildren<CinemachineCamera>(true);
        }
#endif
    }
}
