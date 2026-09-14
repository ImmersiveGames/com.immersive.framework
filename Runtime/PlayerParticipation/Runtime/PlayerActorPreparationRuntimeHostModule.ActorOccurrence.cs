using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
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
                materialization.Presentation);
            return occurrence.IsValid;
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
