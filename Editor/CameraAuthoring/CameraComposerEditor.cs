using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraComposer))]
    public sealed class CameraComposerEditor : UnityEditor.Editor
    {
        private SerializedProperty recipe;
        private SerializedProperty mode;
        private SerializedProperty ownershipScope;
        private SerializedProperty targetSourceKind;
        private SerializedProperty playerComposer;
        private SerializedProperty explicitFollowTarget;
        private SerializedProperty explicitLookAtTarget;
        private SerializedProperty followRequirement;
        private SerializedProperty lookAtRequirement;
        private SerializedProperty priority;
        private SerializedProperty unityCamera;
        private SerializedProperty cinemachineCamera;
        private SerializedProperty createUnityCameraIfMissing;
        private SerializedProperty createCinemachineCameraIfMissing;
        private SerializedProperty unityCameraObjectName;
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
            mode = serializedObject.FindProperty("mode");
            ownershipScope = serializedObject.FindProperty("ownershipScope");
            targetSourceKind = serializedObject.FindProperty("targetSourceKind");
            playerComposer = serializedObject.FindProperty("playerComposer");
            explicitFollowTarget = serializedObject.FindProperty("explicitFollowTarget");
            explicitLookAtTarget = serializedObject.FindProperty("explicitLookAtTarget");
            followRequirement = serializedObject.FindProperty("followRequirement");
            lookAtRequirement = serializedObject.FindProperty("lookAtRequirement");
            priority = serializedObject.FindProperty("priority");
            unityCamera = serializedObject.FindProperty("unityCamera");
            cinemachineCamera = serializedObject.FindProperty("cinemachineCamera");
            createUnityCameraIfMissing = serializedObject.FindProperty("createUnityCameraIfMissing");
            createCinemachineCameraIfMissing = serializedObject.FindProperty("createCinemachineCameraIfMissing");
            unityCameraObjectName = serializedObject.FindProperty("unityCameraObjectName");
            cinemachineCameraObjectName = serializedObject.FindProperty("cinemachineCameraObjectName");
            logApplyRebuildDiagnostics = serializedObject.FindProperty("logApplyRebuildDiagnostics");
            lastApplyRebuildStatus = serializedObject.FindProperty("lastApplyRebuildStatus");
            lastBlockingIssue = serializedObject.FindProperty("lastBlockingIssue");
            lastTargetResolutionSummary = serializedObject.FindProperty("lastTargetResolutionSummary");
            lastMaterializationSummary = serializedObject.FindProperty("lastMaterializationSummary");
            lastResolvedFollowTarget = serializedObject.FindProperty("lastResolvedFollowTarget");
            lastResolvedLookAtTarget = serializedObject.FindProperty("lastResolvedLookAtTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Camera Product Surface", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "CameraComposer is the designer-first surface for Cinemachine camera intent. This MVP supports SinglePlayerFollowCamera using an explicit PlayerComposer or explicit Transform target source.",
                MessageType.Info);

            DrawDesignerSection();
            DrawActions();
            DrawAdvancedSection();
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDesignerSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Designer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(recipe);
            EditorGUILayout.PropertyField(mode);
            EditorGUILayout.PropertyField(ownershipScope);
            EditorGUILayout.PropertyField(targetSourceKind);
            EditorGUILayout.PropertyField(playerComposer);
            EditorGUILayout.PropertyField(explicitFollowTarget);
            EditorGUILayout.PropertyField(explicitLookAtTarget);
            EditorGUILayout.PropertyField(followRequirement);
            EditorGUILayout.PropertyField(lookAtRequirement);
            EditorGUILayout.PropertyField(priority);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    foreach (Object targetObject in targets)
                    {
                        var composer = (CameraComposer)targetObject;
                        Undo.RecordObject(composer, "Apply Camera Recipe Defaults");
                        if (!composer.EditorApplyRecipeDefaults(false, out string issue))
                        {
                            Debug.LogWarning($"[Immersive.Framework][CameraComposer] Apply Recipe Defaults failed. camera='{composer.name}' issue='{issue}'", composer);
                        }

                        EditorUtility.SetDirty(composer);
                    }
                }

                if (GUILayout.Button("Overwrite From Recipe"))
                {
                    foreach (Object targetObject in targets)
                    {
                        var composer = (CameraComposer)targetObject;
                        Undo.RecordObject(composer, "Overwrite Camera Composer From Recipe");
                        if (!composer.EditorApplyRecipeDefaults(true, out string issue))
                        {
                            Debug.LogWarning($"[Immersive.Framework][CameraComposer] Overwrite From Recipe failed. camera='{composer.name}' issue='{issue}'", composer);
                        }

                        EditorUtility.SetDirty(composer);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    foreach (Object targetObject in targets)
                    {
                        CameraComposerApplyRebuildUtility.Validate((CameraComposer)targetObject);
                    }
                }

                if (GUILayout.Button("Apply / Rebuild"))
                {
                    foreach (Object targetObject in targets)
                    {
                        CameraComposerApplyRebuildUtility.ApplyOrRebuild((CameraComposer)targetObject);
                    }
                }
            }
        }

        private void DrawAdvancedSection()
        {
            EditorGUILayout.Space();
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced / Technical Materialization", true);
            if (!showAdvanced)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(unityCamera);
            EditorGUILayout.PropertyField(cinemachineCamera);
            EditorGUILayout.PropertyField(createUnityCameraIfMissing);
            EditorGUILayout.PropertyField(createCinemachineCameraIfMissing);
            EditorGUILayout.PropertyField(unityCameraObjectName);
            EditorGUILayout.PropertyField(cinemachineCameraObjectName);
            EditorGUILayout.PropertyField(logApplyRebuildDiagnostics);
            EditorGUI.indentLevel--;
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.Space();
            showDebug = EditorGUILayout.Foldout(showDebug, "Debug", true);
            if (!showDebug)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(lastApplyRebuildStatus);
            EditorGUILayout.PropertyField(lastBlockingIssue);
            EditorGUILayout.PropertyField(lastTargetResolutionSummary);
            EditorGUILayout.PropertyField(lastMaterializationSummary);
            EditorGUILayout.PropertyField(lastResolvedFollowTarget);
            EditorGUILayout.PropertyField(lastResolvedLookAtTarget);
            EditorGUI.EndDisabledGroup();

            CameraComposer composer = (CameraComposer)target;
            CameraComposerDebugSnapshot snapshot = composer.CreateDebugSnapshot();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resolved Evidence", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mode", snapshot.Mode.ToString());
            EditorGUILayout.LabelField("Ownership", snapshot.OwnershipScope.ToString());
            EditorGUILayout.LabelField("Target Source", snapshot.TargetSourceKind.ToString());
            EditorGUILayout.LabelField("Logical Source Id", snapshot.LogicalSourceId);
            EditorGUILayout.LabelField("Diagnostic Label", snapshot.DiagnosticLabel);
            EditorGUILayout.LabelField("Priority", snapshot.Priority.ToString());
            EditorGUILayout.LabelField("Unity Camera", snapshot.UnityCameraName);
            EditorGUILayout.LabelField("Cinemachine Camera", snapshot.CinemachineCameraName);
            EditorGUILayout.LabelField("Follow Target", snapshot.ResolvedFollowTargetName);
            EditorGUILayout.LabelField("LookAt Target", snapshot.ResolvedLookAtTargetName);
        }
    }
}
