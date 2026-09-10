using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

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
            GameObject presentation,
            out string issue)
        {
            issue = string.Empty;
            if (!preparation.IsPrepared ||
                !preparation.Token.IsValid ||
                actor == null ||
                actor.transform == null ||
                presentation == null ||
                presentation.transform == null)
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

            if (!TryResolveObservation(
                    actor,
                    presentation,
                    out Transform observation,
                    out issue))
            {
                if (_publications.TryGetValue(playerSlotId, out Publication current))
                {
                    CameraSubjectAvailabilityResult removal =
                        _availability.TryMakeUnavailable(current.AvailabilityToken);
                    if (!removal.Succeeded)
                    {
                        issue += " Existing Camera Subject publication could not be released. " +
                            removal.Message;
                    }
                    else
                    {
                        _publications.Remove(playerSlotId);
                    }
                }

                return false;
            }

            var subject = new CameraSubject(
                new CameraSubjectId(
                    $"camera.subject.player-actor:{preparation.Token.StableText}"),
                observation,
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

        private static bool TryResolveObservation(
            PlayerActorDeclaration actor,
            GameObject presentation,
            out Transform observation,
            out string issue)
        {
            observation = null;
            issue = string.Empty;

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
