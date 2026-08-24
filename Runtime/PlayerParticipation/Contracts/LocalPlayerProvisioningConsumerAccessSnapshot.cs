using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-03 immutable scoped consumer access diagnostic.")]
    public sealed class LocalPlayerProvisioningConsumerAccessSnapshot
    {
        internal LocalPlayerProvisioningConsumerAccessSnapshot(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            bool isAvailable,
            bool isDisposed,
            string diagnostic)
        {
            Scope = scope;
            Owner = owner;
            IsAvailable = isAvailable;
            IsDisposed = isDisposed;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public LocalPlayerProvisioningConsumerScope Scope { get; }

        public RuntimeContentOwner Owner { get; }

        public bool IsAvailable { get; }

        public bool IsDisposed { get; }

        public string Diagnostic { get; }

        internal static LocalPlayerProvisioningConsumerAccessSnapshot Unavailable(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            string diagnostic)
        {
            return new LocalPlayerProvisioningConsumerAccessSnapshot(
                scope,
                owner,
                false,
                false,
                diagnostic);
        }
    }

    /// <summary>
    /// Immutable current evidence for one configured Session Slot as observed
    /// through a live scoped Local Player provisioning consumer endpoint.
    /// It composes existing authority snapshots and owns no Player state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-04 typed per-Slot consumer observation.")]
    public readonly struct LocalPlayerProvisioningConsumerSlotObservation
    {
        internal LocalPlayerProvisioningConsumerSlotObservation(
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

        /// <summary>
        /// Advanced correlation of Slot, retained Host and current Actor.
        /// It is present only when the existing preparation authority can
        /// confirm it as current.
        /// </summary>
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
    /// Immutable read-only composition of the existing Manager-Provisioned
    /// Player authorities for one live Route or Activity consumer scope.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-04 scoped Manager-Provisioned Player observation projection.")]
    public sealed class LocalPlayerProvisioningConsumerObservationSnapshot
    {
        private static readonly IReadOnlyList<
            LocalPlayerProvisioningConsumerSlotObservation> NoSlots =
                new ReadOnlyCollection<
                    LocalPlayerProvisioningConsumerSlotObservation>(
                        Array.Empty<
                            LocalPlayerProvisioningConsumerSlotObservation>());

        private readonly IReadOnlyList<
            LocalPlayerProvisioningConsumerSlotObservation> _slots;

        internal LocalPlayerProvisioningConsumerObservationSnapshot(
            bool isAvailable,
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner scopeOwner,
            PlayerParticipationSnapshot participation,
            EffectivePlayerSessionConfiguration initializationConfiguration,
            ManagerProvisionedPlayerLifecycleSnapshot lifecycle,
            RuntimeContentOwner activityOwner,
            int activityOccurrence,
            IReadOnlyList<LocalPlayerProvisioningConsumerSlotObservation> slots,
            string diagnostic)
        {
            IsAvailable = isAvailable;
            Scope = scope;
            ScopeOwner = scopeOwner;
            Participation = participation;
            InitializationConfiguration = initializationConfiguration;
            Lifecycle = lifecycle;
            ActivityOwner = activityOwner;
            ActivityOccurrence = Math.Max(0, activityOccurrence);
            this._slots = CopySlots(slots);
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool IsAvailable { get; }
        public LocalPlayerProvisioningConsumerScope Scope { get; }
        public RuntimeContentOwner ScopeOwner { get; }

        /// <summary>
        /// Current Session participation truth, including joining,
        /// Slot allocation and Session revision.
        /// </summary>
        public PlayerParticipationSnapshot Participation { get; }

        /// <summary>
        /// Immutable creation-time Session evidence. It is never interpreted as
        /// current Joining state or Slot occupancy.
        /// </summary>
        public EffectivePlayerSessionConfiguration InitializationConfiguration { get; }

        public bool HasInitializationEvidence =>
            InitializationConfiguration != null;

        /// <summary>
        /// Existing Manager-Provisioned lifecycle/readiness projection. Its
        /// Activity correlation remains diagnostic evidence, not authority.
        /// </summary>
        public ManagerProvisionedPlayerLifecycleSnapshot Lifecycle { get; }

        public RuntimeContentOwner ActivityOwner { get; }
        public int ActivityOccurrence { get; }
        public bool HasCurrentActivityOccurrence =>
            ActivityOwner.IsValid && ActivityOccurrence > 0;
        public IReadOnlyList<LocalPlayerProvisioningConsumerSlotObservation>
            Slots => _slots;
        public int SessionRevision => Participation?.Revision ?? 0;
        public int AppliedSessionRevision =>
            Lifecycle?.AppliedSessionRevision ?? 0;
        public string Diagnostic { get; }

        internal static LocalPlayerProvisioningConsumerObservationSnapshot
            Unavailable(
                LocalPlayerProvisioningConsumerScope scope,
                RuntimeContentOwner scopeOwner,
                string diagnostic)
        {
            return new LocalPlayerProvisioningConsumerObservationSnapshot(
                false,
                scope,
                scopeOwner,
                null,
                null,
                ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                    diagnostic),
                default,
                0,
                NoSlots,
                diagnostic);
        }

        private static IReadOnlyList<
            LocalPlayerProvisioningConsumerSlotObservation> CopySlots(
                IReadOnlyList<LocalPlayerProvisioningConsumerSlotObservation>
                    source)
        {
            if (source == null || source.Count == 0)
            {
                return NoSlots;
            }

            var copy = new LocalPlayerProvisioningConsumerSlotObservation[
                source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return new ReadOnlyCollection<
                LocalPlayerProvisioningConsumerSlotObservation>(copy);
        }
    }
}
