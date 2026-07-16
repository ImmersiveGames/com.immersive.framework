using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    public sealed class CameraRigComposerEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _recipe;
        private SerializedProperty _presentationIntent;
        private SerializedProperty _targetSourceKind;
        private SerializedProperty _playerComposer;
        private SerializedProperty _explicitFollowTarget;
        private SerializedProperty _explicitLookAtTarget;
        private SerializedProperty _followRequirement;
        private SerializedProperty _lookAtRequirement;
        private SerializedProperty _followOffset;
        private SerializedProperty _cinemachineCamera;
        private SerializedProperty _createCinemachineCameraIfMissing;
        private SerializedProperty _cinemachineCameraObjectName;
        private SerializedProperty _logApplyRebuildDiagnostics;
        private SerializedProperty _lastApplyRebuildStatus;
        private SerializedProperty _lastBlockingIssue;
        private SerializedProperty _lastTargetResolutionSummary;
        private SerializedProperty _lastMaterializationSummary;
        private SerializedProperty _lastResolvedFollowTarget;
        private SerializedProperty _lastResolvedLookAtTarget;

        private bool _showAdvanced;
        private bool _showDebug = true;

        private void OnEnable()
        {
            _recipe = serializedObject.FindProperty("recipe");
            _presentationIntent =
                serializedObject.FindProperty("presentationIntent");
            _targetSourceKind =
                serializedObject.FindProperty("targetSourceKind");
            _playerComposer =
                serializedObject.FindProperty("preAuthoredPlayerComposer");
            _explicitFollowTarget =
                serializedObject.FindProperty("explicitFollowTarget");
            _explicitLookAtTarget =
                serializedObject.FindProperty("explicitLookAtTarget");
            _followRequirement =
                serializedObject.FindProperty("followRequirement");
            _lookAtRequirement =
                serializedObject.FindProperty("lookAtRequirement");
            _followOffset =
                serializedObject.FindProperty("followOffset");
            _cinemachineCamera =
                serializedObject.FindProperty("cinemachineCamera");
            _createCinemachineCameraIfMissing =
                serializedObject.FindProperty(
                    "createCinemachineCameraIfMissing");
            _cinemachineCameraObjectName =
                serializedObject.FindProperty(
                    "cinemachineCameraObjectName");
            _logApplyRebuildDiagnostics =
                serializedObject.FindProperty(
                    "logApplyRebuildDiagnostics");
            _lastApplyRebuildStatus =
                serializedObject.FindProperty(
                    "lastApplyRebuildStatus");
            _lastBlockingIssue =
                serializedObject.FindProperty(
                    "lastBlockingIssue");
            _lastTargetResolutionSummary =
                serializedObject.FindProperty(
                    "lastTargetResolutionSummary");
            _lastMaterializationSummary =
                serializedObject.FindProperty(
                    "lastMaterializationSummary");
            _lastResolvedFollowTarget =
                serializedObject.FindProperty(
                    "lastResolvedFollowTarget");
            _lastResolvedLookAtTarget =
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

            EditorGUILayout.PropertyField(_recipe);
            EditorGUILayout.PropertyField(_presentationIntent);
            EditorGUILayout.PropertyField(_targetSourceKind);
            EditorGUILayout.PropertyField(_playerComposer);
            EditorGUILayout.PropertyField(_explicitFollowTarget);
            EditorGUILayout.PropertyField(_explicitLookAtTarget);
            EditorGUILayout.PropertyField(_followRequirement);
            EditorGUILayout.PropertyField(_lookAtRequirement);
            EditorGUILayout.PropertyField(_followOffset);

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

            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                "Advanced / Technical Materialization",
                true);

            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_cinemachineCamera);
                EditorGUILayout.PropertyField(
                    _createCinemachineCameraIfMissing);
                EditorGUILayout.PropertyField(
                    _cinemachineCameraObjectName);
                EditorGUILayout.PropertyField(
                    _logApplyRebuildDiagnostics);
                EditorGUI.indentLevel--;
            }

            _showDebug = EditorGUILayout.Foldout(
                _showDebug,
                "Debug",
                true);

            if (_showDebug)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(
                    _lastApplyRebuildStatus);
                EditorGUILayout.PropertyField(
                    _lastBlockingIssue);
                EditorGUILayout.PropertyField(
                    _lastTargetResolutionSummary);
                EditorGUILayout.PropertyField(
                    _lastMaterializationSummary);
                EditorGUILayout.PropertyField(
                    _lastResolvedFollowTarget);
                EditorGUILayout.PropertyField(
                    _lastResolvedLookAtTarget);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
