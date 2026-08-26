using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

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
        public PlayerProvisioningStatusAvailability Availability => ResolveAvailability();

        public bool IsAvailable => Availability ==
            PlayerProvisioningStatusAvailability.Available;

        public new string Diagnostic
        {
            get
            {
                return TryGetObservation(
                    out LocalPlayerProvisioningConsumerObservationSnapshot observation)
                    ? observation.Diagnostic
                    : ScopedAccessDiagnostic;
            }
        }

        /// <summary>
        /// Current P2 observation when this observer has live P1 scoped access.
        /// No unavailable snapshot is fabricated by this presentation component.
        /// </summary>
        public LocalPlayerProvisioningConsumerObservationSnapshot CurrentObservation =>
            TryGetObservation(
                out LocalPlayerProvisioningConsumerObservationSnapshot observation)
                ? observation
                : null;

        public string InitializationSummary => DescribeInitialization(CurrentObservation);

        public string ActivitySummary => DescribeActivity(CurrentObservation);

        /// <summary>
        /// Reads the current public P2 observation through P1. It does not
        /// cache, subscribe to, mutate or reconcile Player state.
        /// </summary>
        public bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            observation = null;
            if (!TryGetAccess(
                    out ILocalPlayerProvisioningConsumerAccess access,
                    out _))
            {
                return false;
            }

            return access.TryGetObservation(out observation) &&
                observation != null && observation.IsAvailable;
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
            LocalPlayerProvisioningConsumerSlotObservation slot)
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
                return "Preparing Logical Actor";
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
            LocalPlayerProvisioningConsumerSlotObservation slot)
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
            LocalPlayerProvisioningConsumerSlotObservation slot)
        {
            if (!slot.HasGameplayAdmissionEvidence)
            {
                return "Not admitted";
            }

            return slot.GameplayAdmission.GameplayReady
                ? "Gameplay Ready"
                : slot.GameplayAdmission.State.ToString();
        }

        private PlayerProvisioningStatusAvailability ResolveAvailability()
        {
            if (ScopedAccessState == PlayerSessionScopedAccessState.Released ||
                ScopedAccessSnapshot.IsDisposed)
            {
                return PlayerProvisioningStatusAvailability.Stale;
            }

            return TryGetObservation(
                out LocalPlayerProvisioningConsumerObservationSnapshot _)
                ? PlayerProvisioningStatusAvailability.Available
                : PlayerProvisioningStatusAvailability.Unavailable;
        }

        private static string DescribeInitialization(
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
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
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
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
