using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Player-specific adapter from exact Session physical Actor occurrences to the
    /// general Camera Subject availability authority.
    /// </summary>
    internal sealed class PlayerCameraSubjectAvailabilityProjection
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

        private readonly CameraSubjectAvailabilityContext _availability;
        private readonly CameraSubjectAvailabilityOwnerId _ownerId;
        private readonly Dictionary<PlayerSlotId, Publication> _publications =
            new Dictionary<PlayerSlotId, Publication>();

        internal PlayerCameraSubjectAvailabilityProjection(
            string sessionContextId,
            CameraSubjectAvailabilityContext availability)
        {
            if (string.IsNullOrWhiteSpace(sessionContextId))
            {
                throw new ArgumentException(
                    "Player Camera Subject projection requires a Session context id.",
                    nameof(sessionContextId));
            }

            _availability = availability ?? throw new ArgumentNullException(nameof(availability));
            _ownerId = new CameraSubjectAvailabilityOwnerId(
                $"camera-subject-owner.player-actors:{sessionContextId}");
        }

        internal bool TryPublishCurrent(
            PlayerActorPreparationSummary preparation,
            PlayerActorDeclaration actor,
            out string issue)
        {
            issue = string.Empty;
            if (!preparation.IsPrepared ||
                !preparation.Token.IsValid ||
                actor == null ||
                actor.transform == null)
            {
                issue = "Player Camera Subject publication requires exact prepared Actor evidence.";
                return false;
            }

            PlayerSlotId playerSlotId = preparation.PlayerSlotId;
            if (_publications.TryGetValue(playerSlotId, out Publication previous) &&
                previous.PreparationToken != preparation.Token)
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

            var subject = new CameraSubject(
                new CameraSubjectId(
                    $"camera.subject.player-actor:{preparation.Token.StableText}"),
                actor.transform,
                $"Current Session Player Actor for {playerSlotId.StableText}");
            CameraSubjectAvailabilityResult publication =
                _availability.TryMakeAvailable(subject, _ownerId);
            if (!publication.Succeeded)
            {
                issue = publication.Message;
                return false;
            }

            _publications[playerSlotId] = new Publication(
                preparation.Token,
                publication.Token);
            return true;
        }

        internal bool TryRemoveCurrent(PlayerSlotId playerSlotId, out string issue)
        {
            issue = string.Empty;
            if (!_publications.TryGetValue(playerSlotId, out Publication publication))
            {
                return true;
            }

            CameraSubjectAvailabilityResult result =
                _availability.TryMakeUnavailable(publication.AvailabilityToken);
            if (!result.Succeeded)
            {
                issue = result.Message;
                return false;
            }

            _publications.Remove(playerSlotId);
            return true;
        }

        internal void ReleaseScope()
        {
            _availability.ReleaseOwner(_ownerId);
            _publications.Clear();
        }
    }
}
