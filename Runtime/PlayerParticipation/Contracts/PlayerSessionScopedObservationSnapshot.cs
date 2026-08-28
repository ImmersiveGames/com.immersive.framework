using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ACCESS-2 typed per-Slot provider-neutral Player Session observation.")]
    public readonly struct PlayerSessionScopedSlotObservation
    {
        internal PlayerSessionScopedSlotObservation(
            PlayerSlotRuntimeSnapshot slot,
            PlayerHostEvidenceSummary hostEvidence,
            bool hasHostEvidence,
            PlayerActorPreparationSummary preparation,
            bool hasPreparationEvidence,
            CurrentPlayerSlotActorSnapshot currentActor,
            bool hasCurrentActorEvidence,
            PlayerGameplayAdmissionSummary gameplayAdmission,
            bool hasGameplayAdmissionEvidence)
        {
            Slot = slot;
            HostEvidence = hostEvidence;
            HasHostEvidence = hasHostEvidence;
            Preparation = preparation;
            HasPreparationEvidence = hasPreparationEvidence;
            CurrentActor = currentActor;
            HasCurrentActorEvidence = hasCurrentActorEvidence;
            GameplayAdmission = gameplayAdmission;
            HasGameplayAdmissionEvidence = hasGameplayAdmissionEvidence;
        }

        public PlayerSlotRuntimeSnapshot Slot { get; }
        public PlayerHostEvidenceSummary HostEvidence { get; }
        public bool HasHostEvidence { get; }
        public PlayerActorPreparationSummary Preparation { get; }
        public bool HasPreparationEvidence { get; }
        public CurrentPlayerSlotActorSnapshot CurrentActor { get; }
        public bool HasCurrentActorEvidence { get; }
        public PlayerGameplayAdmissionSummary GameplayAdmission { get; }
        public bool HasGameplayAdmissionEvidence { get; }
        public bool IsJoined => Slot.IsJoined;
        public bool HasSelectedActor => Slot.HasSelectedActor;
        public bool IsLogicalActorPrepared =>
            HasPreparationEvidence && Preparation.IsPrepared;
        public bool IsPhysicallyMaterialized =>
            HasPreparationEvidence && Preparation.HasMaterialization;
        public bool IsGameplayAdmitted =>
            HasGameplayAdmissionEvidence && GameplayAdmission.IsAdmitted;
    }

    /// <summary>
    /// Immutable composition of current Player Session, Actor preparation and
    /// gameplay evidence. It does not require Manager lifecycle evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ACCESS-2 scoped provider-neutral Player Session observation.")]
    public sealed class PlayerSessionScopedObservationSnapshot
    {
        private static readonly IReadOnlyList<PlayerSessionScopedSlotObservation>
            NoSlots = new ReadOnlyCollection<PlayerSessionScopedSlotObservation>(
                Array.Empty<PlayerSessionScopedSlotObservation>());

        private readonly IReadOnlyList<PlayerSessionScopedSlotObservation> _slots;

        internal PlayerSessionScopedObservationSnapshot(
            bool isAvailable,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner scopeOwner,
            PlayerParticipationSnapshot participation,
            EffectivePlayerSessionConfiguration initializationConfiguration,
            RuntimeContentOwner activityOwner,
            int activityOccurrence,
            IReadOnlyList<PlayerSessionScopedSlotObservation> slots,
            string diagnostic)
        {
            IsAvailable = isAvailable;
            Scope = scope;
            ScopeOwner = scopeOwner;
            Participation = participation;
            InitializationConfiguration = initializationConfiguration;
            ActivityOwner = activityOwner;
            ActivityOccurrence = Math.Max(0, activityOccurrence);
            _slots = CopySlots(slots);
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool IsAvailable { get; }
        public LocalPlayerProvisioningConsumerScope Scope { get; }
        public RuntimeContentOwner ScopeOwner { get; }
        public PlayerParticipationSnapshot Participation { get; }
        public EffectivePlayerSessionConfiguration InitializationConfiguration { get; }
        public bool HasInitializationEvidence => InitializationConfiguration != null;
        public RuntimeContentOwner ActivityOwner { get; }
        public int ActivityOccurrence { get; }
        public bool HasCurrentActivityOccurrence =>
            ActivityOwner.IsValid && ActivityOccurrence > 0;
        public IReadOnlyList<PlayerSessionScopedSlotObservation> Slots => _slots;
        public int SessionRevision => Participation?.Revision ?? 0;
        public string Diagnostic { get; }

        internal static PlayerSessionScopedObservationSnapshot Unavailable(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner scopeOwner,
            string diagnostic)
        {
            return new PlayerSessionScopedObservationSnapshot(
                false, scope, scopeOwner, null, null, default, 0,
                NoSlots, diagnostic);
        }

        private static IReadOnlyList<PlayerSessionScopedSlotObservation> CopySlots(
            IReadOnlyList<PlayerSessionScopedSlotObservation> source)
        {
            if (source == null || source.Count == 0)
            {
                return NoSlots;
            }

            var copy = new PlayerSessionScopedSlotObservation[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return new ReadOnlyCollection<PlayerSessionScopedSlotObservation>(copy);
        }
    }
}
