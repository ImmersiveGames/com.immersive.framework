using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Editor.Camera.Cinemachine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "IF-ADR-010 CameraRigComposer Editor validation and Apply/Rebuild tooling; QAFramework is a known tooling consumer.")]
    public static class CameraRigComposerApplyRebuildUtility
    {
        public static CameraRigComposerApplyRebuildResult Validate(CameraRigComposer composer)
        {
            if (composer == null) return CameraRigComposerApplyRebuildResult.Failed("ValidationFailed", "Composer is missing.");
            string issue;
            bool valid = composer.TryValidateForApply(out issue);
            if (valid && composer.PresentationIntent == CameraRigPresentationIntent.Group)
            {
                valid = CameraGroupProvenance.Validate(composer, false, out issue);
            }
            else if (valid && CameraGroupProvenance.HasRecordedMaterialization(composer))
            {
                valid = CameraGroupProvenance.ValidateForRemoval(composer, out issue);
            }
            var result = valid
                ? CameraRigComposerApplyRebuildResult.ValidationSucceeded(
                    $"Behavior Definition '{composer.BehaviorDefinition.name}' projects Presentation model '{composer.PresentationIntent}'. Settings and Group provenance are valid; Apply / Rebuild preflights pipeline ownership.")
                : CameraRigComposerApplyRebuildResult.Failed("ValidationFailed", issue);
            return result;
        }

        public static CameraRigComposerApplyRebuildResult ValidateExistingComposition(CameraRigComposer composer)
        {
            CameraRigComposerApplyRebuildResult validation = Validate(composer);
            if (!validation.Succeeded)
                return validation;

            CinemachineRigMaterializationRequest request = CreateMaterializationRequest(composer, false);
            request.CreateCinemachineCameraIfMissing = false;
            request.CreateCinemachineFollowIfMissing = false;
            request.CreateCinemachinePositionComposerIfMissing = false;
            CinemachineRigMaterializationReport report =
                CinemachineRigMaterializer.ValidateExistingComposition(request, true);

            return report.Succeeded
                ? CameraRigComposerApplyRebuildResult.ValidationSucceeded(report.CreateSummary())
                : CameraRigComposerApplyRebuildResult.Failed(
                    "ValidationFailed",
                    report.FirstBlockingIssue,
                    report.CreateSummary());
        }

        public static CameraRigComposerApplyRebuildResult ApplyOrRebuild(CameraRigComposer composer, bool logDiagnostics = true, bool useUndo = true)
        {
            return ApplyOrRebuildCore(composer, logDiagnostics, useUndo, false);
        }

        internal static CameraRigComposerApplyRebuildResult ApplyOrRebuildFromPreview(
            CameraRigComposer composer,
            bool logDiagnostics,
            bool useUndo)
        {
            if (!CameraRigPreviewController.IsPreviewActive(composer))
                return CameraRigComposerApplyRebuildResult.Failed(
                    "ApplyFailed",
                    "Save Configuration requires an active Camera Rig Preview.");

            return ApplyOrRebuildCore(composer, logDiagnostics, useUndo, true);
        }

        private static CameraRigComposerApplyRebuildResult ApplyOrRebuildCore(
            CameraRigComposer composer,
            bool logDiagnostics,
            bool useUndo,
            bool allowActivePreview)
        {
            if (!allowActivePreview && CameraRigPreviewController.IsPreviewActive(composer))
                return CameraRigComposerApplyRebuildResult.Failed("ApplyFailed", "Disable Camera Rig Preview before applying or rebuilding the definitive rig.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return CameraRigComposerApplyRebuildResult.Failed("ApplyFailed", "Rig materialization requires Edit Mode.");
            var validation = Validate(composer);
            if (!validation.Succeeded) return validation;
            int group = -1;
            if (useUndo)
            {
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Apply Camera Rig Composer");
                Undo.RecordObject(composer, "Apply Camera Rig Composer");
            }
            CinemachineRigMaterializationRequest request = CreateMaterializationRequest(composer, useUndo);

            CinemachineRigMaterializationReport report = CinemachineRigMaterializer.ApplyOrRebuild(request);
            if (report.Succeeded)
            {
                composer.EditorSetGeneratedReference(report.Evidence.CinemachineCamera);
                composer.EditorCommitMaterializationEvidence(
                    report.Evidence.PresentationIntent, report.Evidence.CinemachineCamera,
                    report.Evidence.CinemachineCameraOwnership == CinemachineRigMaterializationOwnership.FrameworkOwned,
                    report.Evidence.PositionControl,
                    report.Evidence.PositionControlOwnership == CinemachineRigMaterializationOwnership.FrameworkOwned,
                    report.Evidence.RotationControl,
                    report.Evidence.RotationControlOwnership == CinemachineRigMaterializationOwnership.FrameworkOwned,
                    report.Evidence.MaterializationRevision);
                if (composer.PresentationIntent == CameraRigPresentationIntent.Group)
                {
                    CameraGroupMaterializer.Materialize(composer, useUndo, report);
                }
                else if (CameraGroupProvenance.HasRecordedMaterialization(composer))
                {
                    CameraGroupMaterializer.Dematerialize(composer, useUndo, report);
                }
            }
            var result = CameraRigComposerApplyRebuildResult.Applied(
                report.Succeeded, report.Succeeded ? "ApplySucceeded" : "ApplyCompletedWithBlockingIssues",
                report.Succeeded ? string.Empty : report.FirstBlockingIssue, report.CreateSummary(),
                report.CreatedCount, report.RepairedCount, report.AlreadyValidCount, report.SkippedCount, report.BlockedCount);
            if (report.Succeeded)
            {
                Record(composer, result, logDiagnostics);
                PrefabUtility.RecordPrefabInstancePropertyModifications(composer);
            }
            else
            {
                LogResult(composer, result, logDiagnostics);
            }
            if (group >= 0) Undo.CollapseUndoOperations(group);
            return result;
        }

        private static CinemachineRigMaterializationRequest CreateMaterializationRequest(
            CameraRigComposer composer,
            bool useUndo)
        {
            var request = new CinemachineRigMaterializationRequest
            {
                RigRoot = composer.transform,
                PresentationIntent = composer.PresentationIntent,
                MaterializeUnityOutput = false,
                UnityCamera = null,
                CinemachineCamera = composer.CinemachineCamera,
                LookAtRequirement = composer.EffectiveLookAtRequirement,
                CreateUnityCameraIfMissing = false,
                CreateCinemachineCameraIfMissing = composer.CreateCinemachineCameraIfMissing,
                CreateCinemachineFollowIfMissing = true,
                FrameworkOwnedCinemachineCamera = composer.FrameworkOwnedCinemachineCamera,
                FrameworkOwnedPositionControl = composer.FrameworkOwnedPositionControl,
                FrameworkOwnedRotationControl = composer.FrameworkOwnedRotationControl,
                PreviousMaterializationRevision = composer.MaterializationRevision,
                UseUndo = useUndo,
                CinemachineCameraObjectName = composer.CinemachineCameraObjectName
            };

            composer.EditorGetCapturedCameraSettings(
                out bool applyCapturedPosition,
                out Vector3 capturedPosition,
                out bool applyCapturedRotation,
                out Vector3 capturedRotation,
                out bool applyCapturedLens,
                out LensSettings.OverrideModes capturedLensMode,
                out float capturedFieldOfView,
                out float capturedOrthographicSize,
                out float capturedNearClipPlane,
                out float capturedFarClipPlane,
                out float capturedDutch);
            request.ApplyCapturedCameraPosition = applyCapturedPosition;
            request.CapturedCameraLocalPosition = capturedPosition;
            request.ApplyCapturedCameraRotation = applyCapturedRotation;
            request.CapturedCameraLocalEulerAngles = capturedRotation;
            request.ApplyCapturedCameraLens = applyCapturedLens;
            request.CapturedLensMode = capturedLensMode;
            request.CapturedFieldOfView = capturedFieldOfView;
            request.CapturedOrthographicSize = capturedOrthographicSize;
            request.CapturedNearClipPlane = capturedNearClipPlane;
            request.CapturedFarClipPlane = capturedFarClipPlane;
            request.CapturedDutch = capturedDutch;

            switch (composer.BehaviorDefinition)
            {
                case FollowCameraRigBehaviorDefinition follow:
                    request.FollowOffset = follow.FollowOffset;
                    request.ApplyCapturedFollowTrackerSettings = follow.HasCapturedTrackerSettings;
                    request.CapturedFollowTrackerSettings = follow.CapturedTrackerSettings;
                    request.FollowMode = follow.FollowMode;
                    request.ScreenComposerCameraDistance = follow.CameraDistance;
                    request.ScreenComposerDeadZoneDepth = follow.DeadZoneDepth;
                    request.ScreenComposerComposition = follow.Composition;
                    request.ScreenComposerCenterOnActivate = follow.CenterOnActivate;
                    request.ScreenComposerTargetOffset = follow.TargetOffset;
                    request.ScreenComposerDamping = follow.Damping;
                    request.ScreenComposerLookahead = follow.Lookahead;
                    break;

                case GroupCameraRigBehaviorDefinition groupBehavior:
                    request.FollowOffset = groupBehavior.FollowOffset;
                    request.ApplyCapturedFollowTrackerSettings = groupBehavior.HasCapturedTrackerSettings;
                    request.CapturedFollowTrackerSettings = groupBehavior.CapturedTrackerSettings;
                    break;

                case MountedCameraRigBehaviorDefinition mounted:
                    request.MountedPositionDamping = mounted.PositionDamping;
                    request.MountedRotationDamping = mounted.RotationDamping;
                    break;

                case ThirdPersonCameraRigBehaviorDefinition thirdPerson:
                    request.ThirdPersonShoulderOffset = thirdPerson.ShoulderOffset;
                    request.ThirdPersonVerticalArmLength = thirdPerson.VerticalArmLength;
                    request.ThirdPersonCameraSide = thirdPerson.CameraSide;
                    request.ThirdPersonCameraDistance = thirdPerson.CameraDistance;
                    request.ThirdPersonDamping = thirdPerson.Damping;
                    break;
            }

            return request;
        }

        private static void Record(CameraRigComposer composer, CameraRigComposerApplyRebuildResult result, bool log)
        {
            composer.EditorSetApplyRebuildResult(result.Status, result.BlockingIssue, result.MaterializationSummary);
            EditorUtility.SetDirty(composer);
            LogResult(composer, result, log);
        }

        private static void LogResult(
            CameraRigComposer composer,
            CameraRigComposerApplyRebuildResult result,
            bool log)
        {
            if (!log || !composer.LogApplyRebuildDiagnostics) return;
            var logger = FrameworkLogger.Create(typeof(CameraRigComposerApplyRebuildUtility));
            string message = $"Camera rig '{composer.name}': {result.Status}. {result.BlockingIssue} {result.MaterializationSummary}";
            if (result.Succeeded) logger.Info(message); else logger.Error(message);
        }
    }
}
