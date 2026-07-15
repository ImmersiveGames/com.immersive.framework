using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped transaction authority that consumes current occupancy, gameplay
    /// input binding and camera eligibility evidence, optionally publishes one local
    /// Player camera request, derives GameplayReady and owns reverse release.
    /// It does not materialize Actors, choose camera winners or gate Activity activation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.5 gameplay admission, camera publication and reverse release authority.")]
    internal sealed class PlayerGameplayAdmissionRuntimeContext
    {
        private sealed class AdmissionRecord
        {
            internal PlayerGameplayAdmissionToken Token;
            internal PlayerGameplayOccupancyToken OccupancyToken;
            internal PlayerGameplayInputBindingToken InputBindingToken;
            internal PlayerGameplayCameraEligibilityToken CameraEligibilityToken;
            internal PlayerGameplayCameraEligibilityState CameraEligibilityState;
            internal PlayerGameplayCameraRequiredness CameraRequiredness;
            internal LocalPlayerCameraRequestPublisher Publisher;
            internal CameraRequest Request;
            internal bool CameraRequestReleased;
            internal bool CameraEligibilityReleased;
            internal bool InputBindingReleased;
            internal bool OccupancyReleased;

            internal bool CameraRequestPublished =>
                Publisher != null && Publisher.IsPublished;
        }

        private sealed class CurrentChainEvidence
        {
            internal PlayerGameplayOccupancySummary Occupancy;
            internal PlayerGameplayInputBindingSummary InputBinding;
            internal PlayerGameplayCameraEligibilitySummary CameraEligibility;
        }

        private readonly string sessionContextId;
        private readonly PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext inputContext;
        private readonly PlayerGameplayCameraEligibilityRuntimeContext cameraContext;
        private readonly PlayerSlotId[] orderedSlots;
        private readonly Dictionary<PlayerSlotId, PlayerGameplayAdmissionSummary> slots;
        private readonly Dictionary<PlayerSlotId, AdmissionRecord> records;

        private int revision = 1;
        private int admissionSequence;
        private PlayerGameplayAdmissionStatus lastOperationStatus;
        private string lastOperationMessage =
            "Player gameplay admission runtime initialized.";

        private PlayerGameplayAdmissionRuntimeContext(
            string sessionContextId,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerSlotId[] orderedSlots)
        {
            this.sessionContextId = sessionContextId;
            this.occupancyContext = occupancyContext;
            this.inputContext = inputContext;
            this.cameraContext = cameraContext;
            this.orderedSlots = orderedSlots;
            slots = new Dictionary<PlayerSlotId, PlayerGameplayAdmissionSummary>(
                orderedSlots.Length);
            records = new Dictionary<PlayerSlotId, AdmissionRecord>(
                orderedSlots.Length);

            for (int index = 0; index < orderedSlots.Length; index++)
            {
                PlayerSlotId slot = orderedSlots[index];
                slots.Add(
                    slot,
                    PlayerGameplayAdmissionSummary.NotAdmitted(
                        sessionContextId,
                        slot,
                        0,
                        nameof(PlayerGameplayAdmissionRuntimeContext),
                        "runtime-initialization",
                        "Configured Player Slot is not gameplay admitted."));
            }
        }

        internal string SessionContextId => sessionContextId;
        internal int Revision => revision;

        internal static bool TryCreate(
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            out PlayerGameplayAdmissionRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (occupancyContext == null ||
                inputContext == null ||
                cameraContext == null)
            {
                issue =
                    "Gameplay admission requires explicit occupancy, input and camera eligibility authorities.";
                return false;
            }

            PlayerGameplayOccupancySnapshot occupancySnapshot =
                occupancyContext.CreateSnapshot();
            PlayerGameplayInputBindingSnapshot inputSnapshot =
                inputContext.CreateSnapshot();
            PlayerGameplayCameraEligibilitySnapshot cameraSnapshot =
                cameraContext.CreateSnapshot();

            if (occupancySnapshot == null ||
                inputSnapshot == null ||
                cameraSnapshot == null ||
                !occupancySnapshot.IsInitialized ||
                !inputSnapshot.IsInitialized ||
                !cameraSnapshot.IsInitialized)
            {
                issue =
                    "Gameplay admission requires initialized occupancy, input and camera snapshots.";
                return false;
            }

            if (!string.Equals(
                    occupancySnapshot.SessionContextId,
                    inputSnapshot.SessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    occupancySnapshot.SessionContextId,
                    cameraSnapshot.SessionContextId,
                    StringComparison.Ordinal))
            {
                issue =
                    "Gameplay admission authorities belong to different Session contexts.";
                return false;
            }

            if (occupancySnapshot.ConfiguredSlotCount <= 0 ||
                occupancySnapshot.ConfiguredSlotCount !=
                    inputSnapshot.ConfiguredSlotCount ||
                occupancySnapshot.ConfiguredSlotCount !=
                    cameraSnapshot.ConfiguredSlotCount)
            {
                issue =
                    "Gameplay admission requires matching non-empty Slot rosters.";
                return false;
            }

            var ordered = new PlayerSlotId[
                occupancySnapshot.ConfiguredSlotCount];
            var unique = new HashSet<PlayerSlotId>();

            for (int index = 0;
                 index < occupancySnapshot.Slots.Count;
                 index++)
            {
                PlayerGameplayOccupancySummary occupancy =
                    occupancySnapshot.Slots[index];
                if (!occupancy.IsValid ||
                    !occupancy.PlayerSlotId.IsValid ||
                    !inputSnapshot.TryGetSummary(
                        occupancy.PlayerSlotId,
                        out PlayerGameplayInputBindingSummary input) ||
                    !input.IsValid ||
                    !cameraSnapshot.TryGetSummary(
                        occupancy.PlayerSlotId,
                        out PlayerGameplayCameraEligibilitySummary camera) ||
                    !camera.IsValid)
                {
                    issue =
                        $"Gameplay admission rejected invalid Slot evidence at index '{index}'.";
                    return false;
                }

                if (!unique.Add(occupancy.PlayerSlotId))
                {
                    issue =
                        $"Gameplay admission rejected duplicate Slot '{occupancy.PlayerSlotId.StableText}'.";
                    return false;
                }

                ordered[index] = occupancy.PlayerSlotId;
            }

            context = new PlayerGameplayAdmissionRuntimeContext(
                occupancySnapshot.SessionContextId,
                occupancyContext,
                inputContext,
                cameraContext,
                ordered);
            return true;
        }

        internal PlayerGameplayAdmissionResult TryAdmit(
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerGameplayCameraEligibilitySummary cameraEligibility,
            CameraOutputSessionBinding outputSession,
            string source,
            string reason)
        {
            const string Operation = "AdmitGameplay";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayAdmissionRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "admit-player-gameplay");

            if (!TryValidateCurrentChain(
                    in occupancy,
                    in inputBinding,
                    in cameraEligibility,
                    out CurrentChainEvidence liveChain,
                    out PlayerSlotId playerSlotId,
                    out PlayerGameplayAdmissionStatus rejectedStatus,
                    out string issue))
            {
                return Reject(
                    rejectedStatus,
                    Operation,
                    playerSlotId,
                    GetSummaryOrDefault(playerSlotId),
                    issue);
            }

            occupancy = liveChain.Occupancy;
            inputBinding = liveChain.InputBinding;
            cameraEligibility = liveChain.CameraEligibility;

            PlayerGameplayAdmissionSummary previous = slots[playerSlotId];
            if (previous.IsAdmitted)
            {
                if (previous.OccupancyToken == occupancy.Token &&
                    previous.InputBindingToken == inputBinding.Token &&
                    previous.CameraEligibilityToken == cameraEligibility.Token &&
                    previous.IsReleaseFailed == false)
                {
                    return RefreshCurrentAdmission(
                        Operation,
                        playerSlotId,
                        previous,
                        inputBinding,
                        resolvedSource,
                        resolvedReason,
                        PlayerGameplayAdmissionStatus
                            .SucceededAlreadyAdmitted,
                        "Gameplay admission is already current.");
                }

                return Reject(
                    PlayerGameplayAdmissionStatus
                        .RejectedSlotAlreadyAdmitted,
                    Operation,
                    playerSlotId,
                    previous,
                    "Player Slot already has another current gameplay admission.");
            }

            admissionSequence++;
            var token = new PlayerGameplayAdmissionToken(
                sessionContextId,
                occupancy.Owner,
                playerSlotId,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.RuntimeContentIdentity,
                occupancy.Token.MaterializationRevision,
                occupancy.OccupancyRevision,
                inputBinding.BindingRevision,
                cameraEligibility.EligibilityRevision,
                admissionSequence);

            var record = new AdmissionRecord
            {
                Token = token,
                OccupancyToken = occupancy.Token,
                InputBindingToken = inputBinding.Token,
                CameraEligibilityToken = cameraEligibility.Token,
                CameraEligibilityState = cameraEligibility.State,
                CameraRequiredness = cameraEligibility.Requiredness,
                CameraRequestReleased = cameraEligibility.IsSkippedOptional
            };

            PlayerGameplayAdmissionStatus failureStatus =
                PlayerGameplayAdmissionStatus.None;
            string failureIssue = string.Empty;

            if (cameraEligibility.IsEligible)
            {
                if (!TryPublishCamera(
                        cameraEligibility,
                        outputSession,
                        record,
                        resolvedSource,
                        resolvedReason,
                        out failureStatus,
                        out failureIssue))
                {
                    return RollbackFailedAdmission(
                        Operation,
                        playerSlotId,
                        previous,
                        occupancy,
                        inputBinding,
                        cameraEligibility,
                        record,
                        failureStatus,
                        failureIssue,
                        resolvedSource,
                        resolvedReason);
                }
            }

            PlayerGameplayAdmissionState state = inputBinding.IsAllowed
                ? PlayerGameplayAdmissionState.Ready
                : PlayerGameplayAdmissionState.BlockedByInputGate;

            revision++;
            PlayerGameplayAdmissionSummary current = CreateSummary(
                occupancy,
                inputBinding,
                cameraEligibility,
                record,
                state,
                resolvedSource,
                resolvedReason,
                state == PlayerGameplayAdmissionState.Ready
                    ? "Gameplay admission completed and GameplayReady is true."
                    : "Gameplay admission completed but GameplayReady is blocked by the current input Gate.");

            if (!current.IsValid)
            {
                return RollbackFailedAdmission(
                    Operation,
                    playerSlotId,
                    previous,
                    occupancy,
                    inputBinding,
                    cameraEligibility,
                    record,
                    PlayerGameplayAdmissionStatus.RejectedInvalidRequest,
                    "Gameplay admission summary creation produced incoherent evidence.",
                    resolvedSource,
                    resolvedReason);
            }

            slots[playerSlotId] = current;
            records[playerSlotId] = record;
            lastOperationStatus = state == PlayerGameplayAdmissionState.Ready
                ? PlayerGameplayAdmissionStatus.SucceededReady
                : PlayerGameplayAdmissionStatus
                    .SucceededBlockedByInputGate;
            lastOperationMessage = current.Message;

            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                false,
                false,
                string.Empty,
                lastOperationMessage);
        }

        internal PlayerGameplayAdmissionResult TryRefreshReadiness(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken expectedAdmission,
            string source,
            string reason)
        {
            const string Operation = "RefreshGameplayReadiness";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayAdmissionRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "refresh-player-gameplay-readiness");

            if (!slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary previous))
            {
                return Reject(
                    PlayerGameplayAdmissionStatus.RejectedSlotNotConfigured,
                    Operation,
                    playerSlotId,
                    default,
                    "Gameplay readiness refresh targets an unconfigured Slot.");
            }

            if (!previous.IsAdmitted ||
                previous.IsReleaseFailed ||
                !expectedAdmission.IsValid ||
                previous.Token != expectedAdmission ||
                !records.TryGetValue(playerSlotId, out AdmissionRecord record))
            {
                return Reject(
                    PlayerGameplayAdmissionStatus
                        .RejectedForeignOrStaleAdmission,
                    Operation,
                    playerSlotId,
                    previous,
                    "Gameplay readiness refresh requires the exact current admission token.");
            }

            if (!TryResolveCurrentChain(
                    record,
                    out CurrentChainEvidence liveChain,
                    out PlayerGameplayAdmissionStatus rejectedStatus,
                    out string issue))
            {
                return Reject(
                    rejectedStatus,
                    Operation,
                    playerSlotId,
                    previous,
                    issue);
            }

            return RefreshCurrentAdmission(
                Operation,
                playerSlotId,
                previous,
                liveChain.InputBinding,
                resolvedSource,
                resolvedReason,
                PlayerGameplayAdmissionStatus.SucceededReadinessRefreshed,
                liveChain.InputBinding.IsAllowed
                    ? "GameplayReady refreshed to true."
                    : "GameplayReady refreshed to false because input is blocked by Gate.");
        }

        internal PlayerGameplayAdmissionResult TryRelease(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken expectedAdmission,
            string source,
            string reason)
        {
            const string Operation = "ReleaseGameplayAdmission";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayAdmissionRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-player-gameplay-admission");

            if (!slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary previous))
            {
                return Reject(
                    PlayerGameplayAdmissionStatus.RejectedSlotNotConfigured,
                    Operation,
                    playerSlotId,
                    default,
                    "Gameplay admission release targets an unconfigured Slot.");
            }

            if (previous.IsNotAdmitted)
            {
                if (expectedAdmission.IsValid)
                {
                    return Reject(
                        PlayerGameplayAdmissionStatus
                            .RejectedForeignOrStaleAdmission,
                        Operation,
                        playerSlotId,
                        previous,
                        "Gameplay admission is already released and the supplied token is stale.");
                }

                lastOperationStatus =
                    PlayerGameplayAdmissionStatus.SucceededAlreadyReleased;
                lastOperationMessage =
                    "Gameplay admission is already released.";
                return Result(
                    lastOperationStatus,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    lastOperationMessage);
            }

            if (!expectedAdmission.IsValid ||
                previous.Token != expectedAdmission ||
                !records.TryGetValue(playerSlotId, out AdmissionRecord record))
            {
                return Reject(
                    PlayerGameplayAdmissionStatus
                        .RejectedForeignOrStaleAdmission,
                    Operation,
                    playerSlotId,
                    previous,
                    "Gameplay admission release requires the exact current token and record.");
            }

            if (!TryReleaseDependencies(
                    record,
                    resolvedSource,
                    resolvedReason,
                    out PlayerGameplayAdmissionStatus failureStatus,
                    out string issue))
            {
                revision++;
                PlayerGameplayAdmissionSummary failed =
                    CreateReleaseFailedSummary(
                        previous,
                        record,
                        resolvedSource,
                        resolvedReason,
                        issue);
                slots[playerSlotId] = failed;
                lastOperationStatus = failureStatus;
                lastOperationMessage = issue;
                return Result(
                    failureStatus,
                    Operation,
                    playerSlotId,
                    previous,
                    failed,
                    false,
                    false,
                    issue,
                    issue);
            }

            records.Remove(playerSlotId);
            revision++;
            PlayerGameplayAdmissionSummary current =
                PlayerGameplayAdmissionSummary.NotAdmitted(
                    sessionContextId,
                    playerSlotId,
                    previous.AdmissionRevision,
                    resolvedSource,
                    resolvedReason,
                    "Gameplay admission released in reverse order.");
            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayAdmissionStatus.SucceededReleased;
            lastOperationMessage = current.Message;

            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                false,
                false,
                string.Empty,
                lastOperationMessage);
        }

        internal bool TryReleaseAll(
            string source,
            string reason,
            out int releasedCount,
            out int failedCount,
            out string issue)
        {
            releasedCount = 0;
            failedCount = 0;
            var failures = new List<string>();
            PlayerGameplayAdmissionSnapshot snapshot = CreateSnapshot();

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerGameplayAdmissionSummary summary = snapshot.Slots[index];
                if (!summary.IsAdmitted)
                {
                    continue;
                }

                PlayerGameplayAdmissionResult result = TryRelease(
                    summary.PlayerSlotId,
                    summary.Token,
                    source,
                    reason);

                if (result.Succeeded) releasedCount++;
                else
                {
                    failedCount++;
                    failures.Add(result.ToDiagnosticString());
                }
            }

            issue = failures.Count == 0
                ? string.Empty
                : string.Join(" | ", failures);
            return failedCount == 0;
        }

        internal PlayerGameplayAdmissionSnapshot CreateSnapshot()
        {
            var ordered = new PlayerGameplayAdmissionSummary[
                orderedSlots.Length];
            for (int index = 0; index < orderedSlots.Length; index++)
            {
                ordered[index] = slots[orderedSlots[index]];
            }

            return new PlayerGameplayAdmissionSnapshot(
                sessionContextId,
                revision,
                ordered,
                lastOperationStatus,
                lastOperationMessage);
        }

        private bool TryPublishCamera(
            PlayerGameplayCameraEligibilitySummary cameraEligibility,
            CameraOutputSessionBinding outputSession,
            AdmissionRecord record,
            string source,
            string reason,
            out PlayerGameplayAdmissionStatus failureStatus,
            out string issue)
        {
            failureStatus = PlayerGameplayAdmissionStatus.None;
            issue = string.Empty;

            if (!cameraContext.TryGetEligibilityEvidence(
                    cameraEligibility.Token,
                    out PreparedPlayerCameraEligibilityEvidence evidence,
                    out issue))
            {
                failureStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleCameraEligibility;
                return false;
            }

            if (outputSession == null ||
                !outputSession.TryGetSession(
                    out CameraOutputSession session,
                    out issue))
            {
                failureStatus = PlayerGameplayAdmissionStatus
                    .FailedCameraOutputResolution;
                issue = string.IsNullOrEmpty(issue)
                    ? "Eligible per-Player camera requires an explicit initialized CameraOutputSessionBinding."
                    : issue;
                return false;
            }

            CameraRequestCreateResult requestCreation =
                CameraRequestCreateResult.Create(
                    new CameraRequestId(evidence.RequestId),
                    session.OutputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.LocalPlayer,
                        cameraEligibility.PlayerSlotId.Value.Value),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.LocalPlayerEligibility,
                        evidence.LifetimeScopeId),
                    CameraRigReference.FromComposer(evidence.CameraRig),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        evidence.FollowTarget,
                        $"Prepared Player camera target {cameraEligibility.PlayerSlotId.Value.Value}"),
                    new CameraRequestPolicy(
                        evidence.Precedence,
                        evidence.TieBreakerId),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    source,
                    $"{reason}; actor='{cameraEligibility.ActorId.StableText}'");

            if (!requestCreation.IsSucceeded)
            {
                failureStatus = PlayerGameplayAdmissionStatus
                    .FailedCameraRequestCreation;
                issue =
                    $"Gameplay admission could not create camera request. {requestCreation.BlockingIssue}";
                return false;
            }

            CameraRequestPublisherCreateResult publisherCreation =
                LocalPlayerCameraRequestPublisher.Create(
                    session,
                    requestCreation.Request);

            if (!publisherCreation.Succeeded ||
                !(publisherCreation.Publisher is
                    LocalPlayerCameraRequestPublisher publisher))
            {
                failureStatus = PlayerGameplayAdmissionStatus
                    .FailedCameraPublisherCreation;
                issue =
                    $"Gameplay admission could not create local Player camera publisher. {publisherCreation.DiagnosticSummary}";
                return false;
            }

            record.Publisher = publisher;
            record.Request = requestCreation.Request;
            CameraRequestPublisherResult publishResult = publisher.Publish();
            if (!publishResult.Succeeded || !publisher.IsPublished)
            {
                failureStatus = PlayerGameplayAdmissionStatus
                    .FailedCameraPublication;
                issue =
                    $"Gameplay admission could not publish local Player camera request. {publishResult.DiagnosticSummary}";
                return false;
            }

            return true;
        }

        private PlayerGameplayAdmissionResult RollbackFailedAdmission(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previous,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerGameplayCameraEligibilitySummary cameraEligibility,
            AdmissionRecord record,
            PlayerGameplayAdmissionStatus originalFailureStatus,
            string originalIssue,
            string source,
            string reason)
        {
            bool rollbackSucceeded = TryReleaseDependencies(
                record,
                source,
                $"{reason}; rollback",
                out PlayerGameplayAdmissionStatus rollbackFailureStatus,
                out string rollbackIssue);

            if (rollbackSucceeded)
            {
                lastOperationStatus = originalFailureStatus;
                lastOperationMessage =
                    $"{originalIssue} Admission prerequisites were rolled back.";
                return Result(
                    originalFailureStatus,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    true,
                    true,
                    string.Empty,
                    lastOperationMessage);
            }

            revision++;
            PlayerGameplayAdmissionSummary failed = CreateSummary(
                occupancy,
                inputBinding,
                cameraEligibility,
                record,
                PlayerGameplayAdmissionState.ReleaseFailed,
                source,
                reason,
                $"Admission failed and rollback did not complete. original='{originalIssue}' rollback='{rollbackIssue}'.");
            slots[playerSlotId] = failed;
            records[playerSlotId] = record;
            lastOperationStatus =
                PlayerGameplayAdmissionStatus.FailedAdmissionRollback;
            lastOperationMessage = failed.Message;

            return Result(
                PlayerGameplayAdmissionStatus.FailedAdmissionRollback,
                operation,
                playerSlotId,
                previous,
                failed,
                true,
                false,
                $"rollbackStatus='{rollbackFailureStatus}' {rollbackIssue}",
                lastOperationMessage);
        }

        private bool TryReleaseDependencies(
            AdmissionRecord record,
            string source,
            string reason,
            out PlayerGameplayAdmissionStatus failureStatus,
            out string issue)
        {
            failureStatus = PlayerGameplayAdmissionStatus.None;
            issue = string.Empty;

            if (!record.CameraRequestReleased)
            {
                if (record.Publisher == null || !record.Publisher.IsPublished)
                {
                    record.CameraRequestReleased = true;
                }
                else
                {
                    CameraRequestPublisherResult cameraRelease =
                        record.Publisher.Release();
                    if (!cameraRelease.Succeeded ||
                        record.Publisher.IsPublished)
                    {
                        failureStatus = PlayerGameplayAdmissionStatus
                            .FailedCameraRelease;
                        issue =
                            $"Camera request release failed. {cameraRelease.DiagnosticSummary}";
                        return false;
                    }

                    record.CameraRequestReleased = true;
                }
            }

            if (!record.CameraEligibilityReleased)
            {
                PlayerGameplayCameraEligibilityResult cameraRelease =
                    cameraContext.TryRelease(
                        record.Token.PlayerSlotId,
                        record.CameraEligibilityToken,
                        source,
                        reason);
                if (!cameraRelease.Succeeded)
                {
                    failureStatus = PlayerGameplayAdmissionStatus
                        .FailedCameraEligibilityRelease;
                    issue =
                        $"Camera eligibility release failed. {cameraRelease.ToDiagnosticString()}";
                    return false;
                }

                record.CameraEligibilityReleased = true;
            }

            if (!record.InputBindingReleased)
            {
                PlayerGameplayInputBindingResult inputRelease =
                    inputContext.TryRelease(
                        record.Token.PlayerSlotId,
                        record.InputBindingToken,
                        source,
                        reason);
                if (!inputRelease.Succeeded)
                {
                    failureStatus = PlayerGameplayAdmissionStatus
                        .FailedInputBindingRelease;
                    issue =
                        $"Gameplay input release failed. {inputRelease.ToDiagnosticString()}";
                    return false;
                }

                record.InputBindingReleased = true;
            }

            if (!record.OccupancyReleased)
            {
                PlayerGameplayOccupancyResult occupancyRelease =
                    occupancyContext.TryReleaseOccupancy(
                        record.Token.PlayerSlotId,
                        record.OccupancyToken,
                        source,
                        reason);
                if (!occupancyRelease.Succeeded)
                {
                    failureStatus = PlayerGameplayAdmissionStatus
                        .FailedOccupancyRelease;
                    issue =
                        $"Effective occupancy release failed. {occupancyRelease.ToDiagnosticString()}";
                    return false;
                }

                record.OccupancyReleased = true;
            }

            return true;
        }

        private bool TryValidateCurrentChain(
            in PlayerGameplayOccupancySummary suppliedOccupancy,
            in PlayerGameplayInputBindingSummary suppliedInputBinding,
            in PlayerGameplayCameraEligibilitySummary suppliedCameraEligibility,
            out CurrentChainEvidence chain,
            out PlayerSlotId playerSlotId,
            out PlayerGameplayAdmissionStatus rejectedStatus,
            out string issue)
        {
            chain = null;
            playerSlotId = suppliedOccupancy.PlayerSlotId.IsValid
                ? suppliedOccupancy.PlayerSlotId
                : suppliedInputBinding.PlayerSlotId.IsValid
                    ? suppliedInputBinding.PlayerSlotId
                    : suppliedCameraEligibility.PlayerSlotId;
            rejectedStatus =
                PlayerGameplayAdmissionStatus.RejectedInvalidRequest;
            issue = string.Empty;

            if (!suppliedOccupancy.IsValid ||
                !suppliedOccupancy.IsOccupied)
            {
                rejectedStatus =
                    PlayerGameplayAdmissionStatus.RejectedOccupancyNotReady;
                issue =
                    "Gameplay admission requires current effective occupancy.";
                return false;
            }

            if (!suppliedInputBinding.IsValid ||
                !suppliedInputBinding.IsBound)
            {
                rejectedStatus =
                    PlayerGameplayAdmissionStatus.RejectedInputBindingNotReady;
                issue =
                    "Gameplay admission requires a current Bound gameplay input binding.";
                return false;
            }

            if (!suppliedCameraEligibility.IsValid ||
                !suppliedCameraEligibility.HasCurrentDecision)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedCameraDecisionNotReady;
                issue =
                    "Gameplay admission requires Eligible or explicitly SkippedOptional camera evidence.";
                return false;
            }

            if (!string.Equals(
                    suppliedOccupancy.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    suppliedInputBinding.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    suppliedCameraEligibility.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                rejectedStatus =
                    PlayerGameplayAdmissionStatus.RejectedSessionMismatch;
                issue =
                    "Gameplay admission evidence belongs to another Session context.";
                return false;
            }

            if (!slots.ContainsKey(playerSlotId))
            {
                rejectedStatus =
                    PlayerGameplayAdmissionStatus.RejectedSlotNotConfigured;
                issue =
                    "Gameplay admission targets an unconfigured Player Slot.";
                return false;
            }

            var resolved = new CurrentChainEvidence();

            if (!occupancyContext.TryGetSummary(
                    playerSlotId,
                    out resolved.Occupancy) ||
                !resolved.Occupancy.IsOccupied ||
                resolved.Occupancy.Token != suppliedOccupancy.Token)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleOccupancy;
                issue =
                    "Supplied effective occupancy is foreign, released or stale.";
                return false;
            }

            PlayerGameplayInputBindingSnapshot inputSnapshot =
                inputContext.CreateSnapshot();
            if (!inputSnapshot.TryGetSummary(
                    playerSlotId,
                    out resolved.InputBinding) ||
                !resolved.InputBinding.IsBound ||
                resolved.InputBinding.Token != suppliedInputBinding.Token)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleInputBinding;
                issue =
                    "Supplied gameplay input binding is foreign, released or stale.";
                return false;
            }

            PlayerGameplayCameraEligibilitySnapshot cameraSnapshot =
                cameraContext.CreateSnapshot();
            if (!cameraSnapshot.TryGetSummary(
                    playerSlotId,
                    out resolved.CameraEligibility) ||
                !resolved.CameraEligibility.HasCurrentDecision ||
                resolved.CameraEligibility.Token !=
                    suppliedCameraEligibility.Token)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleCameraEligibility;
                issue =
                    "Supplied camera eligibility is foreign, released or stale.";
                return false;
            }

            if (resolved.InputBinding.OccupancyToken !=
                    resolved.Occupancy.Token ||
                resolved.CameraEligibility.OccupancyToken !=
                    resolved.Occupancy.Token ||
                resolved.CameraEligibility.InputBindingToken !=
                    resolved.InputBinding.Token ||
                resolved.InputBinding.PreparationToken !=
                    resolved.Occupancy.PreparationToken ||
                resolved.CameraEligibility.PreparationToken !=
                    resolved.Occupancy.PreparationToken ||
                resolved.InputBinding.PlayerSlotId !=
                    resolved.Occupancy.PlayerSlotId ||
                resolved.CameraEligibility.PlayerSlotId !=
                    resolved.Occupancy.PlayerSlotId ||
                resolved.InputBinding.ActorProfileId !=
                    resolved.Occupancy.ActorProfileId ||
                resolved.CameraEligibility.ActorProfileId !=
                    resolved.Occupancy.ActorProfileId ||
                resolved.InputBinding.ActorId !=
                    resolved.Occupancy.ActorId ||
                resolved.CameraEligibility.ActorId !=
                    resolved.Occupancy.ActorId ||
                resolved.InputBinding.Owner !=
                    resolved.Occupancy.Owner ||
                resolved.CameraEligibility.Owner !=
                    resolved.Occupancy.Owner ||
                resolved.InputBinding.RuntimeContentIdentity !=
                    resolved.Occupancy.RuntimeContentIdentity ||
                resolved.CameraEligibility.RuntimeContentIdentity !=
                    resolved.Occupancy.RuntimeContentIdentity)
            {
                rejectedStatus =
                    PlayerGameplayAdmissionStatus.RejectedInvalidRequest;
                issue =
                    "Occupancy, input and camera eligibility identities are incoherent.";
                return false;
            }

            chain = resolved;
            return true;
        }

        private bool TryResolveCurrentChain(
            AdmissionRecord record,
            out CurrentChainEvidence chain,
            out PlayerGameplayAdmissionStatus rejectedStatus,
            out string issue)
        {
            chain = null;
            rejectedStatus =
                PlayerGameplayAdmissionStatus.RejectedInvalidRequest;
            issue = string.Empty;

            var resolved = new CurrentChainEvidence();

            if (!occupancyContext.TryGetSummary(
                    record.Token.PlayerSlotId,
                    out resolved.Occupancy) ||
                !resolved.Occupancy.IsOccupied ||
                resolved.Occupancy.Token != record.OccupancyToken)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleOccupancy;
                issue =
                    "Current gameplay admission occupancy is no longer live.";
                return false;
            }

            PlayerGameplayInputBindingSnapshot inputSnapshot =
                inputContext.CreateSnapshot();
            if (!inputSnapshot.TryGetSummary(
                    record.Token.PlayerSlotId,
                    out resolved.InputBinding) ||
                !resolved.InputBinding.IsBound ||
                resolved.InputBinding.Token != record.InputBindingToken)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleInputBinding;
                issue =
                    "Current gameplay admission input binding is no longer live.";
                return false;
            }

            PlayerGameplayCameraEligibilitySnapshot cameraSnapshot =
                cameraContext.CreateSnapshot();
            if (!cameraSnapshot.TryGetSummary(
                    record.Token.PlayerSlotId,
                    out resolved.CameraEligibility) ||
                !resolved.CameraEligibility.HasCurrentDecision ||
                resolved.CameraEligibility.Token !=
                    record.CameraEligibilityToken)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleCameraEligibility;
                issue =
                    "Current gameplay admission camera eligibility is no longer live.";
                return false;
            }

            if (resolved.CameraEligibility.IsEligible &&
                !record.CameraRequestPublished)
            {
                rejectedStatus = PlayerGameplayAdmissionStatus
                    .RejectedForeignOrStaleAdmission;
                issue =
                    "Current gameplay admission lost its published camera request.";
                return false;
            }

            chain = resolved;
            return true;
        }

        private PlayerGameplayAdmissionResult RefreshCurrentAdmission(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previous,
            PlayerGameplayInputBindingSummary inputBinding,
            string source,
            string reason,
            PlayerGameplayAdmissionStatus status,
            string message)
        {
            PlayerGameplayAdmissionState state = inputBinding.IsAllowed
                ? PlayerGameplayAdmissionState.Ready
                : PlayerGameplayAdmissionState.BlockedByInputGate;
            PlayerGameplayAdmissionSummary current =
                new PlayerGameplayAdmissionSummary(
                    previous.SessionContextId,
                    previous.PlayerSlotId,
                    state,
                    previous.ActorProfileId,
                    previous.ActorId,
                    previous.Owner,
                    previous.RuntimeContentIdentity,
                    previous.PreparationToken,
                    previous.OccupancyToken,
                    previous.InputBindingToken,
                    previous.CameraEligibilityToken,
                    previous.Token,
                    previous.CameraEligibilityState,
                    previous.CameraRequiredness,
                    previous.CameraRequestPublished,
                    previous.CameraRequestId,
                    previous.CameraOutputId,
                    previous.CameraRequestReleased,
                    previous.CameraEligibilityReleased,
                    previous.InputBindingReleased,
                    previous.OccupancyReleased,
                    previous.AdmissionRevision,
                    source,
                    reason,
                    message);

            if (current.State != previous.State)
            {
                revision++;
            }

            slots[playerSlotId] = current;
            lastOperationStatus = status;
            lastOperationMessage = current.Message;
            return Result(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                false,
                false,
                string.Empty,
                current.Message);
        }

        private PlayerGameplayAdmissionSummary CreateSummary(
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerGameplayCameraEligibilitySummary cameraEligibility,
            AdmissionRecord record,
            PlayerGameplayAdmissionState state,
            string source,
            string reason,
            string message)
        {
            bool cameraPublished = record.CameraRequestPublished;
            string requestId = record.Request.IsValid
                ? record.Request.RequestId.ToString()
                : string.Empty;
            string outputId = record.Request.IsValid
                ? record.Request.OutputId.ToString()
                : string.Empty;

            return new PlayerGameplayAdmissionSummary(
                sessionContextId,
                occupancy.PlayerSlotId,
                state,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.Owner,
                occupancy.RuntimeContentIdentity,
                occupancy.PreparationToken,
                occupancy.Token,
                inputBinding.Token,
                cameraEligibility.Token,
                record.Token,
                cameraEligibility.State,
                cameraEligibility.Requiredness,
                cameraPublished,
                requestId,
                outputId,
                record.CameraRequestReleased,
                record.CameraEligibilityReleased,
                record.InputBindingReleased,
                record.OccupancyReleased,
                record.Token.AdmissionRevision,
                source,
                reason,
                message);
        }

        private PlayerGameplayAdmissionSummary CreateReleaseFailedSummary(
            PlayerGameplayAdmissionSummary previous,
            AdmissionRecord record,
            string source,
            string reason,
            string message)
        {
            return new PlayerGameplayAdmissionSummary(
                previous.SessionContextId,
                previous.PlayerSlotId,
                PlayerGameplayAdmissionState.ReleaseFailed,
                previous.ActorProfileId,
                previous.ActorId,
                previous.Owner,
                previous.RuntimeContentIdentity,
                previous.PreparationToken,
                previous.OccupancyToken,
                previous.InputBindingToken,
                previous.CameraEligibilityToken,
                previous.Token,
                previous.CameraEligibilityState,
                previous.CameraRequiredness,
                record.CameraRequestPublished,
                previous.CameraRequestId,
                previous.CameraOutputId,
                record.CameraRequestReleased,
                record.CameraEligibilityReleased,
                record.InputBindingReleased,
                record.OccupancyReleased,
                previous.AdmissionRevision,
                source,
                reason,
                message);
        }

        private PlayerGameplayAdmissionSummary GetSummaryOrDefault(
            PlayerSlotId playerSlotId)
        {
            return playerSlotId.IsValid &&
                slots.TryGetValue(playerSlotId, out PlayerGameplayAdmissionSummary summary)
                    ? summary
                    : default;
        }

        private PlayerGameplayAdmissionResult Reject(
            PlayerGameplayAdmissionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary current,
            string message)
        {
            lastOperationStatus = status;
            lastOperationMessage = message.NormalizeText();
            return Result(
                status,
                operation,
                playerSlotId,
                current,
                current,
                false,
                false,
                string.Empty,
                lastOperationMessage);
        }

        private PlayerGameplayAdmissionResult Result(
            PlayerGameplayAdmissionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previous,
            PlayerGameplayAdmissionSummary current,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackIssue,
            string message)
        {
            return new PlayerGameplayAdmissionResult(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                CreateSnapshot(),
                rollbackAttempted,
                rollbackSucceeded,
                rollbackIssue,
                message);
        }
    }
}
