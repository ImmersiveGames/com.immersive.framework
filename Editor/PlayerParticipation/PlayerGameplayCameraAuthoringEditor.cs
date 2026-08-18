using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerGameplayCameraAuthoring))]
    internal sealed class PlayerGameplayCameraAuthoringEditor :
        UnityEditor.Editor
    {
        private SerializedProperty requiredness;
        private SerializedProperty cameraRig;
        private SerializedProperty precedence;
        private bool showAdvancedDebug;

        private void OnEnable()
        {
            requiredness =
                serializedObject.FindProperty(
                    "requiredness");

            cameraRig =
                serializedObject.FindProperty(
                    "cameraRig");

            precedence =
                serializedObject.FindProperty(
                    "precedence");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Player Gameplay Camera",
                    "Declares gameplay Camera participation for this Logical Player Actor and selects its Actor-owned Camera Rig. Targets and framing remain authored on Camera Rig Composer."),
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
                requiredness,
                new GUIContent(
                    "Requiredness",
                    "Declares whether gameplay Camera participation is optional or required for this Player Actor."));

            EditorGUILayout.PropertyField(
                cameraRig,
                new GUIContent(
                    "Camera Rig",
                    "Actor-owned Camera Rig Composer that supplies targets, target requirements and framing for this Player's gameplay Camera request."));

            EditorGUILayout.PropertyField(
                precedence,
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
            showAdvancedDebug =
                EditorGUILayout.Foldout(
                    showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (!showAdvancedDebug)
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

            if (actor == null)
            {
                diagnostic =
                    "Player Gameplay Camera must belong to a PlayerActorDeclaration hierarchy.";
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
                    actor.transform,
                    rig.transform))
            {
                diagnostic =
                    "The Camera Rig Composer must belong to the same Player Actor hierarchy.";
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
                    actor.transform,
                    targets.FollowTarget))
            {
                diagnostic =
                    "The resolved Follow target must belong to the same Player Actor hierarchy.";
                return false;
            }

            if (targets.LookAtTarget != null &&
                !IsOwnedBy(
                    actor.transform,
                    targets.LookAtTarget))
            {
                diagnostic =
                    "The resolved Look At target must belong to the same Player Actor hierarchy.";
                return false;
            }

            diagnostic =
                "Camera Rig and Actor-owned targets are coherent.";

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
