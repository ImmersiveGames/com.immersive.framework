using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.Events;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Read-only availability classification for a scoped Player Session observation.
    /// It describes P1/P2 transport lifetime, never Player state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-06 public Player Session observation availability.")]
    public enum PlayerProvisioningStatusAvailability
    {
        Available = 10,
        Unavailable = 20,
        Stale = 30
    }

    /// <summary>
    /// Read-only scoped observer of the current Player Session. It may be used
    /// by Hub, UI, presentation or other scenes without requiring a reference
    /// to the physically materialized Player.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Session Observer")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-06 read-only scoped Player Session observer.")]
    public sealed class PlayerSessionObserver : PlayerSessionScopedAccessConsumer
    {
        private IPlayerSessionScopedAccess _observedAccess;

        [SerializeField] private UnityEvent onJoiningOpened = new UnityEvent();
        [SerializeField] private UnityEvent onJoiningClosed = new UnityEvent();
        [SerializeField] private UnityEvent onPlayerJoined = new UnityEvent();
        [SerializeField] private UnityEvent onPlayerLeft = new UnityEvent();
        [SerializeField] private UnityEvent onActorSelected = new UnityEvent();
        [SerializeField] private UnityEvent onActorChanged = new UnityEvent();
        [SerializeField] private UnityEvent onActorCleared = new UnityEvent();

        /// <summary>
        /// Raised after an authoritative Player Session mutation has committed.
        /// Subscription remains valid only while this component has live scoped
        /// access; it is released with that access.
        /// </summary>
        public event Action<PlayerSessionChange> Changed;

        public UnityEvent OnJoiningOpened => onJoiningOpened;
        public UnityEvent OnJoiningClosed => onJoiningClosed;
        public UnityEvent OnPlayerJoined => onPlayerJoined;
        public UnityEvent OnPlayerLeft => onPlayerLeft;
        public UnityEvent OnActorSelected => onActorSelected;
        public UnityEvent OnActorChanged => onActorChanged;
        public UnityEvent OnActorCleared => onActorCleared;

        public PlayerProvisioningStatusAvailability Availability => ResolveAvailability();

        public bool IsAvailable => Availability ==
            PlayerProvisioningStatusAvailability.Available;

        public new string Diagnostic
        {
            get
            {
                return TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot observation)
                    ? observation.Diagnostic
                    : ScopedAccessDiagnostic;
            }
        }

        /// <summary>
        /// Current P2 observation when this observer has live P1 scoped access.
        /// No unavailable snapshot is fabricated by this presentation component.
        /// </summary>
        public PlayerSessionScopedObservationSnapshot CurrentObservation =>
            TryGetObservation(
                out PlayerSessionScopedObservationSnapshot observation)
                ? observation
                : null;

        public string InitializationSummary => DescribeInitialization(CurrentObservation);

        public string ActivitySummary => DescribeActivity(CurrentObservation);

        /// <summary>
        /// Reads the current public P2 observation through P1. It does not
        /// cache, subscribe to, mutate or reconcile Player state.
        /// </summary>
        public bool TryGetObservation(
            out PlayerSessionScopedObservationSnapshot observation)
        {
            observation = null;
            if (!TryGetAccess(
                    out IPlayerSessionScopedAccess access,
                    out _))
            {
                return false;
            }

            return access.TryGetObservation(out observation) &&
                observation != null && observation.IsAvailable;
        }

        /// <summary>
        /// Obsolete Manager-Provisioned observation overload retained for
        /// existing presentation consumers during ACCESS-2 migration.
        /// </summary>
        [Obsolete(
            "Use TryGetObservation(out PlayerSessionScopedObservationSnapshot).")]
        public bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            observation = null;
            return TryGetAccess(
                    out ILocalPlayerProvisioningConsumerAccess access,
                    out _) &&
                access.TryGetObservation(out observation) &&
                observation != null && observation.IsAvailable;
        }

        /// <summary>
        /// Reads the current authoritative Player Session snapshot through the
        /// live scoped observation. No unavailable snapshot is fabricated.
        /// </summary>
        public bool TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
        {
            snapshot = null;
            if (!TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot
                        observation) ||
                observation.Participation == null)
            {
                return false;
            }

            snapshot = observation.Participation;
            return true;
        }

        /// <summary>
        /// Validates authoring relationships only. It never resolves a runtime
        /// authority or changes the current observation.
        /// </summary>
        public bool TryValidateConfiguration(out string issue) =>
            TryValidateScope(out issue);

        /// <summary>
        /// Designer-facing lifecycle label derived only from the supplied P2
        /// Slot observation. It is presentation text, not a lifecycle state.
        /// </summary>
        public string DescribeSlotLifecycle(
            PlayerSessionScopedSlotObservation slot)
        {
            if (!slot.Slot.PlayerSlotId.IsValid)
            {
                return "Unavailable";
            }

            if (slot.Slot.AllocationState == PlayerSlotAllocationState.Leaving)
            {
                return "Leaving Session";
            }

            if (!slot.IsJoined)
            {
                return "Waiting for Join";
            }

            if (!slot.HasSelectedActor)
            {
                return "Waiting for Actor Selection";
            }

            if (!slot.IsLogicalActorPrepared)
            {
                return "Preparing Actor Runtime";
            }

            if (!slot.IsPhysicallyMaterialized)
            {
                return "Materializing";
            }

            if (!slot.IsGameplayAdmitted)
            {
                return "Gameplay Admission";
            }

            return "Ready";
        }

        public string DescribeSelectedActor(
            PlayerSessionScopedSlotObservation slot)
        {
            if (!slot.HasSelectedActor)
            {
                return "None";
            }

            return slot.Slot.SelectedActorProfile != null
                ? slot.Slot.SelectedActorProfile.name
                : slot.Slot.SelectedActorProfileId.StableText;
        }

        public string DescribeGameplay(
            PlayerSessionScopedSlotObservation slot)
        {
            if (!slot.HasGameplayAdmissionEvidence)
            {
                return "Not admitted";
            }

            return slot.GameplayAdmission.GameplayReady
                ? "Gameplay Ready"
                : slot.GameplayAdmission.State.ToString();
        }

        protected override void OnScopedAccessBound(
            IPlayerSessionScopedAccess scopedAccess)
        {
            if (_observedAccess != null)
            {
                _observedAccess.Changed -= ForwardChange;
            }

            _observedAccess = scopedAccess;
            _observedAccess.Changed += ForwardChange;
        }

        protected override void OnScopedAccessReleasing(
            IPlayerSessionScopedAccess scopedAccess)
        {
            if (_observedAccess != null)
            {
                _observedAccess.Changed -= ForwardChange;
                _observedAccess = null;
            }
        }

        private void ForwardChange(PlayerSessionChange change)
        {
            Changed?.Invoke(change);
            ProjectDesignerFacingEvent(change);
        }

        private void ProjectDesignerFacingEvent(PlayerSessionChange change)
        {
            if (change == null)
            {
                return;
            }

            switch (change.Kind)
            {
                case PlayerSessionChangeKind.JoiningChanged:
                    if (!change.PreviousJoiningOpen && change.CurrentJoiningOpen)
                    {
                        onJoiningOpened?.Invoke();
                    }
                    else if (change.PreviousJoiningOpen && !change.CurrentJoiningOpen)
                    {
                        onJoiningClosed?.Invoke();
                    }

                    break;

                case PlayerSessionChangeKind.SlotAllocationChanged:
                    if (change.CurrentSlot.AllocationState ==
                            PlayerSlotAllocationState.Joined &&
                        change.PreviousSlot.AllocationState !=
                            PlayerSlotAllocationState.Joined)
                    {
                        onPlayerJoined?.Invoke();
                    }
                    else if (change.PreviousSlot.AllocationState ==
                                 PlayerSlotAllocationState.Leaving &&
                             change.CurrentSlot.AllocationState ==
                                 PlayerSlotAllocationState.Available)
                    {
                        onPlayerLeft?.Invoke();
                    }

                    break;

                case PlayerSessionChangeKind.ActorSelectionChanged:
                    bool hadPreviousActor = change.PreviousSlot.SelectedActorProfile != null;
                    bool hasCurrentActor = change.CurrentSlot.SelectedActorProfile != null;
                    if (!hadPreviousActor && hasCurrentActor)
                    {
                        onActorSelected?.Invoke();
                    }
                    else if (hadPreviousActor && hasCurrentActor &&
                             !ReferenceEquals(
                                 change.PreviousSlot.SelectedActorProfile,
                                 change.CurrentSlot.SelectedActorProfile))
                    {
                        onActorChanged?.Invoke();
                    }
                    else if (hadPreviousActor && !hasCurrentActor)
                    {
                        onActorCleared?.Invoke();
                    }

                    break;
            }
        }

        private PlayerProvisioningStatusAvailability ResolveAvailability()
        {
            if (ScopedAccessState == PlayerSessionScopedAccessState.Released ||
                ScopedAccessSnapshot.IsDisposed)
            {
                return PlayerProvisioningStatusAvailability.Stale;
            }

            return TryGetObservation(
                out PlayerSessionScopedObservationSnapshot _)
                ? PlayerProvisioningStatusAvailability.Available
                : PlayerProvisioningStatusAvailability.Unavailable;
        }

        private static string DescribeInitialization(
            PlayerSessionScopedObservationSnapshot observation)
        {
            if (observation == null || !observation.IsAvailable)
            {
                return "Unavailable";
            }

            if (!observation.HasInitializationEvidence)
            {
                return "No creation-time Session evidence is published.";
            }

            EffectivePlayerSessionConfiguration configuration =
                observation.InitializationConfiguration;
            return $"Resolved at Session creation: supportedSlots='{configuration.SupportedSlotCount}' initialJoiningOpen='{configuration.InitialJoiningOpen}' hostProvisioning='{configuration.HostProvisioning}' actorResolution='{configuration.ActorResolutionPolicy}'.";
        }

        private static string DescribeActivity(
            PlayerSessionScopedObservationSnapshot observation)
        {
            if (observation == null || !observation.IsAvailable)
            {
                return "Unavailable";
            }

            return observation.HasCurrentActivityOccurrence
                ? $"{observation.ActivityOwner.OwnerName} (occurrence {observation.ActivityOccurrence})"
                : "No current Activity occurrence is published.";
        }
    }
}
