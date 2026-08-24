using System.Collections.Generic;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerGameplayRuntimeHostModule
    {
        internal enum SessionPlayerLeaveGameplayReleaseStatus
        {
            None = 0,
            SucceededReleased = 10,
            SucceededAlreadyReleased = 20,
            SucceededNoCurrentGameplay = 30,
            RejectedInvalidRequest = 100,
            RejectedLeaveCorrelation = 110,
            RejectedPreparationCorrelation = 120,
            FailedAdmissionRelease = 200,
            FailedCameraRelease = 210,
            FailedInputRelease = 220,
            FailedOccupancyRelease = 230,
            FailedInvariant = 240
        }

        internal sealed class SessionPlayerLeaveGameplayReleaseResult
        {
            internal SessionPlayerLeaveGameplayReleaseResult(
                SessionPlayerLeaveGameplayReleaseStatus status,
                SessionPlayerLeaveToken leaveToken,
                bool hadGameplayChain,
                bool admissionReleased,
                bool cameraReleased,
                bool inputReleased,
                bool occupancyReleased,
                string message)
            {
                Status = status;
                LeaveToken = leaveToken;
                HadGameplayChain = hadGameplayChain;
                AdmissionReleased = admissionReleased;
                CameraReleased = cameraReleased;
                InputReleased = inputReleased;
                OccupancyReleased = occupancyReleased;
                Message = message ?? string.Empty;
            }

            internal SessionPlayerLeaveGameplayReleaseStatus Status { get; }
            internal SessionPlayerLeaveToken LeaveToken { get; }
            internal bool HadGameplayChain { get; }
            internal bool AdmissionReleased { get; }
            internal bool CameraReleased { get; }
            internal bool InputReleased { get; }
            internal bool OccupancyReleased { get; }
            internal string Message { get; }

            internal bool Succeeded => Status is
                SessionPlayerLeaveGameplayReleaseStatus.SucceededReleased or
                SessionPlayerLeaveGameplayReleaseStatus.SucceededAlreadyReleased or
                SessionPlayerLeaveGameplayReleaseStatus.SucceededNoCurrentGameplay;
        }

        private sealed class SessionPlayerLeaveGameplayReleaseProgress
        {
            internal SessionPlayerLeaveToken leaveToken;
            internal PlayerActorPreparationToken preparationToken;
            internal bool hadGameplayChain;
            internal PlayerGameplayAdmissionToken admissionToken;
            internal PlayerGameplayCameraEligibilityToken cameraToken;
            internal PlayerGameplayInputBindingToken inputToken;
            internal PlayerGameplayOccupancyToken occupancyToken;
            internal bool admissionReleased;
            internal bool cameraReleased;
            internal bool inputReleased;
            internal bool occupancyReleased;
            internal bool completed;
        }

        private readonly Dictionary<SessionPlayerLeaveToken,
            SessionPlayerLeaveGameplayReleaseProgress>
            _sessionPlayerLeaveGameplayReleaseProgress = new();

        /// <summary>
        /// Captures and validates the exact current P3K Activity gameplay chain without releasing
        /// any capability. The captured tokens are retained by Leave token so downstream retry
        /// never re-resolves a different gameplay occurrence after partial irreversible release.
        /// </summary>
        internal bool TryInspectActivityGameplayForSessionPlayerLeave(
            SessionPlayerLeaveToken leaveToken,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason,
            out bool hadGameplayChain,
            out SessionPlayerLeaveGameplayReleaseStatus failureStatus,
            out string issue)
        {
            hadGameplayChain = false;
            failureStatus = SessionPlayerLeaveGameplayReleaseStatus.None;
            issue = string.Empty;

            if (!IsReady || _participationContext == null || !leaveToken.IsValid)
            {
                failureStatus = SessionPlayerLeaveGameplayReleaseStatus.RejectedInvalidRequest;
                issue = "Player gameplay runtime and a valid Leave token are required to inspect Activity gameplay release evidence.";
                return false;
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                _participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    source,
                    reason);
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                failureStatus = SessionPlayerLeaveGameplayReleaseStatus.RejectedLeaveCorrelation;
                issue = leaveConfirmation != null
                    ? leaveConfirmation.ToDiagnosticString()
                    : "Session Player Leave confirmation returned no result.";
                return false;
            }

            if (expectedPreparation.IsValid &&
                (expectedPreparation.PlayerSlotId != leaveToken.PlayerSlotId ||
                 !string.Equals(
                     expectedPreparation.SessionContextId,
                     leaveToken.ContextId,
                     System.StringComparison.Ordinal)))
            {
                failureStatus = SessionPlayerLeaveGameplayReleaseStatus.RejectedPreparationCorrelation;
                issue = "Expected preparation token does not belong to the exact Leaving Session Player occurrence.";
                return false;
            }

            if (_sessionPlayerLeaveGameplayReleaseProgress.TryGetValue(
                    leaveToken,
                    out SessionPlayerLeaveGameplayReleaseProgress existing))
            {
                if (existing.preparationToken != expectedPreparation)
                {
                    failureStatus = SessionPlayerLeaveGameplayReleaseStatus.RejectedPreparationCorrelation;
                    issue = "Retry changed the expected Activity preparation token for the same Session Player Leave occurrence.";
                    return false;
                }

                hadGameplayChain = existing.hadGameplayChain;
                return true;
            }

            SessionPlayerLeaveGameplayReleaseResult captureFailure =
                TryCaptureSessionPlayerLeaveGameplayProgress(
                    leaveToken,
                    expectedPreparation,
                    out SessionPlayerLeaveGameplayReleaseProgress progress);
            if (captureFailure != null)
            {
                failureStatus = captureFailure.Status;
                issue = captureFailure.Message;
                return false;
            }

            _sessionPlayerLeaveGameplayReleaseProgress.Add(
                leaveToken,
                progress);
            hadGameplayChain = progress.hadGameplayChain;
            return true;
        }

        /// <summary>
        /// Releases the exact P3K Activity gameplay capability chain for one staged Leave.
        /// Progress is retained by Leave token so partial irreversible release is retried without
        /// compensation or recreation. The prepared Actor is deliberately not released here.
        /// </summary>
        internal SessionPlayerLeaveGameplayReleaseResult
            TryReleaseActivityGameplayForSessionPlayerLeave(
                SessionPlayerLeaveToken leaveToken,
                PlayerActorPreparationToken expectedPreparation,
                string source,
                string reason)
        {
            if (!TryInspectActivityGameplayForSessionPlayerLeave(
                    leaveToken,
                    expectedPreparation,
                    source,
                    reason,
                    out _,
                    out SessionPlayerLeaveGameplayReleaseStatus inspectionFailure,
                    out string inspectionIssue))
            {
                return GameplayLeaveResult(
                    inspectionFailure,
                    leaveToken,
                    _sessionPlayerLeaveGameplayReleaseProgress.TryGetValue(
                        leaveToken,
                        out SessionPlayerLeaveGameplayReleaseProgress failedProgress)
                            ? failedProgress
                            : null,
                    inspectionIssue);
            }

            SessionPlayerLeaveGameplayReleaseProgress progress =
                _sessionPlayerLeaveGameplayReleaseProgress[leaveToken];
            if (progress.completed)
            {
                return GameplayLeaveResult(
                    progress.hadGameplayChain
                        ? SessionPlayerLeaveGameplayReleaseStatus.SucceededAlreadyReleased
                        : SessionPlayerLeaveGameplayReleaseStatus.SucceededNoCurrentGameplay,
                    leaveToken,
                    progress,
                    "The exact Leave occurrence already has no current Activity gameplay capability chain.");
            }

            if (!progress.admissionReleased)
            {
                PlayerGameplayAdmissionResult result = _admissionContext.TryRelease(
                    leaveToken.PlayerSlotId,
                    progress.admissionToken,
                    source,
                    reason);
                // PlayerGameplayAdmissionResult is a value type (struct) and cannot be compared to null.
                // Check the Succeeded flag to determine success and use the diagnostic string from the result.
                if (!result.Succeeded)
                {
                    return GameplayLeaveResult(
                        SessionPlayerLeaveGameplayReleaseStatus.FailedAdmissionRelease,
                        leaveToken,
                        progress,
                        result.ToDiagnosticString());
                }

                progress.admissionReleased = true;
            }

            if (!progress.cameraReleased)
            {
                PlayerGameplayCameraEligibilityResult result = _cameraContext.TryRelease(
                    leaveToken.PlayerSlotId,
                    progress.cameraToken,
                    source,
                    reason);
                if (result == null || !result.Succeeded)
                {
                    return GameplayLeaveResult(
                        SessionPlayerLeaveGameplayReleaseStatus.FailedCameraRelease,
                        leaveToken,
                        progress,
                        result != null
                            ? result.ToDiagnosticString()
                            : "Gameplay Camera eligibility release returned no result.");
                }

                progress.cameraReleased = true;
            }

            if (!progress.inputReleased)
            {
                PlayerGameplayInputBindingResult result = _inputContext.TryRelease(
                    leaveToken.PlayerSlotId,
                    progress.inputToken,
                    source,
                    reason);
                if (result == null || !result.Succeeded)
                {
                    return GameplayLeaveResult(
                        SessionPlayerLeaveGameplayReleaseStatus.FailedInputRelease,
                        leaveToken,
                        progress,
                        result != null
                            ? result.ToDiagnosticString()
                            : "Gameplay Input binding release returned no result.");
                }

                progress.inputReleased = true;
            }

            if (!progress.occupancyReleased)
            {
                PlayerGameplayOccupancyResult result =
                    _occupancyContext.TryReleaseOccupancy(
                        leaveToken.PlayerSlotId,
                        progress.occupancyToken,
                        source,
                        reason);
                if (result == null || !result.Succeeded)
                {
                    return GameplayLeaveResult(
                        SessionPlayerLeaveGameplayReleaseStatus.FailedOccupancyRelease,
                        leaveToken,
                        progress,
                        result != null
                            ? result.ToDiagnosticString()
                            : "Gameplay occupancy release returned no result.");
                }

                progress.occupancyReleased = true;
            }

            progress.completed = true;
            return GameplayLeaveResult(
                progress.hadGameplayChain
                    ? SessionPlayerLeaveGameplayReleaseStatus.SucceededReleased
                    : SessionPlayerLeaveGameplayReleaseStatus.SucceededNoCurrentGameplay,
                leaveToken,
                progress,
                progress.hadGameplayChain
                    ? "Exact Activity gameplay Admission, Camera and Input capabilities plus retained Session occupancy were released for Session Player Leave."
                    : "The Leaving Session Player has no current Activity gameplay capability chain; retained Session occupancy was terminally released when present.");
        }

        private SessionPlayerLeaveGameplayReleaseResult
            TryCaptureSessionPlayerLeaveGameplayProgress(
                SessionPlayerLeaveToken leaveToken,
                PlayerActorPreparationToken expectedPreparation,
                out SessionPlayerLeaveGameplayReleaseProgress progress)
        {
            progress = null;
            bool hasAdmission =
                _admissionContext.CreateSnapshot().TryGetSummary(
                    leaveToken.PlayerSlotId,
                    out PlayerGameplayAdmissionSummary admission) &&
                admission.IsAdmitted;
            bool hasCamera =
                _cameraContext.CreateSnapshot().TryGetSummary(
                    leaveToken.PlayerSlotId,
                    out PlayerGameplayCameraEligibilitySummary camera) &&
                camera.HasCurrentDecision;
            bool hasInput =
                _inputContext.TryGetRetainedInputBinding(
                    leaveToken.PlayerSlotId,
                    out PlayerGameplayInputBindingSummary input);
            bool hasOccupancy =
                _occupancyContext.TryGetSummary(
                    leaveToken.PlayerSlotId,
                    out PlayerGameplayOccupancySummary occupancy) &&
                occupancy.IsOccupied;
            bool hadActivityGameplayChain =
                hasAdmission || hasCamera || hasInput;
            bool hasAnyGameplayEvidence =
                hadActivityGameplayChain || hasOccupancy;

            if (!expectedPreparation.IsValid && hadActivityGameplayChain)
            {
                return GameplayLeaveResult(
                    SessionPlayerLeaveGameplayReleaseStatus.RejectedPreparationCorrelation,
                    leaveToken,
                    null,
                    "Activity contextual gameplay evidence exists without a retained Activity preparation token in the lifecycle ledger.");
            }

            if (!expectedPreparation.IsValid && hasOccupancy)
            {
                if (!_preparationModule.TryGetCurrentPreparation(
                        leaveToken.PlayerSlotId,
                        out PlayerActorPreparationSummary sessionPreparation,
                        out _) ||
                    !sessionPreparation.IsPrepared ||
                    sessionPreparation.Token != occupancy.PreparationToken)
                {
                    return GameplayLeaveResult(
                        SessionPlayerLeaveGameplayReleaseStatus.FailedInvariant,
                        leaveToken,
                        null,
                        "Retained Session gameplay occupancy does not correlate with the current prepared Session physical Player for the Leaving occurrence.");
                }
            }

            if (expectedPreparation.IsValid)
            {
                if ((hasAdmission && admission.PreparationToken != expectedPreparation) ||
                    (hasCamera && camera.PreparationToken != expectedPreparation) ||
                    (hasInput && input.PreparationToken != expectedPreparation) ||
                    (hasOccupancy && occupancy.PreparationToken != expectedPreparation))
                {
                    return GameplayLeaveResult(
                        SessionPlayerLeaveGameplayReleaseStatus.RejectedPreparationCorrelation,
                        leaveToken,
                        null,
                        "Current Activity gameplay capability evidence belongs to another preparation occurrence.");
                }
            }

            if (hasAdmission &&
                (!hasCamera || !hasInput || !hasOccupancy ||
                 admission.CameraEligibilityToken != camera.Token ||
                 admission.InputBindingToken != input.Token ||
                 admission.OccupancyToken != occupancy.Token))
            {
                return GameplayLeaveResult(
                    SessionPlayerLeaveGameplayReleaseStatus.FailedInvariant,
                    leaveToken,
                    null,
                    "Current Gameplay Admission does not resolve to one exact Camera/Input/Occupancy capability chain.");
            }

            progress = new SessionPlayerLeaveGameplayReleaseProgress
            {
                leaveToken = leaveToken,
                preparationToken = expectedPreparation,
                hadGameplayChain = hadActivityGameplayChain,
                admissionToken = hasAdmission ? admission.Token : default,
                cameraToken = hasCamera ? camera.Token : default,
                inputToken = hasInput ? input.Token : default,
                occupancyToken = hasOccupancy ? occupancy.Token : default,
                admissionReleased = !hasAdmission,
                cameraReleased = !hasCamera,
                inputReleased = !hasInput,
                occupancyReleased = !hasOccupancy,
                completed = !hasAnyGameplayEvidence
            };
            return null;
        }

        private static SessionPlayerLeaveGameplayReleaseResult GameplayLeaveResult(
            SessionPlayerLeaveGameplayReleaseStatus status,
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerLeaveGameplayReleaseProgress progress,
            string message)
        {
            return new SessionPlayerLeaveGameplayReleaseResult(
                status,
                leaveToken,
                progress != null && progress.hadGameplayChain,
                progress != null && progress.admissionReleased,
                progress != null && progress.cameraReleased,
                progress != null && progress.inputReleased,
                progress != null && progress.occupancyReleased,
                message);
        }
    }
}
