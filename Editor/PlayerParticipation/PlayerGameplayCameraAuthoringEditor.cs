using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(
        typeof(PlayerGameplayCameraAuthoring))]
    internal sealed class
        PlayerGameplayCameraAuthoringEditor :
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
            serializedObject.Update();

            EditorGUILayout.LabelField(
                "Player Gameplay Camera",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Add this component inside a Logical Player Actor hierarchy. It declares Camera participation and references one Actor-owned Camera Rig Composer. Targets are configured only on the Composer.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Participation",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                requiredness,
                new GUIContent(
                    "Requiredness"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Camera",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                cameraRig,
                new GUIContent(
                    "Camera Rig"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Arbitration",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                precedence,
                new GUIContent(
                    "Precedence"));

            serializedObject
                .ApplyModifiedProperties();

            DrawStatus();

            EditorGUILayout.Space(6f);
            showAdvancedDebug =
                EditorGUILayout.Foldout(
                    showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (showAdvancedDebug)
            {
                DrawAdvancedDebug();
            }
        }

        private void DrawStatus()
        {
            var authoring =
                (PlayerGameplayCameraAuthoring)
                    target;

            bool valid =
                TryValidateAuthoring(
                    authoring,
                    out string diagnostic,
                    out _);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Status",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Authoring",
                valid
                    ? "Ready"
                    : "Incomplete");

            EditorGUILayout.HelpBox(
                valid
                    ? "Player Gameplay Camera authoring is ready."
                    : diagnostic,
                valid
                    ? MessageType.Info
                    : MessageType.Warning);
        }

        private void DrawAdvancedDebug()
        {
            var authoring =
                (PlayerGameplayCameraAuthoring)
                    target;

            TryValidateAuthoring(
                authoring,
                out string diagnostic,
                out CameraResolvedTargets targets);

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                "Resolved Follow Target",
                targets.FollowTarget != null
                    ? targets.FollowTarget.name
                    : "<none>");

            EditorGUILayout.LabelField(
                "Resolved Look At Target",
                targets.LookAtTarget != null
                    ? targets.LookAtTarget.name
                    : "<none>");

            EditorGUILayout.LabelField(
                "Rig Status",
                authoring.CameraRig != null
                    ? authoring.CameraRig
                        .LastApplyRebuildStatus
                    : "<missing>");

            EditorGUILayout.HelpBox(
                diagnostic,
                MessageType.None);

            EditorGUI.indentLevel--;
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
