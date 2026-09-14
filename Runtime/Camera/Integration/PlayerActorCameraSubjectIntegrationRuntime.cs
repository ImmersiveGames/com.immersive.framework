using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    internal sealed class PlayerActorCameraSubjectIntegrationRuntime : IDisposable
    {
        private readonly struct Publication
        {
            internal Publication(
                PlayerActorPreparationToken preparationToken,
                CameraSubjectAvailabilityToken availabilityToken)
            {
                PreparationToken = preparationToken;
                AvailabilityToken = availabilityToken;
            }

            internal PlayerActorPreparationToken PreparationToken { get; }
            internal CameraSubjectAvailabilityToken AvailabilityToken { get; }
        }

        private readonly IPlayerPreparedActorOccurrenceSource _playerActors;
        private readonly CameraSubjectAvailabilityContext _availability;
        private readonly CameraSubjectAvailabilityOwnerId _ownerId;
        private readonly Dictionary<PlayerSlotId, Publication> _publications = new();
        private bool _disposed;

        internal PlayerActorCameraSubjectIntegrationRuntime(
            IPlayerPreparedActorOccurrenceSource playerActors,
            CameraSubjectAvailabilityContext availability)
        {
            _playerActors = playerActors ??
                throw new ArgumentNullException(nameof(playerActors));
            _availability = availability ??
                throw new ArgumentNullException(nameof(availability));
            if (string.IsNullOrWhiteSpace(playerActors.SessionContextId))
            {
                throw new ArgumentException(
                    "Player Actor Camera Subject integration requires a Session context id.",
                    nameof(playerActors));
            }

            _ownerId = new CameraSubjectAvailabilityOwnerId(
                $"camera-subject-owner.player-actors:{playerActors.SessionContextId}");
            _playerActors.CurrentActorInvalidated += OnCurrentActorInvalidated;
            LastReconciliationSucceeded = true;
            Diagnostic = "Player Actor Camera Subject integration is ready.";
        }

        internal bool LastReconciliationSucceeded { get; private set; }
        internal string Diagnostic { get; private set; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _playerActors.CurrentActorInvalidated -= OnCurrentActorInvalidated;
            _availability.ReleaseOwner(_ownerId);
            _publications.Clear();
            LastReconciliationSucceeded = true;
            Diagnostic = "Player Actor Camera Subject integration was released.";
        }

        private void OnCurrentActorInvalidated(PlayerSlotId playerSlotId)
        {
            try
            {
                LastReconciliationSucceeded = Reconcile(playerSlotId, out string issue);
                Diagnostic = LastReconciliationSucceeded
                    ? $"Player Actor Camera Subject is current for '{playerSlotId.StableText}'."
                    : issue;
            }
            catch (Exception exception)
            {
                LastReconciliationSucceeded = false;
                Diagnostic =
                    $"Player Actor Camera Subject reconciliation failed for '{playerSlotId.StableText}'. " +
                    exception.Message;
            }
        }

        private bool Reconcile(PlayerSlotId playerSlotId, out string issue)
        {
            issue = string.Empty;
            if (_disposed || !playerSlotId.IsValid)
            {
                issue = "Player Actor Camera Subject reconciliation requires an active integration and valid Player Slot.";
                return false;
            }

            if (!_playerActors.TryGetCurrentActorOccurrence(
                    playerSlotId,
                    out PlayerPreparedActorOccurrence occurrence))
            {
                return TryRemoveCurrent(playerSlotId, out issue);
            }

            if (!occurrence.IsValid || occurrence.PlayerSlotId != playerSlotId)
            {
                issue = "Player Actor Camera Subject reconciliation received invalid or divergent current Actor evidence.";
                TryRemoveCurrent(playerSlotId, out _);
                return false;
            }

            if (_publications.TryGetValue(playerSlotId, out Publication previous) &&
                previous.PreparationToken != occurrence.PreparationToken)
            {
                CameraSubjectAvailabilityResult removal =
                    _availability.TryMakeUnavailable(previous.AvailabilityToken);
                if (!removal.Succeeded)
                {
                    issue = removal.Message;
                    return false;
                }

                _publications.Remove(playerSlotId);
            }

            if (!TryResolveObservation(
                    occurrence.ActorDeclaration,
                    occurrence.Presentation,
                    out Transform observation,
                    out issue))
            {
                TryRemoveCurrent(playerSlotId, out string removalIssue);
                if (!string.IsNullOrEmpty(removalIssue))
                {
                    issue += " Existing Camera Subject publication could not be released. " +
                        removalIssue;
                }

                return false;
            }

            var subject = new CameraSubject(
                new CameraSubjectId(
                    $"camera.subject.player-actor:{occurrence.PreparationToken.StableText}"),
                observation,
                $"Current Session Player Actor for {playerSlotId.StableText}");
            CameraSubjectAvailabilityResult publication =
                _availability.TryMakeAvailable(subject, _ownerId);
            if (!publication.Succeeded)
            {
                issue = "Camera Subject publication conflict or failure. " + publication.Message;
                return false;
            }

            _publications[playerSlotId] = new Publication(
                occurrence.PreparationToken,
                publication.Token);
            return true;
        }

        private bool TryRemoveCurrent(PlayerSlotId playerSlotId, out string issue)
        {
            issue = string.Empty;
            if (!_publications.TryGetValue(playerSlotId, out Publication publication))
            {
                return true;
            }

            CameraSubjectAvailabilityResult removal =
                _availability.TryMakeUnavailable(publication.AvailabilityToken);
            if (!removal.Succeeded)
            {
                issue = removal.Message;
                return false;
            }

            _publications.Remove(playerSlotId);
            return true;
        }

        private static bool TryResolveObservation(
            PlayerActorDeclaration actor,
            GameObject presentation,
            out Transform observation,
            out string issue)
        {
            observation = null;
            issue = string.Empty;
            if (actor == null || actor.transform == null ||
                presentation == null || presentation.transform == null)
            {
                issue = "Player Actor Camera Subject requires exact prepared Actor Presentation evidence.";
                return false;
            }

            ActorCameraSubjectAuthoring[] authoredSubjects =
                presentation.GetComponentsInChildren<ActorCameraSubjectAuthoring>(true);
            if (authoredSubjects.Length == 0)
            {
                observation = actor.transform;
                return true;
            }

            if (authoredSubjects.Length != 1)
            {
                issue =
                    $"Prepared Actor Presentation requires zero or one Actor Camera Subject authoring component. Found '{authoredSubjects.Length}'.";
                return false;
            }

            if (!authoredSubjects[0].TryResolveObservation(
                    presentation.transform,
                    out observation,
                    out issue))
            {
                issue = "Prepared Actor Camera Subject is invalid. " + issue;
                return false;
            }

            return true;
        }
    }
}
