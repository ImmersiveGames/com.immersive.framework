using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

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
                LocalPlayerHostAuthoring sessionPhysicalHost,
                PlayerActorDeclaration sceneLogicalPlayerActor,
                SceneLocalPlayerAdmissionToken sceneAdmissionToken,
                PlayerSlotAssignmentSnapshot assignment,
                PlayerSlotAssignmentToken evidenceAssignmentToken,
                PlayerHostBindingIdentity evidenceHostBindingIdentity)
            {
                LeaveToken = leaveToken;
                Authoring = authoring;
                Host = host;
                SessionPhysicalHost = sessionPhysicalHost;
                SceneLogicalPlayerActor = sceneLogicalPlayerActor;
                SceneAdmissionToken = sceneAdmissionToken;
                Assignment = assignment;
                EvidenceAssignmentToken = evidenceAssignmentToken;
                EvidenceHostBindingIdentity = evidenceHostBindingIdentity;
            }

            internal SessionPlayerLeaveToken LeaveToken { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal LocalPlayerHostAuthoring SessionPhysicalHost { get; }
            internal PlayerActorDeclaration SceneLogicalPlayerActor { get; }
            internal SceneLocalPlayerAdmissionToken SceneAdmissionToken { get; }
            internal PlayerSlotAssignmentSnapshot Assignment { get; }
            internal PlayerSlotAssignmentToken EvidenceAssignmentToken { get; }
            internal PlayerHostBindingIdentity EvidenceHostBindingIdentity { get; }
            internal PlayerHostEvidenceResult HostEvidenceRelease { get; set; }
            internal PlayerSlotAssignmentResult AssignmentResult { get; set; }
            internal bool HostEvidenceReleased { get; set; }
            internal bool HostAdmissionReleased { get; set; }
            internal bool SessionPhysicalHostReleased { get; set; }
            internal bool AssignmentReleased { get; set; }
            internal bool ContextualRecordReleased { get; set; }

            internal bool IsComplete =>
                HostEvidenceReleased &&
                HostAdmissionReleased &&
                SessionPhysicalHostReleased &&
                AssignmentReleased &&
                ContextualRecordReleased;
        }

        private readonly Dictionary<
            SessionPlayerLeaveToken,
            SceneProvidedSessionPlayerLeaveProgress>
            _sceneProvidedSessionPlayerLeaveProgress = new();

        /// <summary>
        /// Releases Framework authority associated with the exact Scene-Provided occurrence owned
        /// by one active Session Player Leave. For a successfully adopted occurrence, the
        /// canonical preparation release has already released the Session-owned physical
        /// composition before this contextual-record cleanup.
        /// </summary>
        internal SceneProvidedSessionPlayerLeaveReleaseResult
            TryReleaseSceneProvidedPlayerForSessionLeave(
                SessionPlayerLeaveToken leaveToken,
                LocalPlayerHostAuthoring sessionPhysicalHost,
                string source,
                string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(SceneLocalPlayerAdmissionRuntimeHostModule));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "scene-provided-session-player-leave-release");

            if (!IsReady || _hostEvidenceOwner == null)
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

            if (ReferenceEquals(sessionPhysicalHost, null))
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
                    "Scene-Provided terminal release requires the Session-captured physical Host reference.");
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                _participationContext.TryConfirmSessionPlayerLeave(
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

            if (!_participationContext.TryGetEffectiveHostProvisioningMode(
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

            if (_sceneProvidedSessionPlayerLeaveProgress.TryGetValue(
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

            SceneProvidedSessionPlayerLeaveProgress progress = existingProgress;
            if (progress == null)
            {
                if (!_runtime.TryGetSessionPlayerLeaveRepresentation(
                        leaveToken.PlayerSlotId,
                        out SceneLocalPlayerAdmissionAuthoring authoring,
                        out LocalPlayerHostAuthoring host,
                        out SceneLocalPlayerAdmissionToken sceneAdmissionToken,
                        out PlayerSlotAssignmentSnapshot assignment))
                {
                    bool hasAssignment = _participationContext.TryGetCurrentAssignment(
                        leaveToken.PlayerSlotId,
                        out PlayerSlotAssignmentSnapshot residualAssignment) &&
                        residualAssignment.IsAssigned;
                    bool hasHostEvidence = _hostEvidenceOwner.TryGetRetainedHostEvidence(
                        leaveToken.PlayerSlotId,
                        out PlayerHostEvidenceSnapshot residualHostEvidence);
                    // Activity exit deliberately retires its contextual assignment. A retained
                    // physical Host with no current contextual assignment is therefore the valid
                    // Session-owned residue that terminal Leave must clear.
                    if (!hasAssignment && hasHostEvidence &&
                        residualHostEvidence.PhysicalProvisioningMode ==
                            PlayerHostProvisioningMode.SceneProvided)
                    {
                        progress = new SceneProvidedSessionPlayerLeaveProgress(
                            leaveToken,
                            null,
                            residualHostEvidence.Host,
                            sessionPhysicalHost,
                            null,
                            default,
                            default,
                            residualHostEvidence.AssignmentToken,
                            residualHostEvidence.HostBindingIdentity)
                        {
                            // The prior Activity exit already retired the contextual projection
                            // and assignment. Stage C is therefore a no-op: terminal Leave must
                            // release only the retained Session physical Host in Stage D.
                            HostEvidenceReleased = true,
                            HostAdmissionReleased = true,
                            AssignmentReleased = true,
                            ContextualRecordReleased = true
                        };
                        _sceneProvidedSessionPlayerLeaveProgress.Add(leaveToken, progress);
                    }
                    else if (hasAssignment || hasHostEvidence)
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

                    if (progress == null)
                    {
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
                }

                if (progress == null && (authoring == null ||
                    host == null ||
                    sceneAdmissionToken.AssignmentToken != assignment.AssignmentToken ||
                    assignment.AssignmentOrigin != PlayerSlotAssignmentOrigin.SceneProvided ||
                    assignment.HostBindingIdentity != sceneAdmissionToken.AssignmentToken.HostBindingIdentity))
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

                if (progress == null)
                {
                    progress = new SceneProvidedSessionPlayerLeaveProgress(
                        leaveToken,
                        authoring,
                        host,
                        sessionPhysicalHost,
                        authoring.SceneLogicalPlayerActor,
                        sceneAdmissionToken,
                        assignment,
                        assignment.AssignmentToken,
                        assignment.HostBindingIdentity);
                    _sceneProvidedSessionPlayerLeaveProgress.Add(leaveToken, progress);
                }
            }

            if (!progress.HostEvidenceReleased)
            {
                PlayerHostEvidenceResult evidenceRelease =
                    !progress.Assignment.IsAssigned || progress.SessionPhysicalHost == null
                    ? _hostEvidenceOwner.ClearDivergentHostEvidence(
                        leaveToken.PlayerSlotId,
                        progress.EvidenceAssignmentToken,
                        progress.EvidenceHostBindingIdentity,
                        progress.SessionPhysicalHost,
                        resolvedSource,
                        resolvedReason + "; physical-host-already-terminally-released")
                    : _hostEvidenceOwner.ReleaseHostEvidence(
                        leaveToken.PlayerSlotId,
                        progress.EvidenceAssignmentToken,
                        progress.EvidenceHostBindingIdentity,
                        progress.SessionPhysicalHost,
                        resolvedSource,
                        resolvedReason);
                progress.HostEvidenceRelease = evidenceRelease;
                if (evidenceRelease == null ||
                    (evidenceRelease.Status != PlayerHostEvidenceStatus.SucceededReleased &&
                     evidenceRelease.Status != PlayerHostEvidenceStatus.SucceededClearedDivergent) ||
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
                    // The physical terminal adapter owns destruction of an adopted Host.
                    // Its absence here is expected after stage D, not evidence of an external
                    // Actor that must be restored or preserved.
                    progress.HostAdmissionReleased = true;
                }
                else if (!progress.Host.TryValidateCommittedAdmissionRelease(
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
                        hostValidationIssue);
                }
                else if (!progress.Host.TryReleaseCommittedAdmission(
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
                        hostReleaseIssue);
                }

                if (!progress.HostAdmissionReleased)
                {
                    progress.HostAdmissionReleased = true;
                }
            }

            if (!progress.SessionPhysicalHostReleased)
            {
                PlayerHostEvidenceResult physicalRelease =
                    _hostEvidenceOwner.ReleaseSessionPhysicalHost(
                        leaveToken.PlayerSlotId,
                        progress.SessionPhysicalHost,
                        resolvedSource,
                        resolvedReason + "; release-session-physical-host");
                if (physicalRelease == null ||
                    physicalRelease.Status != PlayerHostEvidenceStatus.SucceededReleased ||
                    !physicalRelease.PreviousEvidence.HasSessionPhysicalHost ||
                    physicalRelease.CurrentEvidence.HasSessionPhysicalHost)
                {
                    return FromProgress(
                        SceneProvidedSessionPlayerLeaveReleaseStatus.FailedHostEvidenceRelease,
                        progress,
                        leaveConfirmation,
                        resolvedSource,
                        resolvedReason,
                        physicalRelease != null
                            ? "Scene-Provided Session physical Host evidence terminal release failed. " + physicalRelease.ToDiagnosticString()
                            : "Scene-Provided Session physical Host evidence terminal release returned no result.");
                }

                progress.SessionPhysicalHostReleased = true;
            }

            if (!progress.AssignmentReleased)
            {
                if (!progress.Assignment.IsAssigned)
                {
                    progress.AssignmentReleased = true;
                }
                else
                {
                PlayerSlotAssignmentResult assignmentConfirmation =
                    _participationContext.TryConfirmCurrentAssignment(
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
                    _participationContext.ReleaseAssignment(
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
            }

            if (!progress.ContextualRecordReleased)
            {
                if (!_runtime.TryReleaseSessionPlayerLeaveRepresentationRecord(
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
                "Scene-Provided terminal physical Host evidence released after the exact contextual authority was retired; Session membership remains Leaving for terminal cleanup.");
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
