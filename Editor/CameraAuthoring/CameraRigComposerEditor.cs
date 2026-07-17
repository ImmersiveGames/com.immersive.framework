using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    public sealed class CameraRigComposerEditor : UnityEditor.Editor
    {
        private SerializedProperty _recipe;
        private SerializedProperty _presentationIntent;
        private SerializedProperty _targetSourceKind;
        private SerializedProperty _targetSource;
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
            _targetSource = serializedObject.FindProperty("targetSource");
            _explicitFollowTarget =
                serializedObject.FindProperty("explicitFollowTarget");
            _explicitLookAtTarget =
                serializedObject.FindProperty("explicitLookAtTarget");
            _followRequirement =
                serializedObject.FindProperty("followRequirement");
            _lookAtRequirement =
                serializedObject.FindProperty("lookAtRequirement");
            _followOffset = serializedObject.FindProperty("followOffset");
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
                serializedObject.FindProperty("lastApplyRebuildStatus");
            _lastBlockingIssue =
                serializedObject.FindProperty("lastBlockingIssue");
            _lastTargetResolutionSummary =
                serializedObject.FindProperty("lastTargetResolutionSummary");
            _lastMaterializationSummary =
                serializedObject.FindProperty("lastMaterializationSummary");
            _lastResolvedFollowTarget =
                serializedObject.FindProperty("lastResolvedFollowTarget");
            _lastResolvedLookAtTarget =
                serializedObject.FindProperty("lastResolvedLookAtTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Camera Rig", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "CameraRigComposer materializes one local Cinemachine Camera rig. Assign a typed Target Source component or author explicit Follow and Look At transforms. It never creates a Unity Camera, CinemachineBrain, AudioListener or runtime output.",
                MessageType.Info);

            EditorGUILayout.LabelField("Designer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_recipe);
            EditorGUILayout.PropertyField(_presentationIntent);
            EditorGUILayout.PropertyField(
                _targetSource,
                new GUIContent("Target Source"));
            DrawTargetSourceStatus();

            if (_targetSource.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(
                    _explicitFollowTarget,
                    new GUIContent("Follow Target"));
                EditorGUILayout.PropertyField(
                    _explicitLookAtTarget,
                    new GUIContent("Look At Target"));
            }

            EditorGUILayout.PropertyField(_followRequirement);
            EditorGUILayout.PropertyField(_lookAtRequirement);
            EditorGUILayout.PropertyField(_followOffset);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    ApplyRecipeDefaults(false);
                }

                if (GUILayout.Button("Overwrite From Recipe"))
                {
                    ApplyRecipeDefaults(true);
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
                        CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
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
                EditorGUILayout.PropertyField(
                    _targetSourceKind,
                    new GUIContent("Serialized Target Source Kind"));
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
                CameraRigComposer composer =
                    targets.Length == 1
                        ? (CameraRigComposer)target
                        : null;
                EditorGUILayout.EnumPopup(
                    "Effective Target Source Kind",
                    composer != null
                        ? composer.CreateDebugSnapshot().TargetSourceKind
                        : CameraTargetSourceKind.None);
                EditorGUILayout.PropertyField(_lastApplyRebuildStatus);
                EditorGUILayout.PropertyField(_lastBlockingIssue);
                EditorGUILayout.PropertyField(_lastTargetResolutionSummary);
                EditorGUILayout.PropertyField(_lastMaterializationSummary);
                EditorGUILayout.PropertyField(_lastResolvedFollowTarget);
                EditorGUILayout.PropertyField(_lastResolvedLookAtTarget);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetSourceStatus()
        {
            Object assigned = _targetSource.objectReferenceValue;
            if (assigned == null)
            {
                EditorGUILayout.HelpBox(
                    "No target-source component is assigned. The rig will use the explicit Follow and Look At fields below.",
                    MessageType.None);
                return;
            }

            if (assigned is not ICameraTargetSource)
            {
                EditorGUILayout.HelpBox(
                    $"Assigned component '{assigned.GetType().FullName}' does not implement ICameraTargetSource. Validate / Apply / Rebuild are blocked until this field is cleared or a typed provider is assigned.",
                    MessageType.Error);

                if (GUILayout.Button("Clear Invalid Target Source"))
                {
                    _targetSource.objectReferenceValue = null;
                }

                return;
            }

            EditorGUILayout.HelpBox(
                "Target source is typed and explicit. Required missing targets will block Validate / Apply.",
                MessageType.None);
        }

        private void ApplyRecipeDefaults(bool overwriteExisting)
        {
            foreach (Object item in targets)
            {
                var composer = (CameraRigComposer)item;
                Undo.RecordObject(
                    composer,
                    overwriteExisting
                        ? "Overwrite Camera Rig Composer From Recipe"
                        : "Apply Camera Rig Recipe Defaults");
                composer.EditorApplyRecipeDefaults(
                    overwriteExisting,
                    out _);
                EditorUtility.SetDirty(composer);
            }
        }
    }
}
