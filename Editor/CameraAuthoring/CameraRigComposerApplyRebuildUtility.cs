using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Editor.Camera.Cinemachine;
using UnityEditor;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    public static class CameraRigComposerApplyRebuildUtility
    {
        public static CameraRigComposerApplyRebuildResult Validate(CameraRigComposer composer, bool logDiagnostics = true)
        {
            if (composer == null) return CameraRigComposerApplyRebuildResult.Failed("ValidationFailed", "Composer is missing.");
            string issue;
            bool valid = composer.TryValidateForApply(out issue);
            if (valid && composer.PresentationIntent == CameraRigPresentationIntent.Follow)
                valid = CameraSharedFollowProvenance.Validate(composer, false, out issue);
            var result = valid
                ? CameraRigComposerApplyRebuildResult.ValidationSucceeded("Presentation settings and shared Follow provenance are valid; Apply / Rebuild preflights pipeline ownership.")
                : CameraRigComposerApplyRebuildResult.Failed("ValidationFailed", issue);
            Record(composer, result, logDiagnostics);
            return result;
        }

        public static CameraRigComposerApplyRebuildResult ApplyOrRebuild(CameraRigComposer composer, bool logDiagnostics = true, bool useUndo = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return CameraRigComposerApplyRebuildResult.Failed("ApplyFailed", "Rig materialization requires Edit Mode.");
            var validation = Validate(composer, false);
            if (!validation.Succeeded) return validation;
            int group = -1;
            if (useUndo)
            {
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Apply Camera Rig Composer");
                Undo.RecordObject(composer, "Apply Camera Rig Composer");
            }
            var request =
                new CinemachineRigMaterializationRequest
                {
                    RigRoot = composer.transform,
                    PresentationIntent =
                        composer.PresentationIntent,
                    MaterializeUnityOutput = false,
                    UnityCamera = null,
                    CinemachineCamera =
                        composer.CinemachineCamera,
                    LookAtRequirement = composer.EffectiveLookAtRequirement,
                    CreateUnityCameraIfMissing = false,
                    CreateCinemachineCameraIfMissing =
                        composer.CreateCinemachineCameraIfMissing,
                    CreateCinemachineFollowIfMissing = true,
                    FollowOffset = composer.FollowOffset,
                    MountedPositionDamping =
                        composer.MountedPositionDamping,
                    MountedRotationDamping =
                        composer.MountedRotationDamping,
                    ThirdPersonShoulderOffset =
                        composer.ThirdPersonShoulderOffset,
                    ThirdPersonVerticalArmLength =
                        composer.ThirdPersonVerticalArmLength,
                    ThirdPersonCameraSide =
                        composer.ThirdPersonCameraSide,
                    ThirdPersonCameraDistance =
                        composer.ThirdPersonCameraDistance,
                    ThirdPersonDamping =
                        composer.ThirdPersonDamping,
                    FrameworkOwnedCinemachineCamera =
                        composer.FrameworkOwnedCinemachineCamera,
                    FrameworkOwnedPositionControl =
                        composer.FrameworkOwnedPositionControl,
                    FrameworkOwnedRotationControl =
                        composer.FrameworkOwnedRotationControl,
                    PreviousMaterializationRevision =
                        composer.MaterializationRevision,
                    UseUndo = useUndo,
                    CinemachineCameraObjectName =
                        composer.CinemachineCameraObjectName
                };


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
                if (composer.PresentationIntent == CameraRigPresentationIntent.Follow)
                {
                    CameraSharedFollowMaterializer.Materialize(composer, useUndo, report);
                }
            }
            var result = CameraRigComposerApplyRebuildResult.Applied(
                report.Succeeded, report.Succeeded ? "ApplySucceeded" : "ApplyCompletedWithBlockingIssues",
                report.Succeeded ? string.Empty : report.FirstBlockingIssue, report.CreateSummary(),
                report.CreatedCount, report.RepairedCount, report.AlreadyValidCount, report.SkippedCount, report.BlockedCount);
            Record(composer, result, logDiagnostics);
            PrefabUtility.RecordPrefabInstancePropertyModifications(composer);
            if (group >= 0) Undo.CollapseUndoOperations(group);
            return result;
        }

        private static void Record(CameraRigComposer composer, CameraRigComposerApplyRebuildResult result, bool log)
        {
            composer.EditorSetApplyRebuildResult(result.Status, result.BlockingIssue, result.MaterializationSummary);
            EditorUtility.SetDirty(composer);
            if (!log || !composer.LogApplyRebuildDiagnostics) return;
            var logger = FrameworkLogger.Create(typeof(CameraRigComposerApplyRebuildUtility));
            string message = $"Camera rig '{composer.name}': {result.Status}. {result.BlockingIssue} {result.MaterializationSummary}";
            if (result.Succeeded) logger.Info(message); else logger.Error(message);
        }
    }
}
