using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        private IActivityPlayerLifecycleAdmissionRuntime
            activityPlayerLifecycleAdmissionRuntime;

        internal void SetActivityPlayerLifecycleAdmissionRuntime(
            IActivityPlayerLifecycleAdmissionRuntime runtime)
        {
            activityPlayerLifecycleAdmissionRuntime = runtime;
        }

        private ActivityPlayerLifecycleAdmissionResult
            PrepareActivityPlayerLifecycleAdmission(
                ActivityAsset previousActivity,
                ActivityAsset targetActivity,
                string source,
                string reason)
        {
            if (!RequiresGameplayReady(targetActivity))
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .NotRequiredResult(
                        "PrepareSameRouteActivityPlayerAdmission",
                        source,
                        reason,
                        "Target Activity does not require GameplayReady.");
            }

            if (activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .RejectedRuntimeUnavailable(
                        "PrepareSameRouteActivityPlayerAdmission",
                        source,
                        reason,
                        "GameplayReady Activity switch requires the official P3K.7G Player lifecycle admission runtime.");
            }

            return activityPlayerLifecycleAdmissionRuntime
                .TryPrepareSameRouteSwitch(
                    previousActivity,
                    targetActivity,
                    source,
                    reason);
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
                activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .RejectedRuntimeUnavailable(
                        "AuthorizeActivityTransition",
                        source,
                        reason,
                        "Activity Player lifecycle admission is not ready to authorize transition.");
            }

            return activityPlayerLifecycleAdmissionRuntime
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
                activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityActivationGateResult.Blocked(
                    source,
                    reason,
                    "Activity Player lifecycle admission is not available at the activation boundary.");
            }

            ActivityPlayerLifecycleAdmissionResult commit =
                activityPlayerLifecycleAdmissionRuntime.TryCommit(
                    authorization.CurrentSnapshot.Token,
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

        private void RollbackPendingActivityPlayerLifecycleAdmission(
            ActivityPlayerLifecycleAdmissionResult authorization,
            string source,
            string reason)
        {
            if (authorization == null ||
                authorization.NotRequired ||
                authorization.CurrentSnapshot == null ||
                !authorization.CurrentSnapshot.Token.IsValid ||
                activityPlayerLifecycleAdmissionRuntime == null)
            {
                return;
            }

            ActivityPlayerLifecycleAdmissionSnapshot live =
                activityPlayerLifecycleAdmissionRuntime.CreateSnapshot();
            if (live == null ||
                live.Token != authorization.CurrentSnapshot.Token ||
                !live.IsRollbackAvailable)
            {
                return;
            }

            activityPlayerLifecycleAdmissionRuntime.TryRollback(
                live.Token,
                source,
                reason);
        }

        private static bool RequiresGameplayReady(
            ActivityAsset activity)
        {
            return activity != null &&
                activity.PlayerParticipationRequirementsProfile != null &&
                activity.PlayerParticipationRequirementsProfile
                    .HasDefinedRequirementLevel &&
                activity.PlayerParticipationRequirementsProfile
                    .RequirementLevel ==
                    PlayerParticipationRequirementLevel.GameplayReady;
        }
    }
}
