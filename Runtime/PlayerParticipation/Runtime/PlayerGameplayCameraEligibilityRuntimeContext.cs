using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal physical evidence retained only for the later camera request publication cut.
    /// Public snapshots intentionally retain no Unity object references.
    /// </summary>
    internal readonly struct PreparedPlayerCameraEligibilityEvidence
    {
        internal PreparedPlayerCameraEligibilityEvidence(
            PlayerGameplayCameraEligibilityToken token,
            PlayerGameplayCameraAuthoring authoring,
            CameraRigComposer cameraRig,
            Transform followTarget,
            Transform lookAtTarget,
            PlayerGameplayCameraRequiredness requiredness,
            int precedence,
            string requestId,
            string lifetimeScopeId,
            string tieBreakerId)
        {
            Token = token;
            Authoring = authoring;
            CameraRig = cameraRig;
            FollowTarget = followTarget;
            LookAtTarget = lookAtTarget;
            Requiredness = requiredness;
            Precedence = precedence;
            RequestId = requestId ?? string.Empty;
            LifetimeScopeId = lifetimeScopeId ?? string.Empty;
            TieBreakerId = tieBreakerId ?? string.Empty;
        }

        internal PlayerGameplayCameraEligibilityToken Token { get; }
        internal PlayerGameplayCameraAuthoring Authoring { get; }
        internal CameraRigComposer CameraRig { get; }
        internal Transform FollowTarget { get; }
        internal Transform LookAtTarget { get; }
        internal PlayerGameplayCameraRequiredness Requiredness { get; }
        internal int Precedence { get; }
        internal string RequestId { get; }
        internal string LifetimeScopeId { get; }
        internal string TieBreakerId { get; }

        internal bool IsValid =>
            Token.IsValid &&
            Authoring != null &&
            CameraRig != null &&
            FollowTarget != null &&
            Requiredness != PlayerGameplayCameraRequiredness.None &&
            !string.IsNullOrEmpty(RequestId) &&
            !string.IsNullOrEmpty(LifetimeScopeId) &&
            !string.IsNullOrEmpty(TieBreakerId);
    }

    /// <summary>
    /// Session-scoped authority that decides whether one current prepared, effectively
    /// occupying and gameplay-input-bound Player Actor has coherent explicit camera
    /// authoring. It does not publish a CameraRequest, select a winner, mutate
    /// Cinemachine or aggregate GameplayReady.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.4 prepared Player camera eligibility authority.")]
    internal sealed class PlayerGameplayCameraEligibilityRuntimeContext
    {
        private sealed class EligibilityRecord
        {
            internal PlayerGameplayCameraAuthoring Authoring;
            internal CameraRigComposer CameraRig;
            internal Transform FollowTarget;
            internal Transform LookAtTarget;
            internal PlayerGameplayCameraRequiredness Requiredness;
            internal int Precedence;
            internal string RequestId;
            internal string LifetimeScopeId;
            internal string TieBreakerId;
        }

        private readonly string sessionContextId;
        private readonly PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext inputContext;
        private readonly PlayerSlotId[] orderedSlots;
        private readonly Dictionary<
            PlayerSlotId,
            PlayerGameplayCameraEligibilitySummary> slots;
        private readonly Dictionary<PlayerSlotId, EligibilityRecord> records;

        private int revision = 1;
        private int eligibilitySequence;
        private PlayerGameplayCameraEligibilityStatus lastOperationStatus;
        private string lastOperationMessage =
            "Player gameplay camera eligibility runtime initialized.";

        private PlayerGameplayCameraEligibilityRuntimeContext(
            string sessionContextId,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerSlotId[] orderedSlots)
        {
            this.sessionContextId = sessionContextId;
            this.occupancyContext = occupancyContext;
            this.inputContext = inputContext;
            this.orderedSlots = orderedSlots;
            slots = new Dictionary<
                PlayerSlotId,
                PlayerGameplayCameraEligibilitySummary>(orderedSlots.Length);
            records = new Dictionary<PlayerSlotId, EligibilityRecord>(
                orderedSlots.Length);

            for (int index = 0; index < orderedSlots.Length; index++)
            {
                PlayerSlotId slot = orderedSlots[index];
                slots.Add(
                    slot,
                    PlayerGameplayCameraEligibilitySummary.NotEvaluated(
                        sessionContextId,
                        slot,
                        0,
                        nameof(PlayerGameplayCameraEligibilityRuntimeContext),
                        "runtime-initialization",
                        "Configured Player Slot has no camera eligibility decision."));
            }
        }

        internal string SessionContextId => sessionContextId;
        internal int Revision => revision;

        internal static bool TryCreate(
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            out PlayerGameplayCameraEligibilityRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (occupancyContext == null)
            {
                issue =
                    "Camera eligibility requires an explicit effective occupancy authority.";
                return false;
            }

            if (inputContext == null)
            {
                issue =
                    "Camera eligibility requires an explicit gameplay input binding authority.";
                return false;
            }

            PlayerGameplayOccupancySnapshot occupancySnapshot =
                occupancyContext.CreateSnapshot();
            PlayerGameplayInputBindingSnapshot inputSnapshot =
                inputContext.CreateSnapshot();

            if (occupancySnapshot == null ||
                !occupancySnapshot.IsInitialized ||
                string.IsNullOrEmpty(occupancySnapshot.SessionContextId))
            {
                issue =
                    "Camera eligibility requires an initialized effective occupancy snapshot.";
                return false;
            }

            if (inputSnapshot == null ||
                !inputSnapshot.IsInitialized ||
                string.IsNullOrEmpty(inputSnapshot.SessionContextId))
            {
                issue =
                    "Camera eligibility requires an initialized gameplay input binding snapshot.";
                return false;
            }

            if (!string.Equals(
                    occupancySnapshot.SessionContextId,
                    inputSnapshot.SessionContextId,
                    StringComparison.Ordinal))
            {
                issue =
                    "Camera eligibility authorities belong to different Session contexts.";
                return false;
            }

            if (occupancySnapshot.ConfiguredSlotCount <= 0 ||
                occupancySnapshot.ConfiguredSlotCount !=
                inputSnapshot.ConfiguredSlotCount)
            {
                issue =
                    "Camera eligibility requires matching non-empty occupancy and input Slot rosters.";
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
                    !string.Equals(
                        occupancy.SessionContextId,
                        occupancySnapshot.SessionContextId,
                        StringComparison.Ordinal))
                {
                    issue =
                        $"Camera eligibility rejected invalid occupancy Slot evidence at index '{index}'.";
                    return false;
                }

                if (!inputSnapshot.TryGetSummary(
                        occupancy.PlayerSlotId,
                        out PlayerGameplayInputBindingSummary input) ||
                    !input.IsValid ||
                    !string.Equals(
                        input.SessionContextId,
                        occupancySnapshot.SessionContextId,
                        StringComparison.Ordinal))
                {
                    issue =
                        $"Camera eligibility could not match input Slot '{occupancy.PlayerSlotId.StableText}'.";
                    return false;
                }

                if (!unique.Add(occupancy.PlayerSlotId))
                {
                    issue =
                        $"Camera eligibility rejected duplicate Slot '{occupancy.PlayerSlotId.StableText}'.";
                    return false;
                }

                ordered[index] = occupancy.PlayerSlotId;
            }

            context = new PlayerGameplayCameraEligibilityRuntimeContext(
                occupancySnapshot.SessionContextId,
                occupancyContext,
                inputContext,
                ordered);
            return true;
        }

        internal PlayerGameplayCameraEligibilityResult TryConfirmEligibility(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerActorDeclaration actorDeclaration,
            PlayerGameplayCameraAuthoring authoring,
            string source,
            string reason)
        {
            const string Operation = "ConfirmCameraEligibility";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayCameraEligibilityRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "confirm-prepared-player-camera-eligibility");

            if (!TryValidateIdentityChain(
                    preparation,
                    occupancy,
                    inputBinding,
                    out PlayerSlotId playerSlotId,
                    out PlayerGameplayCameraEligibilityStatus rejectedStatus,
                    out string issue))
            {
                return Reject(
                    rejectedStatus,
                    Operation,
                    playerSlotId,
                    GetSummaryOrDefault(playerSlotId),
                    issue);
            }

            PlayerGameplayCameraEligibilitySummary previous =
                slots[playerSlotId];

            if (!TryValidateActorAndAuthoring(
                    occupancy,
                    actorDeclaration,
                    authoring,
                    out rejectedStatus,
                    out issue))
            {
                return Reject(
                    rejectedStatus,
                    Operation,
                    playerSlotId,
                    previous,
                    issue);
            }

            if (previous.HasCurrentDecision)
            {
                if (previous.IsEligible &&
                    previous.PreparationToken == preparation.Token &&
                    previous.OccupancyToken == occupancy.Token &&
                    previous.InputBindingToken == inputBinding.Token &&
                    records.TryGetValue(
                        playerSlotId,
                        out EligibilityRecord currentRecord) &&
                    IsSameAuthoring(currentRecord, authoring))
                {
                    lastOperationStatus =
                        PlayerGameplayCameraEligibilityStatus
                            .SucceededAlreadyEligible;
                    lastOperationMessage =
                        "Prepared Player camera authoring is already eligible.";
                    return Result(
                        lastOperationStatus,
                        Operation,
                        playerSlotId,
                        previous,
                        previous,
                        lastOperationMessage);
                }

                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedSlotAlreadyEvaluated,
                    Operation,
                    playerSlotId,
                    previous,
                    "Player Slot already has another current camera eligibility decision.");
            }

            eligibilitySequence++;
            revision++;

            var token = new PlayerGameplayCameraEligibilityToken(
                sessionContextId,
                occupancy.Owner,
                playerSlotId,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.PreparationToken,
                occupancy.Token,
                inputBinding.Token,
                occupancy.RuntimeContentIdentity,
                inputBinding.Token.MaterializationRevision,
                occupancy.OccupancyRevision,
                inputBinding.BindingRevision,
                eligibilitySequence);

            string requestId = CreateRequestId(token);
            string lifetimeScopeId = CreateLifetimeScopeId(token);
            string tieBreakerId = CreateTieBreakerId(token);

            var record = new EligibilityRecord
            {
                Authoring = authoring,
                CameraRig = authoring.CameraRig,
                FollowTarget = authoring.FollowTarget,
                LookAtTarget = authoring.LookAtTarget,
                Requiredness = authoring.Requiredness,
                Precedence = authoring.Precedence,
                RequestId = requestId,
                LifetimeScopeId = lifetimeScopeId,
                TieBreakerId = tieBreakerId
            };

            var current = new PlayerGameplayCameraEligibilitySummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayCameraEligibilityState.Eligible,
                authoring.Requiredness,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.Owner,
                occupancy.RuntimeContentIdentity,
                occupancy.PreparationToken,
                occupancy.Token,
                inputBinding.Token,
                token,
                authoring.CameraRig.name,
                authoring.FollowTarget.name,
                authoring.LookAtTarget != null
                    ? authoring.LookAtTarget.name
                    : string.Empty,
                authoring.Precedence,
                requestId,
                lifetimeScopeId,
                tieBreakerId,
                eligibilitySequence,
                resolvedSource,
                resolvedReason,
                "Prepared occupied and gameplay-input-bound Player Actor has coherent camera authoring.");

            if (!current.IsValid)
            {
                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigConfiguration,
                    Operation,
                    playerSlotId,
                    previous,
                    "Camera eligibility token or summary creation produced incoherent evidence.");
            }

            slots[playerSlotId] = current;
            records[playerSlotId] = record;
            lastOperationStatus =
                PlayerGameplayCameraEligibilityStatus.SucceededEligible;
            lastOperationMessage = current.Message;

            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                lastOperationMessage);
        }

        internal PlayerGameplayCameraEligibilityResult TrySkipOptional(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerGameplayCameraRequiredness requiredness,
            string source,
            string reason)
        {
            const string Operation = "SkipOptionalCamera";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayCameraEligibilityRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "skip-optional-prepared-player-camera");

            if (requiredness != PlayerGameplayCameraRequiredness.Optional)
            {
                PlayerSlotId invalidSlot = occupancy.PlayerSlotId.IsValid
                    ? occupancy.PlayerSlotId
                    : preparation.PlayerSlotId;
                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedOptionalSkipRequired,
                    Operation,
                    invalidSlot,
                    GetSummaryOrDefault(invalidSlot),
                    "Only Optional per-Player camera policy may be skipped.");
            }

            if (!TryValidateIdentityChain(
                    preparation,
                    occupancy,
                    inputBinding,
                    out PlayerSlotId playerSlotId,
                    out PlayerGameplayCameraEligibilityStatus rejectedStatus,
                    out string issue))
            {
                return Reject(
                    rejectedStatus,
                    Operation,
                    playerSlotId,
                    GetSummaryOrDefault(playerSlotId),
                    issue);
            }

            PlayerGameplayCameraEligibilitySummary previous =
                slots[playerSlotId];

            if (previous.HasCurrentDecision)
            {
                if (previous.IsSkippedOptional &&
                    previous.PreparationToken == preparation.Token &&
                    previous.OccupancyToken == occupancy.Token &&
                    previous.InputBindingToken == inputBinding.Token)
                {
                    lastOperationStatus =
                        PlayerGameplayCameraEligibilityStatus
                            .SucceededAlreadySkipped;
                    lastOperationMessage =
                        "Optional per-Player camera is already skipped.";
                    return Result(
                        lastOperationStatus,
                        Operation,
                        playerSlotId,
                        previous,
                        previous,
                        lastOperationMessage);
                }

                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedSlotAlreadyEvaluated,
                    Operation,
                    playerSlotId,
                    previous,
                    "Player Slot already has another current camera eligibility decision.");
            }

            eligibilitySequence++;
            revision++;

            var token = new PlayerGameplayCameraEligibilityToken(
                sessionContextId,
                occupancy.Owner,
                playerSlotId,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.PreparationToken,
                occupancy.Token,
                inputBinding.Token,
                occupancy.RuntimeContentIdentity,
                inputBinding.Token.MaterializationRevision,
                occupancy.OccupancyRevision,
                inputBinding.BindingRevision,
                eligibilitySequence);

            var current = new PlayerGameplayCameraEligibilitySummary(
                sessionContextId,
                playerSlotId,
                PlayerGameplayCameraEligibilityState.SkippedOptional,
                PlayerGameplayCameraRequiredness.Optional,
                occupancy.ActorProfileId,
                occupancy.ActorId,
                occupancy.Owner,
                occupancy.RuntimeContentIdentity,
                occupancy.PreparationToken,
                occupancy.Token,
                inputBinding.Token,
                token,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                eligibilitySequence,
                resolvedSource,
                resolvedReason,
                "Optional per-Player camera was explicitly skipped for this preparation.");

            if (!current.IsValid)
            {
                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedInvalidRequest,
                    Operation,
                    playerSlotId,
                    previous,
                    "Optional camera skip produced incoherent eligibility evidence.");
            }

            slots[playerSlotId] = current;
            records.Remove(playerSlotId);
            lastOperationStatus =
                PlayerGameplayCameraEligibilityStatus
                    .SucceededSkippedOptional;
            lastOperationMessage = current.Message;

            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                lastOperationMessage);
        }

        internal PlayerGameplayCameraEligibilityResult TryRelease(
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilityToken expectedEligibility,
            string source,
            string reason)
        {
            const string Operation = "ReleaseCameraEligibility";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerGameplayCameraEligibilityRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-prepared-player-camera-eligibility");

            if (!slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayCameraEligibilitySummary previous))
            {
                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedSlotNotConfigured,
                    Operation,
                    playerSlotId,
                    default,
                    "Camera eligibility release targets an unconfigured Player Slot.");
            }

            if (previous.IsNotEvaluated)
            {
                if (expectedEligibility.IsValid)
                {
                    return Reject(
                        PlayerGameplayCameraEligibilityStatus
                            .RejectedForeignOrStaleEligibility,
                        Operation,
                        playerSlotId,
                        previous,
                        "Camera eligibility is already released and the supplied token is stale.");
                }

                lastOperationStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededAlreadyReleased;
                lastOperationMessage =
                    "Camera eligibility is already released.";
                return Result(
                    lastOperationStatus,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    lastOperationMessage);
            }

            if (!expectedEligibility.IsValid ||
                previous.Token != expectedEligibility)
            {
                return Reject(
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleEligibility,
                    Operation,
                    playerSlotId,
                    previous,
                    "Camera eligibility release requires the exact current token.");
            }

            records.Remove(playerSlotId);
            revision++;

            PlayerGameplayCameraEligibilitySummary current =
                PlayerGameplayCameraEligibilitySummary.NotEvaluated(
                    sessionContextId,
                    playerSlotId,
                    previous.EligibilityRevision,
                    resolvedSource,
                    resolvedReason,
                    "Prepared Player camera eligibility released.");

            slots[playerSlotId] = current;
            lastOperationStatus =
                PlayerGameplayCameraEligibilityStatus.SucceededReleased;
            lastOperationMessage = current.Message;

            return Result(
                lastOperationStatus,
                Operation,
                playerSlotId,
                previous,
                current,
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
            PlayerGameplayCameraEligibilitySnapshot snapshot =
                CreateSnapshot();

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerGameplayCameraEligibilitySummary summary =
                    snapshot.Slots[index];

                if (!summary.HasCurrentDecision)
                {
                    continue;
                }

                PlayerGameplayCameraEligibilityResult result =
                    TryRelease(
                        summary.PlayerSlotId,
                        summary.Token,
                        source,
                        reason);

                if (result.Succeeded)
                {
                    releasedCount++;
                }
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

        internal PlayerGameplayCameraEligibilitySnapshot CreateSnapshot()
        {
            var ordered =
                new PlayerGameplayCameraEligibilitySummary[
                    orderedSlots.Length];

            for (int index = 0; index < orderedSlots.Length; index++)
            {
                ordered[index] = slots[orderedSlots[index]];
            }

            return new PlayerGameplayCameraEligibilitySnapshot(
                sessionContextId,
                revision,
                ordered,
                lastOperationStatus,
                lastOperationMessage);
        }

        internal bool TryGetEligibilityEvidence(
            PlayerGameplayCameraEligibilityToken token,
            out PreparedPlayerCameraEligibilityEvidence evidence,
            out string issue)
        {
            evidence = default;
            issue = string.Empty;

            if (!token.IsValid ||
                !slots.TryGetValue(
                    token.PlayerSlotId,
                    out PlayerGameplayCameraEligibilitySummary summary) ||
                !summary.IsEligible ||
                summary.Token != token)
            {
                issue =
                    "Prepared Player camera eligibility token is foreign, stale or not Eligible.";
                return false;
            }

            if (!records.TryGetValue(
                    token.PlayerSlotId,
                    out EligibilityRecord record))
            {
                issue =
                    "Prepared Player camera physical eligibility evidence is missing.";
                return false;
            }

            evidence = new PreparedPlayerCameraEligibilityEvidence(
                token,
                record.Authoring,
                record.CameraRig,
                record.FollowTarget,
                record.LookAtTarget,
                record.Requiredness,
                record.Precedence,
                record.RequestId,
                record.LifetimeScopeId,
                record.TieBreakerId);

            if (!evidence.IsValid)
            {
                evidence = default;
                issue =
                    "Prepared Player camera physical eligibility evidence is incoherent.";
                return false;
            }

            return true;
        }

        private bool TryValidateIdentityChain(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            out PlayerSlotId playerSlotId,
            out PlayerGameplayCameraEligibilityStatus rejectedStatus,
            out string issue)
        {
            playerSlotId = occupancy.PlayerSlotId.IsValid
                ? occupancy.PlayerSlotId
                : preparation.PlayerSlotId;
            rejectedStatus =
                PlayerGameplayCameraEligibilityStatus.RejectedInvalidRequest;
            issue = string.Empty;

            if (!playerSlotId.IsValid ||
                !preparation.IsValid ||
                !occupancy.IsValid ||
                !inputBinding.IsValid)
            {
                issue =
                    "Camera eligibility requires valid preparation, occupancy and input binding evidence.";
                return false;
            }

            if (!string.Equals(
                    preparation.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    occupancy.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    inputBinding.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedSessionMismatch;
                issue =
                    "Preparation, occupancy or input binding belongs to another Session context.";
                return false;
            }

            if (!slots.ContainsKey(playerSlotId))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedSlotNotConfigured;
                issue =
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this camera eligibility context.";
                return false;
            }

            if (!preparation.IsPrepared ||
                !preparation.Materialization.IsActive ||
                !preparation.Token.IsValid)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedPreparationNotReady;
                issue =
                    "Camera eligibility requires an Active prepared Logical Player Actor.";
                return false;
            }

            if (!occupancy.IsOccupied ||
                !occupancy.Token.IsValid)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedOccupancyNotReady;
                issue =
                    "Camera eligibility requires current effective occupancy.";
                return false;
            }

            if (!occupancyContext.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayOccupancySummary currentOccupancy) ||
                !currentOccupancy.IsOccupied ||
                currentOccupancy.Token != occupancy.Token)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleOccupancy;
                issue =
                    "Supplied occupancy is no longer current in the effective occupancy authority.";
                return false;
            }

            occupancy = currentOccupancy;

            if (!IsPreparationAndOccupancyCoherent(
                    preparation,
                    occupancy))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleOccupancy;
                issue =
                    "Preparation and occupancy identities are foreign, mismatched or stale.";
                return false;
            }

            if (!inputBinding.IsBound ||
                !inputBinding.Token.IsValid)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedInputBindingNotReady;
                issue =
                    "Camera eligibility requires a current gameplay input binding.";
                return false;
            }

            PlayerGameplayInputBindingSnapshot inputSnapshot =
                inputContext.CreateSnapshot();

            if (!inputSnapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary currentInput) ||
                !currentInput.IsBound ||
                currentInput.Token != inputBinding.Token)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleInputBinding;
                issue =
                    "Supplied gameplay input binding is no longer current.";
                return false;
            }

            if (!IsInputCoherent(
                    preparation,
                    occupancy,
                    currentInput))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleInputBinding;
                issue =
                    "Gameplay input binding does not match the current preparation and occupancy.";
                return false;
            }

            rejectedStatus =
                PlayerGameplayCameraEligibilityStatus.SucceededEligible;
            return true;
        }

        private static bool TryValidateActorAndAuthoring(
            PlayerGameplayOccupancySummary occupancy,
            PlayerActorDeclaration actorDeclaration,
            PlayerGameplayCameraAuthoring authoring,
            out PlayerGameplayCameraEligibilityStatus rejectedStatus,
            out string issue)
        {
            rejectedStatus =
                PlayerGameplayCameraEligibilityStatus.RejectedActorMismatch;
            issue = string.Empty;

            if (actorDeclaration == null)
            {
                issue =
                    "Camera eligibility requires the prepared PlayerActorDeclaration.";
                return false;
            }

            ActorId actorId;
            try
            {
                actorId = actorDeclaration.ActorId;
            }
            catch (Exception exception)
            {
                issue =
                    $"Prepared PlayerActorDeclaration has an invalid ActorId. {exception.Message}";
                return false;
            }

            if (actorId != occupancy.ActorId)
            {
                issue =
                    "Prepared PlayerActorDeclaration ActorId does not match effective occupancy.";
                return false;
            }

            if (authoring == null)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedAuthoringMissing;
                issue =
                    "Camera eligibility requires explicit PlayerGameplayCameraAuthoring.";
                return false;
            }

            if (!IsOwnedByActor(
                    actorDeclaration.transform,
                    authoring.transform))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedAuthoringHierarchyMismatch;
                issue =
                    "PlayerGameplayCameraAuthoring does not belong to the prepared Actor hierarchy.";
                return false;
            }

            if (authoring.Requiredness !=
                    PlayerGameplayCameraRequiredness.Optional &&
                authoring.Requiredness !=
                    PlayerGameplayCameraRequiredness.Required)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRequirednessInvalid;
                issue =
                    "Player gameplay camera requiredness must be Optional or Required.";
                return false;
            }

            CameraRigComposer rig = authoring.CameraRig;
            if (rig == null)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus.RejectedRigMissing;
                issue =
                    "Player gameplay camera authoring requires an explicit CameraRigComposer.";
                return false;
            }

            if (!IsOwnedByActor(
                    actorDeclaration.transform,
                    rig.transform))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigHierarchyMismatch;
                issue =
                    "CameraRigComposer does not belong to the prepared Actor hierarchy.";
                return false;
            }

            if (rig.TargetSourceKind !=
                    CameraTargetSourceKind.ExplicitTransform ||
                rig.PreAuthoredPlayerComposer != null)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigUsesPlayerComposer;
                issue =
                    "Prepared Player camera rig must use ExplicitTransform and must not reference PreAuthoredPlayerComposer.";
                return false;
            }

            Transform followTarget = authoring.FollowTarget;
            if (followTarget == null)
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedFollowTargetMissing;
                issue =
                    "Prepared Player camera authoring requires an explicit Follow target.";
                return false;
            }

            if (!IsOwnedByActor(
                    actorDeclaration.transform,
                    followTarget))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedFollowTargetHierarchyMismatch;
                issue =
                    "Prepared Player camera Follow target does not belong to the Actor hierarchy.";
                return false;
            }

            Transform lookAtTarget = authoring.LookAtTarget;
            if (lookAtTarget != null &&
                !IsOwnedByActor(
                    actorDeclaration.transform,
                    lookAtTarget))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedLookAtTargetHierarchyMismatch;
                issue =
                    "Prepared Player camera LookAt target does not belong to the Actor hierarchy.";
                return false;
            }

            if (!ReferenceEquals(
                    rig.ExplicitFollowTarget,
                    followTarget) ||
                !ReferenceEquals(
                    rig.ExplicitLookAtTarget,
                    lookAtTarget))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigTargetMismatch;
                issue =
                    "CameraRigComposer explicit targets do not match PlayerGameplayCameraAuthoring.";
                return false;
            }

            if (rig.FollowRequirement ==
                    CameraTargetRequirement.NotUsed ||
                (rig.LookAtRequirement ==
                    CameraTargetRequirement.Required &&
                 lookAtTarget == null) ||
                (rig.LookAtRequirement ==
                    CameraTargetRequirement.NotUsed &&
                 lookAtTarget != null))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigConfiguration;
                issue =
                    "CameraRigComposer target requirements are incompatible with the explicit Actor targets.";
                return false;
            }

            if (!rig.TryValidateForApply(out string rigIssue))
            {
                rejectedStatus =
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigConfiguration;
                issue =
                    $"CameraRigComposer validation failed. {rigIssue}";
                return false;
            }

            rejectedStatus =
                PlayerGameplayCameraEligibilityStatus.SucceededEligible;
            return true;
        }

        private static bool IsPreparationAndOccupancyCoherent(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy)
        {
            return
                preparation.PlayerSlotId == occupancy.PlayerSlotId &&
                preparation.PreparedActorProfileId ==
                    occupancy.ActorProfileId &&
                preparation.Materialization.ActorId ==
                    occupancy.ActorId &&
                preparation.Materialization.Owner ==
                    occupancy.Owner &&
                preparation.Materialization.RuntimeContentIdentity ==
                    occupancy.RuntimeContentIdentity &&
                preparation.Token == occupancy.PreparationToken &&
                occupancy.Token.PreparationToken ==
                    preparation.Token &&
                occupancy.Token.ActorId ==
                    preparation.Materialization.ActorId &&
                occupancy.Token.RuntimeContentIdentity ==
                    preparation.Materialization.RuntimeContentIdentity;
        }

        private static bool IsInputCoherent(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary input)
        {
            return
                input.PlayerSlotId == occupancy.PlayerSlotId &&
                input.ActorProfileId == occupancy.ActorProfileId &&
                input.ActorId == occupancy.ActorId &&
                input.Owner == occupancy.Owner &&
                input.RuntimeContentIdentity ==
                    occupancy.RuntimeContentIdentity &&
                input.PreparationToken == preparation.Token &&
                input.OccupancyToken == occupancy.Token &&
                input.Token.PreparationToken ==
                    preparation.Token &&
                input.Token.OccupancyToken ==
                    occupancy.Token;
        }

        private static bool IsOwnedByActor(
            Transform actorRoot,
            Transform target)
        {
            return actorRoot != null &&
                target != null &&
                (ReferenceEquals(actorRoot, target) ||
                 target.IsChildOf(actorRoot));
        }

        private static bool IsSameAuthoring(
            EligibilityRecord record,
            PlayerGameplayCameraAuthoring authoring)
        {
            return record != null &&
                authoring != null &&
                ReferenceEquals(record.Authoring, authoring) &&
                ReferenceEquals(record.CameraRig, authoring.CameraRig) &&
                ReferenceEquals(record.FollowTarget, authoring.FollowTarget) &&
                ReferenceEquals(record.LookAtTarget, authoring.LookAtTarget) &&
                record.Requiredness == authoring.Requiredness &&
                record.Precedence == authoring.Precedence;
        }

        private static string CreateRequestId(
            PlayerGameplayCameraEligibilityToken token)
        {
            return token.IsValid
                ? $"player.camera:{token.SessionContextId}:" +
                  $"{token.PlayerSlotId.Value.Value}:" +
                  $"{token.ActorId.Value.Value}:" +
                  $"{token.MaterializationRevision}:" +
                  $"{token.EligibilityRevision}"
                : string.Empty;
        }

        private static string CreateLifetimeScopeId(
            PlayerGameplayCameraEligibilityToken token)
        {
            return token.IsValid
                ? $"player.camera.eligibility:{token.SessionContextId}:" +
                  $"{token.Owner.Scope}:" +
                  $"{token.Owner.OwnerIdentity.Value.Value}:" +
                  $"{token.PlayerSlotId.Value.Value}:" +
                  $"{token.ActorId.Value.Value}:" +
                  $"{token.MaterializationRevision}:" +
                  $"{token.OccupancyRevision}:" +
                  $"{token.InputBindingRevision}"
                : string.Empty;
        }

        private static string CreateTieBreakerId(
            PlayerGameplayCameraEligibilityToken token)
        {
            return token.IsValid
                ? $"player.camera:{token.PlayerSlotId.Value.Value}:" +
                  $"{token.ActorId.Value.Value}"
                : string.Empty;
        }

        private PlayerGameplayCameraEligibilitySummary GetSummaryOrDefault(
            PlayerSlotId playerSlotId)
        {
            return playerSlotId.IsValid &&
                slots.TryGetValue(
                    playerSlotId,
                    out PlayerGameplayCameraEligibilitySummary summary)
                ? summary
                : default;
        }

        private PlayerGameplayCameraEligibilityResult Reject(
            PlayerGameplayCameraEligibilityStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilitySummary previous,
            string message)
        {
            lastOperationStatus = status;
            lastOperationMessage = message.NormalizeText();
            return Result(
                status,
                operation,
                playerSlotId,
                previous,
                previous,
                lastOperationMessage);
        }

        private PlayerGameplayCameraEligibilityResult Result(
            PlayerGameplayCameraEligibilityStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilitySummary previous,
            PlayerGameplayCameraEligibilitySummary current,
            string message)
        {
            return new PlayerGameplayCameraEligibilityResult(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                CreateSnapshot(),
                message);
        }
    }
}
