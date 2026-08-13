using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class LocalPlayerProvisioningBridge
    {
        private readonly Dictionary<
            SessionPlayerLeaveToken,
            ManagerProvisionedSessionPlayerLeaveReleaseResult>
            completedSessionPlayerLeaveReleases = new();

        /// <summary>
        /// Releases the exact Manager-Provisioned technical Host owned by one active Session
        /// Player Leave occurrence. The caller must first release the retained Host evidence and
        /// pass that exact release result as the handoff proving the projection no longer owns the
        /// physical Host reference. Canonical assignment and logical Slot membership intentionally
        /// remain current for later ADR-020 cleanup stages.
        /// </summary>
        internal ManagerProvisionedSessionPlayerLeaveReleaseResult
            TryReleaseAdmittedPlayerForSessionLeave(
                SessionPlayerLeaveToken leaveToken,
                PlayerHostEvidenceResult hostEvidenceRelease,
                string source,
                string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(LocalPlayerProvisioningBridge));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "manager-provisioned-session-player-leave-release");

            if (disposed)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedRuntimeUnavailable,
                    leaveToken,
                    null,
                    hostEvidenceRelease,
                    null,
                    null,
                    null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Manager-Provisioned Session Player release rejected because the provisioning bridge is disposed.");
            }

            if (!leaveToken.IsValid)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedInvalidRequest,
                    leaveToken,
                    null,
                    hostEvidenceRelease,
                    null,
                    null,
                    null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Manager-Provisioned Session Player release requires a valid Session Player Leave token.");
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    resolvedSource,
                    resolvedReason);
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedLeaveCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    null,
                    null,
                    null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    leaveConfirmation != null
                        ? "Manager-Provisioned resource release rejected because the Leave token no longer owns the exact Leaving occurrence. " +
                          leaveConfirmation.ToDiagnosticString()
                        : "Manager-Provisioned resource release received no Leave confirmation result.");
            }

            if (completedSessionPlayerLeaveReleases.TryGetValue(
                    leaveToken,
                    out ManagerProvisionedSessionPlayerLeaveReleaseResult completed))
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.SucceededAlreadyReleased,
                    leaveToken,
                    leaveConfirmation,
                    completed.HostEvidenceRelease,
                    completed.AssignmentConfirmation,
                    completed.LocalPlayerHost,
                    completed.PlayerInput,
                    hostAdmissionReleased: true,
                    physicalPlayerReleased: true,
                    resolvedSource,
                    resolvedReason,
                    "The exact Manager-Provisioned resources were already released for this active Session Player Leave occurrence.");
            }

            if (hostEvidenceRelease == null ||
                hostEvidenceRelease.Status != PlayerHostEvidenceStatus.SucceededReleased ||
                !hostEvidenceRelease.PreviousEvidence.IsRecorded ||
                hostEvidenceRelease.CurrentEvidence.IsRecorded)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedHostEvidenceRelease,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    null,
                    null,
                    null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Physical Manager-Provisioned release requires exact evidence that the retained Host projection was released first.");
            }

            PlayerHostEvidenceSnapshot releasedEvidence =
                hostEvidenceRelease.PreviousEvidence;
            if (releasedEvidence.PlayerSlotId != leaveToken.PlayerSlotId ||
                releasedEvidence.AssignmentOrigin !=
                    PlayerSlotAssignmentOrigin.ManagerProvisioned ||
                !releasedEvidence.AssignmentToken.IsValid ||
                !releasedEvidence.HostBindingIdentity.IsValid)
            {
                return Result(
                    releasedEvidence.AssignmentOrigin !=
                        PlayerSlotAssignmentOrigin.ManagerProvisioned
                        ? ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedAssignmentOrigin
                        : ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedHostCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    null,
                    releasedEvidence.Host,
                    releasedEvidence.Host != null
                        ? releasedEvidence.Host.PlayerInput
                        : null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Released Host evidence does not identify the exact Manager-Provisioned assignment owned by this Leaving Slot occurrence.");
            }

            PlayerSlotAssignmentResult assignmentConfirmation =
                participationContext.TryConfirmCurrentAssignment(
                    leaveToken.PlayerSlotId,
                    releasedEvidence.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            if (assignmentConfirmation == null ||
                !assignmentConfirmation.Succeeded ||
                !assignmentConfirmation.HasCurrentAssignment)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedAssignmentCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    releasedEvidence.Host,
                    releasedEvidence.Host != null
                        ? releasedEvidence.Host.PlayerInput
                        : null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    assignmentConfirmation != null
                        ? "Manager-Provisioned resource release rejected stale or foreign canonical assignment evidence. " +
                          assignmentConfirmation.ToDiagnosticString()
                        : "Canonical assignment confirmation returned no result.");
            }

            PlayerSlotAssignmentSnapshot currentAssignment =
                assignmentConfirmation.CurrentAssignment;
            if (currentAssignment.AssignmentOrigin !=
                PlayerSlotAssignmentOrigin.ManagerProvisioned)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedAssignmentOrigin,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    releasedEvidence.Host,
                    releasedEvidence.Host != null
                        ? releasedEvidence.Host.PlayerInput
                        : null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    $"Current Slot assignment origin is '{currentAssignment.AssignmentOrigin}', not ManagerProvisioned.");
            }

            if (currentAssignment.AssignmentToken != releasedEvidence.AssignmentToken ||
                currentAssignment.HostBindingIdentity !=
                    releasedEvidence.HostBindingIdentity)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedAssignmentCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    releasedEvidence.Host,
                    releasedEvidence.Host != null
                        ? releasedEvidence.Host.PlayerInput
                        : null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Released Host evidence and current canonical assignment do not carry the same assignment token and Host binding identity.");
            }

            LocalPlayerHostAuthoring host = releasedEvidence.Host;
            if (ReferenceEquals(host, null) || host == null)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.FailedInvariant,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    null,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Released Manager-Provisioned Host evidence no longer references a live technical Host before physical release began.");
            }

            if (host.HasLogicalActor)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedActivityRepresentationActive,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    host.PlayerInput,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Manager-Provisioned technical Host cannot be released while its Actor Mount still contains a Logical Actor representation.");
            }

            PlayerInput playerInput = host.PlayerInput;
            if (ReferenceEquals(playerInput, null) || playerInput == null ||
                !ReferenceEquals(
                    playerInput.GetComponent<LocalPlayerHostAuthoring>(),
                    host))
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedHostCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    playerInput,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Manager-Provisioned Host no longer owns the exact live PlayerInput expected for physical release.");
            }

            if (!admittedPlayers.Contains(playerInput))
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedPlayerNotAdmitted,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    playerInput,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "The exact PlayerInput is not tracked as an admitted Manager-Provisioned Player by this provisioning bridge.");
            }

            if (!(backend is IAdmittedLocalPlayerReleaseBackend releaseBackend))
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedReleaseBackendUnavailable,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    playerInput,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    $"Provisioning backend '{backend.GetType().FullName}' does not implement {nameof(IAdmittedLocalPlayerReleaseBackend)}. RejectPlayer is not a Leave fallback.");
            }

            if (!host.TryValidateCommittedAdmissionRelease(
                    leaveToken.PlayerSlotId,
                    out string hostValidationIssue))
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.RejectedHostCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    playerInput,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Local Player Host admission cannot be released for this Leaving occurrence. " +
                    hostValidationIssue);
            }

            bool hostAdmissionReleased = host.TryReleaseCommittedAdmission(
                leaveToken.PlayerSlotId,
                resolvedSource,
                resolvedReason,
                out string hostReleaseIssue);
            if (!hostAdmissionReleased)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.FailedHostAdmissionRelease,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    playerInput,
                    hostAdmissionReleased: false,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    "Local Player Host admission release failed. " + hostReleaseIssue);
            }

            try
            {
                releaseBackend.ReleaseAdmittedPlayer(
                    playerInput,
                    resolvedSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                return Result(
                    ManagerProvisionedSessionPlayerLeaveReleaseStatus.FailedPhysicalRelease,
                    leaveToken,
                    leaveConfirmation,
                    hostEvidenceRelease,
                    assignmentConfirmation,
                    host,
                    playerInput,
                    hostAdmissionReleased: true,
                    physicalPlayerReleased: false,
                    resolvedSource,
                    resolvedReason,
                    $"Manager-Provisioned physical Player release threw '{exception.GetType().Name}': {exception.Message}");
            }

            admittedPlayers.Remove(playerInput);
            awaitingCallbackConfirmations.Remove(playerInput);

            ManagerProvisionedSessionPlayerLeaveReleaseResult released = Result(
                ManagerProvisionedSessionPlayerLeaveReleaseStatus.SucceededReleased,
                leaveToken,
                leaveConfirmation,
                hostEvidenceRelease,
                assignmentConfirmation,
                host,
                playerInput,
                hostAdmissionReleased: true,
                physicalPlayerReleased: true,
                resolvedSource,
                resolvedReason,
                "Exact Manager-Provisioned Local Player Host admission and physical PlayerInput were released. Canonical assignment and logical Leaving membership remain for downstream Leave cleanup.");
            completedSessionPlayerLeaveReleases.Add(leaveToken, released);
            return released;
        }

        private static ManagerProvisionedSessionPlayerLeaveReleaseResult Result(
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
            return new ManagerProvisionedSessionPlayerLeaveReleaseResult(
                status,
                leaveToken,
                leaveConfirmation,
                hostEvidenceRelease,
                assignmentConfirmation,
                localPlayerHost,
                playerInput,
                hostAdmissionReleased,
                physicalPlayerReleased,
                source,
                reason,
                message);
        }
    }
}
