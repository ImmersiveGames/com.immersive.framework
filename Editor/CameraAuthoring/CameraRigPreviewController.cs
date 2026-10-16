using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal enum CameraRigPreviewPlayModeDecision
    {
        SaveAndPlay,
        CancelPlayMode,
        DiscardAndPlay
    }

    /// <summary>
    /// Owns one non-persistent, Edit Mode output session for the source CinemachineCamera.
    /// The temporary Brain drives a temporary Unity Camera through ManualUpdate; Save
    /// synchronizes authoring data and Disable restores uncaptured source edits.
    /// </summary>
    [InitializeOnLoad]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Temporary real-camera composition for CameraRigComposer authoring.")]
    public static class CameraRigPreviewController
    {
        private const string PreviewRootName = "[Camera Preview Output]";
        private const int OutputChannelCount = 16;

        [Serializable]
        private sealed class CaptureSnapshot
        {
            public bool CapturesPosition;
            public Vector3 CameraPosition;
            public bool CapturesRotation;
            public Vector3 CameraEulerAngles;
            public LensSettings.OverrideModes LensMode;
            public float FieldOfView;
            public float OrthographicSize;
            public float NearClipPlane;
            public float FarClipPlane;
            public float Dutch;

            public bool HasFollow;
            public Vector3 FollowOffset;
            public TrackerSettings FollowTrackerSettings;

            public bool HasScreenComposer;
            public float CameraDistance;
            public float DeadZoneDepth;
            public ScreenComposerSettings Composition;
            public bool CenterOnActivate;
            public Vector3 TargetOffset;
            public Vector3 Damping;
            public LookaheadSettings Lookahead;

            public bool HasMounted;
            public float MountedPositionDamping;
            public float MountedRotationDamping;

            public bool HasThirdPerson;
            public Vector3 ShoulderOffset;
            public float VerticalArmLength;
            public float CameraSide;
            public float ThirdPersonDistance;
            public Vector3 ThirdPersonDamping;

            public bool HasGroup;
            public float GroupMemberWeight;
            public float GroupMemberRadius;
            public float GroupFramingSize;
            public float GroupDamping;
            public Vector2 GroupFovRange;
            public Vector2 GroupDollyRange;
            public Vector2 GroupOrthoSizeRange;
            public CinemachineGroupFraming.FramingModes GroupFramingMode;
            public Vector2 GroupCenterOffset;
            public CinemachineGroupFraming.SizeAdjustmentModes GroupSizeAdjustment;
            public CinemachineGroupFraming.LateralAdjustmentModes GroupLateralAdjustment;
        }

        private sealed class Session
        {
            public CameraRigComposer Owner;
            public CameraRigBehaviorDefinition BehaviorDefinition;
            public GameObject Root;
            public CinemachineCamera SourceCamera;
            public UnityEngine.Camera OutputCamera;
            public CinemachineBrain Brain;
            public Transform Target;
            public bool TargetWasAssigned;
            public bool CleanupFailed;
            public uint ChannelMask;
            public OutputChannels OriginalCameraChannel;
            public CameraTarget OriginalCameraTarget;
            public CinemachineTargetGroup SourceGroup;
            public readonly List<GroupMemberState> OriginalGroupMembers = new List<GroupMemberState>();
            public readonly List<ComponentState> AuthoringState = new List<ComponentState>();
            public readonly Dictionary<CinemachineBrain, bool> OriginalBrainChannel =
                new Dictionary<CinemachineBrain, bool>();
            public UnityEngine.Object[] SelectionBeforePreview;
            public string CapturedSnapshot;
            public CameraRigPresentationIntent PresentationIntent;
            public FollowCameraRigMode FollowMode;
        }

        private sealed class ComponentState
        {
            public Component Component;
            public string Json;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public bool IsTransform;
        }

        private sealed class GroupMemberState
        {
            public Transform Target;
            public float Weight;
            public float Radius;
        }

        private static Session _session;
        private static bool _playModePromptPending;
        private static bool _cancelingPlayModeEntry;
        private static int _manualUpdateFrame;
        private static double _lastManualUpdateTime;

        static CameraRigPreviewController()
        {
            EditorApplication.delayCall += DestroyOrphanedPreviewObjects;
        }

        public static bool IsPreviewActive(CameraRigComposer composer) =>
            composer != null && _session != null && _session.Owner == composer && _session.Root != null;

        public static string GetPreviewState(CameraRigComposer composer) =>
            IsPreviewActive(composer)
                ? _session.CleanupFailed ? "Cleanup failed — output disabled" : "Active — original camera / temporary output"
                : "Inactive";

        public static Transform GetPreviewTarget(CameraRigComposer composer) =>
            IsPreviewActive(composer) ? _session.Target : null;

        public static CinemachineCamera GetPreviewCamera(CameraRigComposer composer) =>
            IsPreviewActive(composer) ? _session.SourceCamera : null;

        public static GameObject GetPreviewRoot(CameraRigComposer composer) =>
            IsPreviewActive(composer) ? _session.Root : null;

        public static bool CanEditPreviewFollowControls(CameraRigComposer composer, out string issue)
        {
            issue = string.Empty;
            if (!IsPreviewActive(composer) || !ValidateSession(_session, out issue))
            {
                if (string.IsNullOrWhiteSpace(issue))
                    issue = "Camera Rig Preview is not active.";
                return false;
            }

            if (!(_session.BehaviorDefinition is FollowCameraRigBehaviorDefinition))
            {
                issue = "Framing controls are available only for Follow camera rigs.";
                return false;
            }

            if (_session.Owner.FrameworkOwnedCinemachineCamera != _session.SourceCamera)
            {
                issue = "The CinemachineCamera is External / Unknown. Framing edits are read-only to preserve component ownership.";
                return false;
            }

            Component positionControl = _session.FollowMode == FollowCameraRigMode.ScreenComposer
                ? (Component)_session.SourceCamera.GetComponent<CinemachinePositionComposer>()
                : _session.SourceCamera.GetComponent<CinemachineFollow>();
            if (positionControl == null)
            {
                issue = $"The {_session.FollowMode} position control is missing.";
                return false;
            }

            if (_session.Owner.FrameworkOwnedPositionControl != positionControl)
            {
                issue = $"The {positionControl.GetType().Name} position control is External / Unknown. Framing edits are read-only to preserve component ownership.";
                return false;
            }

            return true;
        }

        public static bool TryUpdatePreviewFollowControls(
            CameraRigComposer composer,
            float distance,
            float pitch,
            float yaw,
            float fieldOfView,
            float targetHeight,
            Vector2 deadZone,
            Vector2 damping,
            Vector2 screenPosition,
            out string issue)
        {
            issue = string.Empty;
            if (!IsPreviewActive(composer) || !ValidateSession(_session, out issue))
            {
                if (string.IsNullOrWhiteSpace(issue))
                    issue = "Camera Rig Preview is not active.";
                return false;
            }

            if (Application.isPlaying ||
                !(_session.BehaviorDefinition is FollowCameraRigBehaviorDefinition behavior))
            {
                issue = "Framing controls require an active Edit Mode Follow preview.";
                return false;
            }

            if (!CanEditPreviewFollowControls(composer, out issue))
                return false;

            if (!IsFinite(distance) || distance < (behavior.FollowMode == FollowCameraRigMode.FixedOffset ? 0f : 0.01f) ||
                !IsFinite(pitch) || !IsFinite(yaw) || !IsFinite(fieldOfView) ||
                fieldOfView < 1f || fieldOfView > 179f)
            {
                issue = "Distance, pitch, yaw or Field of View is outside its supported range.";
                return false;
            }

            CinemachineCamera camera = _session.SourceCamera;
            CinemachineFollow follow = camera.GetComponent<CinemachineFollow>();
            CinemachinePositionComposer screen = camera.GetComponent<CinemachinePositionComposer>();
            if (behavior.FollowMode == FollowCameraRigMode.FixedOffset && follow == null)
            {
                issue = "FixedOffset requires the materialized Cinemachine Follow control.";
                return false;
            }
            if (behavior.FollowMode == FollowCameraRigMode.ScreenComposer && screen == null)
            {
                issue = "ScreenComposer requires the materialized Cinemachine Position Composer control.";
                return false;
            }

            ScreenComposerSettings composition = screen != null ? screen.Composition : default;
            Vector3 targetOffset = screen != null ? screen.TargetOffset : Vector3.zero;
            Vector3 currentDamping = screen != null ? screen.Damping : Vector3.zero;
            if (screen != null)
            {
                composition.DeadZone.Size = deadZone;
                composition.ScreenPosition = screenPosition;
                targetOffset.y = targetHeight;
                currentDamping.x = damping.x;
                currentDamping.y = damping.y;
                if (!FollowCameraRigBehaviorDefinition.TryValidateScreenComposerSettings(
                        distance,
                        screen.DeadZoneDepth,
                        composition,
                        targetOffset,
                        currentDamping,
                        screen.Lookahead,
                        out issue))
                    return false;
            }

            var undoObjects = new List<UnityEngine.Object> { camera };
            if (follow != null)
                undoObjects.Add(follow);
            if (screen != null)
                undoObjects.Add(screen);
            Undo.RecordObjects(undoObjects.ToArray(), "Adjust Camera Rig Preview");

            if (follow != null)
                follow.FollowOffset = CameraRigFramingEditorUtility.ToFollowOffset(distance, pitch, yaw);
            if (screen != null)
            {
                screen.CameraDistance = distance;
                screen.Composition = composition;
                screen.TargetOffset = targetOffset;
                screen.Damping = currentDamping;
                Quaternion rigLocalRotation = Quaternion.Inverse(composer.transform.rotation) * camera.transform.rotation;
                Vector3 rotation = rigLocalRotation.eulerAngles;
                camera.transform.rotation = composer.transform.rotation *
                                            Quaternion.Euler(pitch, yaw, rotation.z);
            }

            LensSettings lens = camera.Lens;
            lens.FieldOfView = fieldOfView;
            camera.Lens = lens;
            EditorUtility.SetDirty(camera);
            if (follow != null)
                EditorUtility.SetDirty(follow);
            if (screen != null)
                EditorUtility.SetDirty(screen);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            if (follow != null)
                PrefabUtility.RecordPrefabInstancePropertyModifications(follow);
            if (screen != null)
                PrefabUtility.RecordPrefabInstancePropertyModifications(screen);

            SceneView.RepaintAll();
            return true;
        }

        public static bool HasUncapturedChanges(CameraRigComposer composer)
        {
            if (!IsPreviewActive(composer))
                return false;

            return !string.Equals(
                _session.CapturedSnapshot,
                CreateCaptureSnapshot(_session),
                StringComparison.Ordinal);
        }

        public static bool TryEnablePreview(CameraRigComposer composer, out string issue)
        {
            issue = string.Empty;
            if (composer == null)
            {
                issue = "CameraRigComposer is missing.";
                return false;
            }

            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                issue = "Enable Preview is available only in Edit Mode.";
                return false;
            }

            if (IsPreviewActive(composer))
                return true;

            if (_session != null)
            {
                issue = "Another Camera Rig Preview is already active. Disable it before enabling another rig.";
                return false;
            }

            if (composer.gameObject.scene.IsValid() == false ||
                EditorUtility.IsPersistent(composer))
            {
                issue = "Enable Preview requires a CameraRigComposer in a loaded scene or Prefab Stage.";
                return false;
            }

            if (CinemachineCore.SoloCamera != null)
            {
                issue = "A Cinemachine Solo Camera is already active. Clear Solo Camera before composing a preview.";
                return false;
            }

            CameraRigComposerApplyRebuildResult validation =
                CameraRigComposerApplyRebuildUtility.ValidateExistingComposition(composer);
            if (!validation.Succeeded)
            {
                issue = validation.BlockingIssue;
                return false;
            }

            if (!TryResolvePreviewSourceCamera(composer, out CinemachineCamera sourceCamera, out issue))
                return false;

            if (!TryFindAvailableOutputChannel(out uint channelMask))
            {
                issue = "All 16 Cinemachine output channels are in use by active cameras in loaded scenes. Free one channel to isolate the preview.";
                return false;
            }

            var session = new Session
            {
                Owner = composer,
                BehaviorDefinition = composer.BehaviorDefinition,
                SourceCamera = sourceCamera,
                OriginalCameraTarget = sourceCamera.Target,
                OriginalCameraChannel = sourceCamera.OutputChannel,
                SourceGroup = composer.FrameworkOwnedGroupTargetGroup,
                ChannelMask = channelMask,
                SelectionBeforePreview = Selection.objects,
                PresentationIntent = composer.PresentationIntent,
                FollowMode = (composer.BehaviorDefinition as FollowCameraRigBehaviorDefinition)?.FollowMode ??
                             FollowCameraRigMode.FixedOffset
            };
            _session = session;

            try
            {
                CaptureOriginalGroupMembers(session);
                session.AuthoringState.AddRange(CreateAuthoringStateSnapshot(session));
                RegisterEditorCallbacks();
                CreatePreviewObjects(session);
                FilterOtherBrains(session);
                SetAllPreviewObjectsDontSave(session.Root);
                session.CapturedSnapshot = CreateCaptureSnapshot(session);
                SceneView.RepaintAll();
                return true;
            }
            catch (Exception exception)
            {
                issue = $"Could not enable Camera Rig Preview: {exception.Message}";
                if (!TryRemoveSession(session, out string cleanupIssue))
                    issue += $" Cleanup also failed: {cleanupIssue}";
                return false;
            }
        }

        public static bool SetPreviewTarget(CameraRigComposer composer, Transform target, out string issue)
        {
            issue = string.Empty;
            if (!IsPreviewActive(composer))
            {
                issue = "Camera Rig Preview is not active.";
                return false;
            }

            if (!ValidateSession(_session, out issue))
                return false;

            if (target != null &&
                (EditorUtility.IsPersistent(target) || !target.gameObject.scene.IsValid() ||
                 !target.gameObject.scene.isLoaded || target.gameObject.scene != _session.Owner.gameObject.scene))
            {
                issue = "Preview target must be a Transform in the same loaded scene or Prefab Stage as the CameraRigComposer.";
                return false;
            }

            Session session = _session;
            if (session.PresentationIntent == CameraRigPresentationIntent.Fixed && target != null)
            {
                issue = "Fixed Camera Rigs do not use a tracking target.";
                return false;
            }

            try
            {
                if (session.PresentationIntent == CameraRigPresentationIntent.Group)
                {
                    CinemachineTargetGroup group = session.SourceGroup;
                    if (group == null)
                    {
                        issue = "The preview Group Target Group could not be resolved.";
                        return false;
                    }

                    if (session.Target != null && session.Target != target &&
                        !IsOriginalGroupMember(session, session.Target))
                        group.RemoveMember(session.Target);
                    if (target != null && !ContainsTarget(group, target))
                    {
                        GroupCameraRigBehaviorDefinition behavior =
                            session.Owner.BehaviorDefinition as GroupCameraRigBehaviorDefinition;
                        group.AddMember(target, behavior.MemberWeight, behavior.MemberRadius);
                    }

                    session.SourceCamera.Follow = group.transform;
                    session.SourceCamera.LookAt = group.transform;
                }
                else
                {
                    session.SourceCamera.Target = default;
                    if (target != null)
                    {
                        session.SourceCamera.Follow = target;
                        if (session.Owner.EffectiveLookAtRequirement != CameraTargetRequirement.NotUsed)
                            session.SourceCamera.LookAt = target;
                    }
                    else
                        session.SourceCamera.Target = session.OriginalCameraTarget;
                }

                session.Target = target;
                session.TargetWasAssigned = target != null;

                SceneView.RepaintAll();
                return true;
            }
            catch (Exception exception)
            {
                issue = $"Could not bind the preview target: {exception.Message}";
                return false;
            }
        }

        public static bool TryCapture(CameraRigComposer composer, out string issue) =>
            TryCaptureCore(composer, out issue, true);

        private static bool TryCaptureCore(
            CameraRigComposer composer,
            out string issue,
            bool confirmSharedDefinition)
        {
            issue = string.Empty;
            if (Application.isPlaying)
            {
                issue = "Camera Rig Preview settings can only be captured in Edit Mode.";
                return false;
            }

            if (!IsPreviewActive(composer))
            {
                issue = "Camera Rig Preview is not active.";
                return false;
            }

            Session session = _session;
            if (!ValidateSession(session, out issue) ||
                !TryReadCaptureSnapshot(session, out CaptureSnapshot snapshot, out issue))
                return false;

            CameraRigBehaviorDefinition definition = composer.BehaviorDefinition;
            if (definition == null)
            {
                issue = "The source Camera Rig Behavior Definition was removed.";
                return false;
            }

            if (session.BehaviorDefinition != definition ||
                session.PresentationIntent != composer.PresentationIntent ||
                (definition is FollowCameraRigBehaviorDefinition currentFollow &&
                 currentFollow.FollowMode != session.FollowMode))
            {
                issue = "The source Behavior Definition changed after Enable Preview. Disable and enable the preview again before capture.";
                return false;
            }

            if (!TryValidateCaptureSnapshot(session, snapshot, out issue))
                return false;

            if (EditorUtility.IsPersistent(definition) && confirmSharedDefinition &&
                !EditorUtility.DisplayDialog(
                    "Save Shared Camera Behavior",
                    $"'{definition.name}' is a project asset and may be shared. Saving writes to this asset and affects every rig that references it. Continue?",
                    "Save Shared Asset",
                    "Cancel"))
            {
                issue = "Save Configuration was canceled because the Behavior Definition asset may be shared.";
                return false;
            }

            List<ComponentState> capturedAuthoringState;
            try
            {
                capturedAuthoringState = CreateAuthoringStateSnapshot(session);
            }
            catch (Exception exception)
            {
                issue = $"Could not snapshot the original rig for safe Disable: {exception.Message}";
                return false;
            }

            UnityEngine.Object[] undoObjects = { composer, definition };
            Undo.RecordObjects(undoObjects, "Capture Camera Rig Preview");

            switch (definition)
            {
                case FollowCameraRigBehaviorDefinition follow:
                    if (session.FollowMode == FollowCameraRigMode.FixedOffset)
                    {
                        CinemachineFollow previewFollow = session.SourceCamera.GetComponent<CinemachineFollow>();
                        follow.EditorCaptureFollowOffset(previewFollow.FollowOffset);
                        follow.EditorCaptureTrackerSettings(previewFollow.TrackerSettings);
                    }
                    else
                    {
                        CinemachinePositionComposer position =
                            session.SourceCamera.GetComponent<CinemachinePositionComposer>();
                        follow.EditorCaptureScreenComposerSettings(
                            position.CameraDistance,
                            position.DeadZoneDepth,
                            position.Composition,
                            position.CenterOnActivate,
                            position.TargetOffset,
                            position.Damping,
                            position.Lookahead);
                    }
                    break;

                case GroupCameraRigBehaviorDefinition groupBehavior:
                    CaptureGroupSettings(session, groupBehavior, snapshot);
                    break;

                case MountedCameraRigBehaviorDefinition mounted:
                    mounted.EditorCaptureDamping(
                        session.SourceCamera.GetComponent<CinemachineHardLockToTarget>().Damping,
                        session.SourceCamera.GetComponent<CinemachineRotateWithFollowTarget>().Damping);
                    break;

                case ThirdPersonCameraRigBehaviorDefinition thirdPerson:
                    CinemachineThirdPersonFollow previewThirdPerson =
                        session.SourceCamera.GetComponent<CinemachineThirdPersonFollow>();
                    thirdPerson.EditorCaptureSettings(
                        previewThirdPerson.ShoulderOffset,
                        previewThirdPerson.VerticalArmLength,
                        previewThirdPerson.CameraSide,
                        previewThirdPerson.CameraDistance,
                        previewThirdPerson.Damping);
                    break;
            }

            composer.EditorCaptureCameraSettings(
                snapshot.CapturesPosition,
                snapshot.CameraPosition,
                snapshot.CapturesRotation,
                snapshot.CameraEulerAngles,
                snapshot.LensMode,
                snapshot.FieldOfView,
                snapshot.OrthographicSize,
                snapshot.NearClipPlane,
                snapshot.FarClipPlane,
                snapshot.Dutch);

            EditorUtility.SetDirty(definition);
            EditorUtility.SetDirty(composer);
            foreach (UnityEngine.Object changed in undoObjects)
                if (changed != null)
                {
                    EditorUtility.SetDirty(changed);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
                }
            MarkCapturedAuthoringStateDirty(session, capturedAuthoringState);

            session.AuthoringState.Clear();
            session.AuthoringState.AddRange(capturedAuthoringState);
            session.CapturedSnapshot = CreateCaptureSnapshot(session);
            SceneView.RepaintAll();
            return true;
        }

        public static bool TrySaveConfiguration(CameraRigComposer composer, out string issue)
        {
            issue = string.Empty;
            if (!IsPreviewActive(composer))
            {
                issue = "Save Configuration requires an active Camera Rig Preview.";
                return false;
            }

            Session session = _session;
            if (!ValidateSession(session, out issue))
                return false;

            CameraRigComposerApplyRebuildResult preflight =
                CameraRigComposerApplyRebuildUtility.ValidateExistingComposition(composer);
            if (!preflight.Succeeded)
            {
                issue = preflight.BlockingIssue;
                return false;
            }

            if (!TryReadCaptureSnapshot(session, out CaptureSnapshot snapshot, out issue) ||
                !TryValidateCaptureSnapshot(session, snapshot, out issue))
                return false;

            CameraRigBehaviorDefinition definition = composer.BehaviorDefinition;
            if (definition == null || session.BehaviorDefinition != definition ||
                session.PresentationIntent != composer.PresentationIntent ||
                (definition is FollowCameraRigBehaviorDefinition currentFollow &&
                 currentFollow.FollowMode != session.FollowMode))
            {
                issue = "The Behavior Definition changed after Enable Preview. Disable and re-enable Preview before saving.";
                return false;
            }

            if (definition is FollowCameraRigBehaviorDefinition &&
                !CanEditPreviewFollowControls(composer, out issue))
                return false;

            if (EditorUtility.IsPersistent(definition) &&
                !EditorUtility.DisplayDialog(
                    "Save Shared Camera Behavior",
                    $"'{definition.name}' is a project asset and may be shared. Save Configuration will update that asset and every rig that references it. Continue?",
                    "Save Configuration",
                    "Cancel"))
            {
                issue = "Save Configuration was canceled because the Behavior Definition asset may be shared.";
                return false;
            }

            var previousBaseline = new List<ComponentState>(session.AuthoringState);
            string previousCapturedSnapshot = session.CapturedSnapshot;
            int undoGroup;
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Save Camera Rig Configuration");

            if (!TryCaptureCore(composer, out issue, false))
            {
                Undo.RevertAllDownToGroup(undoGroup);
                RestorePreviewBaseline(session, previousBaseline, previousCapturedSnapshot);
                return false;
            }

            CameraRigComposerApplyRebuildResult rebuild =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuildFromPreview(composer, true, true);
            if (!rebuild.Succeeded)
            {
                issue = string.IsNullOrWhiteSpace(rebuild.BlockingIssue)
                    ? "Apply / Rebuild did not confirm the saved camera configuration."
                    : rebuild.BlockingIssue;
                Undo.RevertAllDownToGroup(undoGroup);
                RestorePreviewBaseline(session, previousBaseline, previousCapturedSnapshot);
                return false;
            }

            bool prefabStage = PrefabStageUtility.GetPrefabStage(composer.gameObject) is PrefabStage stage &&
                               stage.scene == composer.gameObject.scene;
            // Retire bindings temporários antes de aplicar overrides (inclusive quando
            // o próprio CinemachineCamera foi criado como objeto de prefab override).
            RestoreTransientSourceBindings(session);
            RestoreBrainChannels(session);
            bool persisted;
            try
            {
                persisted = CameraRigPrefabConfigurationPersistence.TryPersist(
                    composer,
                    session.SourceCamera,
                    snapshot.CapturesPosition,
                    snapshot.CapturesRotation,
                    out issue);
            }
            catch (Exception exception)
            {
                persisted = false;
                issue = $"Could not persist the authored camera configuration: {exception.Message}";
            }

            FilterOtherBrains(session);
            string targetIssue = string.Empty;
            if (session.Target != null)
                SetPreviewTarget(composer, session.Target, out targetIssue);
            if (!string.IsNullOrWhiteSpace(targetIssue))
            {
                persisted = false;
                issue = string.IsNullOrWhiteSpace(issue) ? targetIssue : $"{issue} {targetIssue}";
            }

            if (!persisted)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                RestorePreviewBaseline(session, previousBaseline, previousCapturedSnapshot);
                return false;
            }

            if (EditorUtility.IsPersistent(definition))
            {
                try
                {
                    AssetDatabase.SaveAssetIfDirty(definition);
                }
                catch (Exception exception)
                {
                    issue = $"Could not save Behavior Definition asset '{definition.name}': {exception.Message}";
                    Undo.RevertAllDownToGroup(undoGroup);
                    RestorePreviewBaseline(session, previousBaseline, previousCapturedSnapshot);
                    return false;
                }

                if (EditorUtility.IsDirty(definition))
                {
                    issue = $"Unity did not confirm saving Behavior Definition asset '{definition.name}'.";
                    Undo.RevertAllDownToGroup(undoGroup);
                    RestorePreviewBaseline(session, previousBaseline, previousCapturedSnapshot);
                    return false;
                }
            }
            else
            {
                if (!prefabStage && !PrefabUtility.IsPartOfPrefabInstance(composer.gameObject))
                    EditorSceneManager.MarkSceneDirty(composer.gameObject.scene);
            }

            try
            {
                session.AuthoringState.Clear();
                session.AuthoringState.AddRange(CreateAuthoringStateSnapshot(session));
                session.CapturedSnapshot = CreateCaptureSnapshot(session);
            }
            catch (Exception exception)
            {
                issue = $"Configuration was persisted, but Preview could not refresh its restore baseline: {exception.Message}";
                Undo.CollapseUndoOperations(undoGroup);
                return false;
            }

            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
            return true;
        }

        private static void RestorePreviewBaseline(
            Session session,
            IReadOnlyList<ComponentState> baseline,
            string capturedSnapshot)
        {
            session.AuthoringState.Clear();
            session.AuthoringState.AddRange(baseline);
            session.CapturedSnapshot = capturedSnapshot;
            SceneView.RepaintAll();
        }

        public static bool TryDisablePreview(CameraRigComposer composer, out string issue)
        {
            if (_session == null)
            {
                issue = string.Empty;
                return true;
            }

            if (composer != null && _session.Owner != composer)
            {
                issue = "The active Camera Rig Preview belongs to another Composer.";
                return false;
            }

            return TryRemoveSession(_session, out issue, true, true);
        }

        private static void CreatePreviewObjects(Session session)
        {
            Scene ownerScene = session.Owner.gameObject.scene;
            var root = new GameObject(PreviewRootName);
            session.Root = root;
            if (root.scene != ownerScene)
                SceneManager.MoveGameObjectToScene(root, ownerScene);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(session.Owner.transform, true);
            root.transform.SetPositionAndRotation(
                session.SourceCamera.transform.position,
                session.SourceCamera.transform.rotation);

            session.OutputCamera = root.AddComponent<UnityEngine.Camera>();
            session.OutputCamera.enabled = true;
            session.Brain = root.AddComponent<CinemachineBrain>();
            session.Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            session.Brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            session.Brain.ChannelMask = (OutputChannels)session.ChannelMask;
            session.Brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut, 0f);

            session.SourceCamera.OutputChannel = (OutputChannels)session.ChannelMask;
            SetAllPreviewObjectsDontSave(root);
            _manualUpdateFrame = Time.frameCount;
            _lastManualUpdateTime = EditorApplication.timeSinceStartup;
        }

        private static void CaptureOriginalGroupMembers(Session session)
        {
            session.OriginalGroupMembers.Clear();
            if (session.SourceGroup == null || session.SourceGroup.Targets == null)
                return;

            foreach (CinemachineTargetGroup.Target target in session.SourceGroup.Targets)
            {
                if (target == null || target.Object == null)
                    continue;
                session.OriginalGroupMembers.Add(new GroupMemberState
                {
                    Target = target.Object,
                    Weight = target.Weight,
                    Radius = target.Radius
                });
            }
        }

        private static bool IsOriginalGroupMember(Session session, Transform target)
        {
            foreach (GroupMemberState member in session.OriginalGroupMembers)
                if (member.Target == target)
                    return true;
            return false;
        }

        private static List<ComponentState> CreateAuthoringStateSnapshot(Session session)
        {
            var state = new List<ComponentState>();
            var seen = new HashSet<Component>();
            CameraTarget currentTarget = session.SourceCamera.Target;
            OutputChannels currentChannel = session.SourceCamera.OutputChannel;
            List<GroupMemberState> currentGroupMembers = CaptureGroupMembers(session.SourceGroup);

            try
            {
                session.SourceCamera.Target = session.OriginalCameraTarget;
                session.SourceCamera.OutputChannel = session.OriginalCameraChannel;
                if (session.SourceGroup != null)
                    RestoreGroupMembers(session.SourceGroup, GetOriginalGroupMembersWithCurrentSettings(session));

                AddGameObjectComponentStates(session.SourceCamera.gameObject, state, seen);
                if (session.SourceGroup != null)
                    AddGameObjectComponentStates(session.SourceGroup.gameObject, state, seen);
                if (session.Owner.FrameworkOwnedGroupFraming != null)
                    AddGameObjectComponentStates(
                        session.Owner.FrameworkOwnedGroupFraming.gameObject,
                        state,
                        seen);
                return state;
            }
            finally
            {
                session.SourceCamera.Target = currentTarget;
                session.SourceCamera.OutputChannel = currentChannel;
                if (session.SourceGroup != null)
                    RestoreGroupMembers(session.SourceGroup, currentGroupMembers);
            }
        }

        private static void MarkCapturedAuthoringStateDirty(
            Session session,
            IReadOnlyList<ComponentState> state)
        {
            CameraTarget currentTarget = session.SourceCamera.Target;
            OutputChannels currentChannel = session.SourceCamera.OutputChannel;
            List<GroupMemberState> currentGroupMembers = CaptureGroupMembers(session.SourceGroup);
            try
            {
                session.SourceCamera.Target = session.OriginalCameraTarget;
                session.SourceCamera.OutputChannel = session.OriginalCameraChannel;
                if (session.SourceGroup != null)
                    RestoreGroupMembers(
                        session.SourceGroup,
                        GetOriginalGroupMembersWithCurrentSettings(session));

                foreach (ComponentState componentState in state)
                    if (componentState.Component != null)
                    {
                        EditorUtility.SetDirty(componentState.Component);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(componentState.Component);
                    }
            }
            finally
            {
                session.SourceCamera.Target = currentTarget;
                session.SourceCamera.OutputChannel = currentChannel;
                if (session.SourceGroup != null)
                    RestoreGroupMembers(session.SourceGroup, currentGroupMembers);
            }
        }

        private static void AddGameObjectComponentStates(
            GameObject gameObject,
            List<ComponentState> state,
            HashSet<Component> seen)
        {
            if (gameObject == null)
                return;
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component == null || !seen.Add(component))
                    continue;
                var componentState = new ComponentState { Component = component };
                if (component is Transform transform)
                {
                    componentState.IsTransform = true;
                    componentState.LocalPosition = transform.localPosition;
                    componentState.LocalRotation = transform.localRotation;
                    componentState.LocalScale = transform.localScale;
                }
                else
                {
                    componentState.Json = EditorJsonUtility.ToJson(component);
                }
                state.Add(componentState);
            }
        }

        private static List<GroupMemberState> CaptureGroupMembers(CinemachineTargetGroup group)
        {
            var members = new List<GroupMemberState>();
            if (group == null || group.Targets == null)
                return members;
            foreach (CinemachineTargetGroup.Target target in group.Targets)
            {
                if (target == null || target.Object == null)
                    continue;
                members.Add(new GroupMemberState
                {
                    Target = target.Object,
                    Weight = target.Weight,
                    Radius = target.Radius
                });
            }
            return members;
        }

        private static List<GroupMemberState> GetOriginalGroupMembersWithCurrentSettings(Session session)
        {
            var members = new List<GroupMemberState>();
            List<GroupMemberState> currentMembers = CaptureGroupMembers(session.SourceGroup);
            foreach (GroupMemberState original in session.OriginalGroupMembers)
            {
                GroupMemberState current = null;
                foreach (GroupMemberState candidate in currentMembers)
                    if (candidate.Target == original.Target)
                    {
                        current = candidate;
                        break;
                    }
                members.Add(new GroupMemberState
                {
                    Target = original.Target,
                    Weight = current != null ? current.Weight : original.Weight,
                    Radius = current != null ? current.Radius : original.Radius
                });
            }
            return members;
        }

        private static void RestoreGroupMembers(
            CinemachineTargetGroup group,
            IReadOnlyList<GroupMemberState> members)
        {
            if (group == null || group.Targets == null)
                return;
            group.Targets.Clear();
            foreach (GroupMemberState member in members)
                if (member != null && member.Target != null)
                    group.AddMember(member.Target, member.Weight, member.Radius);
        }

        private static bool RestoreAuthoringState(
            Session session,
            bool useUndo,
            out string issue)
        {
            issue = string.Empty;
            foreach (ComponentState state in session.AuthoringState)
            {
                if (state.Component == null)
                {
                    issue = "A source rig component recorded at the last Enable/Capture was removed. Restore it with Undo or choose to keep the current rig and disable Preview.";
                    return false;
                }
            }

            var objects = new UnityEngine.Object[session.AuthoringState.Count];
            for (int index = 0; index < session.AuthoringState.Count; index++)
                objects[index] = session.AuthoringState[index].Component;
            if (useUndo && objects.Length > 0)
            {
                RecordAuthoringStateForUndo(session, objects);
            }

            foreach (ComponentState state in session.AuthoringState)
            {
                if (state.IsTransform)
                {
                    Transform transform = (Transform)state.Component;
                    transform.localPosition = state.LocalPosition;
                    transform.localRotation = state.LocalRotation;
                    transform.localScale = state.LocalScale;
                }
                else
                {
                    EditorJsonUtility.FromJsonOverwrite(state.Json, state.Component);
                }
                EditorUtility.SetDirty(state.Component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(state.Component);
            }
            return true;
        }

        private static void RecordAuthoringStateForUndo(
            Session session,
            UnityEngine.Object[] objects)
        {
            CameraTarget currentTarget = session.SourceCamera.Target;
            OutputChannels currentChannel = session.SourceCamera.OutputChannel;
            List<GroupMemberState> currentGroupMembers = CaptureGroupMembers(session.SourceGroup);
            try
            {
                session.SourceCamera.Target = session.OriginalCameraTarget;
                session.SourceCamera.OutputChannel = session.OriginalCameraChannel;
                if (session.SourceGroup != null)
                    RestoreGroupMembers(
                        session.SourceGroup,
                        GetOriginalGroupMembersWithCurrentSettings(session));

                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Disable Camera Rig Preview");
                Undo.RecordObjects(objects, "Disable Camera Rig Preview");
            }
            finally
            {
                session.SourceCamera.Target = currentTarget;
                session.SourceCamera.OutputChannel = currentChannel;
                if (session.SourceGroup != null)
                    RestoreGroupMembers(session.SourceGroup, currentGroupMembers);
            }
        }

        private static bool AuthoringStatesEqual(
            IReadOnlyList<ComponentState> left,
            IReadOnlyList<ComponentState> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int index = 0; index < left.Count; index++)
            {
                ComponentState a = left[index];
                ComponentState b = right[index];
                if (a.Component != b.Component || a.IsTransform != b.IsTransform)
                    return false;
                if (a.IsTransform)
                {
                    if (a.LocalPosition != b.LocalPosition ||
                        a.LocalRotation != b.LocalRotation ||
                        a.LocalScale != b.LocalScale)
                        return false;
                }
                else if (!string.Equals(a.Json, b.Json, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static void CaptureGroupSettings(
            Session session,
            GroupCameraRigBehaviorDefinition behavior,
            CaptureSnapshot snapshot)
        {
            CinemachineFollow follow = session.SourceCamera.GetComponent<CinemachineFollow>();
            behavior.EditorCaptureSettings(
                follow.FollowOffset,
                snapshot.GroupMemberWeight,
                snapshot.GroupMemberRadius,
                snapshot.GroupFramingSize,
                snapshot.GroupDamping,
                snapshot.GroupFovRange,
                snapshot.GroupDollyRange,
                snapshot.GroupOrthoSizeRange,
                snapshot.GroupFramingMode,
                snapshot.GroupCenterOffset,
                snapshot.GroupSizeAdjustment,
                snapshot.GroupLateralAdjustment,
                snapshot.FollowTrackerSettings);
        }

        private static bool TryValidateCaptureSnapshot(Session session, CaptureSnapshot snapshot, out string issue)
        {
            issue = string.Empty;
            if (!IsFinite(snapshot.CameraEulerAngles) ||
                (snapshot.CapturesPosition && !IsFinite(snapshot.CameraPosition)) ||
                !IsFinite(snapshot.FieldOfView) || snapshot.FieldOfView < 1f || snapshot.FieldOfView > 179f ||
                !IsFinite(snapshot.OrthographicSize) || snapshot.OrthographicSize <= 0f ||
                !IsFinite(snapshot.NearClipPlane) || snapshot.NearClipPlane <= 0f ||
                !IsFinite(snapshot.FarClipPlane) || snapshot.FarClipPlane <= snapshot.NearClipPlane ||
                !IsFinite(snapshot.Dutch))
            {
                issue = "The preview camera contains invalid position, rotation or lens values.";
                return false;
            }

            if (snapshot.LensMode != LensSettings.OverrideModes.Perspective &&
                snapshot.LensMode != LensSettings.OverrideModes.Orthographic &&
                snapshot.LensMode != LensSettings.OverrideModes.None)
            {
                issue = "Capture supports Perspective and Orthographic camera modes. Physical camera settings are not represented by CameraRigComposer yet.";
                return false;
            }

            switch (session.Owner.BehaviorDefinition)
            {
                case FollowCameraRigBehaviorDefinition follow when session.FollowMode == FollowCameraRigMode.FixedOffset:
                    if (!snapshot.HasFollow || !IsFinite(snapshot.FollowOffset) ||
                        !IsValidTrackerSettings(snapshot.FollowTrackerSettings))
                    {
                        issue = "Cinemachine Follow is missing or its offset/tracker settings are invalid.";
                        return false;
                    }
                    break;

                case FollowCameraRigBehaviorDefinition follow when session.FollowMode == FollowCameraRigMode.ScreenComposer:
                    if (!snapshot.HasScreenComposer ||
                        !FollowCameraRigBehaviorDefinition.TryValidateScreenComposerSettings(
                            snapshot.CameraDistance,
                            snapshot.DeadZoneDepth,
                            snapshot.Composition,
                            snapshot.TargetOffset,
                            snapshot.Damping,
                            snapshot.Lookahead,
                            out issue))
                    {
                        if (string.IsNullOrWhiteSpace(issue))
                            issue = "Cinemachine Position Composer is missing.";
                        return false;
                    }
                    break;

                case GroupCameraRigBehaviorDefinition:
                    if (!snapshot.HasGroup ||
                        !IsFinite(snapshot.FollowOffset) ||
                        !IsFinite(snapshot.GroupMemberWeight) || snapshot.GroupMemberWeight <= 0f ||
                        !IsFinite(snapshot.GroupMemberRadius) || snapshot.GroupMemberRadius <= 0f ||
                        !IsFinite(snapshot.GroupFramingSize) || snapshot.GroupFramingSize < 0.01f || snapshot.GroupFramingSize > 2f ||
                        !IsFinite(snapshot.GroupDamping) || snapshot.GroupDamping < 0f || snapshot.GroupDamping > 20f ||
                        !IsOrderedRange(snapshot.GroupFovRange, 1f, 179f) ||
                        !IsOrderedRange(snapshot.GroupDollyRange, float.MinValue, float.MaxValue) ||
                        !IsOrderedRange(snapshot.GroupOrthoSizeRange, 0.01f, float.MaxValue) ||
                        !IsFinite(snapshot.GroupCenterOffset) ||
                        !IsValidTrackerSettings(snapshot.FollowTrackerSettings) ||
                        !Enum.IsDefined(typeof(CinemachineGroupFraming.FramingModes), snapshot.GroupFramingMode) ||
                        !Enum.IsDefined(typeof(CinemachineGroupFraming.SizeAdjustmentModes), snapshot.GroupSizeAdjustment) ||
                        !Enum.IsDefined(typeof(CinemachineGroupFraming.LateralAdjustmentModes), snapshot.GroupLateralAdjustment))
                    {
                        issue = "The Group framing or member values cannot be represented by the current Group Behavior Definition.";
                        return false;
                    }
                    break;

                case MountedCameraRigBehaviorDefinition:
                    if (!snapshot.HasMounted || !IsFiniteNonNegative(snapshot.MountedPositionDamping) ||
                        !IsFiniteNonNegative(snapshot.MountedRotationDamping))
                    {
                        issue = "Mounted Camera damping values are invalid or missing.";
                        return false;
                    }
                    break;

                case ThirdPersonCameraRigBehaviorDefinition:
                    if (!snapshot.HasThirdPerson || !IsFinite(snapshot.ShoulderOffset) ||
                        !IsFinite(snapshot.VerticalArmLength) || !IsFinite(snapshot.CameraSide) ||
                        snapshot.CameraSide < 0f || snapshot.CameraSide > 1f ||
                        !IsFiniteNonNegative(snapshot.ThirdPersonDistance) ||
                        !IsFiniteNonNegative(snapshot.ThirdPersonDamping))
                    {
                        issue = "Third Person Follow values are invalid or missing.";
                        return false;
                    }
                    break;
            }

            return true;
        }

        private static bool ValidateSession(Session session, out string issue)
        {
            if (session != null && session.CleanupFailed)
            {
                issue = "Preview cleanup failed and its output was disabled. Disable the temporary output before changing or capturing the composition.";
                return false;
            }

            if (session == null || session.Owner == null || session.Root == null ||
                session.SourceCamera == null || session.Brain == null)
            {
                issue = "The preview output, source camera or CameraRigComposer was destroyed. Disable the preview before continuing.";
                return false;
            }

            if (ResolveSourceCamera(session.Owner) != session.SourceCamera)
            {
                issue = "The source CinemachineCamera changed after Enable Preview. Disable and enable the preview again.";
                return false;
            }

            if (session.TargetWasAssigned && session.Target == null)
            {
                issue = "The preview target was destroyed. The preview must be removed before continuing.";
                return false;
            }

            if (session.Owner.BehaviorDefinition != session.BehaviorDefinition ||
                session.Owner.PresentationIntent != session.PresentationIntent ||
                (session.BehaviorDefinition is FollowCameraRigBehaviorDefinition follow &&
                 follow.FollowMode != session.FollowMode))
            {
                issue = "The source Behavior Definition changed after Enable Preview. The temporary output was removed.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryReadCaptureSnapshot(Session session, out CaptureSnapshot snapshot, out string issue)
        {
            snapshot = null;
            issue = string.Empty;
            if (session.SourceCamera == null || session.OutputCamera == null)
            {
                issue = "The source CinemachineCamera or temporary output is missing.";
                return false;
            }

            var result = new CaptureSnapshot();
            CinemachineCamera camera = session.SourceCamera;
            CinemachinePositionComposer screen = camera.GetComponent<CinemachinePositionComposer>();
            CinemachineFollow follow = camera.GetComponent<CinemachineFollow>();
            CinemachineHardLockToTarget hardLock = camera.GetComponent<CinemachineHardLockToTarget>();
            CinemachineRotateWithFollowTarget rotate = camera.GetComponent<CinemachineRotateWithFollowTarget>();
            CinemachineThirdPersonFollow thirdPerson = camera.GetComponent<CinemachineThirdPersonFollow>();
            CinemachineGroupFraming groupFraming = camera.GetComponent<CinemachineGroupFraming>();

            result.CapturesPosition = camera.GetCinemachineComponent(CinemachineCore.Stage.Body) == null;
            result.CameraPosition = session.Owner.transform.InverseTransformPoint(camera.transform.position);
            result.CapturesRotation = camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) == null &&
                                      !(groupFraming != null && groupFraming.LateralAdjustment == CinemachineGroupFraming.LateralAdjustmentModes.ChangeRotation);
            Quaternion rigLocalRotation = Quaternion.Inverse(session.Owner.transform.rotation) * camera.transform.rotation;
            result.CameraEulerAngles = rigLocalRotation.eulerAngles;
            result.LensMode = camera.Lens.ModeOverride;
            if (result.LensMode == LensSettings.OverrideModes.None)
                result.LensMode = session.OutputCamera.orthographic
                    ? LensSettings.OverrideModes.Orthographic
                    : LensSettings.OverrideModes.Perspective;
            result.FieldOfView = camera.Lens.FieldOfView;
            result.OrthographicSize = camera.Lens.OrthographicSize;
            result.NearClipPlane = camera.Lens.NearClipPlane;
            result.FarClipPlane = camera.Lens.FarClipPlane;
            result.Dutch = camera.Lens.Dutch;

            if (follow != null)
            {
                result.HasFollow = true;
                result.FollowOffset = follow.FollowOffset;
                result.FollowTrackerSettings = follow.TrackerSettings;
            }

            if (screen != null)
            {
                result.HasScreenComposer = true;
                result.CameraDistance = screen.CameraDistance;
                result.DeadZoneDepth = screen.DeadZoneDepth;
                result.Composition = screen.Composition;
                result.CenterOnActivate = screen.CenterOnActivate;
                result.TargetOffset = screen.TargetOffset;
                result.Damping = screen.Damping;
                result.Lookahead = screen.Lookahead;
            }

            if (hardLock != null && rotate != null)
            {
                result.HasMounted = true;
                result.MountedPositionDamping = hardLock.Damping;
                result.MountedRotationDamping = rotate.Damping;
            }

            if (thirdPerson != null)
            {
                result.HasThirdPerson = true;
                result.ShoulderOffset = thirdPerson.ShoulderOffset;
                result.VerticalArmLength = thirdPerson.VerticalArmLength;
                result.CameraSide = thirdPerson.CameraSide;
                result.ThirdPersonDistance = thirdPerson.CameraDistance;
                result.ThirdPersonDamping = thirdPerson.Damping;
            }

            if (groupFraming != null && session.SourceGroup != null)
            {
                if (follow == null)
                {
                    issue = "Cinemachine Follow is missing from the source Group rig.";
                    return false;
                }

                result.HasGroup = TryGetUniformGroupMemberSettings(
                    session.SourceGroup,
                    session.Owner.BehaviorDefinition as GroupCameraRigBehaviorDefinition,
                    out result.GroupMemberWeight,
                    out result.GroupMemberRadius,
                    out issue);
                result.FollowOffset = follow.FollowOffset;
                result.FollowTrackerSettings = follow.TrackerSettings;
                result.GroupFramingSize = groupFraming.FramingSize;
                result.GroupDamping = groupFraming.Damping;
                result.GroupFovRange = groupFraming.FovRange;
                result.GroupDollyRange = groupFraming.DollyRange;
                result.GroupOrthoSizeRange = groupFraming.OrthoSizeRange;
                result.GroupFramingMode = groupFraming.FramingMode;
                result.GroupCenterOffset = groupFraming.CenterOffset;
                result.GroupSizeAdjustment = groupFraming.SizeAdjustment;
                result.GroupLateralAdjustment = groupFraming.LateralAdjustment;
                if (!result.HasGroup)
                    return false;
            }

            snapshot = result;
            return true;
        }

        private static bool IsValidTrackerSettings(TrackerSettings settings) =>
            Enum.IsDefined(typeof(BindingMode), settings.BindingMode) &&
            Enum.IsDefined(typeof(AngularDampingMode), settings.AngularDampingMode) &&
            IsFiniteNonNegative(settings.PositionDamping) &&
            IsFiniteNonNegative(settings.RotationDamping) &&
            IsFiniteNonNegative(settings.QuaternionDamping);

        private static bool TryGetUniformGroupMemberSettings(
            CinemachineTargetGroup group,
            GroupCameraRigBehaviorDefinition behavior,
            out float weight,
            out float radius,
            out string issue)
        {
            weight = behavior != null ? behavior.MemberWeight : 1f;
            radius = behavior != null ? behavior.MemberRadius : 0.5f;
            issue = string.Empty;
            if (group.Targets == null || group.Targets.Count == 0)
                return true;

            CinemachineTargetGroup.Target first = group.Targets[0];
            if (first == null)
            {
                issue = "A Group Target entry is missing.";
                return false;
            }
            weight = first.Weight;
            radius = first.Radius;
            for (int index = 1; index < group.Targets.Count; index++)
            {
                CinemachineTargetGroup.Target member = group.Targets[index];
                if (member == null || !Mathf.Approximately(member.Weight, weight) ||
                    !Mathf.Approximately(member.Radius, radius))
                {
                    issue = "Group Behavior Definition supports one shared Member Weight and Member Radius. Make the preview group's members uniform before capture.";
                    return false;
                }
            }
            return true;
        }

        private static string CreateCaptureSnapshot(Session session)
        {
            if (!TryReadCaptureSnapshot(session, out CaptureSnapshot snapshot, out _))
                return "<invalid>";
            return JsonUtility.ToJson(snapshot);
        }

        private static bool TryFindAvailableOutputChannel(out uint channelMask)
        {
            var used = new HashSet<uint>();
            foreach (CinemachineCamera camera in FindLoadedCameras())
            {
                if (camera == null || !camera.isActiveAndEnabled)
                    continue;
                used.Add((uint)camera.OutputChannel);
            }

            for (int index = 0; index < OutputChannelCount; index++)
            {
                uint candidate = 1u << index;
                bool inUse = false;
                foreach (uint value in used)
                {
                    if ((value & candidate) != 0)
                    {
                        inUse = true;
                        break;
                    }
                }

                if (!inUse)
                {
                    channelMask = candidate;
                    return true;
                }
            }

            channelMask = 0;
            return false;
        }

        private static IEnumerable<CinemachineCamera> FindLoadedCameras()
        {
            foreach (CinemachineCamera camera in Resources.FindObjectsOfTypeAll<CinemachineCamera>())
            {
                if (camera == null || !camera.gameObject.scene.IsValid() ||
                    !camera.gameObject.scene.isLoaded || EditorUtility.IsPersistent(camera))
                    continue;
                yield return camera;
            }
        }

        private static IEnumerable<CinemachineBrain> FindLoadedBrains()
        {
            foreach (CinemachineBrain brain in Resources.FindObjectsOfTypeAll<CinemachineBrain>())
            {
                if (brain == null || !brain.gameObject.scene.IsValid() ||
                    !brain.gameObject.scene.isLoaded || EditorUtility.IsPersistent(brain) ||
                    !brain.isActiveAndEnabled)
                    continue;
                yield return brain;
            }
        }

        private static void FilterOtherBrains(Session session)
        {
            foreach (CinemachineBrain brain in FindLoadedBrains())
            {
                if (brain == session.Brain)
                    continue;
                ReserveChannelForPreview(session, brain);
            }
        }

        private static void ReserveChannelForPreview(Session session, CinemachineBrain brain)
        {
            if (brain == null || brain == session.Brain)
                return;

            if (!session.OriginalBrainChannel.ContainsKey(brain))
                session.OriginalBrainChannel.Add(brain, (((uint)brain.ChannelMask & session.ChannelMask) != 0));

            brain.ChannelMask = (OutputChannels)((uint)brain.ChannelMask & ~session.ChannelMask);
        }

        private static void RestoreBrainChannels(Session session)
        {
            foreach (KeyValuePair<CinemachineBrain, bool> entry in session.OriginalBrainChannel)
            {
                if (entry.Key == null)
                    continue;

                uint channel = (uint)entry.Key.ChannelMask;
                entry.Key.ChannelMask = (OutputChannels)(entry.Value
                    ? channel | session.ChannelMask
                    : channel & ~session.ChannelMask);
            }
            session.OriginalBrainChannel.Clear();
        }

        private static void OnEditorUpdate()
        {
            Session session = _session;
            if (session == null)
                return;

            if (session.CleanupFailed)
            {
                TryRemoveSession(session, out _);
                return;
            }

            try
            {
                if (!ValidateSession(session, out string issue))
                {
                    TryRemoveSession(session, out _, false, false);
                    if (!string.IsNullOrWhiteSpace(issue))
                        Debug.LogWarning($"Camera Rig Preview was disabled: {issue}");
                    return;
                }

                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                UnityEngine.Object soloCamera = CinemachineCore.SoloCamera as UnityEngine.Object;
                if (soloCamera != null)
                {
                    TryRemoveSession(session, out _, false, false);
                    Debug.LogWarning("Camera Rig Preview was disabled because a Cinemachine Solo Camera became active.");
                    return;
                }

                foreach (CinemachineCamera camera in FindLoadedCameras())
                {
                    if (camera == session.SourceCamera || !camera.isActiveAndEnabled)
                        continue;
                    if (((uint)camera.OutputChannel & session.ChannelMask) != 0)
                    {
                        TryRemoveSession(session, out _, false, false);
                        Debug.LogWarning("Camera Rig Preview was disabled because another active Cinemachine Camera started using its reserved output channel.");
                        return;
                    }
                }

                FilterOtherBrains(session);
                if (session.PresentationIntent == CameraRigPresentationIntent.Group && session.SourceGroup != null)
                    session.SourceGroup.DoUpdate();

                double now = EditorApplication.timeSinceStartup;
                float deltaTime = Mathf.Max(0f, (float)(now - _lastManualUpdateTime));
                _lastManualUpdateTime = now;
                _manualUpdateFrame++;
                session.Brain.ManualUpdate(_manualUpdateFrame, deltaTime);
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                TryRemoveSession(session, out string cleanupIssue, false, false);
                Debug.LogWarning(string.IsNullOrWhiteSpace(cleanupIssue)
                    ? $"Camera Rig Preview was disabled after an Editor update error: {exception.Message}"
                    : $"Camera Rig Preview update failed ({exception.Message}) and cleanup did not complete: {cleanupIssue}");
            }
        }

        private static bool TryRemoveSession(
            Session session,
            out string issue,
            bool useUndo = false,
            bool promptOnUnsafeRestore = false)
        {
            issue = string.Empty;
            if (session == null)
                return true;

            bool restored;
            string restoreIssue = string.Empty;
            try
            {
                List<ComponentState> currentState = CreateAuthoringStateSnapshot(session);
                restored = AuthoringStatesEqual(currentState, session.AuthoringState) ||
                           RestoreAuthoringState(session, useUndo, out restoreIssue);
                if (restored)
                    restoreIssue = string.Empty;
            }
            catch (Exception exception)
            {
                restored = false;
                restoreIssue = exception.Message;
            }
            if (!restored && promptOnUnsafeRestore &&
                !EditorUtility.DisplayDialog(
                    "Camera Rig Preview Cannot Restore the Original Rig",
                    $"{restoreIssue}\n\nKeep the current rig state and disable the temporary output?",
                    "Keep Rig and Disable",
                    "Cancel"))
            {
                issue = "Disable was canceled. The preview remains active so the rig can be restored with Undo or reviewed.";
                return false;
            }

            bool selectionWasTemporary = IsTemporarySelection(session);
            Exception cleanupException = null;
            try
            {
                RestoreTransientSourceBindings(session);
                RestoreBrainChannels(session);
                if (session.Root != null)
                    UnityEngine.Object.DestroyImmediate(session.Root);
                if (session.Root != null)
                    cleanupException = new InvalidOperationException("Temporary preview output could not be destroyed.");
            }
            catch (Exception exception)
            {
                cleanupException = exception;
            }
            finally
            {
                if (session.Root != null)
                {
                    // Failed destruction must not leave a live output competing with the scene.
                    session.CleanupFailed = true;
                    try
                    {
                        if (session.Brain != null)
                            session.Brain.enabled = false;
                        if (session.OutputCamera != null)
                            session.OutputCamera.enabled = false;
                    }
                    catch (Exception exception)
                    {
                        cleanupException = cleanupException ?? exception;
                    }
                }
                else
                {
                    session.CleanupFailed = false;
                    if (selectionWasTemporary)
                        RestoreSelection(session);
                    UnregisterEditorCallbacks();
                    if (_session == session)
                        _session = null;
                    SceneView.RepaintAll();
                }
            }

            if (cleanupException != null)
            {
                issue = $"Preview cleanup failed: {cleanupException.Message}";
                return false;
            }

            if (!restored)
                issue = $"Preview output was disabled; source authoring state was kept because safe restoration failed: {restoreIssue}";

            return session.Root == null;
        }

        private static void RestoreTransientSourceBindings(Session session)
        {
            if (session.SourceCamera != null)
            {
                bool cameraBindingChanged = !object.Equals(session.SourceCamera.Target, session.OriginalCameraTarget) ||
                                            session.SourceCamera.OutputChannel != session.OriginalCameraChannel;
                session.SourceCamera.Target = session.OriginalCameraTarget;
                session.SourceCamera.OutputChannel = session.OriginalCameraChannel;
                if (cameraBindingChanged)
                {
                    EditorUtility.SetDirty(session.SourceCamera);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(session.SourceCamera);
                }
            }

            if (session.SourceGroup != null && session.TargetWasAssigned &&
                session.Target != null && !IsOriginalGroupMember(session, session.Target))
            {
                session.SourceGroup.RemoveMember(session.Target);
                EditorUtility.SetDirty(session.SourceGroup);
                PrefabUtility.RecordPrefabInstancePropertyModifications(session.SourceGroup);
            }
        }

        private static bool IsTemporarySelection(Session session)
        {
            if (session.Root == null)
                return false;
            Transform selectedTransform = Selection.activeTransform;
            return Selection.activeObject == session.Root ||
                   (selectedTransform != null &&
                    (selectedTransform == session.Root.transform || selectedTransform.IsChildOf(session.Root.transform)));
        }

        private static void RestoreSelection(Session session)
        {
            var selection = new List<UnityEngine.Object>();
            if (session.SelectionBeforePreview != null)
                foreach (UnityEngine.Object item in session.SelectionBeforePreview)
                    if (item != null)
                        selection.Add(item);

            if (selection.Count == 0 && session.Owner != null)
                selection.Add(session.Owner.gameObject);

            Selection.objects = selection.ToArray();
        }

        private static void RegisterEditorCallbacks()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        }

        private static void UnregisterEditorCallbacks()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.quitting -= OnEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (_cancelingPlayModeEntry || state != PlayModeStateChange.ExitingEditMode ||
                _session == null || _playModePromptPending)
                return;

            _cancelingPlayModeEntry = true;
            EditorApplication.isPlaying = false;
            _cancelingPlayModeEntry = false;

            if (!HasUncapturedChanges(_session.Owner))
                EditorApplication.delayCall += CleanupAndResumePlayModeEntry;
            else
            {
                _playModePromptPending = true;
                EditorApplication.delayCall += ShowPlayModePrompt;
            }
        }

        private static void CleanupAndResumePlayModeEntry()
        {
            CameraRigComposer owner = _session != null ? _session.Owner : null;
            string issue = string.Empty;
            if (owner == null || !TryDisablePreview(owner, out issue))
            {
                EditorUtility.DisplayDialog(
                    "Camera Rig Preview could not be removed",
                    string.IsNullOrWhiteSpace(issue)
                        ? "Preview cleanup did not complete. Play Mode remains canceled."
                        : issue,
                    "OK");
                return;
            }

            EditorApplication.delayCall += ResumePlayModeEntry;
        }

        private static void ShowPlayModePrompt()
        {
            if (!_playModePromptPending)
                return;

            if (_session == null)
            {
                _playModePromptPending = false;
                return;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                "Camera Rig Preview is active",
                "The original rig is using a temporary output and has unsaved authoring changes.",
                "Save & Play",
                "Cancel Play Mode",
                "Discard & Play");

            CameraRigComposer owner = _session != null ? _session.Owner : null;
            CameraRigPreviewPlayModeDecision decision = choice switch
            {
                0 => CameraRigPreviewPlayModeDecision.SaveAndPlay,
                2 => CameraRigPreviewPlayModeDecision.DiscardAndPlay,
                _ => CameraRigPreviewPlayModeDecision.CancelPlayMode
            };
            bool proceed = TryResolvePlayModeDecision(owner, decision, out string issue);

            _playModePromptPending = false;
            if (!proceed && decision != CameraRigPreviewPlayModeDecision.CancelPlayMode)
            {
                EditorUtility.DisplayDialog(
                    "Camera Rig Preview could not be removed",
                    string.IsNullOrWhiteSpace(issue)
                        ? "Capture or cleanup did not complete. Play Mode remains canceled; remove the preview manually and retry."
                        : issue,
                    "OK");
                return;
            }

            if (proceed)
                EditorApplication.delayCall += ResumePlayModeEntry;
        }

        internal static bool TryResolvePlayModeDecision(
            CameraRigComposer owner,
            CameraRigPreviewPlayModeDecision decision,
            out string issue)
        {
            issue = string.Empty;
            if (decision == CameraRigPreviewPlayModeDecision.CancelPlayMode)
                return false;

            if (owner == null)
            {
                issue = "The Camera Rig owning the active Preview no longer exists.";
                return false;
            }

            if (decision == CameraRigPreviewPlayModeDecision.SaveAndPlay &&
                !TrySaveConfiguration(owner, out issue))
                return false;

            return TryDisablePreview(owner, out issue);
        }

        private static void ResumePlayModeEntry()
        {
            if (_session == null && !EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
        }

        private static void OnEditorQuitting() => TryRemoveSession(_session, out _);

        private static void OnBeforeAssemblyReload() => TryRemoveSession(_session, out _);

        private static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            if (_session != null && previous != next)
                TryRemoveSession(_session, out _);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (_session != null)
                TryRemoveSession(_session, out _);
        }

        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (_session != null)
                TryRemoveSession(_session, out _);
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            Session session = _session;
            if (session == null || session.Root == null || session.Root.scene != scene)
                return;

            if (!TryRemoveSession(session, out string issue, false, false))
                Debug.LogError($"Camera Rig Preview could not be safely disabled before scene save: {issue}");
            else if (!string.IsNullOrWhiteSpace(issue))
                Debug.LogWarning(issue);
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            if (_session != null && stage != null && _session.Owner != null &&
                _session.Owner.gameObject.scene == stage.scene)
                TryRemoveSession(_session, out _);
        }

        private static void DestroyOrphanedPreviewObjects()
        {
            foreach (GameObject root in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (root == null || EditorUtility.IsPersistent(root) ||
                    !root.scene.IsValid() || !root.scene.isLoaded ||
                    !root.name.StartsWith(PreviewRootName, StringComparison.Ordinal) ||
                    (root.hideFlags & HideFlags.DontSave) != HideFlags.DontSave ||
                    (_session != null && root == _session.Root))
                    continue;

                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetAllPreviewObjectsDontSave(GameObject root)
        {
            if (root == null)
                return;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags |= HideFlags.DontSave;
                foreach (Component component in child.GetComponents<Component>())
                    if (component != null)
                        component.hideFlags |= HideFlags.DontSave;
            }
        }

        private static bool ContainsTarget(CinemachineTargetGroup group, Transform target)
        {
            if (group.Targets == null)
                return false;
            foreach (CinemachineTargetGroup.Target item in group.Targets)
                if (item != null && item.Object == target)
                    return true;
            return false;
        }

        private static CinemachineCamera ResolveSourceCamera(CameraRigComposer composer)
        {
            if (composer == null)
                return null;
            if (composer.CinemachineCamera != null)
                return composer.CinemachineCamera;
            CinemachineCamera[] cameras = composer.GetComponentsInChildren<CinemachineCamera>(true);
            return cameras.Length == 1 ? cameras[0] : null;
        }

        private static bool TryResolvePreviewSourceCamera(
            CameraRigComposer composer,
            out CinemachineCamera sourceCamera,
            out string issue)
        {
            sourceCamera = ResolveSourceCamera(composer);
            issue = string.Empty;
            CinemachineCamera[] localCameras = composer.GetComponentsInChildren<CinemachineCamera>(true);
            if (localCameras.Length > 1)
            {
                issue = "Enable Preview requires one local Cinemachine Camera at most. Resolve multiple local cameras before enabling.";
                return false;
            }

            if (sourceCamera != null &&
                (EditorUtility.IsPersistent(sourceCamera) ||
                 (sourceCamera.transform != composer.transform &&
                  !sourceCamera.transform.IsChildOf(composer.transform)) ||
                 localCameras.Length != 1 || localCameras[0] != sourceCamera))
            {
                issue = "The assigned Cinemachine Camera must be the single local camera within this Composer rig.";
                return false;
            }

            if (sourceCamera == null && localCameras.Length == 1)
                sourceCamera = localCameras[0];

            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static bool IsFiniteNonNegative(float value) => IsFinite(value) && value >= 0f;

        private static bool IsFiniteNonNegative(Vector3 value) =>
            IsFinite(value) && value.x >= 0f && value.y >= 0f && value.z >= 0f;

        private static bool IsOrderedRange(Vector2 value, float minimum, float maximum) =>
            IsFinite(value) && value.x >= minimum && value.y <= maximum && value.x <= value.y;
    }
}
