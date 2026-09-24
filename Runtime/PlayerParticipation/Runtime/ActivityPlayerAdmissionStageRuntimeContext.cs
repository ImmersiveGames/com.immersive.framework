using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Owns at most one target Activity pre-activation Player stage. It creates the
    /// temporary scope through an explicit lifecycle adapter, resolves Player evidence,
    /// evaluates P3K.7A and either hands off a commit or rolls everything back.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7B staged Activity Player pre-activation transaction.")]
    internal sealed class ActivityPlayerAdmissionStageRuntimeContext
    {
        private sealed class ActiveRecord
        {
            internal ActivityPlayerAdmissionStageToken token;
            internal ActivityPlayerAdmissionStageScope scope;
            internal ActivityPlayerAdmissionStageResolution resolution;
            internal ActivityPlayerAdmissionFlowDecision decision;
            internal ActivityPlayerAdmissionStageSnapshot snapshot;
        }

        private readonly IActivityPlayerAdmissionStageScopeRuntime _scopeRuntime;
        private readonly IActivityPlayerAdmissionStageResolver _resolver;
        private readonly ActivityPlayerAdmissionFlowGate _flowGate;
        private ActiveRecord _active;
        private int _stageSequence;
        private ActivityPlayerAdmissionStageSnapshot _lastSnapshot;

        internal ActivityPlayerAdmissionStageRuntimeContext(
            IActivityPlayerAdmissionStageScopeRuntime scopeRuntime,
            IActivityPlayerAdmissionStageResolver resolver,
            ActivityPlayerAdmissionFlowGate flowGate = null)
        {
            this._scopeRuntime = scopeRuntime ??
                throw new ArgumentNullException(nameof(scopeRuntime));
            this._resolver = resolver ??
                throw new ArgumentNullException(nameof(resolver));
            this._flowGate = flowGate ?? new ActivityPlayerAdmissionFlowGate();
            _lastSnapshot = ActivityPlayerAdmissionStageSnapshot.Empty(
                nameof(ActivityPlayerAdmissionStageRuntimeContext),
                "runtime-initialization",
                "No Activity Player admission stage is active.");
        }

        internal int StageSequence => _stageSequence;
        internal bool HasActiveStage => _active != null;

        internal ActivityPlayerAdmissionStageResult TryStage(
            ActivityAsset activity,
            string source,
            string reason)
        {
            const string operation = "StageActivityPlayerAdmission";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerAdmissionStageRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "stage-activity-player-admission");
            ActivityPlayerAdmissionStageSnapshot previous = CreateSnapshot();

            if (activity == null)
            {
                return Result(
                    ActivityPlayerAdmissionStageStatus.RejectedInvalidRequest,
                    operation,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Staged Activity Player admission requires an ActivityAsset.");
            }

            if (_active != null)
            {
                return Result(
                    ActivityPlayerAdmissionStageStatus.RejectedAnotherStageActive,
                    operation,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Another Activity Player admission stage is already active.");
            }

            _stageSequence++;
            ActivityPlayerAdmissionStageScope scope = null;
            string scopeIssue = string.Empty;
            bool scopeCreated;
            try
            {
                scopeCreated = _scopeRuntime.TryCreate(
                    activity,
                    _stageSequence,
                    resolvedSource,
                    resolvedReason,
                    out scope,
                    out scopeIssue);
            }
            catch (Exception exception)
            {
                scopeCreated = false;
                scopeIssue =
                    $"Staged Activity scope runtime threw '{exception.GetType().Name}'. {exception.Message}";
            }

            if (!scopeCreated || scope == null || !scope.IsValid)
            {
                _lastSnapshot = ActivityPlayerAdmissionStageSnapshot.Empty(
                    resolvedSource,
                    resolvedReason,
                    scopeIssue.NormalizeTextOrFallback(
                        "Staged Activity scope creation failed."));
                return Result(
                    ActivityPlayerAdmissionStageStatus.FailedScopeCreation,
                    operation,
                    previous,
                    _lastSnapshot,
                    false,
                    false,
                    string.Empty,
                    _lastSnapshot.Message);
            }

            ActivityPlayerAdmissionStageResolution resolution;
            try
            {
                resolution = _resolver.Resolve(
                    activity,
                    scope,
                    resolvedSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                resolution = null;
                scopeIssue =
                    $"Activity Player stage resolver threw '{exception.GetType().Name}'. {exception.Message}";
            }

            if (resolution == null || !resolution.Succeeded)
            {
                string resolutionIssue = resolution != null
                    ? resolution.Message.NormalizeTextOrFallback(
                        "Activity Player stage resolution failed.")
                    : scopeIssue.NormalizeTextOrFallback(
                        "Activity Player stage resolver returned no resolution.");
                bool rollbackSucceeded = RollbackParts(
                    resolution,
                    scope,
                    resolvedSource,
                    resolvedReason,
                    out string rollbackIssue,
                    out bool resolverRolledBack,
                    out bool scopeReleased);
                var failedToken = new ActivityPlayerAdmissionStageToken(
                    resolution?.ParticipationSnapshot?.ContextId ?? string.Empty,
                    scope.Owner,
                    _stageSequence);
                var failed = new ActivityPlayerAdmissionStageSnapshot(
                    failedToken,
                    rollbackSucceeded
                        ? ActivityPlayerAdmissionStageState.Failed
                        : ActivityPlayerAdmissionStageState.RollbackFailed,
                    null,
                    scope.Owner,
                    true,
                    false,
                    resolverRolledBack,
                    scopeReleased,
                    resolvedSource,
                    resolvedReason,
                    resolutionIssue);
                _lastSnapshot = failed;
                if (!rollbackSucceeded)
                {
                    _active = new ActiveRecord
                    {
                        token = failedToken,
                        scope = scope,
                        resolution = resolution,
                        decision = null,
                        snapshot = failed
                    };
                }
                return Result(
                    rollbackSucceeded
                        ? ActivityPlayerAdmissionStageStatus.FailedResolution
                        : ActivityPlayerAdmissionStageStatus.FailedRollback,
                    operation,
                    previous,
                    failed,
                    true,
                    rollbackSucceeded,
                    rollbackIssue,
                    resolutionIssue);
            }

            ActivityPlayerAdmissionFlowDecision decision = _flowGate.Evaluate(
                activity,
                resolution.ParticipationSnapshot,
                resolution.PreparationSnapshot,
                resolution.GameplayAdmissionSnapshot,
                resolvedSource,
                resolvedReason);

            if (decision == null)
            {
                bool rollbackSucceeded = RollbackParts(
                    resolution,
                    scope,
                    resolvedSource,
                    resolvedReason,
                    out string rollbackIssue,
                    out bool resolverRolledBack,
                    out bool scopeReleased);
                var failedToken = new ActivityPlayerAdmissionStageToken(
                    resolution.ParticipationSnapshot?.ContextId ?? string.Empty,
                    scope.Owner,
                    _stageSequence);
                var failed = new ActivityPlayerAdmissionStageSnapshot(
                    failedToken,
                    rollbackSucceeded
                        ? ActivityPlayerAdmissionStageState.Failed
                        : ActivityPlayerAdmissionStageState.RollbackFailed,
                    null,
                    scope.Owner,
                    true,
                    true,
                    resolverRolledBack,
                    scopeReleased,
                    resolvedSource,
                    resolvedReason,
                    "Activity Player admission flow gate returned no decision.");
                _lastSnapshot = failed;
                if (!rollbackSucceeded)
                {
                    _active = new ActiveRecord
                    {
                        token = failedToken,
                        scope = scope,
                        resolution = resolution,
                        decision = null,
                        snapshot = failed
                    };
                }
                return Result(
                    rollbackSucceeded
                        ? ActivityPlayerAdmissionStageStatus.FailedEvaluation
                        : ActivityPlayerAdmissionStageStatus.FailedRollback,
                    operation,
                    previous,
                    failed,
                    true,
                    rollbackSucceeded,
                    rollbackIssue,
                    failed.Message);
            }

            var token = new ActivityPlayerAdmissionStageToken(
                decision.SessionContextId,
                scope.Owner,
                _stageSequence);

            if (decision.CanProceed && token.IsValid)
            {
                var ready = new ActivityPlayerAdmissionStageSnapshot(
                    token,
                    ActivityPlayerAdmissionStageState.ReadyToCommit,
                    decision,
                    scope.Owner,
                    true,
                    true,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Staged Activity Player admission is ready for explicit commit handoff.");
                _active = new ActiveRecord
                {
                    token = token,
                    scope = scope,
                    resolution = resolution,
                    decision = decision,
                    snapshot = ready
                };
                _lastSnapshot = ready;
                return Result(
                    ActivityPlayerAdmissionStageStatus.SucceededReadyToCommit,
                    operation,
                    previous,
                    ready,
                    false,
                    false,
                    string.Empty,
                    ready.Message);
            }

            bool stageRollbackSucceeded = RollbackParts(
                resolution,
                scope,
                resolvedSource,
                resolvedReason,
                out string stageRollbackIssue,
                out bool stageResolverRolledBack,
                out bool stageScopeReleased);
            var rolledBack = new ActivityPlayerAdmissionStageSnapshot(
                token,
                stageRollbackSucceeded
                    ? ActivityPlayerAdmissionStageState.RolledBack
                    : ActivityPlayerAdmissionStageState.RollbackFailed,
                decision,
                scope.Owner,
                true,
                true,
                stageResolverRolledBack,
                stageScopeReleased,
                resolvedSource,
                resolvedReason,
                stageRollbackSucceeded
                    ? "Staged Activity Player admission did not proceed and was fully rolled back."
                    : "Staged Activity Player admission did not proceed and rollback failed.");
            _lastSnapshot = rolledBack;
            if (!stageRollbackSucceeded)
            {
                _active = new ActiveRecord
                {
                    token = token,
                    scope = scope,
                    resolution = resolution,
                    decision = decision,
                    snapshot = rolledBack
                };
            }

            return Result(
                stageRollbackSucceeded
                    ? ActivityPlayerAdmissionStageStatus.SucceededRolledBack
                    : ActivityPlayerAdmissionStageStatus.FailedRollback,
                operation,
                previous,
                rolledBack,
                true,
                stageRollbackSucceeded,
                stageRollbackIssue,
                rolledBack.Message);
        }

        internal ActivityPlayerAdmissionStageResult TryCommit(
            ActivityPlayerAdmissionStageToken expectedStage,
            out ActivityPlayerAdmissionStageCommit commit)
        {
            const string operation = "CommitActivityPlayerAdmissionStage";
            commit = null;
            ActivityPlayerAdmissionStageSnapshot previous = CreateSnapshot();

            if (!TryValidateActive(expectedStage, out string issue))
            {
                return Result(
                    ActivityPlayerAdmissionStageStatus.RejectedForeignOrStaleStage,
                    operation,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    issue);
            }

            if (!_active.snapshot.IsReadyToCommit)
            {
                return Result(
                    ActivityPlayerAdmissionStageStatus.RejectedNotReadyToCommit,
                    operation,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Activity Player admission stage is not ReadyToCommit.");
            }

            var committed = new ActivityPlayerAdmissionStageSnapshot(
                _active.token,
                ActivityPlayerAdmissionStageState.Committed,
                _active.decision,
                _active.scope.Owner,
                true,
                true,
                false,
                false,
                _active.snapshot.Source,
                _active.snapshot.Reason,
                "Staged Activity Player admission ownership was handed off for Activity commit.");
            commit = new ActivityPlayerAdmissionStageCommit(
                _active.token,
                _active.decision,
                _scopeRuntime,
                _resolver,
                _active.scope,
                _active.resolution);
            _active = null;
            _lastSnapshot = committed;
            return Result(
                ActivityPlayerAdmissionStageStatus.SucceededCommitted,
                operation,
                previous,
                committed,
                false,
                false,
                string.Empty,
                committed.Message);
        }

        internal ActivityPlayerAdmissionStageResult TryRollback(
            ActivityPlayerAdmissionStageToken expectedStage,
            string source,
            string reason)
        {
            const string operation = "RollbackActivityPlayerAdmissionStage";
            ActivityPlayerAdmissionStageSnapshot previous = CreateSnapshot();
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(ActivityPlayerAdmissionStageRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "rollback-activity-player-admission-stage");

            if (_active == null)
            {
                return Result(
                    expectedStage.IsValid
                        ? ActivityPlayerAdmissionStageStatus.RejectedForeignOrStaleStage
                        : ActivityPlayerAdmissionStageStatus.SucceededAlreadyRolledBack,
                    operation,
                    previous,
                    previous,
                    false,
                    true,
                    string.Empty,
                    expectedStage.IsValid
                        ? "No active Activity Player admission stage matches the supplied token."
                        : "No Activity Player admission stage is active.");
            }

            if (!TryValidateActive(expectedStage, out string issue))
            {
                return Result(
                    ActivityPlayerAdmissionStageStatus.RejectedForeignOrStaleStage,
                    operation,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    issue);
            }

            bool rollbackSucceeded = RollbackParts(
                _active.resolution,
                _active.scope,
                resolvedSource,
                resolvedReason,
                out string rollbackIssue,
                out bool resolverRolledBack,
                out bool scopeReleased);
            var current = new ActivityPlayerAdmissionStageSnapshot(
                _active.token,
                rollbackSucceeded
                    ? ActivityPlayerAdmissionStageState.RolledBack
                    : ActivityPlayerAdmissionStageState.RollbackFailed,
                _active.decision,
                _active.scope.Owner,
                true,
                true,
                resolverRolledBack,
                scopeReleased,
                resolvedSource,
                resolvedReason,
                rollbackSucceeded
                    ? "Activity Player admission stage rolled back."
                    : "Activity Player admission stage rollback failed.");
            _lastSnapshot = current;
            if (rollbackSucceeded)
            {
                _active = null;
            }
            else
            {
                _active.snapshot = current;
            }

            return Result(
                rollbackSucceeded
                    ? ActivityPlayerAdmissionStageStatus.SucceededRolledBack
                    : ActivityPlayerAdmissionStageStatus.FailedRollback,
                operation,
                previous,
                current,
                true,
                rollbackSucceeded,
                rollbackIssue,
                current.Message);
        }

        internal ActivityPlayerAdmissionStageSnapshot CreateSnapshot()
        {
            return _active?.snapshot ?? _lastSnapshot;
        }

        private bool TryValidateActive(
            ActivityPlayerAdmissionStageToken expectedStage,
            out string issue)
        {
            if (_active == null)
            {
                issue = "No Activity Player admission stage is active.";
                return false;
            }

            if (!expectedStage.IsValid || expectedStage != _active.token)
            {
                issue = "Activity Player admission stage token is foreign or stale.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool RollbackParts(
            ActivityPlayerAdmissionStageResolution resolution,
            ActivityPlayerAdmissionStageScope scope,
            string source,
            string reason,
            out string issue,
            out bool resolverRolledBack,
            out bool scopeReleased)
        {
            string resolverIssue = string.Empty;
            try
            {
                resolverRolledBack = _resolver.TryRollback(
                    resolution,
                    source,
                    reason,
                    out resolverIssue);
            }
            catch (Exception exception)
            {
                resolverRolledBack = false;
                resolverIssue =
                    $"Activity Player stage resolver rollback threw '{exception.GetType().Name}'. {exception.Message}";
            }

            scopeReleased = false;
            string scopeIssue = string.Empty;
            if (resolverRolledBack)
            {
                try
                {
                    scopeReleased = _scopeRuntime.TryRelease(
                        scope,
                        source,
                        reason,
                        out scopeIssue);
                }
                catch (Exception exception)
                {
                    scopeReleased = false;
                    scopeIssue =
                        $"Staged Activity scope release threw '{exception.GetType().Name}'. {exception.Message}";
                }
            }

            issue = !resolverRolledBack
                ? resolverIssue.NormalizeTextOrFallback(
                    "Activity Player stage resolver rollback failed.")
                : !scopeReleased
                    ? scopeIssue.NormalizeTextOrFallback(
                        "Staged Activity scope release failed.")
                    : string.Empty;
            return resolverRolledBack && scopeReleased;
        }

        private static ActivityPlayerAdmissionStageResult Result(
            ActivityPlayerAdmissionStageStatus status,
            string operation,
            ActivityPlayerAdmissionStageSnapshot previous,
            ActivityPlayerAdmissionStageSnapshot current,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackIssue,
            string message)
        {
            return new ActivityPlayerAdmissionStageResult(
                status,
                operation,
                previous,
                current,
                rollbackAttempted,
                rollbackSucceeded,
                rollbackIssue,
                message);
        }
    }
}
