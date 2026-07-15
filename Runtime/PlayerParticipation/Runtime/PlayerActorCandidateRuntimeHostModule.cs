using System;
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
    /// Explicit FrameworkRuntimeHost-scoped composition adapter for P3K.7C candidate staging.
    /// It is not auto-discovered and does not mutate ActivityFlow or GameFlow.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7C host composition for concurrent target Activity Player Actor candidates.")]
    internal sealed partial class PlayerActorCandidateRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost runtimeHost;
        private PlayerActorCandidateStageRuntimeContext candidateContext;
        private string diagnostic =
            "Player Actor candidate runtime is not initialized.";
        private bool shuttingDown;

        internal bool IsReady =>
            runtimeHost != null &&
            candidateContext != null;

        internal string Diagnostic => diagnostic;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            out PlayerActorCandidateRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue = "Player Actor candidate runtime requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            module = runtimeHost.GetComponent<PlayerActorCandidateRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<PlayerActorCandidateRuntimeHostModule>();
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

                issue = "Player Actor candidate runtime is already bound to another FrameworkRuntimeHost.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext participationContext))
            {
                issue = "FrameworkRuntimeHost has no initialized Player participation authority.";
                diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparationModule))
            {
                issue = "FrameworkRuntimeHost has no ready P3J Player Actor preparation module.";
                diagnostic = issue;
                return false;
            }

            RuntimeContentRuntime runtimeContentRuntime =
                targetRuntimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                issue = "FrameworkRuntimeHost has no RuntimeContentRuntime.";
                diagnostic = issue;
                return false;
            }

            PlayerParticipationSnapshot participation =
                participationContext.CreateSnapshot();
            if (participation == null ||
                !participation.IsInitialized ||
                string.IsNullOrEmpty(participation.ContextId))
            {
                issue = "Player participation snapshot is not initialized.";
                diagnostic = issue;
                return false;
            }

            var adapter = new AttachedPlayerActorMaterializationAdapter(
                runtimeContentRuntime,
                participation.ContextId);
            if (!PlayerActorCandidateStageRuntimeContext.TryCreate(
                    participationContext,
                    preparationModule,
                    adapter,
                    out PlayerActorCandidateStageRuntimeContext context,
                    out issue))
            {
                diagnostic = issue;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            candidateContext = context;
            diagnostic =
                $"Player Actor candidate runtime is ready. session='{participation.ContextId}'.";
            return true;
        }

        internal PlayerActorCandidateStageResult TryStageCandidate(
            RuntimeScopeContext targetActivityContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            if (candidateContext == null)
            {
                return RuntimeUnavailable(
                    "StageCandidate",
                    source,
                    reason,
                    diagnostic);
            }

            PlayerActorCandidateStageResult result =
                candidateContext.TryStage(
                    targetActivityContext,
                    playerSlotId,
                    source,
                    reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerActorCandidateStageResult TryRollbackCandidate(
            PlayerActorCandidateStageToken expectedCandidate,
            string source,
            string reason)
        {
            if (candidateContext == null)
            {
                return RuntimeUnavailable(
                    "RollbackCandidate",
                    source,
                    reason,
                    diagnostic);
            }

            PlayerActorCandidateStageResult result =
                candidateContext.TryRollback(
                    expectedCandidate,
                    source,
                    reason);
            diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal bool TryGetCandidatePhysicalEvidence(
            PlayerActorCandidateStageToken expectedCandidate,
            out LocalPlayerHostAuthoring host,
            out PlayerInput playerInput,
            out PlayerActorDeclaration declaration,
            out GameObject logicalActorHost,
            out string issue)
        {
            if (candidateContext == null)
            {
                host = null;
                playerInput = null;
                declaration = null;
                logicalActorHost = null;
                issue = diagnostic;
                return false;
            }

            return candidateContext.TryGetPhysicalEvidence(
                expectedCandidate,
                out host,
                out playerInput,
                out declaration,
                out logicalActorHost,
                out issue);
        }

        internal bool TryGetSnapshot(
            out PlayerActorCandidateRuntimeHostSnapshot snapshot)
        {
            if (candidateContext == null)
            {
                snapshot = PlayerActorCandidateRuntimeHostSnapshot.Unavailable(
                    diagnostic);
                return false;
            }

            snapshot = candidateContext.CreateSnapshot();
            return true;
        }

        internal bool TryRollbackAllCandidates(
            string source,
            string reason,
            out int rolledBackCount,
            out int failedCount,
            out string issue)
        {
            if (candidateContext == null)
            {
                rolledBackCount = 0;
                failedCount = 0;
                issue = diagnostic;
                return false;
            }

            bool succeeded = candidateContext.TryRollbackAll(
                source,
                reason,
                out rolledBackCount,
                out failedCount,
                out issue);
            diagnostic = succeeded
                ? $"Rolled back '{rolledBackCount}' Player Actor candidates."
                : $"Player Actor candidate rollback-all failed for '{failedCount}' candidates. {issue}";
            return succeeded;
        }

        private static PlayerActorCandidateStageResult RuntimeUnavailable(
            string operation,
            string source,
            string reason,
            string message)
        {
            PlayerActorCandidateStageSnapshot empty =
                PlayerActorCandidateStageSnapshot.Empty(
                    source,
                    reason,
                    message);
            return new PlayerActorCandidateStageResult(
                PlayerActorCandidateStageStatus.RejectedRuntimeUnavailable,
                operation,
                empty,
                empty,
                message);
        }

        private void OnDestroy()
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            if (candidateContext != null)
            {
                candidateContext.TryRollbackAll(
                    nameof(PlayerActorCandidateRuntimeHostModule),
                    "runtime-host-shutdown",
                    out _,
                    out _,
                    out _);
            }

            candidateContext = null;
            runtimeHost = null;
            diagnostic = "Player Actor candidate runtime was released.";
        }
    }

    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7C typed same-host access to Player Actor candidate staging.")]
    internal static class FrameworkRuntimeHostPlayerActorCandidateExtensions
    {
        internal static bool TryGetPlayerActorCandidateRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerActorCandidateRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<PlayerActorCandidateRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }

        internal static bool TryGetPlayerActorCandidateSnapshot(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerActorCandidateRuntimeHostSnapshot snapshot)
        {
            if (runtimeHost == null)
            {
                snapshot = PlayerActorCandidateRuntimeHostSnapshot.Unavailable(
                    "FrameworkRuntimeHost is missing.");
                return false;
            }

            PlayerActorCandidateRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerActorCandidateRuntimeHostModule>();
            if (module == null)
            {
                snapshot = PlayerActorCandidateRuntimeHostSnapshot.Unavailable(
                    "FrameworkRuntimeHost has no Player Actor candidate module.");
                return false;
            }

            return module.TryGetSnapshot(out snapshot);
        }
    }
}
