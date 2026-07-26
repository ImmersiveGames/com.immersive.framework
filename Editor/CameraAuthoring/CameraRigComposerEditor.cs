using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    public sealed class CameraRigComposerEditor :
        UnityEditor.Editor
    {
        private enum TargetAuthoringMode
        {
            ExplicitTransforms = 0,
            TargetSourceComponent = 1
        }

        private SerializedProperty presentationIntent;
        private SerializedProperty targetSourceKind;
        private SerializedProperty targetSource;
        private SerializedProperty explicitFollowTarget;
        private SerializedProperty explicitLookAtTarget;
        private SerializedProperty followRequirement;
        private SerializedProperty lookAtRequirement;
        private SerializedProperty followOffset;
        private SerializedProperty cinemachineCamera;
        private SerializedProperty logApplyRebuildDiagnostics;
        private SerializedProperty lastApplyRebuildStatus;
        private SerializedProperty lastBlockingIssue;
        private SerializedProperty lastTargetResolutionSummary;
        private SerializedProperty lastMaterializationSummary;
        private SerializedProperty lastResolvedFollowTarget;
        private SerializedProperty lastResolvedLookAtTarget;

        private CameraRigComposerApplyRebuildResult? lastOperationResult;
        private bool validationOutdated;
        private bool showAdvancedDiagnostics;

        private void OnEnable()
        {
            presentationIntent =
                serializedObject.FindProperty(
                    "presentationIntent");

            targetSourceKind =
                serializedObject.FindProperty(
                    "targetSourceKind");

            targetSource =
                serializedObject.FindProperty(
                    "targetSource");

            explicitFollowTarget =
                serializedObject.FindProperty(
                    "explicitFollowTarget");

            explicitLookAtTarget =
                serializedObject.FindProperty(
                    "explicitLookAtTarget");

            followRequirement =
                serializedObject.FindProperty(
                    "followRequirement");

            lookAtRequirement =
                serializedObject.FindProperty(
                    "lookAtRequirement");

            followOffset =
                serializedObject.FindProperty(
                    "followOffset");

            cinemachineCamera =
                serializedObject.FindProperty(
                    "cinemachineCamera");

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
            serializedObject.UpdateIfRequiredOrScript();

            DrawHeader();

            EditorGUILayout.Space(8f);
            DrawCameraBehavior();

            EditorGUILayout.Space(8f);
            DrawMaterialization();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                lastOperationResult.HasValue)
            {
                validationOutdated = true;
            }
        }

        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Camera Rig Composer",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This component is the single authority for one concrete Camera rig: targets, requirements, framing and Cinemachine materialization. Use Unity Presets when reusable values are needed.",
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "It creates only the local Cinemachine Camera rig. It never creates a Unity Camera, Cinemachine Brain, Audio Listener or persistent Camera Output.",
                MessageType.None);
        }

        private void DrawCameraBehavior()
        {
            EditorGUILayout.LabelField(
                "Camera Behavior",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    presentationIntent,
                    new GUIContent(
                        "Presentation",
                        "Follow is the currently implemented presentation intent."));
            }

            TargetAuthoringMode currentMode =
                ResolveTargetAuthoringMode();

            TargetAuthoringMode selectedMode =
                (TargetAuthoringMode)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Target Mode",
                        "Use direct Transform references or one typed ICameraTargetSource component."),
                    currentMode);

            if (selectedMode !=
                currentMode)
            {
                SetTargetAuthoringMode(
                    selectedMode);
            }

            if (selectedMode ==
                TargetAuthoringMode.ExplicitTransforms)
            {
                EditorGUILayout.PropertyField(
                    explicitFollowTarget,
                    new GUIContent(
                        "Follow Transform"));

                EditorGUILayout.PropertyField(
                    explicitLookAtTarget,
                    new GUIContent(
                        "Look At Transform"));
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(
                    targetSource,
                    new GUIContent(
                        "Target Source",
                        "Component implementing ICameraTargetSource."));

                if (EditorGUI.EndChangeCheck())
                {
                    SyncSerializedTargetSourceKind();
                }
            }

            EditorGUILayout.PropertyField(
                followRequirement,
                new GUIContent(
                    "Follow Target",
                    "Required blocks validation when missing. Optional allows a missing target. Not Used is incompatible with Follow presentation."));

            EditorGUILayout.PropertyField(
                lookAtRequirement,
                new GUIContent(
                    "Look At Target",
                    "Required blocks validation when missing. Optional allows a missing target. Not Used excludes Look At."));

            EditorGUILayout.PropertyField(
                followOffset,
                new GUIContent(
                    "Follow Offset"));
        }

        private void DrawMaterialization()
        {
            EditorGUILayout.LabelField(
                "Materialization",
                EditorStyles.boldLabel);

            var composer =
                (CameraRigComposer)target;

            CinemachineCamera assigned =
                cinemachineCamera.objectReferenceValue
                    as CinemachineCamera;

            CinemachineCamera local =
                assigned != null
                    ? assigned
                    : composer.GetComponentInChildren<
                        CinemachineCamera>(
                        true);

            if (local == null)
            {
                EditorGUILayout.HelpBox(
                    "No local Cinemachine Camera exists. Apply / Rebuild Rig will create one automatically.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Existing Cinemachine Camera '{local.name}' will be reused and repaired idempotently.",
                    MessageType.None);
            }

            if (GUILayout.Button(
                    "Apply / Rebuild Rig"))
            {
                RunApplyOrRebuild();
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (!lastOperationResult.HasValue)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Configure the rig and run validation.",
                    MessageType.None);
            }
            else if (validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the configuration changed.",
                    MessageType.Warning);
            }
            else if (lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    "Ready — the last validation or materialization operation succeeded.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Needs Attention — the last operation found a blocking issue. Open Advanced / Diagnostics.",
                    MessageType.Error);
            }

            if (GUILayout.Button(
                    "Validate Configuration"))
            {
                RunValidation();
            }
        }

        private void DrawAdvancedDiagnostics()
        {
            showAdvancedDiagnostics =
                EditorGUILayout.Foldout(
                    showAdvancedDiagnostics,
                    "Advanced / Diagnostics",
                    true);

            if (!showAdvancedDiagnostics)
            {
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                "Technical Reference",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                cinemachineCamera,
                new GUIContent(
                    "Cinemachine Camera"));

            EditorGUILayout.PropertyField(
                logApplyRebuildDiagnostics,
                new GUIContent(
                    "Log Apply / Rebuild Diagnostics"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Materialization Evidence",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    lastApplyRebuildStatus,
                    new GUIContent(
                        "Last Status"));

                EditorGUILayout.PropertyField(
                    lastBlockingIssue,
                    new GUIContent(
                        "Last Blocking Issue"));

                EditorGUILayout.PropertyField(
                    lastTargetResolutionSummary,
                    new GUIContent(
                        "Target Resolution"));

                EditorGUILayout.PropertyField(
                    lastMaterializationSummary,
                    new GUIContent(
                        "Materialization Summary"));

                EditorGUILayout.PropertyField(
                    lastResolvedFollowTarget,
                    new GUIContent(
                        "Resolved Follow Target"));

                EditorGUILayout.PropertyField(
                    lastResolvedLookAtTarget,
                    new GUIContent(
                        "Resolved Look At Target"));
            }

            if (lastOperationResult.HasValue &&
                !validationOutdated &&
                !lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    lastOperationResult.Value.BlockingIssue,
                    MessageType.Error);
            }

            EditorGUI.indentLevel--;
        }

        private TargetAuthoringMode ResolveTargetAuthoringMode()
        {
            CameraTargetSourceKind kind =
                (CameraTargetSourceKind)
                    targetSourceKind.intValue;

            return
                targetSource.objectReferenceValue ==
                    null &&
                kind ==
                    CameraTargetSourceKind.ExplicitTransform
                    ? TargetAuthoringMode.ExplicitTransforms
                    : TargetAuthoringMode.TargetSourceComponent;
        }

        private void SetTargetAuthoringMode(
            TargetAuthoringMode mode)
        {
            if (mode ==
                TargetAuthoringMode.ExplicitTransforms)
            {
                targetSource.objectReferenceValue =
                    null;

                targetSourceKind.intValue =
                    (int)CameraTargetSourceKind
                        .ExplicitTransform;

                return;
            }

            if ((CameraTargetSourceKind)
                    targetSourceKind.intValue ==
                CameraTargetSourceKind.ExplicitTransform)
            {
                targetSourceKind.intValue =
                    (int)CameraTargetSourceKind.None;
            }
        }

        private void SyncSerializedTargetSourceKind()
        {
            Object assigned =
                targetSource.objectReferenceValue;

            if (assigned is
                ICameraTargetSource provider)
            {
                targetSourceKind.intValue =
                    (int)provider.TargetSourceKind;
                return;
            }

            targetSourceKind.intValue =
                (int)CameraTargetSourceKind.None;
        }

        private void RunValidation()
        {
            serializedObject.ApplyModifiedProperties();

            lastOperationResult =
                CameraRigComposerApplyRebuildUtility
                    .Validate(
                        (CameraRigComposer)target,
                        false);

            validationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }

        private void RunApplyOrRebuild()
        {
            serializedObject.ApplyModifiedProperties();

            lastOperationResult =
                CameraRigComposerApplyRebuildUtility
                    .ApplyOrRebuild(
                        (CameraRigComposer)target,
                        true,
                        true);

            validationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }
    }
}
