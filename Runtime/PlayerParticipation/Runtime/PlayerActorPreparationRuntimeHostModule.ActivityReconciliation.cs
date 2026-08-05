namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private PlayerActivityReconciliationRuntimeHostModule
            activityReconciliationRuntime;

        /// <summary>
        /// Observes only committed Session snapshots. A revision produced by
        /// default Actor selection during reconcile is handled on the next
        /// LateUpdate pass, preventing recursive reconciliation.
        /// </summary>
        private void LateUpdate()
        {
            if (shuttingDown ||
                !IsReady ||
                participationContext == null ||
                activityLifecycleParticipant == null)
            {
                return;
            }

            if (activityReconciliationRuntime == null)
            {
                activityReconciliationRuntime =
                    new PlayerActivityReconciliationRuntimeHostModule();
            }

            activityReconciliationRuntime.ObserveAndReconcile(
                participationContext.CreateSnapshot(),
                activityLifecycleParticipant,
                nameof(PlayerActorPreparationRuntimeHostModule),
                "stable-session-revision-or-activity-occurrence");
        }

        internal bool TryGetActivityReconciliationSnapshot(
            out PlayerActivityReconciliationRuntimeHostSnapshot snapshot)
        {
            if (activityReconciliationRuntime == null)
            {
                snapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "Player Activity reconciliation has not observed the runtime yet.");
                return false;
            }

            snapshot = activityReconciliationRuntime.LastSnapshot;
            return true;
        }
    }

    internal static class
        FrameworkRuntimeHostPlayerActivityReconciliationExtensions
    {
        internal static bool TryGetPlayerActivityReconciliationSnapshot(
            this Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost
                runtimeHost,
            out PlayerActivityReconciliationRuntimeHostSnapshot snapshot)
        {
            if (runtimeHost == null ||
                !runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparation))
            {
                snapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "FrameworkRuntimeHost has no ready Player Actor preparation runtime.");
                return false;
            }

            return preparation.TryGetActivityReconciliationSnapshot(
                out snapshot);
        }
    }
}
