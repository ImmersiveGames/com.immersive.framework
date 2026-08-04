using System;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private const string ActivityPlayerReadinessObjectName =
            "Player Activity Readiness";
        private ActivityReadinessParticipant activityPlayerReadinessParticipant;

        internal ActivityReadinessParticipant
            GetOrCreateActivityPlayerReadinessParticipant()
        {
            if (runtimeHost == null)
            {
                throw new InvalidOperationException(
                    "Player Activity readiness participant requires an initialized FrameworkRuntimeHost.");
            }

            if (activityPlayerReadinessParticipant != null)
            {
                return activityPlayerReadinessParticipant;
            }

            Transform existing = runtimeHost.transform.Find(
                ActivityPlayerReadinessObjectName);
            GameObject participantObject = existing != null
                ? existing.gameObject
                : new GameObject(ActivityPlayerReadinessObjectName);
            if (existing == null)
            {
                participantObject.transform.SetParent(
                    runtimeHost.transform,
                    false);
            }

            activityPlayerReadinessParticipant =
                participantObject.GetComponent<
                    ActivityReadinessParticipant>();
            if (activityPlayerReadinessParticipant == null)
            {
                activityPlayerReadinessParticipant =
                    participantObject.AddComponent<
                        ActivityReadinessParticipant>();
            }

            activityPlayerReadinessParticipant.ConfigureRuntimeParticipant(
                "framework.player-actor.activity-readiness",
                ActivityContentExecutionRequiredness.Required,
                -190);
            return activityPlayerReadinessParticipant;
        }

        internal bool TryGetPlayerGameplayRuntime(
            out PlayerGameplayRuntimeHostModule gameplayRuntime,
            out string issue)
        {
            gameplayRuntime = runtimeHost != null
                ? runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>()
                : null;
            if (gameplayRuntime == null || !gameplayRuntime.IsReady)
            {
                gameplayRuntime = null;
                issue =
                    "FrameworkRuntimeHost has no ready Player gameplay runtime for Activity reconciliation.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        internal bool TryGetActivityPlayerActorReconcileResult(
            out ActivityPlayerActorReconcileResult result)
        {
            result = activityLifecycleParticipant?.LastReconcileResult;
            return result != null;
        }

        internal ActivityPlayerActorReconcileResult
            TryReconcileActiveActivityPlayerLifecycle(
                ActivityAsset expectedActivity,
                RuntimeContentOwner expectedOwner,
                int expectedOccurrence,
                string source,
                string reason)
        {
            if (activityLifecycleParticipant == null)
            {
                return new ActivityPlayerActorReconcileResult(
                    ActivityPlayerActorReconcileStatus
                        .RejectedNoActiveActivity,
                    expectedActivity != null
                        ? expectedActivity.ActivityName
                        : string.Empty,
                    expectedOwner,
                    expectedOccurrence,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    ActivityPlayerActorReadinessReason.None,
                    false,
                    false,
                    false,
                    ActivityPlayerActorLifecycleSnapshot.Empty(
                        "Activity Player Actor lifecycle participant is unavailable."),
                    "Activity Player Actor lifecycle participant is unavailable.");
            }

            return activityLifecycleParticipant
                .TryReconcileActiveActivityPlayerLifecycle(
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    source,
                    reason);
        }
    }

    /// <summary>
    /// Narrow same-host access for deterministic QA and the future automatic
    /// coordinator. The caller must already hold the scoped runtime host.
    /// </summary>
    internal static class
        FrameworkRuntimeHostActivityPlayerReconcileExtensions
    {
        internal static ActivityPlayerActorReconcileResult
            TryReconcileActiveActivityPlayerLifecycle(
                this FrameworkRuntimeHost runtimeHost,
                ActivityAsset expectedActivity,
                RuntimeContentOwner expectedOwner,
                int expectedOccurrence,
                string source,
                string reason)
        {
            if (runtimeHost == null ||
                !runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule module))
            {
                return new ActivityPlayerActorReconcileResult(
                    ActivityPlayerActorReconcileStatus
                        .RejectedNoActiveActivity,
                    expectedActivity != null
                        ? expectedActivity.ActivityName
                        : string.Empty,
                    expectedOwner,
                    expectedOccurrence,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    ActivityPlayerActorReadinessReason.None,
                    false,
                    false,
                    false,
                    ActivityPlayerActorLifecycleSnapshot.Empty(
                        "FrameworkRuntimeHost has no ready Player Actor preparation runtime."),
                    "FrameworkRuntimeHost has no ready Player Actor preparation runtime.");
            }

            return module.TryReconcileActiveActivityPlayerLifecycle(
                expectedActivity,
                expectedOwner,
                expectedOccurrence,
                source,
                reason);
        }
    }
}
