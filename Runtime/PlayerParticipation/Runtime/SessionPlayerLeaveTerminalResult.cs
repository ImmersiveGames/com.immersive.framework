using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal terminal evidence for one exact ADR-020 Session Player Leave occurrence.
    /// This result is intentionally downstream of Activity representation and provisioning-
    /// specific release. It never performs or implies another physical ownership policy.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Session Player Leave terminal cleanup and commit evidence.")]
    internal sealed class SessionPlayerLeaveTerminalResult
    {
        internal SessionPlayerLeaveTerminalResult(
            SessionPlayerLeaveTerminalStatus status,
            SessionPlayerLeaveToken leaveToken,
            PlayerHostProvisioningMode provisioningMode,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            ManagerProvisionedSessionPlayerLeaveReleaseResult managerProvisioningRelease,
            SceneProvidedSessionPlayerLeaveReleaseResult sceneProvisioningRelease,
            PlayerSlotAssignmentResult assignmentRelease,
            SessionPlayerLeaveRuntimeResult actorSelectionCleanup,
            SessionPlayerLeaveRuntimeResult commit,
            bool assignmentReleased,
            bool actorSelectionCleared,
            bool slotAvailable,
            string source,
            string reason,
            string message)
        {
            Status = status;
            LeaveToken = leaveToken;
            ProvisioningMode = provisioningMode;
            ActivityRelease = activityRelease;
            ManagerProvisioningRelease = managerProvisioningRelease;
            SceneProvisioningRelease = sceneProvisioningRelease;
            AssignmentRelease = assignmentRelease;
            ActorSelectionCleanup = actorSelectionCleanup;
            Commit = commit;
            AssignmentReleased = assignmentReleased;
            ActorSelectionCleared = actorSelectionCleared;
            SlotAvailable = slotAvailable;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal SessionPlayerLeaveTerminalStatus Status { get; }
        internal SessionPlayerLeaveToken LeaveToken { get; }
        internal PlayerHostProvisioningMode ProvisioningMode { get; }
        internal SessionPlayerActivityRepresentationReleaseResult ActivityRelease { get; }
        internal ManagerProvisionedSessionPlayerLeaveReleaseResult ManagerProvisioningRelease { get; }
        internal SceneProvidedSessionPlayerLeaveReleaseResult SceneProvisioningRelease { get; }
        internal PlayerSlotAssignmentResult AssignmentRelease { get; }
        internal SessionPlayerLeaveRuntimeResult ActorSelectionCleanup { get; }
        internal SessionPlayerLeaveRuntimeResult Commit { get; }
        internal bool AssignmentReleased { get; }
        internal bool ActorSelectionCleared { get; }
        internal bool SlotAvailable { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }

        internal bool Succeeded => Status is
            SessionPlayerLeaveTerminalStatus.SucceededCommitted or
            SessionPlayerLeaveTerminalStatus.SucceededAlreadyCommitted;

        internal bool StateChanged =>
            Status == SessionPlayerLeaveTerminalStatus.SucceededCommitted;

        internal string ToDiagnosticString()
        {
            return
                $"status='{Status}' leaveToken='{LeaveToken.StableText}' provisioning='{ProvisioningMode}' " +
                $"assignmentReleased='{AssignmentReleased}' actorSelectionCleared='{ActorSelectionCleared}' " +
                $"slotAvailable='{SlotAvailable}' " +
                $"activityRelease='{ActivityText(ActivityRelease)}' " +
                $"managerRelease='{ManagerText(ManagerProvisioningRelease)}' " +
                $"sceneRelease='{SceneText(SceneProvisioningRelease)}' " +
                $"assignmentRelease='{AssignmentText(AssignmentRelease)}' " +
                $"actorSelectionCleanup='{LeaveText(ActorSelectionCleanup)}' " +
                $"commit='{LeaveText(Commit)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        private static string ActivityText(
            SessionPlayerActivityRepresentationReleaseResult result)
        {
            return result != null ? result.ToDiagnosticString() : string.Empty;
        }

        private static string ManagerText(
            ManagerProvisionedSessionPlayerLeaveReleaseResult result)
        {
            return result != null ? result.ToDiagnosticString() : string.Empty;
        }

        private static string SceneText(
            SceneProvidedSessionPlayerLeaveReleaseResult result)
        {
            return result != null ? result.ToDiagnosticString() : string.Empty;
        }

        private static string AssignmentText(PlayerSlotAssignmentResult result)
        {
            return result != null ? result.ToDiagnosticString() : string.Empty;
        }

        private static string LeaveText(SessionPlayerLeaveRuntimeResult result)
        {
            return result != null ? result.ToDiagnosticString() : string.Empty;
        }
    }
}
