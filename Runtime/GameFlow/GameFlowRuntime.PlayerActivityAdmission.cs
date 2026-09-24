using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.GameFlow.Diagnostics;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        private IActivityPlayerLifecycleAdmissionRuntime
            _activityPlayerLifecycleAdmissionRuntime;
        private IGameFlowDiagnosticFaultPlan _diagnosticFaultPlan =
            NoOpGameFlowDiagnosticFaultPlan.Instance;

        internal void SetActivityPlayerLifecycleAdmissionRuntime(
            IActivityPlayerLifecycleAdmissionRuntime runtime)
        {
            _activityPlayerLifecycleAdmissionRuntime = runtime;
        }

        internal void SetDiagnosticFaultPlan(IGameFlowDiagnosticFaultPlan plan)
        {
            _diagnosticFaultPlan = plan ?? NoOpGameFlowDiagnosticFaultPlan.Instance;
        }

        private bool TryConsumeDiagnosticFault(
            GameFlowDiagnosticFaultCheckpoint checkpoint,
            string operation,
            string transaction,
            string slot,
            out string diagnostic)
        {
            GameFlowDiagnosticFaultDecision decision = _diagnosticFaultPlan.Evaluate(
                new GameFlowDiagnosticFaultRequest(checkpoint, operation, transaction, slot));
            diagnostic = decision.Diagnostic;
            return decision.ShouldFail;
        }

        private ActivityPlayerLifecycleAdmissionResult
            PrepareActivityPlayerLifecycleAdmission(
                ActivityAsset previousActivity,
                ActivityAsset targetActivity,
                string source,
                string reason)
        {
            // Physical candidate staging/handoff is not part of the canonical Activity
            // transition. Activity readiness is established after its contextual admission
            // using the Session-owned physical Player, when one is required.
            return ActivityPlayerLifecycleAdmissionResult.NotRequiredResult(
                "PrepareSameRouteActivityPlayerAdmission",
                source,
                reason,
                "Normal Activity transition uses contextual reprojection; no physical candidate is staged.");
        }

        private ActivityPlayerLifecycleAdmissionResult
            AuthorizeActivityPlayerTransition(
                ActivityPlayerLifecycleAdmissionResult preparation,
                string source,
                string reason)
        {
            if (preparation == null)
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .RejectedRuntimeUnavailable(
                        "AuthorizeActivityTransition",
                        source,
                        reason,
                        "Activity Player lifecycle admission preparation returned no result.");
            }

            if (preparation.NotRequired)
            {
                return preparation;
            }

            if (!preparation.ReadyForTransition ||
                preparation.CurrentSnapshot == null ||
                !preparation.CurrentSnapshot.Token.IsValid ||
                _activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .RejectedRuntimeUnavailable(
                        "AuthorizeActivityTransition",
                        source,
                        reason,
                        "Activity Player lifecycle admission is not ready to authorize transition.");
            }

            return _activityPlayerLifecycleAdmissionRuntime
                .TryAuthorizeTransition(
                    preparation.CurrentSnapshot.Token,
                    source,
                    reason);
        }

        private ActivityActivationGateResult
            CommitActivityPlayerLifecycleAdmission(
                ActivityPlayerLifecycleAdmissionResult authorization,
                string source,
                string reason)
        {
            if (authorization == null)
            {
                return ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Activity Player lifecycle admission authorization is missing.");
            }

            if (!_routeLifecycleRuntime
                    .TryCreatePendingActivityTransitionPreparationContext(
                        out ActivityTransitionPreparationContext
                            placementContext))
            {
                return ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Target Activity scene composition completed without a valid pre-commit Activity occurrence/discovery context.");
            }

            if (_activityPlayerLifecycleAdmissionRuntime != null &&
                !_activityPlayerLifecycleAdmissionRuntime
                    .TryConfigureRelocationContext(
                        placementContext,
                        source,
                        reason,
                        out string placementIssue))
            {
                return ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Activity Player relocation context was rejected. " +
                    placementIssue);
            }

            if (authorization.NotRequired)
            {
                return ActivityActivationGateResult.Allowed(
                    source,
                    reason,
                    authorization.Message);
            }

            if (!authorization.ReadyForTransition ||
                authorization.CurrentSnapshot == null ||
                !authorization.CurrentSnapshot.Token.IsValid ||
                _activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Activity Player lifecycle admission is not available at the activation boundary.");
            }

            ActivityPlayerLifecycleAdmissionResult commit =
                _activityPlayerLifecycleAdmissionRuntime.TryCommit(
                    authorization.CurrentSnapshot.Token,
                    placementContext,
                    source,
                    reason);
            return commit != null && commit.CanActivate
                ? ActivityActivationGateResult.Allowed(
                    source,
                    reason,
                    commit.ToDiagnosticString())
                : ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    commit != null
                        ? commit.ToDiagnosticString()
                        : "Activity Player lifecycle admission Commit returned no result.");
        }

        private ActivityActivationGateResult
            ConfigureActivityPlayerRelocation(
                string source,
                string reason)
        {
            if (!_routeLifecycleRuntime
                    .TryCreatePendingActivityTransitionPreparationContext(
                        out ActivityTransitionPreparationContext context))
            {
                return ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Target Activity has no valid pre-commit occurrence/discovery context for relocation.");
            }

            if (_activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityActivationGateResult.Allowed(
                    source,
                    reason,
                    "Player lifecycle runtime is absent; no Activity Player relocation authority is composed.");
            }

            return _activityPlayerLifecycleAdmissionRuntime
                .TryConfigureRelocationContext(
                    context,
                    source,
                    reason,
                    out string issue)
                ? ActivityActivationGateResult.Allowed(
                    source,
                    reason,
                    "Activity Player relocation context configured for target occurrence.")
                : ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Activity Player relocation context configuration failed. " + issue);
        }

        private void RollbackPendingActivityPlayerLifecycleAdmission(
            ActivityPlayerLifecycleAdmissionResult authorization,
            string source,
            string reason)
        {
            if (authorization == null ||
                authorization.NotRequired ||
                authorization.CurrentSnapshot == null ||
                !authorization.CurrentSnapshot.Token.IsValid ||
                _activityPlayerLifecycleAdmissionRuntime == null)
            {
                return;
            }

            ActivityPlayerLifecycleAdmissionSnapshot live =
                _activityPlayerLifecycleAdmissionRuntime.CreateSnapshot();
            if (live == null ||
                live.Token != authorization.CurrentSnapshot.Token ||
                !live.IsRollbackAvailable)
            {
                return;
            }

            _activityPlayerLifecycleAdmissionRuntime.TryRollback(
                live.Token,
                source,
                reason);
        }

        private static bool RequiresGameplayReady(
            ActivityAsset activity)
        {
            return activity != null &&
                activity.HasDefinedPlayerParticipationRequirementLevel &&
                activity.PlayerParticipationRequirementLevel ==
                    PlayerParticipationRequirementLevel.GameplayReady;
        }
    }
}
