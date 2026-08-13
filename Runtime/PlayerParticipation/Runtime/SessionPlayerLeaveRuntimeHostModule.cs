using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// FrameworkRuntimeHost-scoped composition authority for one canonical ADR-020 Session
    /// Player Leave operation. Domain state remains in the existing Session, Activity,
    /// provisioning and terminal authorities; this module only orders their typed operations.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 canonical host-scoped Session Player Leave orchestration.")]
    internal sealed class SessionPlayerLeaveRuntimeHostModule : MonoBehaviour
    {
        private sealed class ExecutionRecord
        {
            internal ExecutionRecord(
                SessionPlayerLeaveRequest request,
                SessionPlayerLeaveToken leaveToken,
                PlayerHostProvisioningMode provisioningMode,
                SessionPlayerLeaveRuntimeResult beginResult)
            {
                Request = request;
                LeaveToken = leaveToken;
                ProvisioningMode = provisioningMode;
                BeginResult = beginResult;
            }

            internal SessionPlayerLeaveRequest Request { get; }
            internal SessionPlayerLeaveToken LeaveToken { get; }
            internal PlayerHostProvisioningMode ProvisioningMode { get; }
            internal SessionPlayerLeaveRuntimeResult BeginResult { get; set; }
            internal SessionPlayerActivityRepresentationReleaseResult ActivityRelease { get; set; }
            internal PlayerHostEvidenceResult ManagerHostEvidenceRelease { get; set; }
            internal ManagerProvisionedSessionPlayerLeaveReleaseResult ManagerRelease { get; set; }
            internal SceneProvidedSessionPlayerLeaveReleaseResult SceneRelease { get; set; }
            internal SessionPlayerLeaveTerminalResult TerminalResult { get; set; }
            internal bool Completed { get; set; }

            internal bool Matches(SessionPlayerLeaveRequest request)
            {
                return Request.PlayerSlotId == request.PlayerSlotId &&
                    Request.ExpectedOccurrenceRevision ==
                        request.ExpectedOccurrenceRevision;
            }
        }

        private FrameworkRuntimeHost runtimeHost;
        private PlayerParticipationRuntimeContext participationContext;
        private PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly Dictionary<PlayerSlotId, ExecutionRecord> latestBySlot = new();
        private string diagnostic = "Session Player Leave runtime is not initialized.";
        private int requestCount;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            preparationModule != null &&
            preparationModule.IsReady;

        internal string Diagnostic => diagnostic;
        internal int RequestCount => requestCount;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            out SessionPlayerLeaveRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;
            if (runtimeHost == null)
            {
                issue = "Session Player Leave requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            module = runtimeHost.GetComponent<SessionPlayerLeaveRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<SessionPlayerLeaveRuntimeHostModule>();
            }

            return module.TryInitialize(runtimeHost, out issue);
        }

        internal bool TryInitialize(
            FrameworkRuntimeHost targetRuntimeHost,
            out string issue)
        {
            issue = string.Empty;
            if (IsReady)
            {
                if (ReferenceEquals(runtimeHost, targetRuntimeHost))
                {
                    return true;
                }

                issue = "Session Player Leave runtime is already bound to another FrameworkRuntimeHost.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext targetParticipation))
            {
                issue = "FrameworkRuntimeHost has no initialized Session Player participation authority.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule targetPreparation))
            {
                issue = "FrameworkRuntimeHost has no ready Player Actor preparation authority.";
                diagnostic = issue;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipation;
            preparationModule = targetPreparation;
            requestCount = 0;
            diagnostic = "Session Player Leave runtime is ready.";
            return true;
        }

        internal SessionPlayerLeaveResult TryLeave(SessionPlayerLeaveRequest request)
        {
            requestCount++;
            if (!IsReady)
            {
                return Publish(SessionPlayerLeaveResult.RuntimeUnavailable(request, diagnostic));
            }

            if (!request.TryValidate(out string requestIssue))
            {
                return Publish(Result(
                    SessionPlayerLeaveStatus.RejectedInvalidRequest,
                    request,
                    PlayerHostProvisioningMode.Unspecified,
                    default,
                    (SessionPlayerLeaveRuntimeResult)null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    requestIssue));
            }

            if (latestBySlot.TryGetValue(
                    request.PlayerSlotId,
                    out ExecutionRecord latest) &&
                latest.Matches(request) &&
                latest.Completed)
            {
                return Publish(RepeatCompleted(latest));
            }

            PlayerParticipationSnapshot before = participationContext.CreateSnapshot();
            if (before == null || !before.IsInitialized)
            {
                return Publish(SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    "Session Player participation snapshot is unavailable."));
            }

            if (!TryFindSlot(before, request.PlayerSlotId, out PlayerSlotRuntimeSnapshot slot))
            {
                return Publish(Result(
                    SessionPlayerLeaveStatus.RejectedSlotNotConfigured,
                    request,
                    PlayerHostProvisioningMode.Unspecified,
                    default,
                    (SessionPlayerLeaveRuntimeResult)null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "Target Player Slot is not configured in the current Session."));
            }

            if (slot.AllocationState != PlayerSlotAllocationState.Joined &&
                slot.AllocationState != PlayerSlotAllocationState.Leaving)
            {
                SessionPlayerLeaveStatus status =
                    latest != null && latest.Matches(request)
                        ? SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence
                        : SessionPlayerLeaveStatus.RejectedSlotNotJoined;
                return Publish(Result(
                    status,
                    request,
                    latest != null && latest.Matches(request)
                        ? latest.ProvisioningMode
                        : PlayerHostProvisioningMode.Unspecified,
                    slot,
                    latest,
                    null,
                    null,
                    null,
                    null,
                    null,
                    status == SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence
                        ? "The previously correlated Leave occurrence is no longer the current Slot occurrence."
                        : "Session Player Leave requires a currently Joined Player Slot."));
            }

            if (slot.AllocationState == PlayerSlotAllocationState.Joined &&
                slot.Revision != request.ExpectedOccurrenceRevision)
            {
                return Publish(Result(
                    SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence,
                    request,
                    PlayerHostProvisioningMode.Unspecified,
                    slot,
                    (SessionPlayerLeaveRuntimeResult)null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    $"Expected occurrence revision '{request.ExpectedOccurrenceRevision}' does not match current Slot revision '{slot.Revision}'."));
            }

            PlayerHostProvisioningMode provisioningMode;
            bool retryingKnownOccurrence =
                latest != null && latest.Matches(request) &&
                slot.AllocationState == PlayerSlotAllocationState.Leaving;
            if (retryingKnownOccurrence)
            {
                provisioningMode = latest.ProvisioningMode;
            }
            else if (!participationContext.TryGetEffectiveHostProvisioningMode(
                         request.PlayerSlotId,
                         out provisioningMode) ||
                     !provisioningMode.IsDefinedMode())
            {
                return Publish(Result(
                    SessionPlayerLeaveStatus.RejectedProvisioningMode,
                    request,
                    PlayerHostProvisioningMode.Unspecified,
                    slot,
                    (SessionPlayerLeaveRuntimeResult)null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "Session Player Leave could not resolve an explicit effective Host provisioning mode for the target Slot."));
            }

            if (!TryResolveProvisioningRuntime(
                    provisioningMode,
                    out LocalPlayerProvisioningRuntimeHostModule managerProvisioning,
                    out SceneLocalPlayerAdmissionRuntimeHostModule sceneProvisioning,
                    out string runtimeIssue))
            {
                if (retryingKnownOccurrence)
                {
                    return Publish(FromRecord(
                        latest.ActivityRelease != null &&
                        latest.ActivityRelease.Succeeded
                            ? SessionPlayerLeaveStatus.FailedProvisioningRelease
                            : SessionPlayerLeaveStatus.FailedActivityRepresentationRelease,
                        latest,
                        "Active Session Player Leave retry cannot continue because a required scoped runtime is unavailable. " +
                        runtimeIssue));
                }

                return Publish(Result(
                    SessionPlayerLeaveStatus.RejectedRuntimeUnavailable,
                    request,
                    provisioningMode,
                    slot,
                    (SessionPlayerLeaveRuntimeResult)null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    runtimeIssue));
            }

            SessionPlayerLeaveRuntimeResult begin =
                participationContext.TryBeginSessionPlayerLeave(
                    request.PlayerSlotId,
                    request.ExpectedOccurrenceRevision,
                    request.Source,
                    request.Reason);
            if (begin == null || !begin.Succeeded)
            {
                return Publish(Result(
                    MapBeginFailure(begin),
                    request,
                    provisioningMode,
                    CurrentSlot(request.PlayerSlotId),
                    begin,
                    null,
                    null,
                    null,
                    null,
                    null,
                    begin != null
                        ? begin.ToDiagnosticString()
                        : "Session Player Leave begin returned no result."));
            }

            ExecutionRecord record;
            if (latest != null && latest.Matches(request))
            {
                if (latest.LeaveToken != begin.Token ||
                    latest.ProvisioningMode != provisioningMode)
                {
                    return Publish(Result(
                        SessionPlayerLeaveStatus.FailedInvariant,
                        request,
                        provisioningMode,
                        CurrentSlot(request.PlayerSlotId),
                        begin,
                        latest.ActivityRelease,
                        latest.ManagerHostEvidenceRelease,
                        latest.ManagerRelease,
                        latest.SceneRelease,
                        latest.TerminalResult,
                        "Existing Session Player Leave orchestration evidence conflicts with the active Leave token or provisioning mode."));
                }

                record = latest;
                record.BeginResult = begin;
            }
            else
            {
                record = new ExecutionRecord(
                    request,
                    begin.Token,
                    provisioningMode,
                    begin);
                latestBySlot[request.PlayerSlotId] = record;
            }

            if (record.ActivityRelease == null || !record.ActivityRelease.Succeeded)
            {
                record.ActivityRelease = preparationModule
                    .TryReleaseActivityRepresentationForSessionPlayerLeave(
                        record.LeaveToken,
                        request.Source,
                        request.Reason + "; release-activity-representation");
                if (record.ActivityRelease == null || !record.ActivityRelease.Succeeded)
                {
                    return Publish(FromRecord(
                        SessionPlayerLeaveStatus.FailedActivityRepresentationRelease,
                        record,
                        record.ActivityRelease != null
                            ? record.ActivityRelease.ToDiagnosticString()
                            : "Activity representation release returned no result."));
                }
            }

            if (record.ProvisioningMode == PlayerHostProvisioningMode.ManagerProvisioned)
            {
                if (record.ManagerRelease == null || !record.ManagerRelease.Succeeded)
                {
                    bool managerReleased = managerProvisioning
                        .TryReleaseManagerProvisionedPlayerForSessionLeave(
                            record.LeaveToken,
                            record.ManagerHostEvidenceRelease,
                            request.Source,
                            request.Reason,
                            out PlayerHostEvidenceResult hostEvidenceRelease,
                            out ManagerProvisionedSessionPlayerLeaveReleaseResult managerRelease,
                            out string managerIssue);
                    record.ManagerHostEvidenceRelease = hostEvidenceRelease;
                    record.ManagerRelease = managerRelease;
                    if (!managerReleased)
                    {
                        return Publish(FromRecord(
                            SessionPlayerLeaveStatus.FailedProvisioningRelease,
                            record,
                            managerIssue));
                    }
                }
            }
            else if (record.ProvisioningMode == PlayerHostProvisioningMode.SceneProvided)
            {
                if (record.SceneRelease == null || !record.SceneRelease.Succeeded)
                {
                    record.SceneRelease = sceneProvisioning
                        .TryReleaseSceneProvidedPlayerForSessionLeave(
                            record.LeaveToken,
                            request.Source,
                            request.Reason + "; release-scene-provided-authority");
                    if (record.SceneRelease == null || !record.SceneRelease.Succeeded)
                    {
                        return Publish(FromRecord(
                            SessionPlayerLeaveStatus.FailedProvisioningRelease,
                            record,
                            record.SceneRelease != null
                                ? record.SceneRelease.ToDiagnosticString()
                                : "Scene-Provided Session Player release returned no result."));
                    }
                }
            }
            else
            {
                return Publish(FromRecord(
                    SessionPlayerLeaveStatus.FailedInvariant,
                    record,
                    $"Active Leave record contains unsupported provisioning mode '{record.ProvisioningMode}'."));
            }

            record.TerminalResult = record.ProvisioningMode ==
                PlayerHostProvisioningMode.ManagerProvisioned
                ? participationContext.TryFinalizeSessionPlayerLeave(
                    record.LeaveToken,
                    record.ActivityRelease,
                    record.ManagerRelease,
                    request.Source,
                    request.Reason + "; terminal-commit")
                : participationContext.TryFinalizeSessionPlayerLeave(
                    record.LeaveToken,
                    record.ActivityRelease,
                    record.SceneRelease,
                    request.Source,
                    request.Reason + "; terminal-commit");

            if (record.TerminalResult == null || !record.TerminalResult.Succeeded)
            {
                return Publish(FromRecord(
                    record.TerminalResult != null &&
                    record.TerminalResult.Status ==
                        SessionPlayerLeaveTerminalStatus.FailedInvariant
                        ? SessionPlayerLeaveStatus.FailedInvariant
                        : SessionPlayerLeaveStatus.FailedTerminalCommit,
                    record,
                    record.TerminalResult != null
                        ? record.TerminalResult.ToDiagnosticString()
                        : "Terminal Session Player Leave commit returned no result."));
            }

            record.Completed = true;
            return Publish(FromRecord(
                record.TerminalResult.Status ==
                    SessionPlayerLeaveTerminalStatus.SucceededAlreadyCommitted
                    ? SessionPlayerLeaveStatus.SucceededAlreadyLeft
                    : SessionPlayerLeaveStatus.SucceededLeft,
                record,
                "Exact Session Player occurrence left successfully; required Activity and provisioning resources were released before terminal Slot availability."));
        }

        private SessionPlayerLeaveResult RepeatCompleted(ExecutionRecord record)
        {
            record.TerminalResult = record.ProvisioningMode ==
                PlayerHostProvisioningMode.ManagerProvisioned
                ? participationContext.TryFinalizeSessionPlayerLeave(
                    record.LeaveToken,
                    record.ActivityRelease,
                    record.ManagerRelease,
                    record.Request.Source,
                    record.Request.Reason + "; repeat-terminal-confirmation")
                : participationContext.TryFinalizeSessionPlayerLeave(
                    record.LeaveToken,
                    record.ActivityRelease,
                    record.SceneRelease,
                    record.Request.Source,
                    record.Request.Reason + "; repeat-terminal-confirmation");

            if (record.TerminalResult != null && record.TerminalResult.Succeeded)
            {
                return FromRecord(
                    SessionPlayerLeaveStatus.SucceededAlreadyLeft,
                    record,
                    "The exact Session Player occurrence already completed Leave and the terminal Slot state is still current.");
            }

            return FromRecord(
                SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence,
                record,
                record.TerminalResult != null
                    ? record.TerminalResult.ToDiagnosticString()
                    : "Completed Leave correlation could not confirm its terminal Slot state; the old occurrence is stale.");
        }

        private bool TryResolveProvisioningRuntime(
            PlayerHostProvisioningMode provisioningMode,
            out LocalPlayerProvisioningRuntimeHostModule managerProvisioning,
            out SceneLocalPlayerAdmissionRuntimeHostModule sceneProvisioning,
            out string issue)
        {
            managerProvisioning = null;
            sceneProvisioning = null;
            issue = string.Empty;
            if (runtimeHost == null)
            {
                issue = "Session Player Leave has no FrameworkRuntimeHost.";
                return false;
            }

            switch (provisioningMode)
            {
                case PlayerHostProvisioningMode.ManagerProvisioned:
                    if (!runtimeHost.TryGetLocalPlayerProvisioningRuntime(
                            out managerProvisioning))
                    {
                        issue =
                            "Manager-Provisioned Session Player Leave requires the ready Local Player provisioning runtime.";
                        return false;
                    }

                    return true;

                case PlayerHostProvisioningMode.SceneProvided:
                    sceneProvisioning = runtimeHost.GetComponent<
                        SceneLocalPlayerAdmissionRuntimeHostModule>();
                    if (sceneProvisioning == null || !sceneProvisioning.IsReady)
                    {
                        issue =
                            "Scene-Provided Session Player Leave requires the ready Scene Local Player admission runtime.";
                        return false;
                    }

                    return true;

                default:
                    issue = $"Unsupported Session Player Host provisioning mode '{provisioningMode}'.";
                    return false;
            }
        }

        private SessionPlayerLeaveResult FromRecord(
            SessionPlayerLeaveStatus status,
            ExecutionRecord record,
            string message)
        {
            return Result(
                status,
                record.Request,
                record.ProvisioningMode,
                CurrentSlot(record.Request.PlayerSlotId),
                record.BeginResult,
                record.ActivityRelease,
                record.ManagerHostEvidenceRelease,
                record.ManagerRelease,
                record.SceneRelease,
                record.TerminalResult,
                message);
        }

        private static SessionPlayerLeaveStatus MapBeginFailure(
            SessionPlayerLeaveRuntimeResult begin)
        {
            if (begin == null)
            {
                return SessionPlayerLeaveStatus.FailedInvariant;
            }

            return begin.Status switch
            {
                SessionPlayerLeaveRuntimeStatus.RejectedInvalidRequest =>
                    SessionPlayerLeaveStatus.RejectedInvalidRequest,
                SessionPlayerLeaveRuntimeStatus.RejectedSlotNotConfigured =>
                    SessionPlayerLeaveStatus.RejectedSlotNotConfigured,
                SessionPlayerLeaveRuntimeStatus.RejectedSlotNotJoined =>
                    SessionPlayerLeaveStatus.RejectedSlotNotJoined,
                SessionPlayerLeaveRuntimeStatus.RejectedForeignOrStaleOccurrence =>
                    SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence,
                SessionPlayerLeaveRuntimeStatus.FailedInvariant =>
                    SessionPlayerLeaveStatus.FailedInvariant,
                _ => SessionPlayerLeaveStatus.FailedInvariant
            };
        }

        private PlayerSlotRuntimeSnapshot CurrentSlot(PlayerSlotId playerSlotId)
        {
            PlayerParticipationSnapshot snapshot = participationContext?.CreateSnapshot();
            return snapshot != null && TryFindSlot(snapshot, playerSlotId, out PlayerSlotRuntimeSnapshot slot)
                ? slot
                : default;
        }

        private static bool TryFindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot slot)
        {
            if (snapshot != null)
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot candidate = snapshot.Slots[index];
                    if (candidate.PlayerSlotId == playerSlotId)
                    {
                        slot = candidate;
                        return true;
                    }
                }
            }

            slot = default;
            return false;
        }

        private static SessionPlayerLeaveResult Result(
            SessionPlayerLeaveStatus status,
            SessionPlayerLeaveRequest request,
            PlayerHostProvisioningMode provisioningMode,
            PlayerSlotRuntimeSnapshot slot,
            SessionPlayerLeaveRuntimeResult beginResult,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            PlayerHostEvidenceResult managerHostEvidenceRelease,
            ManagerProvisionedSessionPlayerLeaveReleaseResult managerRelease,
            SceneProvidedSessionPlayerLeaveReleaseResult sceneRelease,
            SessionPlayerLeaveTerminalResult terminalResult,
            string message)
        {
            SessionPlayerLeaveToken leaveToken = beginResult != null
                ? beginResult.Token
                : terminalResult != null
                    ? terminalResult.LeaveToken
                    : default;
            return new SessionPlayerLeaveResult(
                status,
                request,
                provisioningMode,
                slot,
                leaveToken,
                beginResult,
                activityRelease,
                managerHostEvidenceRelease,
                managerRelease,
                sceneRelease,
                terminalResult,
                message);
        }

        private static SessionPlayerLeaveResult Result(
            SessionPlayerLeaveStatus status,
            SessionPlayerLeaveRequest request,
            PlayerHostProvisioningMode provisioningMode,
            PlayerSlotRuntimeSnapshot slot,
            ExecutionRecord record,
            SessionPlayerActivityRepresentationReleaseResult activityRelease,
            PlayerHostEvidenceResult managerHostEvidenceRelease,
            ManagerProvisionedSessionPlayerLeaveReleaseResult managerRelease,
            SceneProvidedSessionPlayerLeaveReleaseResult sceneRelease,
            SessionPlayerLeaveTerminalResult terminalResult,
            string message)
        {
            return Result(
                status,
                request,
                provisioningMode,
                slot,
                record?.BeginResult,
                activityRelease ?? record?.ActivityRelease,
                managerHostEvidenceRelease ?? record?.ManagerHostEvidenceRelease,
                managerRelease ?? record?.ManagerRelease,
                sceneRelease ?? record?.SceneRelease,
                terminalResult ?? record?.TerminalResult,
                message);
        }

        private SessionPlayerLeaveResult Publish(SessionPlayerLeaveResult result)
        {
            diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Session Player Leave orchestration returned no result.";
            return result ?? SessionPlayerLeaveResult.RuntimeUnavailable(
                default,
                diagnostic);
        }

        private void OnDestroy()
        {
            latestBySlot.Clear();
            preparationModule = null;
            participationContext = null;
            runtimeHost = null;
            diagnostic = "Session Player Leave runtime was released with the FrameworkRuntimeHost lifetime.";
        }
    }

    /// <summary>
    /// Typed same-host access for the canonical Session Player Leave runtime.
    /// The caller must already hold the FrameworkRuntimeHost reference.
    /// </summary>
    internal static class FrameworkRuntimeHostSessionPlayerLeaveExtensions
    {
        internal static bool TryGetSessionPlayerLeaveRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out SessionPlayerLeaveRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<SessionPlayerLeaveRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }
    }
}
