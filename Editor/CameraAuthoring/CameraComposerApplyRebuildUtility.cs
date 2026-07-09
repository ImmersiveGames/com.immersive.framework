using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Camera.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    /// <summary>
    /// Editor-only Apply/Rebuild entry point for CameraComposer.
    /// The utility resolves explicit target sources and delegates technical Cinemachine rig materialization to C4.
    /// </summary>
    public static class CameraComposerApplyRebuildUtility
    {
        public static CameraComposerApplyRebuildResult Validate(CameraComposer composer, bool logDiagnostics = true)
        {
            if (composer == null)
            {
                const string issue = "CameraComposer validation requires a target composer.";
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][CameraComposer] Validation failed. issue='{issue}'");
                }

                return CameraComposerApplyRebuildResult.Failed("ValidationFailed", issue);
            }

            if (!composer.TryValidateForApply(out string validationIssue))
            {
                composer.EditorSetApplyRebuildResult("ValidationFailed", validationIssue, string.Empty, string.Empty, null, null);
                EditorUtility.SetDirty(composer);
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][CameraComposer] Validation failed. camera='{composer.name}' issue='{validationIssue}'", composer);
                }

                return CameraComposerApplyRebuildResult.Failed("ValidationFailed", validationIssue);
            }

            CameraTargetResolveResult targetResult = composer.ResolveCameraTargets(composer.FollowRequirement, composer.LookAtRequirement);
            if (targetResult.IsBlocked)
            {
                composer.EditorSetApplyRebuildResult("ValidationFailed", targetResult.BlockingIssue, targetResult.DiagnosticSummary, string.Empty, null, null);
                EditorUtility.SetDirty(composer);
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][CameraComposer] Validation failed. camera='{composer.name}' issue='{targetResult.BlockingIssue}'", composer);
                }

                return CameraComposerApplyRebuildResult.Failed("ValidationFailed", targetResult.BlockingIssue, targetResult.DiagnosticSummary);
            }

            composer.EditorSetApplyRebuildResult(
                "ValidationSucceeded",
                string.Empty,
                targetResult.DiagnosticSummary,
                "Validation completed. No materialization was changed.",
                targetResult.Targets.FollowTarget,
                targetResult.Targets.LookAtTarget);
            EditorUtility.SetDirty(composer);

            if (logDiagnostics)
            {
                Debug.Log($"[Immersive.Framework][CameraComposer] Validation succeeded. camera='{composer.name}' mode='{composer.Mode}' source='{composer.TargetSourceKind}' priority='{composer.Priority}'", composer);
            }

            return CameraComposerApplyRebuildResult.ValidationSucceeded(targetResult.DiagnosticSummary);
        }

        public static CameraComposerApplyRebuildResult ApplyOrRebuild(
            CameraComposer composer,
            bool logDiagnostics = true,
            bool useUndo = true)
        {
            if (composer == null)
            {
                const string issue = "CameraComposer Apply/Rebuild requires a target composer.";
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][CameraComposer] Apply/Rebuild failed. issue='{issue}'");
                }

                return CameraComposerApplyRebuildResult.Failed("ApplyFailed", issue);
            }

            int undoGroup = -1;
            if (useUndo)
            {
                Undo.SetCurrentGroupName("Apply Camera Composer");
                undoGroup = Undo.GetCurrentGroup();
            }

            if (!composer.TryValidateForApply(out string validationIssue))
            {
                composer.EditorSetApplyRebuildResult("ApplyFailed", validationIssue, string.Empty, string.Empty, null, null);
                EditorUtility.SetDirty(composer);
                CollapseUndoIfNeeded(useUndo, undoGroup);
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][CameraComposer] Apply/Rebuild failed. camera='{composer.name}' issue='{validationIssue}'", composer);
                }

                return CameraComposerApplyRebuildResult.Failed("ApplyFailed", validationIssue);
            }

            CameraTargetResolveResult targetResult = composer.ResolveCameraTargets(composer.FollowRequirement, composer.LookAtRequirement);
            if (targetResult.IsBlocked)
            {
                composer.EditorSetApplyRebuildResult("ApplyFailed", targetResult.BlockingIssue, targetResult.DiagnosticSummary, string.Empty, null, null);
                EditorUtility.SetDirty(composer);
                CollapseUndoIfNeeded(useUndo, undoGroup);
                if (logDiagnostics)
                {
                    Debug.LogWarning($"[Immersive.Framework][CameraComposer] Apply/Rebuild failed. camera='{composer.name}' issue='{targetResult.BlockingIssue}'", composer);
                }

                return CameraComposerApplyRebuildResult.Failed("ApplyFailed", targetResult.BlockingIssue, targetResult.DiagnosticSummary);
            }

            var request = new CinemachineRigMaterializationRequest
            {
                RigRoot = composer.transform,
                UnityCamera = composer.UnityCamera,
                CinemachineCamera = composer.CinemachineCamera,
                FollowTarget = composer.FollowRequirement == CameraTargetRequirement.NotUsed ? null : targetResult.Targets.FollowTarget,
                LookAtTarget = composer.LookAtRequirement == CameraTargetRequirement.NotUsed ? null : targetResult.Targets.LookAtTarget,
                Priority = composer.Priority,
                RequireFollowTarget = composer.FollowRequirement == CameraTargetRequirement.Required,
                RequireLookAtTarget = composer.LookAtRequirement == CameraTargetRequirement.Required,
                CreateUnityCameraIfMissing = composer.CreateUnityCameraIfMissing,
                CreateCinemachineCameraIfMissing = composer.CreateCinemachineCameraIfMissing,
                UseUndo = useUndo,
                UnityCameraObjectName = composer.UnityCameraObjectName,
                CinemachineCameraObjectName = composer.CinemachineCameraObjectName
            };

            CinemachineRigMaterializationReport materializationReport = CinemachineRigMaterializer.ApplyOrRebuild(request);
            string materializationSummary = materializationReport.CreateSummary();
            string status = materializationReport.Succeeded ? "ApplySucceeded" : "ApplyCompletedWithBlockingIssues";
            string blockingIssue = materializationReport.Succeeded ? string.Empty : materializationReport.FirstBlockingIssue;

            composer.EditorSetGeneratedReferences(
                materializationReport.Evidence.UnityCamera,
                materializationReport.Evidence.CinemachineCamera);
            composer.EditorSetApplyRebuildResult(
                status,
                blockingIssue,
                targetResult.DiagnosticSummary,
                materializationSummary,
                targetResult.Targets.FollowTarget,
                targetResult.Targets.LookAtTarget);
            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(composer.gameObject);
            CollapseUndoIfNeeded(useUndo, undoGroup);

            if (logDiagnostics && composer.LogApplyRebuildDiagnostics)
            {
                Debug.Log($"[Immersive.Framework][CameraComposer] Apply/Rebuild completed. camera='{composer.name}' mode='{composer.Mode}' source='{composer.TargetSourceKind}' priority='{composer.Priority}' created='{materializationReport.CreatedCount}' repaired='{materializationReport.RepairedCount}' alreadyValid='{materializationReport.AlreadyValidCount}' skipped='{materializationReport.SkippedCount}' blocked='{materializationReport.BlockedCount}'", composer);
            }

            return CameraComposerApplyRebuildResult.Applied(
                materializationReport.Succeeded,
                status,
                blockingIssue,
                targetResult.DiagnosticSummary,
                materializationSummary,
                materializationReport.CreatedCount,
                materializationReport.RepairedCount,
                materializationReport.AlreadyValidCount,
                materializationReport.SkippedCount,
                materializationReport.BlockedCount);
        }

        private static void CollapseUndoIfNeeded(bool useUndo, int undoGroup)
        {
            if (useUndo && undoGroup >= 0)
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }
    }
}
