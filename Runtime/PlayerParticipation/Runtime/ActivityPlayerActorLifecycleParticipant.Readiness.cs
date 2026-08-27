using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant :
        IActivityReadinessParticipantSource
    {
        private ActivityReadinessParticipant _playerReadinessParticipant;
        private bool _playerReadinessCallbacksBound;

        public IReadOnlyList<ActivityReadinessParticipant>
            ResolveActivityReadinessParticipants(ActivityAsset activity)
        {
            if (activity == null)
            {
                return Array.Empty<ActivityReadinessParticipant>();
            }

            if (!TryResolveProjection(
                    activity,
                    out PlayerParticipationRequirementLevel requirementLevel,
                    out List<PlayerSlotRuntimeSnapshot> projectedSlots,
                    out string projectionIssue))
            {
                throw new InvalidOperationException(
                    "Player Activity readiness source could not resolve the " +
                    $"Activity participation projection. {projectionIssue}");
            }

            if (requirementLevel ==
                    PlayerParticipationRequirementLevel.None ||
                projectedSlots.Count == 0)
            {
                return Array.Empty<ActivityReadinessParticipant>();
            }

            return new[] { EnsurePlayerReadinessParticipant() };
        }

        private ActivityReadinessParticipant
            EnsurePlayerReadinessParticipant()
        {
            if (_playerReadinessParticipant == null)
            {
                _playerReadinessParticipant = _preparationModule
                    .GetOrCreateActivityPlayerReadinessParticipant();
            }

            if (!_playerReadinessCallbacksBound)
            {
                _playerReadinessParticipant.PreparationStarted.AddListener(
                    OnPlayerReadinessPreparationStarted);
                _playerReadinessParticipant.PreparationReleased.AddListener(
                    OnPlayerReadinessPreparationReleased);
                _playerReadinessCallbacksBound = true;
            }

            return _playerReadinessParticipant;
        }

        private void OnPlayerReadinessPreparationStarted()
        {
            SynchronizePlayerReadinessContributionAfterRecordCreated();
        }

        private void SynchronizePlayerReadinessContributionAfterRecordCreated()
        {
            if (_playerReadinessRecord == null ||
                _playerReadinessParticipant == null ||
                _playerReadinessParticipant.State !=
                    ActivityReadinessParticipantState.Preparing)
            {
                return;
            }

            _playerReadinessRecord.occurrence =
                _playerReadinessParticipant.Occurrence;
            ApplyPlayerReadinessRecordTerminalState();
        }

        private bool ApplyPlayerReadinessRecordTerminalState()
        {
            if (_playerReadinessRecord == null ||
                _playerReadinessParticipant == null ||
                _playerReadinessParticipant.State ==
                    ActivityReadinessParticipantState.Idle ||
                _playerReadinessParticipant.State ==
                    ActivityReadinessParticipantState.Released)
            {
                return false;
            }

            if (_playerReadinessRecord.failed)
            {
                if (_playerReadinessParticipant.State ==
                    ActivityReadinessParticipantState.Failed)
                {
                    return false;
                }

                _playerReadinessParticipant.FailPreparation(
                    _playerReadinessRecord.message);
                return true;
            }

            bool resumed = _playerReadinessParticipant.ResumePreparation(
                _playerReadinessRecord.completed
                    ? "Preparing"
                    : _playerReadinessRecord.readinessReason.ToString());
            if (_playerReadinessRecord.completed)
            {
                _playerReadinessParticipant.CompletePreparation();
                return true;
            }

            return resumed;
        }

        private void OnPlayerReadinessPreparationReleased()
        {
            if (_playerReadinessRecord == null ||
                _playerReadinessParticipant == null ||
                _playerReadinessRecord.occurrence <= 0 ||
                _playerReadinessRecord.occurrence !=
                    _playerReadinessParticipant.Occurrence)
            {
                return;
            }

            _playerReadinessRecord.released = true;
            _playerReadinessRecord.readinessReason =
                ActivityPlayerActorReadinessReason.Released;
        }

        private bool CompletePlayerReadinessContribution(
            string message)
        {
            if (_playerReadinessRecord == null)
            {
                return false;
            }

            _playerReadinessRecord.completed = true;
            _playerReadinessRecord.failed = false;
            _playerReadinessRecord.readinessReason =
                ActivityPlayerActorReadinessReason.RequirementSatisfied;
            _playerReadinessRecord.message = message ?? string.Empty;

            return ApplyPlayerReadinessRecordTerminalState();
        }

        private bool FailPlayerReadinessContribution(
            string message)
        {
            if (_playerReadinessRecord == null)
            {
                return false;
            }

            _playerReadinessRecord.completed = false;
            _playerReadinessRecord.failed = true;
            _playerReadinessRecord.readinessReason =
                ActivityPlayerActorReadinessReason.Failed;
            _playerReadinessRecord.message = string.IsNullOrWhiteSpace(message)
                ? "Player Activity readiness failed."
                : message.Trim();

            return ApplyPlayerReadinessRecordTerminalState();
        }

        private bool ContinuePlayerReadinessContribution()
        {
            if (_playerReadinessRecord == null)
            {
                return false;
            }

            _playerReadinessRecord.completed = false;
            _playerReadinessRecord.failed = false;
            return ApplyPlayerReadinessRecordTerminalState();
        }

        private void ReleasePlayerReadinessRecord(string reason)
        {
            if (_playerReadinessRecord == null)
            {
                return;
            }

            _playerReadinessRecord.released = true;
            _playerReadinessRecord.readinessReason =
                ActivityPlayerActorReadinessReason.Released;
            _playerReadinessRecord.message = string.IsNullOrWhiteSpace(reason)
                ? "ActivityExit"
                : reason.Trim();
            _playerReadinessRecord = null;
        }
    }
}
