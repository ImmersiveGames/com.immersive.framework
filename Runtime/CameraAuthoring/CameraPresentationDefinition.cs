using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Reusable authored recipe for one Camera Presentation.
    ///
    /// The asset contains stable authoring intent only. Live Subject membership,
    /// materialized Rig instances, CameraRequest state and rollback evidence remain
    /// in CameraPresentationRuntime.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Camera Presentation",
        menuName = "Immersive Framework/Camera/Camera Presentation",
        order = 20)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-B reusable Camera Presentation recipe; no mutable runtime occurrence state.")]
    public sealed class CameraPresentationDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string stableId = string.Empty;

        [SerializeField, TextArea(2, 4)]
        private string description = string.Empty;

        [Header("Output")]
        [SerializeField]
        [Tooltip("Exact physical Camera Output definition this Presentation participates in.")]
        private CameraOutputDefinition outputDefinition;

        [Header("Rig")]
        [SerializeField]
        [Tooltip("Prefab containing exactly one already-materialized CameraRigComposer. Runtime instantiates this prefab; it does not rebuild Cinemachine structure.")]
        private GameObject rigPrefab;

        [Header("Transition")]
        [SerializeField]
        [Tooltip("Controls how this Presentation enters when it becomes the winning request. Blend uses the Output Brain's authored blend settings; Cut enters the view immediately.")]
        private CameraPresentationTransitionMode transitionMode =
            CameraPresentationTransitionMode.Blend;

        [Header("Subjects")]
        [SerializeField]
        [Tooltip("Selects how current Camera Subjects are chosen for this Presentation occurrence.")]
        private CameraSharedCompositionSubjectPolicyKind subjectPolicy =
            CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects;

        [Header("Request Arbitration")]
        [SerializeField]
        [Tooltip("Normal CameraRequest precedence used when this Presentation occurrence is eligible.")]
        private int requestPrecedence;

        public bool HasValidId =>
            Guid.TryParseExact(stableId, "N", out Guid id) &&
            id != Guid.Empty &&
            stableId == id.ToString("N");

        public CameraPresentationId PresentationId =>
            HasValidId
                ? new CameraPresentationId(stableId)
                : throw new InvalidOperationException(
                    "Camera Presentation definition requires an explicitly generated stable ID.");

        public string Description => description ?? string.Empty;

        public CameraOutputDefinition OutputDefinition => outputDefinition;

        public GameObject RigPrefab => rigPrefab;

        public CameraPresentationTransitionMode TransitionMode =>
            transitionMode;

        public CameraSharedCompositionSubjectPolicyKind SubjectPolicy =>
            subjectPolicy;

        public int RequestPrecedence => requestPrecedence;

        public bool TryValidate(out string issue)
        {
            if (!HasValidId)
            {
                issue =
                    "Camera Presentation definition requires an explicitly generated stable ID.";
                return false;
            }

            if (outputDefinition == null || !outputDefinition.HasValidId)
            {
                issue =
                    "Camera Presentation definition requires an exact valid Camera Output definition.";
                return false;
            }

            if (rigPrefab == null)
            {
                issue =
                    "Camera Presentation definition requires an explicit Rig Prefab.";
                return false;
            }

            if (transitionMode !=
                    CameraPresentationTransitionMode.Blend &&
                transitionMode !=
                    CameraPresentationTransitionMode.Cut)
            {
                issue =
                    "Camera Presentation definition requires an explicitly supported Transition mode.";
                return false;
            }

            if (subjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects &&
                subjectPolicy !=
                    CameraSharedCompositionSubjectPolicyKind.ExplicitSelection)
            {
                issue =
                    "Camera Presentation definition requires an explicitly supported Subject selection policy.";
                return false;
            }

            CameraRigComposer[] composers =
                rigPrefab.GetComponentsInChildren<CameraRigComposer>(true);
            if (composers.Length != 1 || composers[0] == null)
            {
                issue =
                    $"Camera Presentation Rig Prefab '{rigPrefab.name}' must contain exactly one CameraRigComposer. Found '{composers.Length}'.";
                return false;
            }

            if (rigPrefab.GetComponentInChildren<CameraOutputAuthoring>(true) != null)
            {
                issue =
                    $"Camera Presentation Rig Prefab '{rigPrefab.name}' must not contain CameraOutputAuthoring. Physical Outputs are Session capacity.";
                return false;
            }

            if (rigPrefab.GetComponentInChildren<CameraSharedComposition>(true) != null)
            {
                issue =
                    $"Camera Presentation Rig Prefab '{rigPrefab.name}' must not contain CameraSharedComposition. Runtime occurrence state is materialized separately.";
                return false;
            }

            CameraRigComposer composer = composers[0];
            if (!composer.TryValidateForApply(out issue))
            {
                issue =
                    $"Camera Presentation Rig Prefab '{rigPrefab.name}' has invalid CameraRigComposer configuration. {issue}";
                return false;
            }

            if (composer.CinemachineCamera == null)
            {
                issue =
                    $"Camera Presentation Rig Prefab '{rigPrefab.name}' must be Apply/Rebuild materialized before runtime and contain the Composer's CinemachineCamera.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
