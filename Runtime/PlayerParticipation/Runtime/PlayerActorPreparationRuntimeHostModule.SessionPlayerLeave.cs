namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        /// <summary>
        /// Host-scoped entry point for ADR-020 Activity representation release. The operation is
        /// delegated to the already composed Activity lifecycle authority and never discovers a
        /// Player, Activity or service globally.
        /// </summary>
        internal SessionPlayerActivityRepresentationReleaseResult
            TryReleaseActivityRepresentationForSessionPlayerLeave(
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            if (!IsReady || _activityLifecycleParticipant == null)
            {
                return SessionPlayerActivityRepresentationReleaseResult.RuntimeUnavailable(
                    leaveToken,
                    source,
                    reason,
                    _diagnostic);
            }

            SessionPlayerActivityRepresentationReleaseResult result =
                _activityLifecycleParticipant
                    .TryReleaseActivityRepresentationForSessionPlayerLeave(
                        leaveToken,
                        source,
                        reason);
            _diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Session Player Activity representation release returned no result.";
            return result;
        }
    }
}
