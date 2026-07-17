using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Editor.Camera.Cinemachine;
using Immersive.Logging.Records;
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

                if (logDiagnostics)
                {
                    LogValidationFailed(composer, issue, string.Empty);
                }

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

                if (logDiagnostics)
                {
                    LogValidationFailed(
                        composer,
                        targets.BlockingIssue,
                        targets.DiagnosticSummary);
                }

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
                CreateLogger().Info(
                    "CameraRigComposer validation succeeded.",
                    LogFields.Of(
                        LogFields.Field("rig", composer.name),
                        LogFields.Field("intent", composer.PresentationIntent),
                        LogFields.Field("source", composer.TargetSourceKind),
                        LogFields.Field("targetSummary", targets.DiagnosticSummary)));
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
                    undoGroup,
                    logDiagnostics);
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
                    undoGroup,
                    logDiagnostics);
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
                    CreateCinemachineFollowIfMissing = true,
                    FollowOffset = composer.FollowOffset,
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
                LogField[] fields = LogFields.Of(
                    LogFields.Field("rig", composer.name),
                    LogFields.Field("status", status),
                    LogFields.Field("created", report.CreatedCount),
                    LogFields.Field("repaired", report.RepairedCount),
                    LogFields.Field("alreadyValid", report.AlreadyValidCount),
                    LogFields.Field("skipped", report.SkippedCount),
                    LogFields.Field("blocked", report.BlockedCount),
                    LogFields.Field("targetSummary", targets.DiagnosticSummary),
                    LogFields.Field("materializationSummary", materializationSummary),
                    LogFields.Field("blockingIssue", blockingIssue));

                FrameworkLogger logger = CreateLogger();
                if (report.Succeeded)
                {
                    logger.Info(
                        "CameraRigComposer Apply/Rebuild completed.",
                        fields);
                }
                else
                {
                    logger.Warning(
                        "CameraRigComposer Apply/Rebuild completed with blocking issues.",
                        fields);
                }
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
            int undoGroup,
            bool logDiagnostics)
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

            if (logDiagnostics)
            {
                CreateLogger().Error(
                    "CameraRigComposer Apply/Rebuild failed.",
                    LogFields.Of(
                        LogFields.Field("rig", composer != null ? composer.name : "<none>"),
                        LogFields.Field("status", status),
                        LogFields.Field("issue", issue),
                        LogFields.Field("targetSummary", targetSummary)));
            }

            return CameraRigComposerApplyRebuildResult.Failed(
                status,
                issue,
                targetSummary);
        }

        private static void LogValidationFailed(
            CameraRigComposer composer,
            string issue,
            string targetSummary)
        {
            CreateLogger().Error(
                "CameraRigComposer validation failed.",
                LogFields.Of(
                    LogFields.Field("rig", composer != null ? composer.name : "<none>"),
                    LogFields.Field("intent", composer != null ? composer.PresentationIntent.ToString() : string.Empty),
                    LogFields.Field("source", composer != null ? composer.TargetSourceKind.ToString() : string.Empty),
                    LogFields.Field("issue", issue),
                    LogFields.Field("targetSummary", targetSummary)));
        }

        private static FrameworkLogger CreateLogger()
        {
            return FrameworkLogger.Create(typeof(CameraRigComposerApplyRebuildUtility));
        }
    }
}
