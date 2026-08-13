using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class SceneLocalPlayerAdmissionRuntimeHostModule
    {
        private sealed class SceneProvidedSessionPlayerLeaveProgress
        {
            internal SceneProvidedSessionPlayerLeaveProgress(
                SessionPlayerLeaveToken leaveToken,
                SceneLocalPlayerAdmissionAuthoring authoring,
                LocalPlayerHostAuthoring host,
                PlayerActorDeclaration sceneLogicalPlayerActor,
                SceneLocalPlayerAdmissionToken sceneAdmissionToken,
                PlayerSlotAssignmentSnapshot assignment)
            {
                LeaveToken = leaveToken;
                Authoring = authoring;
                Host = host;
                SceneLogicalPlayerActor = sceneLogicalPlayerActor;
                SceneAdmissionToken = sceneAdmissionToken;
                Assignment = assignment;
            }

            internal SessionPlayerLeaveToken LeaveToken { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration SceneLogicalPlayerActor { get; }
            internal SceneLocalPlayerAdmissionToken SceneAdmissionToken { get; }
            internal PlayerSlotAssignmentSnapshot Assignment { get; }
            internal PlayerHostEvidenceResult HostEvidenceRelease { get; set; }
            internal PlayerSlotAssignmentResult AssignmentResult { get; set; }
            internal bool HostEvidenceReleased { get; set; }
            internal bool HostAdmissionReleased { get; set; }
            internal bool AssignmentReleased { get; set; }
            internal bool ContextualRecordReleased { get; set; }

            internal bool IsComplete =>
                HostEvidenceReleased &&
                HostAdmissionReleased &&
                AssignmentReleased &&
                ContextualRecordReleased;
        }

        private readonly Dictionary<
            SessionPlayerLeaveToken,
            SceneProvidedSessionPlayerLeaveProgress>
            sceneProvidedSessionPlayerLeaveProgress = new();

        /// <summary>
        /// Releases Framework authority associated with the exact Scene-Provided occurrence owned
        /// by one active Session Player Leave. Physical Scene-owned Host, PlayerInput and Actor
        /// objects are deliberately preserved. Slot vacancy and Session Actor-selection cleanup
        /// remain downstream ADR-020 responsibilities.
        /// </summary>
        internal SceneProvidedSessionPlayerLeaveReleaseResult
            TryReleaseSceneProvidedPlayerForSessionLeave(
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(SceneLocalPlayerAdmissionRuntimeHostModule));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "scene-provided-session-player-leave-release");

            if (!IsReady || hostEvidenceOwner == null)
            {
                return Result(
                    SceneProvidedSessionPlayerLeaveReleaseStatus.RejectedRuntimeUnavailable,
                    leaveToken,
                    default,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene-Provided Session Player Leave release requires the ready scoped Scene admission and Host evidence authorities.");
            }

            if (!leaveToken.IsValid)
            {
                return Result(
                    SceneProvidedSessionPlayerLeaveReleaseStatus.RejectedInvalidRequest,
                    leaveToken,
                    default,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    "Scene-Provided Session Player Leave release requires a valid Session Player Leave token.");
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    resolvedSource,
                    resolvedReason);
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                return Result(
                    SceneProvidedSessionPlayerLeaveReleaseStatus.RejectedLeaveCorrelation,
                    leaveToken,
                    default,
                    leaveConfirmation,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    leaveConfirmation != null
                        ? "Scene-Provided resource release rejected because the Leave token no longer owns the exact Leaving occurrence. " +
                          leaveConfirmation.ToDiagnosticString()
                        : "Scene-Provided resource release received no Leave confirmation result.");
            }

            if (!participationContext.TryGetEffectiveHostProvisioningMode(
                    leaveToken.PlayerSlotId,
                    out PlayerHostProvisioningMode provisioningMode) ||
                provisioningMode != PlayerHostProvisioningMode.SceneProvided)
            {
                return Result(
                    SceneProvidedSessionPlayerLeaveReleaseStatus.RejectedProvisioningMode,
                    leaveToken,
                    default,
                    leaveConfirmation,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    false,
                    resolvedSource,
                    resolvedReason,
                    $"Leaving Slot provisioning mode is '{provisioningMode}', not SceneProvided. No ownership fallback was applied.");
            }

            if (sceneProvidedSessionPlayerLeaveProgress.TryGetValue(
                    leaveToken,
                    out SceneProvidedSessionPlayerLeaveProgress existingProgress) &&
                existingProgress.IsComplete)
            {
                return FromProgress(
                    SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededAlreadyReleased,
                    existingProgress,
                    leaveConfirmation,
                    resolvedSource,
                    resolvedReason,
                    "The exact Scene-Provided Framework authority was already released for this active Session Player Leave occurrence.");
            }

            if (hostEvidenceOwner.TryGetRetainedActorEvidence(
                    leaveToken.PlayerSlotId,
                    out PlayerActorCorrelationEvidence actorEvidence))
            {
                return Result(
                    SceneProvidedSessionPlayerLeaveReleaseStatus.RejectedActivityRepresentationActive,
                    leaveToken,
                    existingProgress != null
                        ? existingProgress.SceneAdmissionToken
                        : default,
                    leaveConfirmation,
                    existingProgress?.HostEvidenceRelease,
                    existingProgress?.AssignmentResult,
                    existingProgress?.Authoring,
                    existingProgress?.Host,
                    existingProgress?.SceneLogicalPlayerActor,
                    existingProgress != null && existingProgress.HostEvidenceReleased,
                    existingProgress != null && existingProgress.HostAdmissionReleased,
                    existingProgress != null && existingProgress.AssignmentReleased,
                    existingProgress != null && existingProgress.ContextualRecordReleased,
                    resolvedSource,
                    resolvedReason,
                    "Scene-Provided Session Player resources cannot release while retained Activity Actor evidence remains. " + actorEvidence.ToDiagnosticString());
            }

            SceneProvidedSessionPlayerLeaveProgress progress = existingProgress;
            if (progress == null)
            {
                if (!runtime.TryGetSessionPlayerLeaveRepresentation(
                        leaveToken.PlayerSlotId,
                        out SceneLocalPlayerAdmissionAuthoring authoring,
                        out LocalPlayerHostAuthoring host,
                        out SceneLocalPlayerAdmissionToken sceneAdmissionToken,
                        out PlayerSlotAssignmentSnapshot assignment))
                {
                    bool hasAssignment = participationContext.TryGetCurrentAssignment(
                        leaveToken.PlayerSlotId,
                        out PlayerSlotAssignmentSnapshot residualAssignment) &&
                        residualAssignment.IsAssigned;
                    bool hasHostEvidence = hostEvidenceOwner.TryGetRetainedHostEvidence(
                        leaveToken.PlayerSlotId,
                        out PlayerHostEvidenceSnapshot residualHostEvidence);
                    if (hasAssignment || hasHostEvidence)
                    {
                        return Result(
                            SceneProvidedSessionPlayerLeaveReleaseStatus.FailedInvariant,
                            leaveToken,
                            default,
                            leaveConfirmation,
                            null,
                            null,
                            null,
                            hasHostEvidence ? residualHostEvidence.Host : null,
                            null,
                            false,
                            false,
                            false,
                            false,
                            resolvedSource,
                            resolvedReason,
                            $"No active Scene admission record exists, but authoritative Scene-Provided residue remains. assignment='{(hasAssignment ? residualAssignment.AssignmentToken.StableText : "<none>")}' hostEvidence='{(hasHostEvidence ? residualHostEvidence.AssignmentToken.StableText : "<none>")}'.");
                    }

                    return Result(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededNoCurrentRepresentation,
                        leaveToken,
                        default,
                        leaveConfirmation,
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        true,
                        true,
                        true,
                        resolvedSource,
                        resolvedReason,
                        "The Leaving Scene-Provided Session Player has no current contextual Scene representation or retained Framework association to release.");
                }

                if (authoring == null ||
                    host == null ||
                    sceneAdmissionToken.AssignmentToken != assignment.AssignmentToken ||
                    assignment.AssignmentOrigin != PlayerSlotAssignmentOrigin.SceneProvided ||
                    assignment.HostBindingIdentity != sceneAdmissionToken.AssignmentToken.HostBindingIdentity)
                {
                    return Result(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.RejectedContextualCorrelation,
                        leaveToken,
                        sceneAdmissionToken,
                        leaveConfirmation,
                        null,
                        null,
                        authoring,
                        host,
                        authoring != null ? authoring.SceneLogicalPlayerActor : null,
                        false,
                        false,
                        false,
                        false,
                        resolvedSource,
                        resolvedReason,
                        "Active Scene admission record does not carry one exact SceneProvided assignment/Host binding correlation for this Leaving occurrence.");
                }

                progress = new SceneProvidedSessionPlayerLeaveProgress(
                    leaveToken,
                    authoring,
                    host,
                    authoring.SceneLogicalPlayerActor,
                    sceneAdmissionToken,
                    assignment);
                sceneProvidedSessionPlayerLeaveProgress.Add(leaveToken, progress);
            }

            if (!progress.HostEvidenceReleased)
            {
                PlayerHostEvidenceResult evidenceRelease =
                    hostEvidenceOwner.ReleaseHostEvidence(
                        leaveToken.PlayerSlotId,
                        progress.Assignment.AssignmentToken,
                        progress.Assignment.HostBindingIdentity,
                        progress.Host,
                        resolvedSource,
                        resolvedReason);
                progress.HostEvidenceRelease = evidenceRelease;
                if (evidenceRelease == null ||
                    evidenceRelease.Status != PlayerHostEvidenceStatus.SucceededReleased ||
                    !evidenceRelease.PreviousEvidence.IsRecorded ||
                    evidenceRelease.CurrentEvidence.IsRecorded)
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedHostEvidenceRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        evidenceRelease != null
                            ? "Scene-Provided Host evidence release failed. " + evidenceRelease.ToDiagnosticString()
                            : "Scene-Provided Host evidence release returned no result.");
                }

                progress.HostEvidenceReleased = true;
            }

            if (!progress.HostAdmissionReleased)
            {
                if (ReferenceEquals(progress.Host, null) || progress.Host == null)
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedInvariant,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        "Scene-owned Local Player Host became unavailable before Framework Host admission authority was released. The Framework did not destroy it.");
                }

                if (!progress.Host.TryValidateCommittedAdmissionRelease(
                        leaveToken.PlayerSlotId,
                        out string hostValidationIssue))
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedHostAdmissionRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        "Scene-Provided Local Player Host admission release validation failed. " +
                        hostValidationIssue +
                        " Physical Host/PlayerInput/Actor ownership remains external.");
                }

                if (!progress.Host.TryReleaseCommittedAdmission(
                        leaveToken.PlayerSlotId,
                        resolvedSource,
                        resolvedReason,
                        out string hostReleaseIssue))
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedHostAdmissionRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        "Scene-Provided Local Player Host admission release failed. " +
                        hostReleaseIssue +
                        " Physical Host/PlayerInput/Actor ownership remains external.");
                }

                progress.HostAdmissionReleased = true;
            }

            if (!progress.AssignmentReleased)
            {
                PlayerSlotAssignmentResult assignmentConfirmation =
                    participationContext.TryConfirmCurrentAssignment(
                        leaveToken.PlayerSlotId,
                        progress.Assignment.AssignmentToken,
                        resolvedSource,
                        resolvedReason);
                if (assignmentConfirmation == null ||
                    !assignmentConfirmation.Succeeded ||
                    !assignmentConfirmation.HasCurrentAssignment ||
                    assignmentConfirmation.CurrentAssignment.AssignmentOrigin !=
                        PlayerSlotAssignmentOrigin.SceneProvided ||
                    assignmentConfirmation.CurrentAssignment.HostBindingIdentity !=
                        progress.Assignment.HostBindingIdentity)
                {
                    progress.AssignmentResult = assignmentConfirmation;
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedAssignmentRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        assignmentConfirmation != null
                            ? "Scene-Provided canonical assignment no longer matches the staged Leave correlation. " + assignmentConfirmation.ToDiagnosticString()
                            : "Scene-Provided assignment confirmation returned no result.");
                }

                PlayerSlotAssignmentResult assignmentRelease =
                    participationContext.ReleaseAssignment(
                        leaveToken.PlayerSlotId,
                        progress.Assignment.AssignmentToken,
                        resolvedSource,
                        resolvedReason);
                progress.AssignmentResult = assignmentRelease;
                if (assignmentRelease == null || !assignmentRelease.Succeeded)
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedAssignmentRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        assignmentRelease != null
                            ? "Scene-Provided canonical assignment release failed. " + assignmentRelease.ToDiagnosticString()
                            : "Scene-Provided canonical assignment release returned no result.");
                }

                progress.AssignmentReleased = true;
            }

            if (!progress.ContextualRecordReleased)
            {
                if (!runtime.TryReleaseSessionPlayerLeaveRepresentationRecord(
                        leaveToken,
                        progress.SceneAdmissionToken,
                        resolvedSource,
                        resolvedReason,
                        out string recordIssue))
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedContextualRecordRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        "Scene admission runtime record release failed after prior release steps remained committed. " + recordIssue);
                }

                progress.ContextualRecordReleased = true;
            }

            return FromProgress(
                SceneProvidedSessionPlayerLeaveReleaseStatus.SucceededReleased,
                progress,
                leaveConfirmation,
                resolvedSource,
                resolvedReason,
                "Scene-Provided Framework authority released for the exact Leaving occurrence. Scene-owned Host, PlayerInput and Logical Actor were not destroyed; canonical Slot assignment is released and Session membership remains Leaving for downstream cleanup.");
        }

        private static SceneProvidedSessionPlayerLeaveReleaseResult FromProgress(
            SceneProvidedSessionPlayerLeaveReleaseStatus status,
            SceneProvidedSessionPlayerLeaveProgress progress,
            SessionPlayerLeaveRuntimeResult leaveConfirmation,
            string source,
            string reason,
            string message)
        {
            return Result(
                status,
                progress.LeaveToken,
                progress.SceneAdmissionToken,
                leaveConfirmation,
                progress.HostEvidenceRelease,
                progress.AssignmentResult,
                progress.Authoring,
                progress.Host,
                progress.SceneLogicalPlayerActor,
                progress.HostEvidenceReleased,
                progress.HostAdmissionReleased,
                progress.AssignmentReleased,
                progress.ContextualRecordReleased,
                source,
                reason,
                message);
        }

        private static SceneProvidedSessionPlayerLeaveReleaseResult Result(
            SceneProvidedSessionPlayerLeaveReleaseStatus status,
            SessionPlayerLeaveToken leaveToken,
            SceneLocalPlayerAdmissionToken sceneAdmissionToken,
            SessionPlayerLeaveRuntimeResult leaveConfirmation,
            PlayerHostEvidenceResult hostEvidenceRelease,
            PlayerSlotAssignmentResult assignmentResult,
            SceneLocalPlayerAdmissionAuthoring authoring,
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerActorDeclaration sceneLogicalPlayerActor,
            bool hostEvidenceReleased,
            bool hostAdmissionReleased,
            bool assignmentReleased,
            bool contextualRecordReleased,
            string source,
            string reason,
            string message)
        {
            return new SceneProvidedSessionPlayerLeaveReleaseResult(
                status,
                leaveToken,
                sceneAdmissionToken,
                leaveConfirmation,
                hostEvidenceRelease,
                assignmentResult,
                authoring,
                localPlayerHost,
                sceneLogicalPlayerActor,
                hostEvidenceReleased,
                hostAdmissionReleased,
                assignmentReleased,
                contextualRecordReleased,
                source,
                reason,
                message);
        }
    }
}
