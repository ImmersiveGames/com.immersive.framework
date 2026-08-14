using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// FrameworkRuntimeHost-scoped composition adapter for Session Player Actor preparation.
    /// It coordinates the existing participation, provisioning, RuntimeContent and preparation
    /// authorities without becoming a second domain authority or using global lookup.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.5/P3J.6 FrameworkRuntimeHost integration for real local Player Actor preparation and Activity lifecycle.")]
    internal sealed partial class PlayerActorPreparationRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost runtimeHost;
        private PlayerParticipationRuntimeContext participationContext;
        private PlayerHostEvidenceProjection hostEvidenceProjection;
        private PlayerActorPreparationRuntimeContext preparationContext;
        private RuntimeScopeContext sessionPhysicalScopeContext;
        private ActivityPlayerActorLifecycleParticipant activityLifecycleParticipant;
        private LocalPlayerJoinResult lastJoinResult;
        private string diagnostic = "Player Actor preparation runtime is not initialized.";
        private int joinRequestCount;
        private int preparationRequestCount;
        private bool shuttingDown;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            preparationContext != null &&
            activityLifecycleParticipant != null;

        internal string Diagnostic => diagnostic;
        internal LocalPlayerJoinResult LastJoinResult => lastJoinResult;
        internal int RegisteredHostCount =>
            hostEvidenceProjection?.RetainedEvidenceCount ?? 0;
        internal int JoinRequestCount => joinRequestCount;
        internal int PreparationRequestCount => preparationRequestCount;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            out PlayerActorPreparationRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue = "Player Actor preparation requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            module = runtimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<PlayerActorPreparationRuntimeHostModule>();
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

                issue = "Player Actor preparation runtime is already bound to another FrameworkRuntimeHost.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext targetParticipationContext))
            {
                issue = "FrameworkRuntimeHost has no initialized Session Player participation context.";
                diagnostic = issue;
                return false;
            }

            RuntimeContentRuntime runtimeContentRuntime = targetRuntimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                issue = "FrameworkRuntimeHost has no RuntimeContentRuntime for Player Actor materialization.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryCreateSessionRuntimeScopeContext(
                    nameof(PlayerActorPreparationRuntimeHostModule),
                    "player-physical-lifetime-initialization",
                    out RuntimeScopeContext targetSessionPhysicalScopeContext,
                    out issue))
            {
                diagnostic = issue;
                return false;
            }

            PlayerParticipationSnapshot participationSnapshot =
                targetParticipationContext.CreateSnapshot();
            if (participationSnapshot == null ||
                !participationSnapshot.IsInitialized ||
                string.IsNullOrEmpty(participationSnapshot.ContextId))
            {
                issue = "Session Player participation snapshot is not initialized.";
                diagnostic = issue;
                return false;
            }

            var adapter = new AttachedPlayerActorMaterializationAdapter(
                runtimeContentRuntime,
                participationSnapshot.ContextId);
            var targetHostEvidenceProjection =
                new PlayerHostEvidenceProjection(targetParticipationContext);
            if (!PlayerActorPreparationRuntimeContext.TryCreate(
                    targetParticipationContext,
                    targetHostEvidenceProjection,
                    adapter,
                    out PlayerActorPreparationRuntimeContext targetPreparationContext,
                    out issue))
            {
                diagnostic = issue;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipationContext;
            hostEvidenceProjection = targetHostEvidenceProjection;
            preparationContext = targetPreparationContext;
            sessionPhysicalScopeContext = targetSessionPhysicalScopeContext;
            activityLifecycleParticipant = new ActivityPlayerActorLifecycleParticipant(
                this,
                targetParticipationContext);
            targetRuntimeHost.SetActivityContentExecutionParticipantSource(
                activityLifecycleParticipant);
            targetRuntimeHost.SetPauseActivityBindingPlayerEvidence(
                activityLifecycleParticipant);
            if (!PlayerGameplayRuntimeHostModule.TryAttach(
                    targetRuntimeHost,
                    out _,
                    out string gameplayIssue))
            {
                targetRuntimeHost.SetActivityContentExecutionParticipantSource(null);
                targetRuntimeHost.SetPauseActivityBindingPlayerEvidence(null);
                activityLifecycleParticipant = null;
                preparationContext = null;
                sessionPhysicalScopeContext = default;
                hostEvidenceProjection = null;
                participationContext = null;
                runtimeHost = null;
                diagnostic =
                    "Player Actor preparation could not compose the official Player gameplay runtime. " +
                    gameplayIssue;
                issue = diagnostic;
                return false;
            }
            diagnostic =
                $"Player Actor preparation runtime is ready. session='{participationSnapshot.ContextId}'.";
            return true;
        }

        internal PlayerParticipationOperationResult TryOpenJoining(
            string source,
            string reason)
        {
            return TryGetProvisioningRuntime(out LocalPlayerProvisioningRuntimeHostModule provisioning,
                    out string issue)
                ? provisioning.TryOpenJoining(source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    issue);
        }

        internal PlayerParticipationOperationResult TryCloseJoining(
            string source,
            string reason)
        {
            return TryGetProvisioningRuntime(out LocalPlayerProvisioningRuntimeHostModule provisioning,
                    out string issue)
                ? provisioning.TryCloseJoining(source, reason)
                : PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    issue);
        }

        internal LocalPlayerJoinResult TryJoinLocalPlayer(LocalPlayerJoinRequest request)
        {
            if (!TryGetProvisioningRuntime(
                    out LocalPlayerProvisioningRuntimeHostModule provisioning,
                    out string issue))
            {
                diagnostic = issue;
                lastJoinResult = LocalPlayerJoinResult.RuntimeUnavailable(request, issue);
                return lastJoinResult;
            }

            LocalPlayerJoinResult result = provisioning.TryJoin(request);
            result = provisioning.RegisterJoinWithActorPreparation(result);
            lastJoinResult = result;
            if (result == null)
            {
                diagnostic = "Local Player provisioning returned no join result.";
                lastJoinResult = LocalPlayerJoinResult.RuntimeUnavailable(request, diagnostic);
                return lastJoinResult;
            }

            if (!result.Succeeded)
            {
                diagnostic = result.ToDiagnosticString();
                return result;
            }

            diagnostic =
                $"Local Player joined and registered for Actor preparation. " +
                $"slot='{result.Slot.PlayerSlotId.StableText}' host='{result.LocalPlayerHost.name}'.";
            return result;
        }

        internal bool TryRegisterJoinedHost(
            LocalPlayerJoinResult joinResult,
            out string issue)
        {
            issue = string.Empty;

            if (!IsReady)
            {
                issue = diagnostic;
                return false;
            }

            if (joinResult == null || !joinResult.Succeeded)
            {
                issue = "Only a successful LocalPlayerJoinResult may register a preparation host.";
                return false;
            }

            PlayerSlotRuntimeSnapshot slot = joinResult.Slot;
            LocalPlayerHostAuthoring host = joinResult.LocalPlayerHost;
            if (!slot.IsValid || !slot.IsJoined || !slot.PlayerSlotId.IsValid)
            {
                issue = "Successful join result has no valid Joined Player Slot evidence.";
                return false;
            }

            if (host == null ||
                !host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != slot.PlayerSlotId)
            {
                issue = "Successful join result has no matching joined Local Player Host evidence.";
                return false;
            }

            if (joinResult.PlayerInput == null ||
                !ReferenceEquals(host.PlayerInput, joinResult.PlayerInput))
            {
                issue = "Joined Local Player Host does not own the PlayerInput returned by provisioning.";
                return false;
            }

            PlayerHostEvidenceResult registration = RegisterSessionPhysicalHost(
                slot.PlayerSlotId,
                host,
                nameof(PlayerActorPreparationRuntimeHostModule),
                "register-manager-provisioned-host");
            if (!registration.Succeeded)
            {
                issue = registration.ToDiagnosticString();
                return false;
            }

            RecordSuccessfulJoin(joinResult);
            RegisterActivityLifecycleSource();
            diagnostic =
                $"Joined Local Player Host registered. slot='{slot.PlayerSlotId.StableText}' host='{host.name}'.";
            return true;
        }

        internal bool TryGetRegisteredHost(
            PlayerSlotId playerSlotId,
            out LocalPlayerHostAuthoring host,
            out string issue)
        {
            host = null;
            issue = string.Empty;
            if (!IsReady || hostEvidenceProjection == null)
            {
                issue = diagnostic;
                return false;
            }

            bool found = hostEvidenceProjection.TryGetSessionPhysicalHost(
                playerSlotId,
                out host,
                out PlayerHostEvidenceResult result);
            issue = found ? string.Empty : result.ToDiagnosticString();
            return found;
        }

        internal PlayerHostEvidenceResult RegisterHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.RegisterHostEvidence(
                    playerSlotId,
                    assignmentOrigin,
                    assignmentToken,
                    hostBindingIdentity,
                    host,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "RegisterHostEvidence",
                    source,
                    reason);
        }

        internal PlayerHostEvidenceResult RegisterSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.RegisterSessionPhysicalHost(
                    playerSlotId,
                    host,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "RegisterSessionPhysicalHost",
                    source,
                    reason);
        }

        internal PlayerHostEvidenceResult ReprojectHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.ReprojectHostEvidence(
                    playerSlotId,
                    assignmentOrigin,
                    assignmentToken,
                    hostBindingIdentity,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "ReprojectHostEvidence",
                    source,
                    reason);
        }

        internal PlayerHostEvidenceResult ConfirmHostEvidence(
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.ConfirmHostEvidence(
                    playerSlotId,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "ConfirmHostEvidence",
                    source,
                    reason);
        }

        internal PlayerHostEvidenceResult ReleaseHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.ReleaseHostEvidence(
                    playerSlotId,
                    assignmentToken,
                    hostBindingIdentity,
                    expectedHost,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "ReleaseHostEvidence",
                    source,
                    reason);
        }

        internal PlayerHostEvidenceResult ReleaseSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.ReleaseSessionPhysicalHost(
                    playerSlotId,
                    expectedHost,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "ReleaseSessionPhysicalHost",
                    source,
                    reason);
        }

        internal bool TryGetRetainedHostEvidence(
            PlayerSlotId playerSlotId,
            out PlayerHostEvidenceSnapshot evidence)
        {
            evidence = default;
            return hostEvidenceProjection != null &&
                hostEvidenceProjection.TryGetRetainedEvidence(
                    playerSlotId,
                    out evidence);
        }

        internal PlayerHostEvidenceResult ClearDivergentHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason)
        {
            return hostEvidenceProjection != null
                ? hostEvidenceProjection.ClearDivergentHostEvidence(
                    playerSlotId,
                    assignmentToken,
                    hostBindingIdentity,
                    expectedHost,
                    source,
                    reason)
                : UnavailableHostEvidenceResult(
                    "ClearDivergentHostEvidence",
                    source,
                    reason);
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return preparationContext != null
                ? preparationContext.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    diagnostic);
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return preparationContext != null
                ? preparationContext.TryReplaceActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection",
                    request,
                    diagnostic);
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return preparationContext != null
                ? preparationContext.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    diagnostic);
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            return preparationContext != null
                ? preparationContext.TrySelectDefaultActor(
                    playerSlotId,
                    expectedSelectionRevision,
                    source,
                    reason)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor",
                    new PlayerActorSelectionRequest(
                        playerSlotId,
                        null,
                        source,
                        reason,
                        expectedSelectionRevision),
                    diagnostic);
        }

        internal PlayerActorPreparationResult TryPrepareSelectedActor(
            RuntimeScopeContext scopeContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            preparationRequestCount++;
            if (preparationContext == null)
            {
                return PlayerActorPreparationResult.RuntimeUnavailable(
                    "PrepareSelectedActor",
                    playerSlotId,
                    diagnostic);
            }

            PlayerActorPreparationResult result =
                preparationContext.TryPrepareSelectedActor(
                    scopeContext,
                    sessionPhysicalScopeContext,
                    playerSlotId,
                    source,
                    reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerActorPreparationResult TryReleasePreparedActor(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            preparationRequestCount++;
            if (preparationContext == null)
            {
                return PlayerActorPreparationResult.RuntimeUnavailable(
                    "ReleasePreparedActor",
                    playerSlotId,
                    diagnostic);
            }

            PlayerActorPreparationResult result =
                preparationContext.TryReleasePreparedActor(
                    playerSlotId,
                    expectedPreparation,
                    source,
                    reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerActorPreparationResult TryEnsureSessionPhysicalActor(
            RuntimeScopeContext scopeContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            preparationRequestCount++;
            if (preparationContext == null)
            {
                return PlayerActorPreparationResult.RuntimeUnavailable(
                    "EnsureSessionPhysicalActor",
                    playerSlotId,
                    diagnostic);
            }

            PlayerActorPreparationResult result =
                preparationContext.TryEnsureSessionPhysicalActor(
                    scopeContext,
                    sessionPhysicalScopeContext,
                    playerSlotId,
                    source,
                    reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal bool TryReleaseManagerContextualProjection(
            RuntimeContentOwner activityOwner,
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out string issue)
        {
            if (preparationContext == null)
            {
                issue = diagnostic;
                return false;
            }

            return preparationContext.TryReleaseManagerContextualProjection(
                activityOwner,
                playerSlotId,
                source,
                reason,
                out issue);
        }

        internal bool TryDeactivatePreparedActorPresentation(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (preparationContext == null)
            {
                issue = diagnostic;
                return false;
            }

            bool deactivated = preparationContext.TryDeactivatePreparedActorPresentation(
                playerSlotId,
                expectedPreparation,
                source,
                reason,
                out issue);
            if (!deactivated)
            {
                diagnostic = issue;
            }

            return deactivated;
        }

        internal bool TryGetCurrentActorEvidence(
            PlayerSlotId playerSlotId,
            out PlayerActorCorrelationEvidence evidence,
            out PlayerCurrentActorEvidenceResult result)
        {
            evidence = default;
            result = null;
            return preparationContext != null &&
                preparationContext.TryGetCurrentActorEvidence(
                    playerSlotId,
                    out evidence,
                    out result);
        }

        internal PlayerCurrentActorEvidenceResult ConfirmCurrentActorEvidence(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            return preparationContext != null
                ? preparationContext.ConfirmCurrentActorEvidence(
                    playerSlotId,
                    expectedPreparation,
                    source,
                    reason)
                : new PlayerCurrentActorEvidenceResult(
                    PlayerCurrentActorEvidenceStatus.RejectedInvalidRequest,
                    "ConfirmCurrentActorEvidence",
                    default,
                    default,
                    source,
                    reason,
                    diagnostic);
        }

        internal bool TryGetRetainedActorEvidence(
            PlayerSlotId playerSlotId,
            out PlayerActorCorrelationEvidence evidence)
        {
            evidence = default;
            return preparationContext != null &&
                preparationContext.TryGetRetainedActorEvidence(
                    playerSlotId,
                    out evidence);
        }

        internal bool TryGetCurrentSlotActorSnapshot(
            PlayerSlotId playerSlotId,
            out CurrentPlayerSlotActorSnapshot snapshot)
        {
            snapshot = default;
            return preparationContext != null &&
                preparationContext.TryGetCurrentSlotActorSnapshot(
                    playerSlotId,
                    out snapshot);
        }

        internal PlayerActorPreparationResult TryReplacePreparedActor(
            RuntimeScopeContext scopeContext,
            PlayerActorSelectionRequest replacementRequest,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            preparationRequestCount++;
            if (preparationContext == null)
            {
                return PlayerActorPreparationResult.RuntimeUnavailable(
                    "ReplacePreparedActor",
                    replacementRequest.PlayerSlotId,
                    diagnostic);
            }

            if (!TryGetRegisteredHost(replacementRequest.PlayerSlotId, out _, out string issue))
            {
                return PlayerActorPreparationResult.HostUnavailable(
                    "ReplacePreparedActor",
                    replacementRequest.PlayerSlotId,
                    issue,
                    preparationContext.CreateSnapshot());
            }

            PlayerActorPreparationResult result =
                preparationContext.TryReplacePreparedActor(
                    scopeContext,
                    replacementRequest,
                    expectedPreparation,
                    source,
                    reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal bool TryGetSnapshot(
            out PlayerActorPreparationRuntimeHostSnapshot snapshot)
        {
            PlayerActorPreparationSnapshot preparation =
                preparationContext != null
                    ? preparationContext.CreateSnapshot()
                    : new PlayerActorPreparationSnapshot(
                        string.Empty,
                        0,
                        Array.Empty<PlayerActorPreparationSummary>(),
                        Array.Empty<PlayerActorMaterializationSnapshot>(),
                        PlayerActorPreparationStatus.RejectedRuntimeUnavailable,
                        diagnostic);

            snapshot = new PlayerActorPreparationRuntimeHostSnapshot(
                IsReady,
                preparation.SessionContextId,
                RegisteredHostCount,
                joinRequestCount,
                preparationRequestCount,
                lastJoinResult != null ? lastJoinResult.Status : LocalPlayerJoinStatus.None,
                preparation,
                diagnostic);
            return IsReady;
        }


        internal bool TryGetActivityPlayerActorLifecycleSnapshot(
            out ActivityPlayerActorLifecycleSnapshot snapshot)
        {
            if (activityLifecycleParticipant == null)
            {
                snapshot = ActivityPlayerActorLifecycleSnapshot.Empty(
                    "Activity Player Actor lifecycle participant is unavailable.");
                return false;
            }

            snapshot = activityLifecycleParticipant.Snapshot;
            return true;
        }

        internal bool TryReleaseAllPreparedActors(
            string source,
            string reason,
            out int releasedCount,
            out int failedCount,
            out string issue)
        {
            releasedCount = 0;
            failedCount = 0;
            issue = string.Empty;

            if (preparationContext == null)
            {
                issue = diagnostic;
                return false;
            }

            PlayerActorPreparationSnapshot snapshot = preparationContext.CreateSnapshot();
            var failures = new List<string>();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerActorPreparationSummary summary = snapshot.Slots[index];
                if (!summary.IsPrepared && !summary.IsReleaseFailed)
                {
                    continue;
                }

                PlayerActorPreparationResult result =
                    preparationContext.TryReleasePreparedActor(
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
            diagnostic = failures.Count == 0
                ? $"Released '{releasedCount}' prepared Player Actors."
                : $"Prepared Player Actor shutdown release failed for '{failedCount}' Slots. {issue}";
            return failedCount == 0;
        }


        internal void RegisterActivityLifecycleSource()
        {
            if (runtimeHost == null || activityLifecycleParticipant == null)
            {
                throw new InvalidOperationException(
                    "Activity Player Actor lifecycle source cannot be registered before runtime initialization.");
            }

            runtimeHost.SetActivityContentExecutionParticipantSource(
                activityLifecycleParticipant);
        }

        private void RecordSuccessfulJoin(LocalPlayerJoinResult joinResult)
        {
            if (!ReferenceEquals(lastJoinResult, joinResult))
            {
                joinRequestCount++;
            }

            lastJoinResult = joinResult;
        }

        private PlayerHostEvidenceResult UnavailableHostEvidenceResult(
            string operation,
            string source,
            string reason)
        {
            return new PlayerHostEvidenceResult(
                PlayerHostEvidenceStatus.RejectedInvalidRequest,
                operation,
                default,
                default,
                null,
                source,
                reason,
                diagnostic);
        }

        private bool TryGetProvisioningRuntime(
            out LocalPlayerProvisioningRuntimeHostModule provisioning,
            out string issue)
        {
            provisioning = null;
            issue = string.Empty;

            if (!IsReady)
            {
                issue = diagnostic;
                return false;
            }

            provisioning = runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            if (provisioning == null || !provisioning.IsReady)
            {
                provisioning = null;
                issue = "FrameworkRuntimeHost has no ready Local Player provisioning runtime.";
                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            if (preparationContext != null)
            {
                RetireSceneLocalPlayerContextForSessionTermination();
                TryReleaseAllPreparedActors(
                    nameof(PlayerActorPreparationRuntimeHostModule),
                    "runtime-host-shutdown",
                    out _,
                    out _,
                    out _);
            }

            hostEvidenceProjection?.ClearAll();
            hostEvidenceProjection = null;
            activityLifecycleParticipant = null;
            preparationContext = null;
            sessionPhysicalScopeContext = default;
            participationContext = null;
            runtimeHost = null;
            diagnostic = "Player Actor preparation runtime was released.";
        }
    }

    /// <summary>
    /// Same-host bridge from the existing public provisioning endpoint to Actor preparation.
    /// Successful joins may not escape without explicit host registration evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.5/P3J.6 preparation registration and Activity lifecycle bridge for local Player provisioning.")]
    internal static class LocalPlayerProvisioningPreparationExtensions
    {
        internal static void RegisterActivityPlayerActorLifecycleSource(
            this LocalPlayerProvisioningRuntimeHostModule provisioning)
        {
            if (provisioning == null)
            {
                throw new ArgumentNullException(nameof(provisioning));
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                provisioning.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (preparation == null || !preparation.IsReady)
            {
                throw new InvalidOperationException(
                    "Local Player provisioning cannot register Activity Player Actor lifecycle because the same FrameworkRuntimeHost has no ready preparation module.");
            }

            preparation.RegisterActivityLifecycleSource();
        }

        internal static LocalPlayerJoinResult RegisterJoinWithActorPreparation(
            this LocalPlayerProvisioningRuntimeHostModule provisioning,
            LocalPlayerJoinResult result)
        {
            if (result == null || !result.Succeeded)
            {
                return result;
            }

            if (provisioning == null)
            {
                throw new InvalidOperationException(
                    "Successful local Player join has no provisioning runtime owner.");
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                provisioning.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (preparation == null || !preparation.IsReady)
            {
                throw new InvalidOperationException(
                    "Successful local Player join cannot be returned because the FrameworkRuntimeHost has no ready Player Actor preparation authority.");
            }

            if (!preparation.TryRegisterJoinedHost(result, out string issue))
            {
                return provisioning.RollbackCommittedJoin(
                    result,
                    "actor-preparation-host-registration-failed: " + issue,
                    explicitCallerRollback: false);
            }

            return result;
        }
    }

    /// <summary>
    /// Narrow typed same-host access. The caller must already hold the FrameworkRuntimeHost.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.5 typed FrameworkRuntimeHost access to its Player Actor preparation module.")]
    internal static class FrameworkRuntimeHostPlayerActorPreparationExtensions
    {
        internal static bool TryGetPlayerActorPreparationRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerActorPreparationRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }

        internal static bool TryGetPlayerActorPreparationSnapshot(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerActorPreparationRuntimeHostSnapshot snapshot)
        {
            if (runtimeHost == null)
            {
                snapshot = PlayerActorPreparationRuntimeHostSnapshot.Unavailable(
                    "FrameworkRuntimeHost is missing.");
                return false;
            }

            PlayerActorPreparationRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (module == null)
            {
                snapshot = PlayerActorPreparationRuntimeHostSnapshot.Unavailable(
                    "FrameworkRuntimeHost has no Player Actor preparation module.");
                return false;
            }

            return module.TryGetSnapshot(out snapshot);
        }
    }
}
