using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Official FrameworkRuntimeHost-scoped composition for P3K.2-P3K.7E.
    /// Domain state remains in the plain C# authorities; this component owns only
    /// their explicit Session lifetime and typed cross-authority wiring.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7F official Session Player gameplay authority composition.")]
    internal sealed class PlayerGameplayRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost runtimeHost;
        private PlayerParticipationRuntimeContext participationContext;
        private PlayerActorPreparationRuntimeHostModule preparationModule;
        private PlayerActorCandidateRuntimeHostModule candidateModule;
        private PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private PlayerGameplayInputBindingRuntimeContext inputContext;
        private PlayerGameplayCameraEligibilityRuntimeContext cameraContext;
        private PlayerGameplayAdmissionRuntimeContext admissionContext;
        private PlayerGameplayChainHandoffRuntimeContext handoffContext;
        private ActivityPlayerHandoffGroupRuntimeContext groupContext;
        private PlayerGameplayRuntimeOperationStatus lastOperationStatus;
        private string diagnostic =
            "Player gameplay runtime is not initialized.";
        private bool shuttingDown;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            preparationModule != null &&
            candidateModule != null &&
            occupancyContext != null &&
            inputContext != null &&
            cameraContext != null &&
            admissionContext != null &&
            handoffContext != null &&
            groupContext != null;

        internal string Diagnostic => diagnostic;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            out PlayerGameplayRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue =
                    "Player gameplay runtime requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            module = runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>();
            if (module == null)
            {
                module =
                    runtimeHost.gameObject.AddComponent<PlayerGameplayRuntimeHostModule>();
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

                issue =
                    "Player gameplay runtime is already bound to another FrameworkRuntimeHost.";
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
                issue =
                    "FrameworkRuntimeHost has no initialized Player participation authority.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule targetPreparation))
            {
                issue =
                    "FrameworkRuntimeHost has no ready P3J Player Actor preparation module.";
                diagnostic = issue;
                return false;
            }

            if (!PlayerActorCandidateRuntimeHostModule.TryAttach(
                    targetRuntimeHost,
                    out PlayerActorCandidateRuntimeHostModule targetCandidate,
                    out issue))
            {
                diagnostic =
                    "Player gameplay runtime could not compose P3K.7C candidate staging. " +
                    issue;
                issue = diagnostic;
                return false;
            }

            if (!targetPreparation.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationHost) ||
                preparationHost == null ||
                !preparationHost.IsInitialized ||
                preparationHost.Preparation == null ||
                !preparationHost.Preparation.IsInitialized)
            {
                issue =
                    "Player gameplay runtime requires an initialized P3J preparation snapshot.";
                diagnostic = issue;
                return false;
            }

            if (!PlayerGameplayOccupancyRuntimeContext.TryCreate(
                    preparationHost.Preparation,
                    out PlayerGameplayOccupancyRuntimeContext targetOccupancy,
                    out issue))
            {
                diagnostic = "P3K.2 composition failed. " + issue;
                issue = diagnostic;
                return false;
            }

            if (!PlayerGameplayInputBindingRuntimeContext.TryCreate(
                    targetOccupancy,
                    out PlayerGameplayInputBindingRuntimeContext targetInput,
                    out issue))
            {
                diagnostic = "P3K.3 composition failed. " + issue;
                issue = diagnostic;
                return false;
            }

            if (!PlayerGameplayCameraEligibilityRuntimeContext.TryCreate(
                    targetOccupancy,
                    targetInput,
                    out PlayerGameplayCameraEligibilityRuntimeContext targetCamera,
                    out issue))
            {
                diagnostic = "P3K.4 composition failed. " + issue;
                issue = diagnostic;
                return false;
            }

            if (!PlayerGameplayAdmissionRuntimeContext.TryCreate(
                    targetOccupancy,
                    targetInput,
                    targetCamera,
                    out PlayerGameplayAdmissionRuntimeContext targetAdmission,
                    out issue))
            {
                diagnostic = "P3K.5 composition failed. " + issue;
                issue = diagnostic;
                return false;
            }

            var endpointSource =
                new HostScopedPlayerGameplayChainEndpointSource(
                    targetRuntimeHost,
                    targetPreparation);

            if (!PlayerGameplayChainHandoffRuntimeContext.TryCreate(
                    targetPreparation,
                    targetCandidate,
                    endpointSource,
                    targetOccupancy,
                    targetInput,
                    targetCamera,
                    targetAdmission,
                    out PlayerGameplayChainHandoffRuntimeContext targetHandoff,
                    out issue))
            {
                diagnostic = "P3K.7D composition failed. " + issue;
                issue = diagnostic;
                return false;
            }

            var evidenceSource =
                new ExplicitActivityPlayerHandoffEvidenceSource(
                    targetParticipation,
                    targetPreparation,
                    targetAdmission);
            if (!ActivityPlayerHandoffGroupRuntimeContext.TryCreate(
                    targetHandoff,
                    evidenceSource,
                    out ActivityPlayerHandoffGroupRuntimeContext targetGroup,
                    out issue))
            {
                diagnostic = "P3K.7E composition failed. " + issue;
                issue = diagnostic;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipation;
            preparationModule = targetPreparation;
            candidateModule = targetCandidate;
            occupancyContext = targetOccupancy;
            inputContext = targetInput;
            cameraContext = targetCamera;
            admissionContext = targetAdmission;
            handoffContext = targetHandoff;
            groupContext = targetGroup;
            lastOperationStatus =
                PlayerGameplayRuntimeOperationStatus.None;
            diagnostic =
                $"Player gameplay runtime is ready. session='{preparationHost.SessionContextId}' " +
                $"slots='{preparationHost.Preparation.ConfiguredSlotCount}'.";
            return true;
        }

        internal PlayerGameplayRuntimeOperationResult TryEnsureCurrentGameplay(
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            const string Operation = "EnsureCurrentGameplay";
            PlayerGameplayAdmissionSummary previous =
                GetAdmissionOrDefault(playerSlotId);

            if (!IsReady)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedRuntimeUnavailable,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    diagnostic);
            }

            if (!playerSlotId.IsValid)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedInvalidRequest,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Current gameplay creation requires a valid Player Slot identity.");
            }

            if (!preparationModule.TryGetCurrentPreparation(
                    playerSlotId,
                    out PlayerActorPreparationSummary preparation,
                    out string preparationIssue) ||
                !preparation.IsPrepared)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedInvalidRequest,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    preparationIssue);
            }

            bool succeeded = handoffContext.TryEnsureCurrentGameplayChain(
                preparation,
                source,
                reason,
                out PlayerGameplayAdmissionSummary current,
                out bool rollbackAttempted,
                out bool rollbackSucceeded,
                out string rollbackMessage,
                out string issue);

            if (!succeeded)
            {
                PlayerGameplayRuntimeOperationStatus failure =
                    rollbackAttempted && !rollbackSucceeded
                        ? PlayerGameplayRuntimeOperationStatus.FailedChainRollback
                        : PlayerGameplayRuntimeOperationStatus.FailedChainBuild;
                return Result(
                    failure,
                    Operation,
                    playerSlotId,
                    previous,
                    GetAdmissionOrDefault(playerSlotId),
                    rollbackAttempted,
                    rollbackSucceeded,
                    rollbackMessage,
                    issue);
            }

            bool alreadyAdmitted = previous.IsAdmitted;
            PlayerGameplayRuntimeOperationStatus successStatus =
                current.GameplayReady
                    ? alreadyAdmitted
                        ? PlayerGameplayRuntimeOperationStatus.SucceededAlreadyReady
                        : PlayerGameplayRuntimeOperationStatus.SucceededReady
                    : alreadyAdmitted
                        ? PlayerGameplayRuntimeOperationStatus.SucceededAlreadyBlockedByInputGate
                        : PlayerGameplayRuntimeOperationStatus.SucceededBlockedByInputGate;
            string successMessage = current.GameplayReady
                ? alreadyAdmitted
                    ? "Current Player gameplay chain is already authoritative and GameplayReady."
                    : "Current Player gameplay chain became authoritative and GameplayReady."
                : alreadyAdmitted
                    ? "Current Player gameplay chain is already authoritative but blocked by the input Gate."
                    : "Current Player gameplay chain became authoritative but is blocked by the input Gate.";

            return Result(
                successStatus,
                Operation,
                playerSlotId,
                previous,
                current,
                false,
                true,
                string.Empty,
                successMessage);
        }

        internal PlayerGameplayRuntimeOperationResult TryReleaseCurrentGameplay(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken expectedAdmission,
            string source,
            string reason)
        {
            const string Operation = "ReleaseCurrentGameplay";
            PlayerGameplayAdmissionSummary previous =
                GetAdmissionOrDefault(playerSlotId);

            if (!IsReady)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedRuntimeUnavailable,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    diagnostic);
            }

            if (!playerSlotId.IsValid ||
                !expectedAdmission.IsValid ||
                expectedAdmission.PlayerSlotId != playerSlotId)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedInvalidRequest,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Gameplay release requires a valid Slot and exact admission token.");
            }

            if (!previous.IsAdmitted ||
                previous.Token != expectedAdmission)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus
                        .RejectedForeignOrStaleAdmission,
                    Operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Expected gameplay admission token is foreign or stale.");
            }

            PlayerGameplayAdmissionResult release =
                handoffContext.TryReleaseCurrentGameplayChain(
                    playerSlotId,
                    expectedAdmission,
                    source,
                    reason);
            PlayerGameplayAdmissionSummary current =
                GetAdmissionOrDefault(playerSlotId);

            return release.Succeeded
                ? Result(
                    PlayerGameplayRuntimeOperationStatus.SucceededReleased,
                    Operation,
                    playerSlotId,
                    previous,
                    current,
                    false,
                    true,
                    string.Empty,
                    release.Message)
                : Result(
                    PlayerGameplayRuntimeOperationStatus.FailedRelease,
                    Operation,
                    playerSlotId,
                    previous,
                    current,
                    false,
                    false,
                    string.Empty,
                    release.ToDiagnosticString());
        }

        internal bool TryGetCurrentAdmission(
            PlayerSlotId playerSlotId,
            out PlayerGameplayAdmissionSummary admission)
        {
            admission = default;
            return IsReady &&
                handoffContext.TryGetCurrentAdmission(
                    playerSlotId,
                    out admission);
        }

        internal PlayerActorCandidateStageResult TryStageCandidate(
            RuntimeScopeContext targetActivityContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            return candidateModule != null
                ? candidateModule.TryStageCandidate(
                    targetActivityContext,
                    playerSlotId,
                    source,
                    reason)
                : null;
        }


        internal PlayerActorCandidateStageResult TryRollbackCandidate(
            PlayerActorCandidateStageToken expectedCandidate,
            string source,
            string reason)
        {
            return candidateModule != null
                ? candidateModule.TryRollbackCandidate(
                    expectedCandidate,
                    source,
                    reason)
                : null;
        }

        internal ActivityPlayerHandoffGroupResult TryBeginActivityHandoffGroup(
            Immersive.Framework.Authoring.ActivityAsset activity,
            RuntimeContentOwner targetOwner,
            System.Collections.Generic.IReadOnlyList<
                ActivityPlayerHandoffSlotRequest> orderedSlots,
            string source,
            string reason)
        {
            return groupContext != null
                ? groupContext.TryBegin(
                    activity,
                    targetOwner,
                    orderedSlots,
                    source,
                    reason)
                : null;
        }

        internal ActivityPlayerHandoffGroupResult TryCommitActivityHandoffGroup(
            ActivityPlayerHandoffGroupToken expectedGroup,
            string source,
            string reason)
        {
            return groupContext != null
                ? groupContext.TryCommit(expectedGroup, source, reason)
                : null;
        }

        internal ActivityPlayerHandoffGroupResult TryRollbackActivityHandoffGroup(
            ActivityPlayerHandoffGroupToken expectedGroup,
            string source,
            string reason)
        {
            return groupContext != null
                ? groupContext.TryRollback(expectedGroup, source, reason)
                : null;
        }

        internal ActivityPlayerHandoffGroupResult
            TryRetryActivityHandoffCommitCleanup(
                ActivityPlayerHandoffGroupToken expectedGroup,
                string source,
                string reason)
        {
            return groupContext != null
                ? groupContext.TryRetryCommitCleanup(
                    expectedGroup,
                    source,
                    reason)
                : null;
        }

        internal bool TryGetSnapshot(
            out PlayerGameplayRuntimeHostSnapshot snapshot)
        {
            snapshot = CreateSnapshot();
            return snapshot.IsInitialized;
        }

        private PlayerGameplayRuntimeHostSnapshot CreateSnapshot()
        {
            if (!IsReady)
            {
                return PlayerGameplayRuntimeHostSnapshot.Unavailable(
                    diagnostic);
            }

            candidateModule.TryGetSnapshot(
                out PlayerActorCandidateRuntimeHostSnapshot candidates);
            return new PlayerGameplayRuntimeHostSnapshot(
                true,
                occupancyContext.SessionContextId,
                occupancyContext.CreateSnapshot(),
                inputContext.CreateSnapshot(),
                cameraContext.CreateSnapshot(),
                admissionContext.CreateSnapshot(),
                candidates,
                groupContext.CreateSnapshot(),
                handoffContext.ActiveHandoffCount,
                lastOperationStatus,
                diagnostic);
        }

        private PlayerGameplayAdmissionSummary GetAdmissionOrDefault(
            PlayerSlotId playerSlotId)
        {
            if (admissionContext != null)
            {
                PlayerGameplayAdmissionSnapshot snapshot =
                    admissionContext.CreateSnapshot();
                if (snapshot != null &&
                    snapshot.TryGetSummary(
                        playerSlotId,
                        out PlayerGameplayAdmissionSummary admission))
                {
                    return admission;
                }
            }

            return default;
        }

        private PlayerGameplayRuntimeOperationResult Result(
            PlayerGameplayRuntimeOperationStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previous,
            PlayerGameplayAdmissionSummary current,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            string message)
        {
            lastOperationStatus = status;
            diagnostic = message ?? string.Empty;
            return new PlayerGameplayRuntimeOperationResult(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                rollbackAttempted,
                rollbackSucceeded,
                rollbackMessage,
                CreateSnapshot(),
                message);
        }

        private void OnDestroy()
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            if (admissionContext != null &&
                handoffContext != null)
            {
                PlayerGameplayAdmissionSnapshot snapshot =
                    admissionContext.CreateSnapshot();
                for (int index = snapshot.Slots.Count - 1;
                     index >= 0;
                     index--)
                {
                    PlayerGameplayAdmissionSummary admission =
                        snapshot.Slots[index];
                    if (!admission.IsAdmitted ||
                        !admission.Token.IsValid)
                    {
                        continue;
                    }

                    handoffContext.TryReleaseCurrentGameplayChain(
                        admission.PlayerSlotId,
                        admission.Token,
                        nameof(PlayerGameplayRuntimeHostModule),
                        "runtime-host-shutdown");
                }
            }

            groupContext = null;
            handoffContext = null;
            admissionContext = null;
            cameraContext = null;
            inputContext = null;
            occupancyContext = null;
            candidateModule = null;
            preparationModule = null;
            participationContext = null;
            runtimeHost = null;
            diagnostic = "Player gameplay runtime was released.";
        }
    }

    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7F typed same-host access to official Player gameplay composition.")]
    internal static class FrameworkRuntimeHostPlayerGameplayExtensions
    {
        internal static bool TryGetPlayerGameplayRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerGameplayRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }

        internal static bool TryGetPlayerGameplayRuntimeSnapshot(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerGameplayRuntimeHostSnapshot snapshot)
        {
            if (runtimeHost == null)
            {
                snapshot =
                    PlayerGameplayRuntimeHostSnapshot.Unavailable(
                        "FrameworkRuntimeHost is missing.");
                return false;
            }

            PlayerGameplayRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>();
            if (module == null)
            {
                snapshot =
                    PlayerGameplayRuntimeHostSnapshot.Unavailable(
                        "FrameworkRuntimeHost has no Player gameplay runtime module.");
                return false;
            }

            return module.TryGetSnapshot(out snapshot);
        }
    }
}
