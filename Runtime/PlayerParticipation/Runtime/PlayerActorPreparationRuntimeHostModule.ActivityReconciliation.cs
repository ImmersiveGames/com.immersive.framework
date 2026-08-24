namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private PlayerActivityReconciliationRuntimeHostModule
            _activityReconciliationRuntime;

        /// <summary>
        /// Observes only committed Session snapshots. A revision produced by
        /// default Actor selection during reconcile is handled on the next
        /// LateUpdate pass, preventing recursive reconciliation.
        /// </summary>
        private void LateUpdate()
        {
            if (_shuttingDown ||
                !IsReady ||
                _participationContext == null ||
                _activityLifecycleParticipant == null)
            {
                return;
            }

            if (_activityReconciliationRuntime == null)
            {
                _activityReconciliationRuntime =
                    new PlayerActivityReconciliationRuntimeHostModule();
            }

            _activityReconciliationRuntime.ObserveAndReconcile(
                _participationContext.CreateSnapshot(),
                _activityLifecycleParticipant,
                nameof(PlayerActorPreparationRuntimeHostModule),
                "stable-session-revision-or-activity-occurrence");
        }

        internal bool TryGetActivityReconciliationSnapshot(
            out PlayerActivityReconciliationRuntimeHostSnapshot snapshot)
        {
            if (_activityReconciliationRuntime == null)
            {
                snapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "Player Activity reconciliation has not observed the runtime yet.");
                return false;
            }

            snapshot = _activityReconciliationRuntime.LastSnapshot;
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
