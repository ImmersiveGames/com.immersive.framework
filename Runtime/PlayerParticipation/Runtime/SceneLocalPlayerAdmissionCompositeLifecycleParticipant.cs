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
        "P3M4B2A/P3M4B2B phase-aware composition of Scene Local Player admission and external Actor adoption with canonical Player lifecycle.")]
    internal sealed class SceneLocalPlayerAdmissionCompositeLifecycleParticipant :
        IActivityContentExecutionParticipant,
        IActivityContentExecutionParticipantSource
    {
        private readonly ActivityPlayerActorLifecycleParticipant _canonicalParticipant;
        private readonly SceneLocalPlayerAdmissionRuntimeHostModule _sceneModule;
        private readonly PlayerActorPreparationRuntimeHostModule _preparationModule;
        private readonly SceneLocalPlayerAdmissionActivityLifecycleRuntime _sceneLifecycle;

        internal SceneLocalPlayerAdmissionCompositeLifecycleParticipant(
            ActivityPlayerActorLifecycleParticipant canonicalParticipant,
            SceneLocalPlayerAdmissionRuntimeHostModule sceneModule)
            : this(canonicalParticipant, sceneModule, null)
        {
        }

        internal SceneLocalPlayerAdmissionCompositeLifecycleParticipant(
            ActivityPlayerActorLifecycleParticipant canonicalParticipant,
            SceneLocalPlayerAdmissionRuntimeHostModule sceneModule,
            PlayerActorPreparationRuntimeHostModule preparationModule)
        {
            this._canonicalParticipant = canonicalParticipant ??
                throw new ArgumentNullException(nameof(canonicalParticipant));
            this._sceneModule = sceneModule ??
                throw new ArgumentNullException(nameof(sceneModule));
            this._preparationModule = preparationModule;
            _sceneLifecycle = new SceneLocalPlayerAdmissionActivityLifecycleRuntime(
                this._sceneModule,
                preparationModule);
        }

        internal string Diagnostic => _sceneLifecycle.Diagnostic;
        internal int ActiveEntryCount => _sceneLifecycle.ActiveEntryCount;

        internal SceneLocalPlayerAdmissionActivityLifecycleResult
            TryRetireContextForSessionPlayerLeave(
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            return _sceneLifecycle.TryRetireContextForSessionPlayerLeave(
                leaveToken,
                source,
                reason);
        }

        internal bool TryRetireAllContextForSessionTermination(
            string source,
            string reason,
            out string issue)
        {
            return _sceneLifecycle.TryRetireAllContextForSessionTermination(
                source,
                reason,
                out issue);
        }

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

            _sceneModule.SetActivityLifecycleContext(
                request.RouteContext,
                request.NextActivityContext);

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
            return _canonicalParticipant.GetActivityContentExecutionDescriptor();
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

            try
            {
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
            finally
            {
                // Canonical preparation/provisioning paths may re-register their narrower
                // participant source while this composite executes. Restore the complete
                // source before the next Activity transition is resolved.
                if (_preparationModule != null)
                {
                    _preparationModule.TryComposeSceneLocalPlayerAdmissionLifecycle(
                        _sceneModule,
                        out _);
                }
            }
        }

        private ActivityContentExecutionResult ExecuteEnter(
            ActivityContentExecutionRequest request)
        {
            SceneLocalPlayerAdmissionActivityLifecycleResult scene = _sceneLifecycle.TryEnter(
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
                _canonicalParticipant.ExecuteActivityContent(request);
            if (!canonical.Failed && !canonical.HasBlockingIssues)
            {
                return CreateCombinedSuccess(
                    request,
                    scene,
                    canonical,
                    "scene-local-player-composite-entered");
            }

            SceneLocalPlayerAdmissionActivityLifecycleResult rollback =
                _sceneLifecycle.TryRollbackEnter(
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
                _canonicalParticipant.ExecuteActivityContent(request);
            if (canonical.Failed || canonical.HasBlockingIssues)
            {
                return canonical;
            }

            SceneLocalPlayerAdmissionActivityLifecycleResult scene = _sceneLifecycle.TryExit(
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
