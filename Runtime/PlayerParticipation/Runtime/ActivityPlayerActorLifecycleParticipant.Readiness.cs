using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant :
        IActivityReadinessParticipantSource
    {
        private ActivityReadinessParticipant playerReadinessParticipant;
        private bool playerReadinessCallbacksBound;

        public IReadOnlyList<ActivityReadinessParticipant>
            ResolveActivityReadinessParticipants(ActivityAsset activity)
        {
            if (activity == null ||
                playerReadinessRecord == null ||
                playerReadinessRecord.Released ||
                playerReadinessRecord.ProjectedSlots.Count == 0 ||
                playerReadinessRecord.RequirementLevel ==
                    PlayerParticipationRequirementLevel.None ||
                !ReferenceEquals(playerReadinessRecord.Activity, activity))
            {
                return Array.Empty<ActivityReadinessParticipant>();
            }

            return new[] { EnsurePlayerReadinessParticipant() };
        }

        private ActivityReadinessParticipant
            EnsurePlayerReadinessParticipant()
        {
            if (playerReadinessParticipant == null)
            {
                playerReadinessParticipant = preparationModule
                    .GetOrCreateActivityPlayerReadinessParticipant();
            }

            if (!playerReadinessCallbacksBound)
            {
                playerReadinessParticipant.PreparationStarted.AddListener(
                    OnPlayerReadinessPreparationStarted);
                playerReadinessParticipant.PreparationReleased.AddListener(
                    OnPlayerReadinessPreparationReleased);
                playerReadinessCallbacksBound = true;
            }

            return playerReadinessParticipant;
        }

        private void OnPlayerReadinessPreparationStarted()
        {
            if (playerReadinessRecord == null ||
                playerReadinessParticipant == null)
            {
                return;
            }

            playerReadinessRecord.Occurrence =
                playerReadinessParticipant.Occurrence;
            if (playerReadinessRecord.Failed)
            {
                playerReadinessParticipant.FailPreparation(
                    playerReadinessRecord.Message);
                return;
            }

            if (playerReadinessRecord.Completed)
            {
                playerReadinessParticipant.CompletePreparation();
            }
        }

        private void OnPlayerReadinessPreparationReleased()
        {
            if (playerReadinessRecord == null ||
                playerReadinessParticipant == null ||
                playerReadinessRecord.Occurrence <= 0 ||
                playerReadinessRecord.Occurrence !=
                    playerReadinessParticipant.Occurrence)
            {
                return;
            }

            playerReadinessRecord.Released = true;
            playerReadinessRecord.ReadinessReason =
                ActivityPlayerActorReadinessReason.Released;
        }

        private void CompletePlayerReadinessContribution(
            string message)
        {
            if (playerReadinessRecord == null)
            {
                return;
            }

            playerReadinessRecord.Completed = true;
            playerReadinessRecord.Failed = false;
            playerReadinessRecord.ReadinessReason =
                ActivityPlayerActorReadinessReason.RequirementSatisfied;
            playerReadinessRecord.Message = message ?? string.Empty;

            if (playerReadinessParticipant != null &&
                playerReadinessParticipant.State ==
                    ActivityReadinessParticipantState.Preparing)
            {
                playerReadinessParticipant.CompletePreparation();
            }
        }

        private void FailPlayerReadinessContribution(
            string message)
        {
            if (playerReadinessRecord == null)
            {
                return;
            }

            playerReadinessRecord.Completed = false;
            playerReadinessRecord.Failed = true;
            playerReadinessRecord.ReadinessReason =
                ActivityPlayerActorReadinessReason.Failed;
            playerReadinessRecord.Message = string.IsNullOrWhiteSpace(message)
                ? "Player Activity readiness failed."
                : message.Trim();

            if (playerReadinessParticipant != null &&
                playerReadinessParticipant.State ==
                    ActivityReadinessParticipantState.Preparing)
            {
                playerReadinessParticipant.FailPreparation(
                    playerReadinessRecord.Message);
            }
        }

        private void ReleasePlayerReadinessRecord(string reason)
        {
            if (playerReadinessRecord == null)
            {
                return;
            }

            playerReadinessRecord.Released = true;
            playerReadinessRecord.ReadinessReason =
                ActivityPlayerActorReadinessReason.Released;
            playerReadinessRecord.Message = string.IsNullOrWhiteSpace(reason)
                ? "ActivityExit"
                : reason.Trim();
            playerReadinessRecord = null;
        }
    }
}
