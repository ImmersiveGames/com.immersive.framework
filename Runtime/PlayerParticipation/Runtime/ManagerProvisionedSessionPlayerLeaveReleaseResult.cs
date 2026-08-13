using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal evidence for release of one Manager-Provisioned Session-owned technical Host.
    /// The result never means that logical Session membership has become Available; terminal
    /// membership commit remains owned by the Session Player Leave authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Manager-Provisioned Session Player resource release result and diagnostics.")]
    internal sealed class ManagerProvisionedSessionPlayerLeaveReleaseResult
    {
        internal ManagerProvisionedSessionPlayerLeaveReleaseResult(
            ManagerProvisionedSessionPlayerLeaveReleaseStatus status,
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerLeaveRuntimeResult leaveConfirmation,
            PlayerHostEvidenceResult hostEvidenceRelease,
            PlayerSlotAssignmentResult assignmentConfirmation,
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerInput playerInput,
            bool hostAdmissionReleased,
            bool physicalPlayerReleased,
            string source,
            string reason,
            string message)
        {
            Status = status;
            LeaveToken = leaveToken;
            LeaveConfirmation = leaveConfirmation;
            HostEvidenceRelease = hostEvidenceRelease;
            AssignmentConfirmation = assignmentConfirmation;
            LocalPlayerHost = localPlayerHost;
            PlayerInput = playerInput;
            HostAdmissionReleased = hostAdmissionReleased;
            PhysicalPlayerReleased = physicalPlayerReleased;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal ManagerProvisionedSessionPlayerLeaveReleaseStatus Status { get; }
        internal SessionPlayerLeaveToken LeaveToken { get; }
        internal SessionPlayerLeaveRuntimeResult LeaveConfirmation { get; }
        internal PlayerHostEvidenceResult HostEvidenceRelease { get; }
        internal PlayerSlotAssignmentResult AssignmentConfirmation { get; }
        internal LocalPlayerHostAuthoring LocalPlayerHost { get; }
        internal PlayerInput PlayerInput { get; }
        internal bool HostAdmissionReleased { get; }
        internal bool PhysicalPlayerReleased { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }

        internal bool Succeeded => Status is
            ManagerProvisionedSessionPlayerLeaveReleaseStatus.SucceededReleased or
            ManagerProvisionedSessionPlayerLeaveReleaseStatus.SucceededAlreadyReleased;

        internal bool StateChanged =>
            Status == ManagerProvisionedSessionPlayerLeaveReleaseStatus.SucceededReleased;

        internal string ToDiagnosticString()
        {
            return $"status='{Status}' leaveToken='{LeaveToken.StableText}' " +
                $"host='{UnityObjectText(LocalPlayerHost)}' playerInput='{UnityObjectText(PlayerInput)}' " +
                $"hostAdmissionReleased='{HostAdmissionReleased}' physicalPlayerReleased='{PhysicalPlayerReleased}' " +
                $"assignment='{AssignmentText(AssignmentConfirmation)}' hostEvidence='{HostEvidenceText(HostEvidenceRelease)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        private static string UnityObjectText(Object value)
        {
            if (object.ReferenceEquals(value, null))
            {
                return "<clr-null>";
            }

            return value == null ? "<destroyed>" : value.name;
        }

        private static string AssignmentText(PlayerSlotAssignmentResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : string.Empty;
        }

        private static string HostEvidenceText(PlayerHostEvidenceResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : string.Empty;
        }
    }
}
