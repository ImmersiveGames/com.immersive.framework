using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Camera.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    public static class CameraRigComposerApplyRebuildUtility
    {
        public static CameraRigComposerApplyRebuildResult Validate(
            CameraRigComposer composer,
            bool logDiagnostics = true)
        {
            if (composer == null)
            {
                return CameraRigComposerApplyRebuildResult.Failed(
                    "ValidationFailed",
                    "CameraRigComposer validation requires a target composer.");
            }

            if (!composer.TryValidateForApply(out string issue))
            {
                composer.EditorSetApplyRebuildResult(
                    "ValidationFailed",
                    issue,
                    string.Empty,
                    string.Empty,
                    null,
                    null);
                EditorUtility.SetDirty(composer);

                return CameraRigComposerApplyRebuildResult.Failed(
                    "ValidationFailed",
                    issue);
            }

            CameraTargetResolveResult targets =
                composer.ResolveCameraTargets(
                    composer.FollowRequirement,
                    composer.LookAtRequirement);

            if (targets.IsBlocked)
            {
                composer.EditorSetApplyRebuildResult(
                    "ValidationFailed",
                    targets.BlockingIssue,
                    targets.DiagnosticSummary,
                    string.Empty,
                    null,
                    null);
                EditorUtility.SetDirty(composer);

                return CameraRigComposerApplyRebuildResult.Failed(
                    "ValidationFailed",
                    targets.BlockingIssue,
                    targets.DiagnosticSummary);
            }

            composer.EditorSetApplyRebuildResult(
                "ValidationSucceeded",
                string.Empty,
                targets.DiagnosticSummary,
                "Validation completed. No rig was changed.",
                targets.Targets.FollowTarget,
                targets.Targets.LookAtTarget);
            EditorUtility.SetDirty(composer);

            if (logDiagnostics)
            {
                Debug.Log(
                    $"[Immersive.Framework][CameraRigComposer] Validation succeeded. rig='{composer.name}' intent='{composer.PresentationIntent}' source='{composer.TargetSourceKind}'.",
                    composer);
            }

            return CameraRigComposerApplyRebuildResult
                .ValidationSucceeded(targets.DiagnosticSummary);
        }

        public static CameraRigComposerApplyRebuildResult ApplyOrRebuild(
            CameraRigComposer composer,
            bool logDiagnostics = true,
            bool useUndo = true)
        {
            if (composer == null)
            {
                return CameraRigComposerApplyRebuildResult.Failed(
                    "ApplyFailed",
                    "CameraRigComposer Apply/Rebuild requires a target composer.");
            }

            int undoGroup = -1;

            if (useUndo)
            {
                Undo.SetCurrentGroupName(
                    "Apply Camera Rig Composer");
                undoGroup = Undo.GetCurrentGroup();
            }

            if (!composer.TryValidateForApply(out string issue))
            {
                return Fail(
                    composer,
                    "ApplyFailed",
                    issue,
                    string.Empty,
                    useUndo,
                    undoGroup);
            }

            CameraTargetResolveResult targets =
                composer.ResolveCameraTargets(
                    composer.FollowRequirement,
                    composer.LookAtRequirement);

            if (targets.IsBlocked)
            {
                return Fail(
                    composer,
                    "ApplyFailed",
                    targets.BlockingIssue,
                    targets.DiagnosticSummary,
                    useUndo,
                    undoGroup);
            }

            var request =
                new CinemachineRigMaterializationRequest
                {
                    RigRoot = composer.transform,
                    MaterializeUnityOutput = false,
                    UnityCamera = null,
                    CinemachineCamera =
                        composer.CinemachineCamera,
                    FollowTarget =
                        composer.FollowRequirement ==
                        CameraTargetRequirement.NotUsed
                            ? null
                            : targets.Targets.FollowTarget,
                    LookAtTarget =
                        composer.LookAtRequirement ==
                        CameraTargetRequirement.NotUsed
                            ? null
                            : targets.Targets.LookAtTarget,
                    RequireFollowTarget =
                        composer.FollowRequirement ==
                        CameraTargetRequirement.Required,
                    RequireLookAtTarget =
                        composer.LookAtRequirement ==
                        CameraTargetRequirement.Required,
                    CreateUnityCameraIfMissing = false,
                    CreateCinemachineCameraIfMissing =
                        composer.CreateCinemachineCameraIfMissing,
                    UseUndo = useUndo,
                    CinemachineCameraObjectName =
                        composer.CinemachineCameraObjectName
                };

            CinemachineRigMaterializationReport report =
                CinemachineRigMaterializer.ApplyOrRebuild(request);

            string materializationSummary =
                report.CreateSummary();
            string status = report.Succeeded
                ? "ApplySucceeded"
                : "ApplyCompletedWithBlockingIssues";
            string blockingIssue = report.Succeeded
                ? string.Empty
                : report.FirstBlockingIssue;

            composer.EditorSetGeneratedReference(
                report.Evidence.CinemachineCamera);
            composer.EditorSetApplyRebuildResult(
                status,
                blockingIssue,
                targets.DiagnosticSummary,
                materializationSummary,
                targets.Targets.FollowTarget,
                targets.Targets.LookAtTarget);

            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(composer.gameObject);

            if (useUndo && undoGroup >= 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            if (logDiagnostics &&
                composer.LogApplyRebuildDiagnostics)
            {
                Debug.Log(
                    $"[Immersive.Framework][CameraRigComposer] Apply/Rebuild completed. rig='{composer.name}' created='{report.CreatedCount}' repaired='{report.RepairedCount}' alreadyValid='{report.AlreadyValidCount}' skipped='{report.SkippedCount}' blocked='{report.BlockedCount}'.",
                    composer);
            }

            return CameraRigComposerApplyRebuildResult.Applied(
                report.Succeeded,
                status,
                blockingIssue,
                targets.DiagnosticSummary,
                materializationSummary,
                report.CreatedCount,
                report.RepairedCount,
                report.AlreadyValidCount,
                report.SkippedCount,
                report.BlockedCount);
        }

        private static CameraRigComposerApplyRebuildResult Fail(
            CameraRigComposer composer,
            string status,
            string issue,
            string targetSummary,
            bool useUndo,
            int undoGroup)
        {
            composer.EditorSetApplyRebuildResult(
                status,
                issue,
                targetSummary,
                string.Empty,
                null,
                null);
            EditorUtility.SetDirty(composer);

            if (useUndo && undoGroup >= 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            return CameraRigComposerApplyRebuildResult.Failed(
                status,
                issue,
                targetSummary);
        }
    }
}
