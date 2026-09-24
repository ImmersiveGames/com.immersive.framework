using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Canonical product-visible outcome for one exact Session Player Leave occurrence.
    /// Detailed stage evidence remains internal while bounded diagnostics expose what completed.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-020 canonical Session Player Leave orchestration result and bounded diagnostics.")]
    public sealed class SessionPlayerLeaveResult
    {
        internal SessionPlayerLeaveResult(
            SessionPlayerLeaveStatus status,
            SessionPlayerLeaveRequest request,
            PlayerHostProvisioningMode provisioningMode,
            PlayerSlotRuntimeSnapshot slot,
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerLeaveRuntimeResult beginResult,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            PlayerHostEvidenceResult managerHostEvidenceRelease,
            ManagerProvisionedSessionPlayerLeaveReleaseResult managerProvisioningRelease,
            SceneProvidedSessionPlayerLeaveReleaseResult sceneProvisioningRelease,
            SessionPlayerLeaveTerminalResult terminalResult,
            string message)
        {
            Status = status;
            Request = request;
            ProvisioningMode = provisioningMode;
            Slot = slot;
            LeaveCorrelation = leaveToken.IsValid ? leaveToken.StableText : string.Empty;
            BeginResult = beginResult;
            ActivityRelease = activityRelease;
            ManagerHostEvidenceRelease = managerHostEvidenceRelease;
            ManagerProvisioningRelease = managerProvisioningRelease;
            SceneProvisioningRelease = sceneProvisioningRelease;
            TerminalResult = terminalResult;
            Message = message ?? string.Empty;
        }

        public SessionPlayerLeaveStatus Status { get; }
        public SessionPlayerLeaveRequest Request { get; }
        public PlayerHostProvisioningMode ProvisioningMode { get; }
        public PlayerSlotRuntimeSnapshot Slot { get; }
        public string LeaveCorrelation { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            SessionPlayerLeaveStatus.SucceededLeft or
            SessionPlayerLeaveStatus.SucceededAlreadyLeft;

        public bool Failed => Status is
            SessionPlayerLeaveStatus.FailedActivityRepresentationRelease or
            SessionPlayerLeaveStatus.FailedProvisioningRelease or
            SessionPlayerLeaveStatus.FailedTerminalCommit or
            SessionPlayerLeaveStatus.FailedInvariant;

        public bool Rejected => !Succeeded && !Failed && Status != SessionPlayerLeaveStatus.None;
        public bool Completed => Status != SessionPlayerLeaveStatus.None;
        public bool LeaveStarted => BeginResult != null && BeginResult.Succeeded;
        public bool ActivityRepresentationReleased => ActivityRelease != null && ActivityRelease.Succeeded;
        public bool ProvisioningReleased =>
            (ManagerProvisioningRelease != null && ManagerProvisioningRelease.Succeeded) ||
            (SceneProvisioningRelease != null && SceneProvisioningRelease.Succeeded);
        public bool TerminalCommitted => TerminalResult != null && TerminalResult.Succeeded;
        public bool PartialRelease => LeaveStarted && !TerminalCommitted &&
            (ActivityRepresentationReleased || ProvisioningReleased);

        internal SessionPlayerLeaveRuntimeResult BeginResult { get; }
        internal SessionPlayerActivityRepresentationReleaseResult ActivityRelease { get; }
        internal PlayerHostEvidenceResult ManagerHostEvidenceRelease { get; }
        internal ManagerProvisionedSessionPlayerLeaveReleaseResult ManagerProvisioningRelease { get; }
        internal SceneProvidedSessionPlayerLeaveReleaseResult SceneProvisioningRelease { get; }
        internal SessionPlayerLeaveTerminalResult TerminalResult { get; }

        internal static SessionPlayerLeaveResult RuntimeUnavailable(
            SessionPlayerLeaveRequest request,
            string message)
        {
            return new SessionPlayerLeaveResult(
                SessionPlayerLeaveStatus.RejectedRuntimeUnavailable,
                request,
                PlayerHostProvisioningMode.Unspecified,
                default,
                default,
                null,
                null,
                null,
                null,
                null,
                null,
                string.IsNullOrWhiteSpace(message)
                    ? "Session Player Leave runtime is unavailable."
                    : message.Trim());
        }

        public string ToDiagnosticString()
        {
            return
                $"status='{Status}' request=({Request.ToDiagnosticString()}) " +
                $"provisioning='{ProvisioningMode}' leaveCorrelation='{LeaveCorrelation}' " +
                $"slot='{SlotText(Slot)}' leaveStarted='{LeaveStarted}' " +
                $"activityReleased='{ActivityRepresentationReleased}' provisioningReleased='{ProvisioningReleased}' " +
                $"terminalCommitted='{TerminalCommitted}' partialRelease='{PartialRelease}' " +
                $"message='{Message}'";
        }

        private static string SlotText(PlayerSlotRuntimeSnapshot slot)
        {
            return slot.IsValid
                ? $"{slot.PlayerSlotId.StableText}:{slot.AllocationState}:{slot.Revision}:selection-{slot.SelectionRevision}"
                : string.Empty;
        }
    }
}
