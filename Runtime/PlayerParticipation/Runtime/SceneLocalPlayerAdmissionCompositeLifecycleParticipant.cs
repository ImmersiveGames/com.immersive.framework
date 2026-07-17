using System;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Preserves the canonical Player Activity participant identity while composing Scene Local
    /// Player admission before Enter and after Exit. The generic Activity executor still sees one
    /// explicit participant and one canonical content id.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3M4B2A phase-aware composition of Scene Local Player admission with canonical Player lifecycle.")]
    internal sealed class SceneLocalPlayerAdmissionCompositeLifecycleParticipant :
        IActivityContentExecutionParticipant,
        IActivityContentExecutionParticipantSource
    {
        private readonly ActivityPlayerActorLifecycleParticipant canonicalParticipant;
        private readonly SceneLocalPlayerAdmissionActivityLifecycleRuntime sceneLifecycle;

        internal SceneLocalPlayerAdmissionCompositeLifecycleParticipant(
            ActivityPlayerActorLifecycleParticipant canonicalParticipant,
            SceneLocalPlayerAdmissionRuntimeHostModule sceneModule)
        {
            this.canonicalParticipant = canonicalParticipant ??
                throw new ArgumentNullException(nameof(canonicalParticipant));
            sceneLifecycle = new SceneLocalPlayerAdmissionActivityLifecycleRuntime(
                sceneModule ?? throw new ArgumentNullException(nameof(sceneModule)));
        }

        internal string Diagnostic => sceneLifecycle.Diagnostic;
        internal int ActiveEntryCount => sceneLifecycle.ActiveEntryCount;

        public ActivityContentExecutionParticipantSourceResult
            ResolveActivityContentExecutionParticipants(
                ActivityContentExecutionParticipantSourceRequest request)
        {
            if (!request.IsValid)
            {
                return ActivityContentExecutionParticipantSourceResult.RejectedInvalidRequest(
                    request,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    "scene-local-player-composite-invalid-request",
                    "Scene Local Player composite lifecycle requires a valid Activity transition request.");
            }

            ActivityContentExecutionParticipantCollection collection =
                ActivityContentExecutionParticipantCollection.FromParticipants(
                    new IActivityContentExecutionParticipant[] { this });
            return ActivityContentExecutionParticipantSourceResult.FromCollection(
                request,
                collection,
                nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                "scene-local-player-composite-source",
                "Canonical Player Activity lifecycle supplied with Scene Local Player admission composition.");
        }

        public ActivityContentExecutionParticipantDescriptor
            GetActivityContentExecutionDescriptor()
        {
            return canonicalParticipant.GetActivityContentExecutionDescriptor();
        }

        public ActivityContentExecutionResult ExecuteActivityContent(
            ActivityContentExecutionRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException(
                    "Scene Local Player composite lifecycle received an invalid Activity request.",
                    nameof(request));
            }

            return request.Phase switch
            {
                ActivityContentExecutionPhase.Enter => ExecuteEnter(request),
                ActivityContentExecutionPhase.Exit => ExecuteExit(request),
                _ => ActivityContentExecutionResult.BlockingFailure(
                    request,
                    1,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    "scene-local-player-composite-unsupported-phase",
                    $"Unsupported Scene Local Player composite phase '{request.Phase}'.")
            };
        }

        private ActivityContentExecutionResult ExecuteEnter(
            ActivityContentExecutionRequest request)
        {
            SceneLocalPlayerAdmissionActivityLifecycleResult scene = sceneLifecycle.TryEnter(
                request.Activity,
                request.Owner,
                nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                "activity-enter-scene-local-player-admission");
            if (scene == null || !scene.Succeeded)
            {
                return ActivityContentExecutionResult.BlockingFailure(
                    request,
                    scene?.BlockingIssueCount ?? 1,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    "scene-local-player-enter-failed",
                    scene != null
                        ? scene.ToDiagnosticString()
                        : "Scene Local Player Activity enter returned no result.");
            }

            ActivityContentExecutionResult canonical =
                canonicalParticipant.ExecuteActivityContent(request);
            if (!canonical.Failed && !canonical.HasBlockingIssues)
            {
                return CreateCombinedSuccess(
                    request,
                    scene,
                    canonical,
                    "scene-local-player-composite-entered");
            }

            SceneLocalPlayerAdmissionActivityLifecycleResult rollback =
                sceneLifecycle.TryRollbackEnter(
                    request.Activity,
                    request.Owner,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    "canonical-player-enter-failed");
            string message =
                $"Canonical Player Activity enter failed. {canonical.ToDiagnosticString()} " +
                $"Scene admission rollback=({rollback?.ToDiagnosticString() ?? "<no-result>"}).";
            int blockingIssues = canonical.BlockingIssueCount +
                (rollback?.BlockingIssueCount ?? 1);
            return ActivityContentExecutionResult.BlockingFailure(
                request,
                blockingIssues <= 0 ? 1 : blockingIssues,
                nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                rollback != null && rollback.Succeeded
                    ? "canonical-player-enter-failed-scene-rollback-succeeded"
                    : "canonical-player-enter-failed-scene-rollback-failed",
                message);
        }

        private ActivityContentExecutionResult ExecuteExit(
            ActivityContentExecutionRequest request)
        {
            ActivityContentExecutionResult canonical =
                canonicalParticipant.ExecuteActivityContent(request);
            if (canonical.Failed || canonical.HasBlockingIssues)
            {
                return canonical;
            }

            SceneLocalPlayerAdmissionActivityLifecycleResult scene = sceneLifecycle.TryExit(
                request.Activity,
                request.Owner,
                nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                "activity-exit-scene-local-player-admission");
            if (scene == null || !scene.Succeeded)
            {
                return ActivityContentExecutionResult.BlockingFailure(
                    request,
                    scene?.BlockingIssueCount ?? 1,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    "scene-local-player-exit-failed",
                    scene != null
                        ? scene.ToDiagnosticString()
                        : "Scene Local Player Activity exit returned no result.");
            }

            return CreateCombinedSuccess(
                request,
                scene,
                canonical,
                "scene-local-player-composite-exited");
        }

        private static ActivityContentExecutionResult CreateCombinedSuccess(
            ActivityContentExecutionRequest request,
            SceneLocalPlayerAdmissionActivityLifecycleResult scene,
            ActivityContentExecutionResult canonical,
            string reason)
        {
            string message =
                $"scene=({scene.ToDiagnosticString()}) canonical=({canonical.ToDiagnosticString()})";
            bool sceneNoOp = scene.Status is
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededNoAutomaticPlayers or
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyEntered or
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyExited;
            bool noOp = sceneNoOp &&
                canonical.Status == ActivityContentExecutionStatus.SucceededNoOp;
            return noOp
                ? ActivityContentExecutionResult.SucceededNoOp(
                    request,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    reason,
                    message)
                : ActivityContentExecutionResult.Success(
                    request,
                    nameof(SceneLocalPlayerAdmissionCompositeLifecycleParticipant),
                    reason,
                    message);
        }
    }
}
