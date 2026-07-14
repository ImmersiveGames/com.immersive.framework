using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

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
        "P3J.5 FrameworkRuntimeHost integration for real local Player Actor preparation.")]
    internal sealed class PlayerActorPreparationRuntimeHostModule : MonoBehaviour
    {
        private readonly Dictionary<PlayerSlotId, LocalPlayerHostAuthoring> joinedHosts =
            new Dictionary<PlayerSlotId, LocalPlayerHostAuthoring>();

        private FrameworkRuntimeHost runtimeHost;
        private PlayerParticipationRuntimeContext participationContext;
        private PlayerActorPreparationRuntimeContext preparationContext;
        private LocalPlayerJoinResult lastJoinResult;
        private string diagnostic = "Player Actor preparation runtime is not initialized.";
        private int joinRequestCount;
        private int preparationRequestCount;
        private bool shuttingDown;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            preparationContext != null;

        internal string Diagnostic => diagnostic;
        internal LocalPlayerJoinResult LastJoinResult => lastJoinResult;
        internal int RegisteredHostCount => joinedHosts.Count;
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
            if (!PlayerActorPreparationRuntimeContext.TryCreate(
                    targetParticipationContext,
                    adapter,
                    out PlayerActorPreparationRuntimeContext targetPreparationContext,
                    out issue))
            {
                diagnostic = issue;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipationContext;
            preparationContext = targetPreparationContext;
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

            if (joinedHosts.TryGetValue(slot.PlayerSlotId, out LocalPlayerHostAuthoring existing))
            {
                if (ReferenceEquals(existing, host))
                {
                    RecordSuccessfulJoin(joinResult);
                    return true;
                }

                issue =
                    $"Player Slot '{slot.PlayerSlotId.StableText}' is already registered to another Local Player Host.";
                return false;
            }

            joinedHosts.Add(slot.PlayerSlotId, host);
            RecordSuccessfulJoin(joinResult);
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

            if (!IsReady)
            {
                issue = diagnostic;
                return false;
            }

            if (!playerSlotId.IsValid)
            {
                issue = "Registered host lookup requires a valid Player Slot identity.";
                return false;
            }

            if (!joinedHosts.TryGetValue(playerSlotId, out host) || host == null)
            {
                joinedHosts.Remove(playerSlotId);
                host = null;
                issue =
                    $"No joined Local Player Host is registered for Player Slot '{playerSlotId.StableText}'.";
                return false;
            }

            if (!host.IsJoined ||
                !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId)
            {
                joinedHosts.Remove(playerSlotId);
                host = null;
                issue =
                    $"Registered Local Player Host no longer has matching Joined Slot evidence for '{playerSlotId.StableText}'.";
                return false;
            }

            return true;
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

            if (!TryGetRegisteredHost(playerSlotId, out LocalPlayerHostAuthoring host,
                    out string issue))
            {
                return PlayerActorPreparationResult.HostUnavailable(
                    "PrepareSelectedActor",
                    playerSlotId,
                    issue,
                    preparationContext.CreateSnapshot());
            }

            PlayerActorPreparationResult result =
                preparationContext.TryPrepareSelectedActor(
                    scopeContext,
                    playerSlotId,
                    host,
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
                joinedHosts.Count,
                joinRequestCount,
                preparationRequestCount,
                lastJoinResult != null ? lastJoinResult.Status : LocalPlayerJoinStatus.None,
                preparation,
                diagnostic);
            return IsReady;
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

        private void RecordSuccessfulJoin(LocalPlayerJoinResult joinResult)
        {
            if (!ReferenceEquals(lastJoinResult, joinResult))
            {
                joinRequestCount++;
            }

            lastJoinResult = joinResult;
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
                TryReleaseAllPreparedActors(
                    nameof(PlayerActorPreparationRuntimeHostModule),
                    "runtime-host-shutdown",
                    out _,
                    out _,
                    out _);
            }

            joinedHosts.Clear();
            preparationContext = null;
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
        "P3J.5 preparation registration bridge for successful local Player joins.")]
    internal static class LocalPlayerProvisioningPreparationExtensions
    {
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
                throw new InvalidOperationException(
                    $"Successful local Player join could not register its stable host with Actor preparation authority. {issue}");
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
