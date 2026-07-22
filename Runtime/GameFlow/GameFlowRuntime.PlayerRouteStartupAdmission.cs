using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        private ActivityPlayerLifecycleAdmissionResult
            PrepareRouteStartupPlayerLifecycleAdmission(
                RouteAsset previousRoute,
                RouteAsset targetRoute,
                ActivityAsset previousActivity,
                string source,
                string reason)
        {
            ActivityAsset targetActivity = targetRoute != null &&
                targetRoute.HasStartupActivity
                    ? targetRoute.StartupActivity
                    : null;
            if (!RequiresGameplayReady(targetActivity))
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .NotRequiredResult(
                        "PrepareRouteStartupActivityPlayerAdmission",
                        source,
                        reason,
                        "Target Route Startup Activity does not require GameplayReady.");
            }

            if (activityPlayerLifecycleAdmissionRuntime == null)
            {
                return ActivityPlayerLifecycleAdmissionResult
                    .RejectedRuntimeUnavailable(
                        "PrepareRouteStartupActivityPlayerAdmission",
                        source,
                        reason,
                        "GameplayReady Route Startup Activity requires the official Player Gameplay Admission lifecycle runtime.");
            }

            return activityPlayerLifecycleAdmissionRuntime
                .TryPrepareRouteStartupSwitch(
                    previousRoute,
                    targetRoute,
                    previousActivity,
                    targetActivity,
                    source,
                    reason);
        }

        private bool IsRouteStartupPlayerLifecycleCompleted(
            ActivityPlayerLifecycleAdmissionResult preparation,
            out string issue)
        {
            issue = string.Empty;
            if (preparation == null)
            {
                issue =
                    "Route Startup Activity Player lifecycle preparation returned no result.";
                return false;
            }

            if (preparation.NotRequired)
            {
                return true;
            }

            if (preparation.CurrentSnapshot == null ||
                !preparation.CurrentSnapshot.Token.IsValid ||
                activityPlayerLifecycleAdmissionRuntime == null)
            {
                issue =
                    "Route Startup Activity Player lifecycle preparation has no exact transaction evidence.";
                return false;
            }

            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                activityPlayerLifecycleAdmissionRuntime.CreateSnapshot();
            if (snapshot == null ||
                snapshot.Token != preparation.CurrentSnapshot.Token ||
                !snapshot.IsRouteStartupFlow ||
                !snapshot.IsCompleted)
            {
                issue =
                    "Route Startup Activity Player lifecycle admission did not complete. " +
                    (snapshot != null
                        ? snapshot.ToDiagnosticString()
                        : "No lifecycle snapshot is available.");
                return false;
            }

            return true;
        }
    }
}
