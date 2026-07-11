using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    public sealed class CameraRigComposerEditor :
        UnityEditor.Editor
    {
        private SerializedProperty recipe;
        private SerializedProperty presentationIntent;
        private SerializedProperty targetSourceKind;
        private SerializedProperty playerComposer;
        private SerializedProperty explicitFollowTarget;
        private SerializedProperty explicitLookAtTarget;
        private SerializedProperty followRequirement;
        private SerializedProperty lookAtRequirement;
        private SerializedProperty cinemachineCamera;
        private SerializedProperty createCinemachineCameraIfMissing;
        private SerializedProperty cinemachineCameraObjectName;
        private SerializedProperty logApplyRebuildDiagnostics;
        private SerializedProperty lastApplyRebuildStatus;
        private SerializedProperty lastBlockingIssue;
        private SerializedProperty lastTargetResolutionSummary;
        private SerializedProperty lastMaterializationSummary;
        private SerializedProperty lastResolvedFollowTarget;
        private SerializedProperty lastResolvedLookAtTarget;

        private bool showAdvanced;
        private bool showDebug = true;

        private void OnEnable()
        {
            recipe = serializedObject.FindProperty("recipe");
            presentationIntent =
                serializedObject.FindProperty("presentationIntent");
            targetSourceKind =
                serializedObject.FindProperty("targetSourceKind");
            playerComposer =
                serializedObject.FindProperty("playerComposer");
            explicitFollowTarget =
                serializedObject.FindProperty("explicitFollowTarget");
            explicitLookAtTarget =
                serializedObject.FindProperty("explicitLookAtTarget");
            followRequirement =
                serializedObject.FindProperty("followRequirement");
            lookAtRequirement =
                serializedObject.FindProperty("lookAtRequirement");
            cinemachineCamera =
                serializedObject.FindProperty("cinemachineCamera");
            createCinemachineCameraIfMissing =
                serializedObject.FindProperty(
                    "createCinemachineCameraIfMissing");
            cinemachineCameraObjectName =
                serializedObject.FindProperty(
                    "cinemachineCameraObjectName");
            logApplyRebuildDiagnostics =
                serializedObject.FindProperty(
                    "logApplyRebuildDiagnostics");
            lastApplyRebuildStatus =
                serializedObject.FindProperty(
                    "lastApplyRebuildStatus");
            lastBlockingIssue =
                serializedObject.FindProperty(
                    "lastBlockingIssue");
            lastTargetResolutionSummary =
                serializedObject.FindProperty(
                    "lastTargetResolutionSummary");
            lastMaterializationSummary =
                serializedObject.FindProperty(
                    "lastMaterializationSummary");
            lastResolvedFollowTarget =
                serializedObject.FindProperty(
                    "lastResolvedFollowTarget");
            lastResolvedLookAtTarget =
                serializedObject.FindProperty(
                    "lastResolvedLookAtTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(
                "Camera Rig",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "CameraRigComposer materializes one local Cinemachine Camera rig. It never creates a Unity Camera, CinemachineBrain, AudioListener or runtime output.",
                MessageType.Info);

            EditorGUILayout.LabelField(
                "Designer",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(recipe);
            EditorGUILayout.PropertyField(presentationIntent);
            EditorGUILayout.PropertyField(targetSourceKind);
            EditorGUILayout.PropertyField(playerComposer);
            EditorGUILayout.PropertyField(explicitFollowTarget);
            EditorGUILayout.PropertyField(explicitLookAtTarget);
            EditorGUILayout.PropertyField(followRequirement);
            EditorGUILayout.PropertyField(lookAtRequirement);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    foreach (Object item in targets)
                    {
                        var composer =
                            (CameraRigComposer)item;
                        Undo.RecordObject(
                            composer,
                            "Apply Camera Rig Recipe Defaults");
                        composer.EditorApplyRecipeDefaults(
                            false,
                            out _);
                        EditorUtility.SetDirty(composer);
                    }
                }

                if (GUILayout.Button("Overwrite From Recipe"))
                {
                    foreach (Object item in targets)
                    {
                        var composer =
                            (CameraRigComposer)item;
                        Undo.RecordObject(
                            composer,
                            "Overwrite Camera Rig Composer From Recipe");
                        composer.EditorApplyRecipeDefaults(
                            true,
                            out _);
                        EditorUtility.SetDirty(composer);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    foreach (Object item in targets)
                    {
                        CameraRigComposerApplyRebuildUtility.Validate(
                            (CameraRigComposer)item);
                    }
                }

                if (GUILayout.Button("Apply / Rebuild"))
                {
                    foreach (Object item in targets)
                    {
                        CameraRigComposerApplyRebuildUtility
                            .ApplyOrRebuild(
                                (CameraRigComposer)item);
                    }
                }
            }

            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced / Technical Materialization",
                true);

            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(cinemachineCamera);
                EditorGUILayout.PropertyField(
                    createCinemachineCameraIfMissing);
                EditorGUILayout.PropertyField(
                    cinemachineCameraObjectName);
                EditorGUILayout.PropertyField(
                    logApplyRebuildDiagnostics);
                EditorGUI.indentLevel--;
            }

            showDebug = EditorGUILayout.Foldout(
                showDebug,
                "Debug",
                true);

            if (showDebug)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(
                    lastApplyRebuildStatus);
                EditorGUILayout.PropertyField(
                    lastBlockingIssue);
                EditorGUILayout.PropertyField(
                    lastTargetResolutionSummary);
                EditorGUILayout.PropertyField(
                    lastMaterializationSummary);
                EditorGUILayout.PropertyField(
                    lastResolvedFollowTarget);
                EditorGUILayout.PropertyField(
                    lastResolvedLookAtTarget);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
