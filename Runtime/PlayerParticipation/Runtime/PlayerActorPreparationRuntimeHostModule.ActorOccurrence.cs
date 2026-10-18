using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        internal bool TryOpenActorCompositionContribution(
            PlayerActorCompositionContributionRequest request,
            Func<bool> isScopedAccessCurrent,
            out PlayerActorCompositionContribution contribution,
            out string issue)
        {
            contribution = null;
            issue = string.Empty;
            if (_preparationContext == null || !_sessionPhysicalScopeContext.IsValid ||
                request == null || !request.IsValid || isScopedAccessCurrent == null)
            {
                issue = "Actor composition access requires a ready Session-owned Player Actor preparation runtime and a valid request.";
                return false;
            }

            bool IsTargetCurrent() => isScopedAccessCurrent() &&
                _preparationContext.TryGetCurrentActorCompositionTarget(
                    request,
                    _sessionPhysicalScopeContext.Owner,
                    out _,
                    out _);

            if (!isScopedAccessCurrent())
            {
                issue = "Player Session scoped access is no longer current.";
                return false;
            }

            if (!_preparationContext.TryGetCurrentActorCompositionTarget(
                    request,
                    _sessionPhysicalScopeContext.Owner,
                    out PlayerActorMaterializationHandle handle,
                    out issue))
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "The exact Player and Actor occurrence is no longer current."
                    : issue;
                return false;
            }

            ActorCompositionRuntimeBinding composition = handle.Composition;
            bool attachNewComposition = composition == null;
            if (composition == null)
            {
                if (handle.Request.ActorProfile == null ||
                    handle.Request.ActorProfile.CompositionParts.Count > 0)
                {
                    issue = "The current Actor Profile has authored composition parts but its authoritative Composer binding is unavailable.";
                    return false;
                }

                if (handle.VisualContent != null)
                {
                    issue = "Actor composition cannot adopt a visual instance outside the existing Composer-owned materialization path.";
                    return false;
                }

                Transform mount = handle.PlayerActorRuntimeHost != null
                    ? handle.PlayerActorRuntimeHost.VisualContentMount
                    : null;
                if (!ActorCompositionRuntimeBinding.TryCreate(
                        handle.Request.ActorProfile,
                        mount,
                        null,
                        handle.Request.OperationId,
                        out composition,
                        out issue,
                        createWhenEmpty: true))
                {
                    return false;
                }

                if (composition == null || !composition.TrySetPartsActive(true, out issue))
                {
                    composition?.TryClose(out _);
                    issue = string.IsNullOrWhiteSpace(issue)
                        ? "The empty Actor composition could not be initialized for the exact active Actor occurrence."
                        : issue;
                    return false;
                }
            }

            if (!composition.TryOpenContribution(
                    request,
                    IsTargetCurrent,
                    out contribution,
                    out issue))
            {
                if (attachNewComposition)
                {
                    composition.TryClose(out _);
                }

                return false;
            }

            if (attachNewComposition && !handle.TryAttachComposition(composition))
            {
                composition.TryClose(out _);
                contribution = null;
                issue = "The empty Actor composition could not be attached to the exact active Actor occurrence.";
                return false;
            }

            return true;
        }

        private event Action<PlayerSlotId> CurrentActorInvalidated;
        private string _currentActorInvalidationDiagnostic = string.Empty;

        internal string CurrentActorInvalidationDiagnostic =>
            _currentActorInvalidationDiagnostic;

        event Action<PlayerSlotId>
            IPlayerPreparedActorOccurrenceSource.CurrentActorInvalidated
        {
            add => CurrentActorInvalidated += value;
            remove => CurrentActorInvalidated -= value;
        }

        string IPlayerPreparedActorOccurrenceSource.SessionContextId =>
            _participationContext?.CreateSnapshot()?.ContextId ?? string.Empty;

        bool IPlayerPreparedActorOccurrenceSource.TryGetCurrentActorOccurrence(
            PlayerSlotId playerSlotId,
            out PlayerPreparedActorOccurrence occurrence)
        {
            occurrence = default;
            if (_preparationContext == null)
            {
                return false;
            }

            PlayerCurrentActorEvidenceResult confirmation =
                _preparationContext.ConfirmCurrentActorEvidence(
                    playerSlotId,
                    default,
                    nameof(PlayerActorPreparationRuntimeHostModule),
                    "prepared-actor-occurrence-query");
            if (confirmation == null || !confirmation.Succeeded)
            {
                return false;
            }

            if (!_preparationContext.TryGetPreparedPhysicalEvidence(
                    playerSlotId,
                    confirmation.Preparation.Token,
                    out _,
                    out _,
                    out PlayerActorDeclaration actorDeclaration,
                    out PlayerActorMaterializationHandle materialization,
                    out _))
            {
                return false;
            }

            occurrence = new PlayerPreparedActorOccurrence(
                confirmation.Preparation.Token,
                actorDeclaration,
                materialization.VisualContent);
            return occurrence.IsValid;
        }

        bool IPlayerPreparedActorOccurrenceSource.TryGetCurrentPhysicalActorOccurrence(
            PlayerSlotId playerSlotId,
            out PlayerActorPhysicalOccurrence occurrence)
        {
            occurrence = default;
            return _preparationContext != null &&
                _sessionPhysicalScopeContext.IsValid &&
                _preparationContext.TryGetCurrentPhysicalActorOccurrence(
                    playerSlotId,
                    _sessionPhysicalScopeContext.Owner,
                    out occurrence);
        }

        void IPlayerPreparedActorOccurrenceSource.CollectCurrentPhysicalActorOccurrences(
            ICollection<PlayerActorPhysicalOccurrence> occurrences)
        {
            if (occurrences == null || _participationContext == null || _preparationContext == null ||
                !_sessionPhysicalScopeContext.IsValid) return;
            PlayerParticipationSnapshot snapshot = _participationContext.CreateSnapshot();
            IReadOnlyList<PlayerSlotRuntimeSnapshot> slots = snapshot.Slots;
            for (int index = 0; index < slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = slots[index];
                if (slot.IsJoined && ((IPlayerPreparedActorOccurrenceSource)this)
                    .TryGetCurrentPhysicalActorOccurrence(slot.PlayerSlotId, out PlayerActorPhysicalOccurrence occurrence))
                    occurrences.Add(occurrence);
            }
        }

        internal void InvalidateCurrentActorOccurrence(PlayerSlotId playerSlotId)
        {
            _currentActorInvalidationDiagnostic = string.Empty;
            Action<PlayerSlotId> subscribers = CurrentActorInvalidated;
            if (!playerSlotId.IsValid || subscribers == null)
            {
                return;
            }

            Delegate[] invocationList = subscribers.GetInvocationList();
            for (int index = 0; index < invocationList.Length; index++)
            {
                var subscriber = (Action<PlayerSlotId>)invocationList[index];
                try
                {
                    subscriber(playerSlotId);
                }
                catch (Exception exception)
                {
                    string failure =
                        $"Current Actor invalidation subscriber '{subscriber.Method.Name}' " +
                        $"failed for '{playerSlotId.StableText}': " +
                        exception.GetType().Name;
                    _currentActorInvalidationDiagnostic =
                        string.IsNullOrEmpty(_currentActorInvalidationDiagnostic)
                            ? failure
                            : _currentActorInvalidationDiagnostic + " | " + failure;
                }
            }
        }
    }
}
