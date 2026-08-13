using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerParticipationRuntimeContext
    {
        private sealed class SessionPlayerLeaveTerminalProgress
        {
            internal SessionPlayerLeaveTerminalProgress(
                SessionPlayerLeaveToken leaveToken,
                PlayerHostProvisioningMode provisioningMode,
                PlayerSlotAssignmentToken expectedManagerAssignmentToken)
            {
                LeaveToken = leaveToken;
                ProvisioningMode = provisioningMode;
                ExpectedManagerAssignmentToken = expectedManagerAssignmentToken;
            }

            internal SessionPlayerLeaveToken LeaveToken { get; }
            internal PlayerHostProvisioningMode ProvisioningMode { get; }
            internal PlayerSlotAssignmentToken ExpectedManagerAssignmentToken { get; }
            internal PlayerSlotAssignmentResult AssignmentRelease { get; set; }
            internal SessionPlayerLeaveRuntimeResult ActorSelectionCleanup { get; set; }
            internal SessionPlayerLeaveRuntimeResult Commit { get; set; }
            internal bool AssignmentReleased { get; set; }
            internal bool ActorSelectionCleared { get; set; }
            internal bool Completed { get; set; }
        }

        private readonly Dictionary<
            SessionPlayerLeaveToken,
            SessionPlayerLeaveTerminalProgress>
            sessionPlayerLeaveTerminalProgress = new();

        private readonly Dictionary<
            SessionPlayerLeaveToken,
            SessionPlayerLeaveTerminalResult>
            completedSessionPlayerLeaveTerminalResults = new();

        /// <summary>
        /// Finalizes one Manager-Provisioned Session Player Leave after the exact Activity and
        /// provisioning-specific release stages succeeded. The still-current canonical Manager
        /// assignment is released here as Session-owned association cleanup before Actor selection
        /// is cleared and Slot vacancy is committed.
        /// </summary>
        internal SessionPlayerLeaveTerminalResult TryFinalizeSessionPlayerLeave(
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            ManagerProvisionedSessionPlayerLeaveReleaseResult provisioningRelease,
            string source,
            string reason)
        {
            return TryFinalizeSessionPlayerLeaveCore(
                leaveToken,
                activityRelease,
                PlayerHostProvisioningMode.ManagerProvisioned,
                provisioningRelease,
                null,
                source,
                reason);
        }

        /// <summary>
        /// Finalizes one Scene-Provided Session Player Leave after the exact Activity and
        /// Scene-Provided authority release stages succeeded. Scene release must already have
        /// removed the contextual assignment; this terminal stage verifies that invariant before
        /// clearing Session Actor selection and committing Slot vacancy.
        /// </summary>
        internal SessionPlayerLeaveTerminalResult TryFinalizeSessionPlayerLeave(
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            SceneProvidedSessionPlayerLeaveReleaseResult provisioningRelease,
            string source,
            string reason)
        {
            return TryFinalizeSessionPlayerLeaveCore(
                leaveToken,
                activityRelease,
                PlayerHostProvisioningMode.SceneProvided,
                null,
                provisioningRelease,
                source,
                reason);
        }

        private SessionPlayerLeaveTerminalResult TryFinalizeSessionPlayerLeaveCore(
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            PlayerHostProvisioningMode expectedProvisioningMode,
            ManagerProvisionedSessionPlayerLeaveReleaseResult managerProvisioningRelease,
            SceneProvidedSessionPlayerLeaveReleaseResult sceneProvisioningRelease,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "session-player-leave-terminal-cleanup");

            if (!leaveToken.IsValid || !expectedProvisioningMode.IsDefinedMode())
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.RejectedInvalidRequest,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Terminal Session Player Leave requires a valid Leave token and explicit provisioning mode.");
            }

            if (completedSessionPlayerLeaveTerminalResults.TryGetValue(
                    leaveToken,
                    out SessionPlayerLeaveTerminalResult completed))
            {
                SlotRecord currentRecord = FindSlot(leaveToken.PlayerSlotId);
                bool unchangedTerminalSlot =
                    currentRecord != null &&
                    currentRecord.AllocationState == PlayerSlotAllocationState.Available &&
                    completed.Commit != null &&
                    completed.Commit.CurrentSlot.IsValid &&
                    currentRecord.Revision == completed.Commit.CurrentSlot.Revision;

                return unchangedTerminalSlot
                    ? TerminalResult(
                        SessionPlayerLeaveTerminalStatus.SucceededAlreadyCommitted,
                        leaveToken,
                        completed.ProvisioningMode,
                        completed.ActivityRelease,
                        completed.ManagerProvisioningRelease,
                        completed.SceneProvisioningRelease,
                        completed.AssignmentRelease,
                        completed.ActorSelectionCleanup,
                        completed.Commit,
                        true,
                        true,
                        true,
                        resolvedSource,
                        resolvedReason,
                        "The exact Session Player Leave occurrence already committed and the Slot has not been reused or mutated since that terminal commit.")
                    : TerminalResult(
                        SessionPlayerLeaveTerminalStatus.RejectedForeignOrStalePostCommit,
                        leaveToken,
                        completed.ProvisioningMode,
                        completed.ActivityRelease,
                        completed.ManagerProvisioningRelease,
                        completed.SceneProvisioningRelease,
                        completed.AssignmentRelease,
                        completed.ActorSelectionCleanup,
                        completed.Commit,
                        true,
                        true,
                        false,
                        resolvedSource,
                        resolvedReason,
                        "The Leave token belongs to a completed occurrence whose Slot has since changed. A stale terminal retry cannot affect a later Slot occurrence.");
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                TryConfirmSessionPlayerLeave(
                    leaveToken,
                    resolvedSource,
                    resolvedReason);
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.RejectedLeaveCorrelation,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    leaveConfirmation != null
                        ? "Terminal Session Player Leave rejected because the token no longer owns the exact Leaving occurrence. " +
                          leaveConfirmation.ToDiagnosticString()
                        : "Terminal Session Player Leave received no Leave confirmation result.");
            }

            if (activityRelease == null ||
                !activityRelease.Succeeded ||
                activityRelease.LeaveToken != leaveToken)
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.RejectedActivityReleaseEvidence,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Terminal Session Player Leave requires successful Activity representation release evidence for the exact same Leave occurrence.");
            }

            if (!TryGetEffectiveHostProvisioningMode(
                    leaveToken.PlayerSlotId,
                    out PlayerHostProvisioningMode effectiveProvisioningMode) ||
                effectiveProvisioningMode != expectedProvisioningMode)
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.RejectedProvisioningMode,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    $"Terminal Session Player Leave expected provisioning '{expectedProvisioningMode}' but the configured Slot resolves '{effectiveProvisioningMode}'. No provisioning fallback was applied.");
            }

            PlayerSlotAssignmentToken expectedManagerAssignmentToken = default;
            if (expectedProvisioningMode == PlayerHostProvisioningMode.ManagerProvisioned)
            {
                if (!TryValidateManagerProvisioningReleaseEvidence(
                        leaveToken,
                        managerProvisioningRelease,
                        out expectedManagerAssignmentToken,
                        out string managerEvidenceIssue))
                {
                    return TerminalResult(
                        SessionPlayerLeaveTerminalStatus.RejectedProvisioningReleaseEvidence,
                        leaveToken,
                        expectedProvisioningMode,
                        activityRelease,
                        managerProvisioningRelease,
                        null,
                        null,
                        null,
                        null,
                        false,
                        false,
                        false,
                        resolvedSource,
                        resolvedReason,
                        managerEvidenceIssue);
                }
            }
            else
            {
                if (!TryValidateSceneProvisioningReleaseEvidence(
                        leaveToken,
                        sceneProvisioningRelease,
                        out string sceneEvidenceIssue))
                {
                    return TerminalResult(
                        SessionPlayerLeaveTerminalStatus.RejectedProvisioningReleaseEvidence,
                        leaveToken,
                        expectedProvisioningMode,
                        activityRelease,
                        null,
                        sceneProvisioningRelease,
                        null,
                        null,
                        null,
                        false,
                        false,
                        false,
                        resolvedSource,
                        resolvedReason,
                        sceneEvidenceIssue);
                }
            }

            if (!sessionPlayerLeaveTerminalProgress.TryGetValue(
                    leaveToken,
                    out SessionPlayerLeaveTerminalProgress progress))
            {
                progress = new SessionPlayerLeaveTerminalProgress(
                    leaveToken,
                    expectedProvisioningMode,
                    expectedManagerAssignmentToken);
                sessionPlayerLeaveTerminalProgress.Add(leaveToken, progress);
            }
            else if (progress.ProvisioningMode != expectedProvisioningMode ||
                     (expectedProvisioningMode == PlayerHostProvisioningMode.ManagerProvisioned &&
                      progress.ExpectedManagerAssignmentToken != expectedManagerAssignmentToken))
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.RejectedAssignmentCorrelation,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    progress.AssignmentRelease,
                    progress.ActorSelectionCleanup,
                    progress.Commit,
                    progress.AssignmentReleased,
                    progress.ActorSelectionCleared,
                    progress.Completed,
                    resolvedSource,
                    resolvedReason,
                    "Terminal retry evidence does not match the provisioning mode or exact Manager assignment captured by the active Leave occurrence.");
            }

            if (!progress.AssignmentReleased)
            {
                if (expectedProvisioningMode == PlayerHostProvisioningMode.ManagerProvisioned)
                {
                    if (!TryGetCurrentAssignment(
                            leaveToken.PlayerSlotId,
                            out PlayerSlotAssignmentSnapshot currentAssignment) ||
                        !currentAssignment.IsAssigned ||
                        currentAssignment.AssignmentOrigin !=
                            PlayerSlotAssignmentOrigin.ManagerProvisioned ||
                        currentAssignment.AssignmentToken != expectedManagerAssignmentToken ||
                        currentAssignment.HostBindingIdentity !=
                            expectedManagerAssignmentToken.HostBindingIdentity)
                    {
                        return TerminalResult(
                            SessionPlayerLeaveTerminalStatus.RejectedAssignmentCorrelation,
                            leaveToken,
                            expectedProvisioningMode,
                            activityRelease,
                            managerProvisioningRelease,
                            null,
                            progress.AssignmentRelease,
                            progress.ActorSelectionCleanup,
                            progress.Commit,
                            false,
                            progress.ActorSelectionCleared,
                            false,
                            resolvedSource,
                            resolvedReason,
                            "Manager-Provisioned terminal cleanup could not confirm the exact still-current canonical assignment captured before physical resource release.");
                    }

                    PlayerSlotAssignmentResult assignmentRelease = ReleaseAssignment(
                        leaveToken.PlayerSlotId,
                        expectedManagerAssignmentToken,
                        resolvedSource,
                        resolvedReason + "; release-session-assignment");
                    progress.AssignmentRelease = assignmentRelease;
                    if (assignmentRelease == null || !assignmentRelease.Succeeded)
                    {
                        return TerminalResult(
                            SessionPlayerLeaveTerminalStatus.FailedAssignmentRelease,
                            leaveToken,
                            expectedProvisioningMode,
                            activityRelease,
                            managerProvisioningRelease,
                            null,
                            assignmentRelease,
                            progress.ActorSelectionCleanup,
                            progress.Commit,
                            false,
                            progress.ActorSelectionCleared,
                            false,
                            resolvedSource,
                            resolvedReason,
                            assignmentRelease != null
                                ? assignmentRelease.ToDiagnosticString()
                                : "Manager-Provisioned canonical assignment release returned no result.");
                    }

                    progress.AssignmentReleased = true;
                }
                else
                {
                    if (TryGetCurrentAssignment(
                            leaveToken.PlayerSlotId,
                            out PlayerSlotAssignmentSnapshot residualSceneAssignment) &&
                        residualSceneAssignment.IsAssigned)
                    {
                        return TerminalResult(
                            SessionPlayerLeaveTerminalStatus.FailedInvariant,
                            leaveToken,
                            expectedProvisioningMode,
                            activityRelease,
                            null,
                            sceneProvisioningRelease,
                            progress.AssignmentRelease,
                            progress.ActorSelectionCleanup,
                            progress.Commit,
                            false,
                            progress.ActorSelectionCleared,
                            false,
                            resolvedSource,
                            resolvedReason,
                            $"Scene-Provided release reported success but canonical assignment '{residualSceneAssignment.AssignmentToken.StableText}' still remains current.");
                    }

                    // C owns Scene-Provided contextual assignment release. E only verifies the
                    // absence before terminal Session cleanup.
                    progress.AssignmentReleased = true;
                }
            }

            if (!progress.ActorSelectionCleared)
            {
                SessionPlayerLeaveRuntimeResult actorSelectionCleanup =
                    TryClearActorSelectionForSessionPlayerLeave(
                        leaveToken,
                        resolvedSource,
                        resolvedReason + "; clear-session-actor-selection");
                progress.ActorSelectionCleanup = actorSelectionCleanup;
                if (actorSelectionCleanup == null || !actorSelectionCleanup.Succeeded)
                {
                    return TerminalResult(
                        SessionPlayerLeaveTerminalStatus.FailedActorSelectionCleanup,
                        leaveToken,
                        expectedProvisioningMode,
                        activityRelease,
                        managerProvisioningRelease,
                        sceneProvisioningRelease,
                        progress.AssignmentRelease,
                        actorSelectionCleanup,
                        progress.Commit,
                        progress.AssignmentReleased,
                        false,
                        false,
                        resolvedSource,
                        resolvedReason,
                        actorSelectionCleanup != null
                            ? actorSelectionCleanup.ToDiagnosticString()
                            : "Session-scoped Actor selection cleanup returned no result.");
                }

                progress.ActorSelectionCleared = true;
            }

            if (TryGetCurrentAssignment(
                    leaveToken.PlayerSlotId,
                    out PlayerSlotAssignmentSnapshot residualAssignment) &&
                residualAssignment.IsAssigned)
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.FailedInvariant,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    progress.AssignmentRelease,
                    progress.ActorSelectionCleanup,
                    progress.Commit,
                    progress.AssignmentReleased,
                    progress.ActorSelectionCleared,
                    false,
                    resolvedSource,
                    resolvedReason,
                    $"Terminal Session Player Leave cannot commit while canonical assignment '{residualAssignment.AssignmentToken.StableText}' remains current.");
            }

            SessionPlayerLeaveRuntimeResult commit = TryCommitSessionPlayerLeave(
                leaveToken,
                resolvedSource,
                resolvedReason + "; commit-slot-available");
            progress.Commit = commit;
            if (commit == null ||
                !commit.Succeeded ||
                commit.Status != SessionPlayerLeaveRuntimeStatus.SucceededCommitted ||
                !commit.CurrentSlot.IsValid ||
                commit.CurrentSlot.AllocationState != PlayerSlotAllocationState.Available)
            {
                return TerminalResult(
                    SessionPlayerLeaveTerminalStatus.FailedCommit,
                    leaveToken,
                    expectedProvisioningMode,
                    activityRelease,
                    managerProvisioningRelease,
                    sceneProvisioningRelease,
                    progress.AssignmentRelease,
                    progress.ActorSelectionCleanup,
                    commit,
                    progress.AssignmentReleased,
                    progress.ActorSelectionCleared,
                    false,
                    resolvedSource,
                    resolvedReason,
                    commit != null
                        ? commit.ToDiagnosticString()
                        : "Terminal Session Player Leave commit returned no result.");
            }

            progress.Completed = true;
            SessionPlayerLeaveTerminalResult terminal = TerminalResult(
                SessionPlayerLeaveTerminalStatus.SucceededCommitted,
                leaveToken,
                expectedProvisioningMode,
                activityRelease,
                managerProvisioningRelease,
                sceneProvisioningRelease,
                progress.AssignmentRelease,
                progress.ActorSelectionCleanup,
                commit,
                true,
                true,
                true,
                resolvedSource,
                resolvedReason,
                "All required pre-commit release evidence was accepted. Session-scoped assignment and Actor selection are clear; the exact Session Player occurrence ended and its Slot committed Available.");
            completedSessionPlayerLeaveTerminalResults.Add(leaveToken, terminal);
            return terminal;
        }

        private static bool TryValidateManagerProvisioningReleaseEvidence(
            SessionPlayerLeaveToken leaveToken,
            ManagerProvisionedSessionPlayerLeaveReleaseResult release,
            out PlayerSlotAssignmentToken assignmentToken,
            out string issue)
        {
            assignmentToken = default;
            if (release == null ||
                !release.Succeeded ||
                release.LeaveToken != leaveToken ||
                !release.HostAdmissionReleased ||
                !release.PhysicalPlayerReleased ||
                release.AssignmentConfirmation == null ||
                !release.AssignmentConfirmation.Succeeded ||
                !release.AssignmentConfirmation.HasCurrentAssignment)
            {
                issue =
                    "Terminal Manager-Provisioned Leave requires successful physical/Host release evidence and the exact canonical assignment confirmation captured for the same Leave token.";
                return false;
            }

            PlayerSlotAssignmentSnapshot assignment =
                release.AssignmentConfirmation.CurrentAssignment;
            if (!assignment.IsAssigned ||
                assignment.PlayerSlotId != leaveToken.PlayerSlotId ||
                assignment.AssignmentOrigin !=
                    PlayerSlotAssignmentOrigin.ManagerProvisioned ||
                !assignment.AssignmentToken.IsValid ||
                !assignment.HostBindingIdentity.IsValid ||
                assignment.AssignmentToken.HostBindingIdentity !=
                    assignment.HostBindingIdentity)
            {
                issue =
                    "Manager-Provisioned release evidence does not carry one exact valid Manager assignment/Host binding correlation for the Leaving Slot occurrence.";
                return false;
            }

            assignmentToken = assignment.AssignmentToken;
            issue = string.Empty;
            return true;
        }

        private static bool TryValidateSceneProvisioningReleaseEvidence(
            SessionPlayerLeaveToken leaveToken,
            SceneProvidedSessionPlayerLeaveReleaseResult release,
            out string issue)
        {
            if (release == null ||
                !release.Succeeded ||
                release.LeaveToken != leaveToken ||
                !release.HostEvidenceReleased ||
                !release.HostAdmissionReleased ||
                !release.AssignmentReleased ||
                !release.ContextualRecordReleased)
            {
                issue =
                    "Terminal Scene-Provided Leave requires successful Framework authority release evidence for the exact same Leave token, including Host evidence/admission, assignment and contextual record cleanup.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static SessionPlayerLeaveTerminalResult TerminalResult(
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
            return new SessionPlayerLeaveTerminalResult(
                status,
                leaveToken,
                provisioningMode,
                activityRelease,
                managerProvisioningRelease,
                sceneProvisioningRelease,
                assignmentRelease,
                actorSelectionCleanup,
                commit,
                assignmentReleased,
                actorSelectionCleared,
                slotAvailable,
                source,
                reason,
                message);
        }
    }
}
