namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private PlayerActivityReconciliationRuntimeHostModule
            _activityReconciliationRuntime;
        private bool _activityReconciliationPending;
        private bool _sessionChangeReconciliationBound;

        /// <summary>
        /// Processes only explicitly queued Session/Activity changes after the
        /// frame's synchronous Player operations have completed. A revision
        /// produced by default Actor selection queues a later pass instead of
        /// recursively reconciling.
        /// </summary>
        private void LateUpdate()
        {
            if (_shuttingDown ||
                !IsReady ||
                _participationContext == null ||
                _activityLifecycleParticipant == null ||
                !_activityReconciliationPending)
            {
                return;
            }

            _activityReconciliationPending = false;
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

        internal void BindActivityReconciliation()
        {
            if (_sessionChangeReconciliationBound ||
                _participationContext == null)
            {
                return;
            }

            _participationContext.Changed += OnSessionChangedForActivity;
            _sessionChangeReconciliationBound = true;
            _activityReconciliationPending = true;
        }

        internal void UnbindActivityReconciliation()
        {
            if (_sessionChangeReconciliationBound &&
                _participationContext != null)
            {
                _participationContext.Changed -= OnSessionChangedForActivity;
            }

            _sessionChangeReconciliationBound = false;
            _activityReconciliationPending = false;
        }

        internal void RequestActiveActivityReconciliation()
        {
            if (!_shuttingDown && IsReady)
            {
                _activityReconciliationPending = true;
            }
        }

        private void OnSessionChangedForActivity(PlayerSessionChange change)
        {
            if (change == null ||
                (change.Kind != PlayerSessionChangeKind.SlotAllocationChanged &&
                 change.Kind != PlayerSessionChangeKind.ActorSelectionChanged))
            {
                return;
            }

            _activityReconciliationPending = true;
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
