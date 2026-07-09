using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerAuthoring;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// API status: Experimental. Designer-first authoring surface for a Cinemachine camera rig.
    /// This MVP supports SinglePlayerFollowCamera through an explicit PlayerComposer reference or explicit transforms.
    /// It does not create runtime authority, does not search the scene and does not use Camera.main.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Composer")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Single-player Cinemachine Camera Product Surface MVP.")]
    public sealed class CameraComposer : MonoBehaviour, ICameraTargetSource
    {
        [Header("Designer")]
        [Tooltip("Optional reusable camera intent asset. Use Apply Recipe Defaults to copy reusable defaults into this Composer; local fields remain editable.")]
        [SerializeField] private CameraRecipe recipe;
        [SerializeField] private CameraMode mode = CameraMode.SinglePlayerFollowCamera;
        [SerializeField] private CameraOwnershipScope ownershipScope = CameraOwnershipScope.SinglePlayer;
        [SerializeField] private CameraTargetSourceKind targetSourceKind = CameraTargetSourceKind.PlayerComposer;
        [Tooltip("Explicit PlayerComposer target source for SinglePlayerFollowCamera. No scene lookup is performed.")]
        [SerializeField] private PlayerComposer playerComposer;
        [Tooltip("Explicit follow target used only when Target Source Kind is ExplicitTransform.")]
        [SerializeField] private Transform explicitFollowTarget;
        [Tooltip("Explicit look-at target used only when Target Source Kind is ExplicitTransform.")]
        [SerializeField] private Transform explicitLookAtTarget;
        [SerializeField] private CameraTargetRequirement followRequirement = CameraTargetRequirement.Required;
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private int priority = 10;

        [Header("Advanced")]
        [Tooltip("Existing Unity Camera inside this rig. If empty, Apply/Rebuild may create or reuse a local child camera.")]
        [SerializeField] private UnityEngine.Camera unityCamera;
        [Tooltip("Existing Cinemachine Camera inside this rig. If empty, Apply/Rebuild may create or reuse a local child Cinemachine Camera.")]
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

        public CameraRecipe Recipe => recipe;

        public CameraMode Mode => mode;

        public CameraOwnershipScope OwnershipScope => ownershipScope;

        public CameraTargetSourceKind TargetSourceKind => targetSourceKind;

        public PlayerComposer PlayerComposer => playerComposer;

        public Transform ExplicitFollowTarget => explicitFollowTarget;

        public Transform ExplicitLookAtTarget => explicitLookAtTarget;

        public CameraTargetRequirement FollowRequirement => followRequirement;

        public CameraTargetRequirement LookAtRequirement => lookAtRequirement;

        public int Priority => priority;

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

            if (mode != CameraMode.SinglePlayerFollowCamera)
            {
                issue = $"CameraComposer MVP supports only SinglePlayerFollowCamera. Current mode: '{mode}'.";
                return false;
            }

            if (ownershipScope != CameraOwnershipScope.SinglePlayer)
            {
                issue = $"CameraComposer MVP requires SinglePlayer ownership. Current ownership: '{ownershipScope}'.";
                return false;
            }

            if (targetSourceKind != CameraTargetSourceKind.PlayerComposer && targetSourceKind != CameraTargetSourceKind.ExplicitTransform)
            {
                issue = $"CameraComposer MVP supports only PlayerComposer or ExplicitTransform target sources. Current source: '{targetSourceKind}'.";
                return false;
            }

            if (targetSourceKind == CameraTargetSourceKind.PlayerComposer && playerComposer == null)
            {
                issue = "CameraComposer requires an explicit PlayerComposer when Target Source Kind is PlayerComposer.";
                return false;
            }

            if (targetSourceKind == CameraTargetSourceKind.ExplicitTransform && followRequirement == CameraTargetRequirement.Required && explicitFollowTarget == null)
            {
                issue = "CameraComposer requires an explicit follow target when Target Source Kind is ExplicitTransform and follow is required.";
                return false;
            }

            if (targetSourceKind == CameraTargetSourceKind.ExplicitTransform && lookAtRequirement == CameraTargetRequirement.Required && explicitLookAtTarget == null)
            {
                issue = "CameraComposer requires an explicit look-at target when Target Source Kind is ExplicitTransform and look-at is required.";
                return false;
            }

            if (followRequirement == CameraTargetRequirement.NotUsed)
            {
                issue = "CameraComposer MVP requires Follow to participate. Use Optional or Required for Follow.";
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
                            "Camera target resolution blocked because PlayerComposer is not assigned.",
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
                        $"Unsupported CameraComposer target source: '{targetSourceKind}'.",
                        "Camera target resolution blocked by unsupported source kind.",
                        CameraIssue.Blocking("camera.target-source.unsupported", $"Unsupported CameraComposer target source: '{targetSourceKind}'."));
            }

            return CameraTargetResolveResult.ValidateRequirements(
                source,
                targets,
                requestedFollowRequirement,
                requestedLookAtRequirement);
        }

        public CameraProductIntent CreateIntent()
        {
            return new CameraProductIntent(
                mode,
                ownershipScope,
                CreateTargetSourceDescriptor(),
                followRequirement,
                lookAtRequirement,
                priority);
        }

        public CameraComposerDebugSnapshot CreateDebugSnapshot()
        {
            CameraTargetSourceDescriptor source = CreateTargetSourceDescriptor();
            return new CameraComposerDebugSnapshot(
                mode,
                ownershipScope,
                targetSourceKind,
                source.LogicalSourceId,
                source.DiagnosticLabel,
                priority,
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
            switch (targetSourceKind)
            {
                case CameraTargetSourceKind.PlayerComposer:
                    return new CameraTargetSourceDescriptor(
                        CameraTargetSourceKind.PlayerComposer,
                        playerComposer,
                        playerComposer != null ? playerComposer.ActorId : string.Empty,
                        playerComposer != null ? $"PlayerComposer:{playerComposer.ActorId}" : "PlayerComposer:missing");

                case CameraTargetSourceKind.ExplicitTransform:
                    return CameraTargetSourceDescriptor.ExplicitTransform(
                        explicitFollowTarget,
                        explicitFollowTarget != null ? "ExplicitTransform" : "ExplicitTransform:missing");

                default:
                    return new CameraTargetSourceDescriptor(
                        targetSourceKind,
                        null,
                        string.Empty,
                        targetSourceKind.ToString());
            }
        }

#if UNITY_EDITOR
        public bool EditorApplyRecipeDefaults(bool overwriteExisting, out string issue)
        {
            issue = string.Empty;
            if (recipe == null)
            {
                issue = "CameraComposer requires a CameraRecipe before recipe defaults can be applied.";
                return false;
            }

            if (overwriteExisting || mode == CameraMode.Undefined)
            {
                mode = recipe.Mode;
            }

            if (overwriteExisting || ownershipScope == CameraOwnershipScope.Undefined)
            {
                ownershipScope = recipe.OwnershipScope;
            }

            if (overwriteExisting || targetSourceKind == CameraTargetSourceKind.None)
            {
                targetSourceKind = recipe.TargetSourceKind;
            }

            followRequirement = recipe.FollowRequirement;
            lookAtRequirement = recipe.LookAtRequirement;
            priority = recipe.Priority;
            createUnityCameraIfMissing = recipe.CreateUnityCameraIfMissing;
            createCinemachineCameraIfMissing = recipe.CreateCinemachineCameraIfMissing;
            unityCameraObjectName = recipe.UnityCameraObjectName;
            cinemachineCameraObjectName = recipe.CinemachineCameraObjectName;
            logApplyRebuildDiagnostics = recipe.LogApplyRebuildDiagnostics;
            return true;
        }

        public void EditorSetGeneratedReferences(UnityEngine.Camera generatedUnityCamera, CinemachineCamera generatedCinemachineCamera)
        {
            if (unityCamera == null)
            {
                unityCamera = generatedUnityCamera;
            }

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
            lastTargetResolutionSummary = targetResolutionSummary.NormalizeText();
            lastMaterializationSummary = materializationSummary.NormalizeText();
            lastResolvedFollowTarget = resolvedFollowTarget;
            lastResolvedLookAtTarget = resolvedLookAtTarget;
        }

        private void Reset()
        {
            mode = CameraMode.SinglePlayerFollowCamera;
            ownershipScope = CameraOwnershipScope.SinglePlayer;
            targetSourceKind = CameraTargetSourceKind.PlayerComposer;
            followRequirement = CameraTargetRequirement.Required;
            lookAtRequirement = CameraTargetRequirement.Optional;
            priority = 10;
            unityCamera = GetComponentInChildren<UnityEngine.Camera>(true);
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }
#endif
    }
}
