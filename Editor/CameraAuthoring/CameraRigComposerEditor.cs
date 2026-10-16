using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Editor.Common;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "IF-ADR-010 Inspector implementation for the CameraRigComposer product authoring surface.")]
    public sealed class CameraRigComposerEditor : UnityEditor.Editor
    {

        private SerializedProperty _behaviorDefinition;
        private SerializedProperty _cinemachineCamera;
        private SerializedProperty _materializedPresentationIntent;
        private SerializedProperty _frameworkOwnedCinemachineCamera;
        private SerializedProperty _frameworkOwnedPositionControl;
        private SerializedProperty _frameworkOwnedRotationControl;
        private SerializedProperty _frameworkOwnedGroupTargetGroup;
        private SerializedProperty _frameworkOwnedGroupFraming;
        private SerializedProperty _materializationRevision;
        private SerializedProperty _logApplyRebuildDiagnostics;
        private SerializedProperty _lastApplyRebuildStatus;
        private SerializedProperty _lastBlockingIssue;
        private SerializedProperty _lastMaterializationSummary;

        private CameraRigComposerApplyRebuildResult? _lastValidationResult;
        private CameraRigComposerApplyRebuildResult? _lastApplyResult;
        private bool _validationOutdated;
        private bool _materializationOutdated;
        private bool _showAdvancedDebug;
        private bool _localCinemachineCameraResolved;
        private CinemachineCamera _localCinemachineCamera;
        private string _previewIssue;
        private Transform _pendingPreviewTarget;

        private void OnEnable()
        {
            EditorApplication.update -= RepaintWhilePreviewIsActive;
            EditorApplication.update += RepaintWhilePreviewIsActive;

            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            _behaviorDefinition =
                current.FindProperty("behaviorDefinition");
            _cinemachineCamera =
                current.FindProperty("cinemachineCamera");
            _materializedPresentationIntent =
                current.FindProperty("materializedPresentationIntent");
            _frameworkOwnedCinemachineCamera =
                current.FindProperty("frameworkOwnedCinemachineCamera");
            _frameworkOwnedPositionControl =
                current.FindProperty("frameworkOwnedPositionControl");
            _frameworkOwnedRotationControl =
                current.FindProperty("frameworkOwnedRotationControl");
            _frameworkOwnedGroupTargetGroup =
                current.FindProperty("frameworkOwnedGroupTargetGroup");
            _frameworkOwnedGroupFraming =
                current.FindProperty("frameworkOwnedGroupFraming");
            _materializationRevision =
                current.FindProperty("materializationRevision");
            _logApplyRebuildDiagnostics =
                current.FindProperty("logApplyRebuildDiagnostics");
            _lastApplyRebuildStatus =
                current.FindProperty("lastApplyRebuildStatus");
            _lastBlockingIssue =
                current.FindProperty("lastBlockingIssue");
            _lastMaterializationSummary =
                current.FindProperty("lastMaterializationSummary");
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhilePreviewIsActive;
        }

        private void RepaintWhilePreviewIsActive()
        {
            if (target is CameraRigComposer composer &&
                CameraRigPreviewController.IsPreviewActive(composer))
                Repaint();
        }

        public override void OnInspectorGUI()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            _localCinemachineCameraResolved = false;
            current.UpdateIfRequiredOrScript();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            DrawComposerHeader();

            EditorGUI.BeginChangeCheck();

            DrawPresentation();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            bool authoringChanged =
                EditorGUI.EndChangeCheck();

            bool modified = current.ApplyModifiedProperties();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            if (authoringChanged || modified)
            {
                MarkAuthoringChanged();
            }

            DrawPreviewComposition((CameraRigComposer)inspectedTarget);
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            DrawAdvancedDebug();
        }

        private static void DrawComposerHeader()
        {
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Camera Rig Composer",
                    "Authors one reusable Camera rig. Apply / Rebuild materializes the local Cinemachine Camera and Framework-owned pipeline controls. Session Camera Assignments own normal camera selection."),
                EditorStyles.boldLabel);
        }

        private void DrawPresentation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Behavior");

            EditorGUILayout.PropertyField(
                _behaviorDefinition,
                new GUIContent(
                    "Definition",
                    "Reusable typed presentation intent. The asset owns model tuning; this Composer owns the concrete local rig and its materialization."));

            if (_behaviorDefinition.objectReferenceValue is FollowCameraRigBehaviorDefinition follow)
            {
                DrawFollowModeSelector((CameraRigComposer)target, follow);
            }
        }

        private void DrawFollowModeSelector(
            CameraRigComposer composer,
            FollowCameraRigBehaviorDefinition definition)
        {
            bool previewActive = CameraRigPreviewController.IsPreviewActive(composer);
            FollowCameraRigMode requestedMode = definition.FollowMode;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(previewActive))
            {
                requestedMode = (FollowCameraRigMode)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Follow Mode",
                        "Select FixedOffset or ScreenComposer. A mode change affects the referenced Behavior Definition and requires Apply / Rebuild before Preview."),
                    requestedMode);
            }

            if (!EditorGUI.EndChangeCheck() || requestedMode == definition.FollowMode)
                return;

            if (EditorUtility.IsPersistent(definition) &&
                !EditorUtility.DisplayDialog(
                    "Change Shared Follow Behavior",
                    $"'{definition.name}' is a project asset and may be shared. Changing its mode affects every rig that references it. Continue?",
                    "Change Mode",
                    "Cancel"))
                return;

            int undoGroup;
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Change Camera Follow Mode");
            Undo.RecordObject(definition, "Change Camera Follow Mode");

            var behaviorObject = new SerializedObject(definition);
            SerializedProperty followMode = behaviorObject.FindProperty("followMode");
            if (followMode == null)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                _previewIssue = "Follow Behavior Definition does not expose its mode to the Composer Inspector.";
                return;
            }

            followMode.enumValueIndex = (int)requestedMode;
            behaviorObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            if (EditorUtility.IsPersistent(definition))
            {
                AssetDatabase.SaveAssetIfDirty(definition);
                if (EditorUtility.IsDirty(definition))
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    _previewIssue = $"Unity did not confirm saving Follow Behavior Definition '{definition.name}'.";
                    return;
                }
            }
            else if (composer.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(composer.gameObject.scene);
            }

            Undo.CollapseUndoOperations(undoGroup);
            MarkAuthoringChanged();
            _previewIssue = "Follow Mode changed. Apply / Rebuild in Advanced / Debug before enabling Preview.";
        }







        private void DrawMaterialization(
            CameraRigPresentationIntent presentation)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Materialization");

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();
            CameraRigPresentationIntent materialized =
                ResolveMaterializedPresentationIntent();

            string status =
                ResolveMaterializationStatus(
                    presentation,
                    materialized,
                    local);

            EditorGUILayout.LabelField(
                "Status",
                status);

            DrawMaterializationIssue(
                presentation,
                materialized,
                local);

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying ||
                       CameraRigPreviewController.IsPreviewActive((CameraRigComposer)target) ||
                       presentation ==
                           CameraRigPresentationIntent.Undefined))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Apply / Rebuild",
                            "Preflight and reconcile the local Cinemachine Camera and only Framework-owned Position / Rotation controls. Unknown external conflicts block before mutation.")))
                {
                    RunApplyOrRebuild();
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Authoring actions are unavailable in Play Mode.",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawPreviewComposition(CameraRigComposer composer)
        {
            FrameworkAuthoringInspectorGui.Section("Preview");
            bool active = CameraRigPreviewController.IsPreviewActive(composer);
            if (!active && _pendingPreviewTarget == null &&
                composer.BehaviorDefinition is FollowCameraRigBehaviorDefinition &&
                composer.CinemachineCamera != null)
                _pendingPreviewTarget = composer.CinemachineCamera.Follow;
            EditorGUILayout.LabelField("State", CameraRigPreviewController.GetPreviewState(composer));
            string compositionStatus = ResolveMaterializationStatus(
                ResolvePresentationIntent(),
                ResolveMaterializedPresentationIntent(),
                ResolveLocalCinemachineCamera());
            EditorGUILayout.LabelField("Composition", compositionStatus);
            if (!active && compositionStatus != "Current")
                EditorGUILayout.HelpBox(
                    "Use Validate or Apply / Rebuild in Advanced / Debug before enabling Preview.",
                    MessageType.Info);

            bool usesTarget = composer.EffectiveFollowRequirement != CameraTargetRequirement.NotUsed;
            using (new EditorGUI.DisabledScope(!usesTarget))
            {
                Transform currentTarget = active
                    ? CameraRigPreviewController.GetPreviewTarget(composer)
                    : _pendingPreviewTarget;
                Transform selectedTarget = (Transform)EditorGUILayout.ObjectField(
                    composer.PresentationIntent == CameraRigPresentationIntent.Group ? "Group Member" : "Tracking Target",
                    currentTarget,
                    typeof(Transform),
                    true);
                if (selectedTarget != currentTarget)
                {
                    if (active)
                        CameraRigPreviewController.SetPreviewTarget(composer, selectedTarget, out _previewIssue);
                    _pendingPreviewTarget = selectedTarget;
                }
            }

            if (!active)
            {
                using (new EditorGUI.DisabledScope(
                           Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
                           compositionStatus != "Current"))
                {
                    if (GUILayout.Button(new GUIContent(
                            "Enable Preview",
                            "Uses the original CinemachineCamera with a temporary output below this rig.")))
                    {
                        if (CameraRigPreviewController.TryEnablePreview(composer, out _previewIssue) &&
                            _pendingPreviewTarget != null)
                            CameraRigPreviewController.SetPreviewTarget(
                                composer,
                                _pendingPreviewTarget,
                                out _previewIssue);
                    }
                }

                DrawFollowDesignerControls(composer, false);
            }
            else
            {
                DrawFollowDesignerControls(composer, true);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(
                        "Save Configuration",
                        "Validates, captures canonical authoring values, reconciles the Cinemachine rig and persists only authored prefab properties.")))
                    RunSaveConfiguration(composer);
                if (GUILayout.Button(new GUIContent(
                        "Disable Preview",
                        "Restores unsaved values, restores runtime Targets and Channels, and removes the temporary output.")))
                    CameraRigPreviewController.TryDisablePreview(composer, out _previewIssue);
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField(
                    "Save Status",
                    CameraRigPreviewController.HasUncapturedChanges(composer)
                        ? "Unsaved changes"
                        : "Configuration saved");
                EditorGUILayout.HelpBox(
                    "Adjust the controls here while Preview is active. Disable discards unsaved changes; Save Configuration writes them to their existing Behavior Definition and Composer owners. The selected Target remains preview-only.",
                    MessageType.None);
                if (composer.BehaviorDefinition != null && EditorUtility.IsPersistent(composer.BehaviorDefinition))
                    EditorGUILayout.HelpBox(
                        "Saving updates the assigned Behavior Definition asset. Other rigs that reference it use the same saved Follow settings; no asset copy is made.",
                        MessageType.Warning);
            }

            if (!string.IsNullOrWhiteSpace(_previewIssue))
            {
                EditorGUILayout.HelpBox(FormatBlockingIssueForInspector(_previewIssue), MessageType.Warning);
                if (GUILayout.Button("Dismiss Preview Message"))
                    _previewIssue = string.Empty;
            }
        }

        private void DrawFollowDesignerControls(CameraRigComposer composer, bool previewActive)
        {
            if (!(composer.BehaviorDefinition is FollowCameraRigBehaviorDefinition behavior))
                return;

            FrameworkAuthoringInspectorGui.Section("Framing");
            if (!previewActive)
            {
                EditorGUILayout.HelpBox(
                    "Enable Preview to adjust framing against the selected Target. Changes remain transient until Save Configuration.",
                    MessageType.Info);
                return;
            }

            CinemachineCamera camera = CameraRigPreviewController.GetPreviewCamera(composer);
            if (camera == null)
                return;

            if (!CameraRigPreviewController.CanEditPreviewFollowControls(composer, out string ownershipIssue))
            {
                EditorGUILayout.HelpBox(ownershipIssue, MessageType.Warning);
                return;
            }

            CinemachineFollow follow = camera.GetComponent<CinemachineFollow>();
            CinemachinePositionComposer screen = camera.GetComponent<CinemachinePositionComposer>();
            bool fixedOffset = behavior.FollowMode == FollowCameraRigMode.FixedOffset;
            if ((fixedOffset && follow == null) || (!fixedOffset && screen == null))
            {
                EditorGUILayout.HelpBox(
                    fixedOffset
                        ? "The Follow position control is missing. Resolve the ownership conflict in Advanced before previewing."
                        : "The Position Composer control is missing. Resolve the ownership conflict in Advanced before previewing.",
                    MessageType.Error);
                return;
            }

            Vector3 fixedOffsetValue = follow != null ? follow.FollowOffset : Vector3.zero;
            float distance = fixedOffset
                ? CameraRigFramingEditorUtility.GetDistance(fixedOffsetValue)
                : screen.CameraDistance;
            float pitch;
            float yaw;
            if (fixedOffset)
            {
                pitch = CameraRigFramingEditorUtility.GetPitch(fixedOffsetValue);
                yaw = CameraRigFramingEditorUtility.GetYaw(fixedOffsetValue);
            }
            else
            {
                Quaternion rigLocalRotation = Quaternion.Inverse(composer.transform.rotation) * camera.transform.rotation;
                Vector3 euler = rigLocalRotation.eulerAngles;
                pitch = Mathf.DeltaAngle(0f, euler.x);
                yaw = Mathf.DeltaAngle(0f, euler.y);
            }

            LensSettings lens = camera.Lens;
            float fieldOfView = lens.FieldOfView;
            float targetHeight = screen != null ? screen.TargetOffset.y : fixedOffsetValue.y;
            Vector2 deadZone = screen != null ? screen.Composition.DeadZone.Size : Vector2.zero;
            Vector2 damping = screen != null ? new Vector2(screen.Damping.x, screen.Damping.y) : Vector2.zero;
            Vector2 screenPosition = screen != null ? screen.Composition.ScreenPosition : Vector2.zero;

            EditorGUI.BeginChangeCheck();
            distance = EditorGUILayout.FloatField("Distance", distance);
            pitch = EditorGUILayout.Slider("Pitch", pitch, -89f, 89f);
            yaw = EditorGUILayout.FloatField("Yaw", yaw);
            fieldOfView = EditorGUILayout.Slider("Field of View", fieldOfView, 1f, 179f);
            if (fixedOffset)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField("Target Height (offset Y)", targetHeight);
            }
            else
            {
                targetHeight = EditorGUILayout.FloatField("Target Height", targetHeight);
                deadZone = EditorGUILayout.Vector2Field("Dead Zone (X / Y)", deadZone);
                damping = EditorGUILayout.Vector2Field("Damping (X / Y)", damping);
                screenPosition = EditorGUILayout.Vector2Field("Screen Position", screenPosition);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (CameraRigPreviewController.TryUpdatePreviewFollowControls(
                        composer,
                        distance,
                        pitch,
                        yaw,
                        fieldOfView,
                        targetHeight,
                        deadZone,
                        damping,
                        screenPosition,
                        out string issue))
                    _previewIssue = string.Empty;
                else
                    _previewIssue = issue;
            }
        }

        private string ResolveMaterializationStatus(
            CameraRigPresentationIntent presentation,
            CameraRigPresentationIntent materialized,
            CinemachineCamera local)
        {
            if (_lastApplyResult.HasValue &&
                !_materializationOutdated &&
                !_lastApplyResult.Value.Succeeded)
            {
                return "Blocked";
            }

            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                return "Needs Setup";
            }

            if (local == null)
            {
                return "Not Materialized";
            }

            if (_materializationOutdated)
            {
                return "Needs Apply";
            }

            if (materialized ==
                CameraRigPresentationIntent.Undefined)
            {
                return "Needs Apply";
            }

            if (materialized != presentation)
            {
                return "Needs Rebuild";
            }

            if (target is CameraRigComposer composer &&
                composer.BehaviorDefinition is FollowCameraRigBehaviorDefinition follow)
            {
                CinemachineComponentBase positionControl =
                    local.GetCinemachineComponent(CinemachineCore.Stage.Body);
                bool matchesMode = follow.FollowMode == FollowCameraRigMode.ScreenComposer
                    ? positionControl is CinemachinePositionComposer
                    : positionControl is CinemachineFollow;
                if (!matchesMode)
                    return "Needs Rebuild";
            }

            return "Current";
        }

        private void DrawMaterializationIssue(
            CameraRigPresentationIntent presentation,
            CameraRigPresentationIntent materialized,
            CinemachineCamera local)
        {
            if (_lastApplyResult.HasValue &&
                !_materializationOutdated &&
                !_lastApplyResult.Value.Succeeded)
            {
                string issue =
                    _lastApplyResult.Value.BlockingIssue;

                if (!string.IsNullOrWhiteSpace(issue))
                {
                    EditorGUILayout.HelpBox(
                        FormatBlockingIssueForInspector(issue),
                        MessageType.Error);
                }

                return;
            }

            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Camera Rig Behavior Definition before materializing this rig.",
                    MessageType.Info);
                return;
            }

            if (local == null)
            {
                return;
            }

            if (_materializationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Authored Camera Rig settings changed. Apply / Rebuild to reconcile the materialized rig.",
                    MessageType.Warning);
                return;
            }

            if (materialized ==
                CameraRigPresentationIntent.Undefined)
            {
                EditorGUILayout.HelpBox(
                    "A local Cinemachine Camera exists, but no Framework materialization is recorded. Apply / Rebuild to reconcile the rig.",
                    MessageType.Warning);
                return;
            }

            if (materialized != presentation)
            {
                EditorGUILayout.HelpBox(
                    $"Authored Presentation is '{presentation}' while materialized Presentation is '{materialized}'. Apply / Rebuild is required.",
                    MessageType.Warning);
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           Application.isPlaying))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Validate",
                                "Validates structural Camera Rig settings and Group provenance without requiring runtime Subjects."),
                            GUILayout.Width(96f)))
                    {
                        RunValidation();
                    }
                }

                GUILayout.Space(8f);

                EditorGUILayout.LabelField(
                    ResolveValidationStatus(),
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();
            }

            if (_lastValidationResult.HasValue &&
                !_validationOutdated &&
                !_lastValidationResult.Value.Succeeded &&
                !string.IsNullOrWhiteSpace(
                    _lastValidationResult.Value.BlockingIssue))
            {
                EditorGUILayout.HelpBox(
                    FormatBlockingIssueForInspector(
                        _lastValidationResult.Value.BlockingIssue),
                    MessageType.Error);
            }
        }

        private string ResolveValidationStatus()
        {
            if (!_lastValidationResult.HasValue)
            {
                return "Not Validated";
            }

            if (_validationOutdated)
            {
                return "Outdated";
            }

            return _lastValidationResult.Value.Succeeded
                ? "Valid"
                : "Issue";
        }

        private static string FormatBlockingIssueForInspector(
            string issue)
        {
            if (string.IsNullOrWhiteSpace(issue))
            {
                return "The operation is blocked. Open Advanced / Debug for the technical diagnostic.";
            }

            const string rotationConflictPrefix =
                "cinemachine-rotation-control:external-or-unknown-conflict:";
            const string positionConflictPrefix =
                "cinemachine-position-control:external-or-unknown-conflict:";
            const string rotationDuplicatePrefix =
                "cinemachine-rotation-control:external-or-unknown-duplicate:";
            const string positionDuplicatePrefix =
                "cinemachine-position-control:external-or-unknown-duplicate:";

            if (issue.StartsWith(
                    rotationConflictPrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlConflict(
                    issue.Substring(rotationConflictPrefix.Length),
                    "Rotation");
            }

            if (issue.StartsWith(
                    positionConflictPrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlConflict(
                    issue.Substring(positionConflictPrefix.Length),
                    "Position");
            }

            if (issue.StartsWith(
                    rotationDuplicatePrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlDuplicate(
                    issue.Substring(rotationDuplicatePrefix.Length),
                    "Rotation");
            }

            if (issue.StartsWith(
                    positionDuplicatePrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlDuplicate(
                    issue.Substring(positionDuplicatePrefix.Length),
                    "Position");
            }

            if (issue ==
                "cinemachine-rotation-control:ownership-evidence-ambiguous")
            {
                return "Rotation Control ownership is ambiguous. The Framework cannot safely decide which control it owns. Review the recorded ownership and current pipeline in Advanced / Debug before applying.";
            }

            if (issue ==
                "cinemachine-position-control:ownership-evidence-ambiguous")
            {
                return "Position Control ownership is ambiguous. The Framework cannot safely decide which control it owns. Review the recorded ownership and current pipeline in Advanced / Debug before applying.";
            }

            if (issue == "cinemachine-follow:create-disabled")
            {
                return "The selected Presentation requires a Follow Position Control, but creation of that control is disabled. Review the Camera Rig configuration in Advanced / Debug.";
            }


            if (issue.EndsWith(
                    ":settings-invalid",
                    System.StringComparison.Ordinal))
            {
                return "One or more settings for the selected Presentation are invalid. Review the Model Settings and try again.";
            }

            if (issue == "presentation:Undefined:not-supported")
            {
                return "Assign a Camera Rig Behavior Definition before validating or materializing this rig.";
            }

            if (issue == "cinemachine-camera:missing")
            {
                return "No local Cinemachine Camera could be resolved or materialized for this rig. Review the Camera reference in Advanced / Debug.";
            }

            return "The operation is blocked by the current Camera Rig configuration. Open Advanced / Debug for the technical diagnostic.";
        }

        private static string FormatExternalControlConflict(
            string detail,
            string stageLabel)
        {
            const string desiredMarker = ":desired=";
            int desiredIndex =
                detail.IndexOf(
                    desiredMarker,
                    System.StringComparison.Ordinal);

            string existingType = desiredIndex >= 0
                ? detail.Substring(0, desiredIndex)
                : detail;
            string desired = desiredIndex >= 0
                ? detail.Substring(
                    desiredIndex + desiredMarker.Length)
                : string.Empty;

            string existingLabel =
                ResolveTechnicalTypeLabel(existingType);
            string desiredLabel =
                ResolveDesiredControlLabel(
                    desired,
                    stageLabel);

            return $"{stageLabel} Control conflict. This Camera already contains external or unrecognized '{existingLabel}'. The selected Presentation requires {desiredLabel}. Remove the conflicting control or review it in Advanced / Debug.";
        }

        private static string FormatExternalControlDuplicate(
            string detail,
            string stageLabel)
        {
            string existingLabel =
                ResolveTechnicalTypeLabel(detail);

            return $"{stageLabel} Control conflict. This Camera contains multiple external or unrecognized controls of type '{existingLabel}'. Resolve the duplicate controls before Apply / Rebuild. See Advanced / Debug for technical evidence.";
        }

        private static string ResolveTechnicalTypeLabel(
            string technicalType)
        {
            if (string.IsNullOrWhiteSpace(technicalType))
            {
                return "control";
            }

            int separator =
                technicalType.LastIndexOf('.');

            return separator >= 0 &&
                   separator < technicalType.Length - 1
                ? technicalType.Substring(separator + 1)
                : technicalType;
        }

        private static string ResolveDesiredControlLabel(
            string desired,
            string stageLabel)
        {
            switch (desired)
            {
                case "HardLookAt":
                    return "a Look At rotation control";

                case "RotateWithFollowTarget":
                    return "rotation that follows the Camera Mount";

                case "Follow":
                    return "a Follow position control";

                case "HardLockToTarget":
                    return "a hard-lock position control";

                case "ThirdPersonFollow":
                    return "a Third Person Follow position control";

                case "None":
                    return $"no {stageLabel.ToLowerInvariant()} control";

                default:
                    return $"a different {stageLabel.ToLowerInvariant()} control";
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows technical target contracts, Cinemachine materialization evidence, ownership provenance and the last recorded authoring operation."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawMaterialization(ResolvePresentationIntent());
            if (!TryGetValidSerializedObject(target, out _))
                return;

            DrawValidation();
            if (!TryGetValidSerializedObject(target, out _))
                return;

            DrawTargetContractEvidence();
            if (!TryGetValidSerializedObject(target, out _))
                return;

            DrawMaterializationEvidence();
            if (!TryGetValidSerializedObject(target, out _))
                return;

            DrawLastOperationEvidence();
            if (!TryGetValidSerializedObject(target, out _))
                return;

            DrawDiagnostics();

            EditorGUI.indentLevel--;
        }

        private void DrawTargetContractEvidence()
        {
            EditorGUILayout.LabelField(
                "Target Contract",
                EditorStyles.miniBoldLabel);

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();
            CameraTargetRequirement effectiveFollow =
                ResolveEffectiveFollowRequirement(
                    presentation);
            CameraTargetRequirement effectiveLookAt =
                ResolveEffectiveLookAtRequirement(
                    presentation);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    ResolveFollowRoleLabel(presentation),
                    effectiveFollow.ToString());

                EditorGUILayout.TextField(
                    "Look At",
                    effectiveLookAt.ToString());



            }

            EditorGUILayout.Space(4f);
        }

        private void DrawMaterializationEvidence()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            EditorGUILayout.LabelField(
                "Materialization",
                EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                _cinemachineCamera,
                new GUIContent(
                    "Cinemachine Camera",
                    "Explicit local Cinemachine Camera reference used by this Composer. A missing reference may be materialized locally by Apply / Rebuild."));

            bool technicalAuthoringChanged =
                EditorGUI.EndChangeCheck();

            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            bool advancedModified = current.ApplyModifiedProperties();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            if (technicalAuthoringChanged)
            {
                _localCinemachineCameraResolved = false;
                MarkAuthoringChanged();
            }

            if (advancedModified)
            {
                _localCinemachineCameraResolved = false;
                current.UpdateIfRequiredOrScript();
                if (!IsCurrentTargetValid(inspectedTarget, current))
                    return;
            }

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _materializedPresentationIntent,
                    new GUIContent(
                        "Materialized Presentation"));

                EditorGUILayout.PropertyField(
                    _materializationRevision,
                    new GUIContent(
                        "Materialization Revision"));

                if (local != null)
                {
                    EditorGUILayout.Vector3Field(
                        "World Position",
                        local.transform.position);
                    EditorGUILayout.Vector3Field(
                        "World Rotation",
                        local.transform.eulerAngles);
                }

                EditorGUILayout.PropertyField(
                    _frameworkOwnedCinemachineCamera,
                    new GUIContent(
                        "Owned Cinemachine Camera"));

                EditorGUILayout.PropertyField(
                    _frameworkOwnedGroupTargetGroup,
                    new GUIContent("Owned Group Target Group"));

                EditorGUILayout.PropertyField(
                    _frameworkOwnedGroupFraming,
                    new GUIContent("Owned Group Framing"));
            }

            DrawPipelineEvidence(local);

            EditorGUILayout.Space(4f);
        }

        private void DrawPipelineEvidence(
            CinemachineCamera local)
        {
            if (local == null)
            {
                EditorGUILayout.LabelField(
                    "Pipeline",
                    "No local Cinemachine Camera");
                return;
            }

            CinemachineComponentBase body =
                local.GetCinemachineComponent(
                    CinemachineCore.Stage.Body);
            CinemachineComponentBase aim =
                local.GetCinemachineComponent(
                    CinemachineCore.Stage.Aim);

            DrawPipelineControl(
                "Position Control",
                body,
                _frameworkOwnedPositionControl.objectReferenceValue as Component);

            DrawPipelineControl(
                "Rotation Control",
                aim,
                _frameworkOwnedRotationControl.objectReferenceValue as Component);
        }

        private static void DrawPipelineControl(
            string label,
            Component current,
            Component recordedFrameworkOwned)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    label,
                    current,
                    typeof(Component),
                    true);
            }

            string ownership;

            if (current == null)
            {
                ownership = "None";
            }
            else if (current ==
                     recordedFrameworkOwned)
            {
                ownership = "Framework-owned";
            }
            else
            {
                ownership = "External / Unknown";
            }

            EditorGUILayout.LabelField(
                $"{label} Ownership",
                ownership,
                EditorStyles.miniLabel);
        }

        private void DrawLastOperationEvidence()
        {
            EditorGUILayout.LabelField(
                "Last Operation",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _lastApplyRebuildStatus,
                    new GUIContent(
                        "Status"));

                EditorGUILayout.PropertyField(
                    _lastBlockingIssue,
                    new GUIContent(
                        "Blocking Issue"));


                EditorGUILayout.PropertyField(
                    _lastMaterializationSummary,
                    new GUIContent(
                        "Materialization Summary"));
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawDiagnostics()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            EditorGUILayout.LabelField(
                "Diagnostics",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(
                _logApplyRebuildDiagnostics,
                new GUIContent(
                    "Log Apply / Rebuild Diagnostics"));

            if (IsCurrentTargetValid(inspectedTarget, current))
                current.ApplyModifiedProperties();
        }

        private void MarkAuthoringChanged()
        {
            _materializationOutdated = true;

            if (_lastValidationResult.HasValue)
            {
                _validationOutdated = true;
            }
        }

        private CameraRigPresentationIntent ResolvePresentationIntent()
        {
            var definition =
                _behaviorDefinition.objectReferenceValue as CameraRigBehaviorDefinition;
            return definition != null
                ? definition.PresentationIntent
                : CameraRigPresentationIntent.Undefined;
        }

        private CameraRigPresentationIntent ResolveMaterializedPresentationIntent()
        {
            return (CameraRigPresentationIntent)
                _materializedPresentationIntent.intValue;
        }

        private CameraTargetRequirement ResolveEffectiveLookAtRequirement(
            CameraRigPresentationIntent presentation)
        {
            var definition =
                _behaviorDefinition.objectReferenceValue as CameraRigBehaviorDefinition;
            return definition != null
                ? definition.LookAtRequirement
                : CameraTargetRequirement.NotUsed;
        }

        private CameraTargetRequirement ResolveEffectiveFollowRequirement(
            CameraRigPresentationIntent presentation)
        {
            var definition =
                _behaviorDefinition.objectReferenceValue as CameraRigBehaviorDefinition;
            return definition != null
                ? definition.FollowRequirement
                : CameraTargetRequirement.NotUsed;
        }

        private static string ResolveFollowRoleLabel(
            CameraRigPresentationIntent presentation)
        {
            switch (presentation)
            {
                case CameraRigPresentationIntent.Mounted:
                    return "Camera Mount";

                case CameraRigPresentationIntent.ThirdPerson:
                    return "Tracking Pivot";

                case CameraRigPresentationIntent.Group:
                    return "Group Target";

                default:
                    return "Tracking / Follow";
            }
        }

        private CinemachineCamera ResolveLocalCinemachineCamera()
        {
            if (!TryGetValidSerializedObject(target, out _) ||
                _cinemachineCamera == null)
                return null;

            if (_localCinemachineCameraResolved)
                return _localCinemachineCamera;

            CameraRigComposer composer =
                (CameraRigComposer)target;

            CinemachineCamera assigned =
                _cinemachineCamera.objectReferenceValue
                    as CinemachineCamera;

            _localCinemachineCamera = assigned != null
                ? assigned
                : composer.GetComponentInChildren<
                    CinemachineCamera>(
                    true);
            _localCinemachineCameraResolved = true;
            return _localCinemachineCamera;
        }







        private void RunValidation()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            current.ApplyModifiedProperties();
            if (!TryGetValidSerializedObject(inspectedTarget, out current))
                return;

            var composer = inspectedTarget as CameraRigComposer;
            if (composer == null)
                return;

            CameraRigComposerApplyRebuildResult result =
                CameraRigComposerApplyRebuildUtility
                    .Validate(
                        composer);
            if (!TryGetValidSerializedObject(inspectedTarget, out current))
                return;

            _lastValidationResult = result;
            _validationOutdated = false;

            current.UpdateIfRequiredOrScript();
        }

        private void RunApplyOrRebuild()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            current.ApplyModifiedProperties();
            if (!TryGetValidSerializedObject(inspectedTarget, out current))
                return;

            var composer = inspectedTarget as CameraRigComposer;
            if (composer == null)
                return;

            CameraRigComposerApplyRebuildResult result =
                CameraRigComposerApplyRebuildUtility
                    .ApplyOrRebuild(
                        composer,
                        true,
                        true);
            if (!TryGetValidSerializedObject(inspectedTarget, out current))
                return;

            _lastApplyResult = result;
            _materializationOutdated = false;

            current.UpdateIfRequiredOrScript();
        }

        private void RunSaveConfiguration(CameraRigComposer composer)
        {
            if (!CameraRigPreviewController.TrySaveConfiguration(composer, out _previewIssue))
                return;

            _materializationOutdated = false;
            _lastApplyResult = null;
            _lastValidationResult = CameraRigComposerApplyRebuildUtility.Validate(composer);
            _validationOutdated = false;
            Repaint();
        }

        private bool TryGetValidSerializedObject(
            UnityEngine.Object inspectedTarget,
            out SerializedObject current)
        {
            current = null;
            if (this == null || inspectedTarget == null || target != inspectedTarget)
                return false;

            current = serializedObject;
            return current != null && current.targetObject == inspectedTarget;
        }

        private bool IsCurrentTargetValid(
            UnityEngine.Object inspectedTarget,
            SerializedObject current)
        {
            return current != null && inspectedTarget != null &&
                   target == inspectedTarget &&
                   current.targetObject == inspectedTarget;
        }
    }
}
