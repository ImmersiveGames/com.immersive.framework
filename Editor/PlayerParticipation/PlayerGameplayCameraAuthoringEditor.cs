using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerGameplayCameraAuthoring))]
    internal sealed class PlayerGameplayCameraAuthoringEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _requiredness;
        private SerializedProperty _cameraRig;
        private SerializedProperty _precedence;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _requiredness =
                serializedObject.FindProperty(
                    "requiredness");

            _cameraRig =
                serializedObject.FindProperty(
                    "cameraRig");

            _precedence =
                serializedObject.FindProperty(
                    "precedence");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Player Gameplay Camera",
                    "Declares gameplay Camera participation for this Player Actor Runtime and selects its Actor-owned Camera Rig. Targets and framing remain authored on Camera Rig Composer."),
                EditorStyles.boldLabel);

            DrawConfiguration();

            serializedObject.ApplyModifiedProperties();

            DrawConfigurationStatus();
            DrawAdvancedDebug();
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Declares whether gameplay Camera participation is optional or required for this Player Actor."));

            EditorGUILayout.PropertyField(
                _cameraRig,
                new GUIContent(
                    "Camera Rig",
                    "Actor-owned Camera Rig Composer that supplies targets, target requirements and framing for this Player's gameplay Camera request."));

            EditorGUILayout.PropertyField(
                _precedence,
                new GUIContent(
                    "Precedence",
                    "Arbitration precedence used when this Player gameplay Camera participates in the Camera output selection."));
        }

        private void DrawConfigurationStatus()
        {
            PlayerGameplayCameraAuthoring authoring =
                (PlayerGameplayCameraAuthoring)target;

            bool valid =
                TryValidateAuthoring(
                    authoring,
                    out string diagnostic,
                    out _);

            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            EditorGUILayout.LabelField(
                "Status",
                valid
                    ? "Ready"
                    : "Incomplete");

            if (!valid)
            {
                EditorGUILayout.HelpBox(
                    diagnostic,
                    MessageType.Warning);
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(6f);
            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            PlayerGameplayCameraAuthoring authoring =
                (PlayerGameplayCameraAuthoring)target;

            TryValidateAuthoring(
                authoring,
                out string diagnostic,
                out CameraResolvedTargets targets);

            FrameworkAuthoringInspectorGui.Section(
                "Resolved Camera Targets");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Follow Target",
                    targets.FollowTarget,
                    typeof(Transform),
                    true);

                EditorGUILayout.ObjectField(
                    "Look At Target",
                    targets.LookAtTarget,
                    typeof(Transform),
                    true);
            }

            FrameworkAuthoringInspectorGui.Section(
                "Composer Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Last Apply / Rebuild",
                    authoring.CameraRig != null
                        ? authoring.CameraRig.LastApplyRebuildStatus
                        : "<missing>");
            }

            if (!string.IsNullOrWhiteSpace(diagnostic))
            {
                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Resolution Diagnostic",
                        "Latest target-resolution diagnostic derived from the current authored configuration."),
                    new GUIContent(diagnostic),
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static bool TryValidateAuthoring(
            PlayerGameplayCameraAuthoring authoring,
            out string diagnostic,
            out CameraResolvedTargets targets)
        {
            targets = default;

            if (authoring == null)
            {
                diagnostic =
                    "Player Gameplay Camera authoring is missing.";
                return false;
            }

            PlayerActorDeclaration actor =
                authoring.GetComponentInParent<
                    PlayerActorDeclaration>(
                    true);

            bool standalonePrefabAuthoring = false;
            Transform ownershipRoot;

            if (actor != null)
            {
                ownershipRoot =
                    actor.transform;
            }
            else if (TryResolveStandalonePrefabBoundary(
                         authoring,
                         out ownershipRoot))
            {
                standalonePrefabAuthoring = true;
            }
            else
            {
                diagnostic =
                    "Player Gameplay Camera must belong to a PlayerActorDeclaration hierarchy or be authored inside an isolated prefab boundary that will be mounted as Actor Presentation.";
                return false;
            }

            CameraRigComposer rig =
                authoring.CameraRig;

            if (rig == null)
            {
                diagnostic =
                    "Assign an Actor-owned Camera Rig Composer.";
                return false;
            }

            if (!IsOwnedBy(
                    ownershipRoot,
                    rig.transform))
            {
                diagnostic =
                    standalonePrefabAuthoring
                        ? "The Camera Rig Composer must belong to the same isolated Presentation prefab."
                        : "The Camera Rig Composer must belong to the same Player Actor hierarchy.";
                return false;
            }

            if (!rig.TryValidateForApply(
                    out string rigIssue))
            {
                diagnostic =
                    $"Camera Rig configuration is invalid. {rigIssue}";
                return false;
            }

            if (!authoring.TryResolveCameraTargets(
                    out targets,
                    out diagnostic))
            {
                return false;
            }

            if (targets.FollowTarget == null)
            {
                diagnostic =
                    "The Camera Rig must resolve a Follow target.";
                return false;
            }

            if (!IsOwnedBy(
                    ownershipRoot,
                    targets.FollowTarget))
            {
                diagnostic =
                    standalonePrefabAuthoring
                        ? "The resolved Follow target must belong to the same isolated Presentation prefab."
                        : "The resolved Follow target must belong to the same Player Actor hierarchy.";
                return false;
            }

            if (targets.LookAtTarget != null &&
                !IsOwnedBy(
                    ownershipRoot,
                    targets.LookAtTarget))
            {
                diagnostic =
                    standalonePrefabAuthoring
                        ? "The resolved Look At target must belong to the same isolated Presentation prefab."
                        : "The resolved Look At target must belong to the same Player Actor hierarchy.";
                return false;
            }

            diagnostic =
                standalonePrefabAuthoring
                    ? "Camera Rig and prefab-local targets are coherent. Player Actor ownership will be established when the Presentation is mounted."
                    : "Camera Rig and Actor-owned targets are coherent.";

            return true;
        }

        private static bool TryResolveStandalonePrefabBoundary(
            PlayerGameplayCameraAuthoring authoring,
            out Transform prefabRoot)
        {
            prefabRoot = null;

            PrefabStage prefabStage =
                PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage == null ||
                prefabStage.prefabContentsRoot == null)
            {
                return false;
            }

            Transform candidateRoot =
                prefabStage.prefabContentsRoot.transform;

            if (!IsOwnedBy(
                    candidateRoot,
                    authoring.transform))
            {
                return false;
            }

            PlayerActorDeclaration[] declarations =
                prefabStage.prefabContentsRoot
                    .GetComponentsInChildren<
                        PlayerActorDeclaration>(
                        true);

            if (declarations.Length != 0)
            {
                return false;
            }

            prefabRoot =
                candidateRoot;

            return true;
        }

        private static bool IsOwnedBy(
            Transform owner,
            Transform candidate)
        {
            return owner != null &&
                candidate != null &&
                (ReferenceEquals(
                     owner,
                     candidate) ||
                 candidate.IsChildOf(
                     owner));
        }
    }
}
