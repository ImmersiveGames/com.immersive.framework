using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Read-only availability classification for a Player Provisioning status
    /// binding. It describes P1/P2 transport lifetime, never Player state.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-06 public status binding availability.")]
    public enum PlayerProvisioningStatusAvailability
    {
        MissingBinding = 0,
        Available = 10,
        Unavailable = 20,
        Stale = 30
    }

    /// <summary>
    /// Dependency-neutral scene/prefab binding over the P1 scoped access and
    /// P2 immutable observation. It stores no Player truth and performs no
    /// automatic update; consumers explicitly pull current public evidence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Player Session Status")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-06 read-only designer status and diagnostics binding.")]
    public sealed class PlayerSessionStatus : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit Route or Activity scoped P1 binding used to read the public P2 observation.")]
        private LocalPlayerProvisioningConsumerAccessBinding consumerAccessBinding;

        [SerializeField]
        [Tooltip("Optional explicit P3 command trigger in the same scoped consumer path. It is the only Last Operation source used by this binding.")]
        private PlayerSessionCommandTrigger commandTrigger;

        public LocalPlayerProvisioningConsumerAccessBinding ConsumerAccessBinding =>
            consumerAccessBinding;

        public PlayerSessionCommandTrigger CommandTrigger => commandTrigger;

        public PlayerProvisioningStatusAvailability Availability =>
            ResolveAvailability();

        public bool IsAvailable => Availability ==
            PlayerProvisioningStatusAvailability.Available;

        public string Diagnostic
        {
            get
            {
                if (TryGetObservation(
                        out LocalPlayerProvisioningConsumerObservationSnapshot
                            observation))
                {
                    return observation.Diagnostic;
                }

                return consumerAccessBinding != null
                    ? consumerAccessBinding.Diagnostic
                    : "Player Session Status requires an explicit Local Player Provisioning Consumer Access binding.";
            }
        }

        /// <summary>
        /// Current P2 observation when the explicitly configured P1 binding is
        /// live. No unavailable snapshot is fabricated by this presentation
        /// component.
        /// </summary>
        public LocalPlayerProvisioningConsumerObservationSnapshot
            CurrentObservation
        {
            get
            {
                return TryGetObservation(
                    out LocalPlayerProvisioningConsumerObservationSnapshot
                        observation)
                    ? observation
                    : null;
            }
        }

        public bool HasLastOperation => commandTrigger != null &&
            commandTrigger.HasLastTypedResult;

        public PlayerProvisioningCommandResultKind LastOperationResultKind =>
            commandTrigger != null
                ? commandTrigger.LastResultKind
                : PlayerProvisioningCommandResultKind.None;

        public string LastOperationSummary => commandTrigger != null
            ? commandTrigger.LastResultSummary
            : "No Player Session Command Trigger is explicitly linked, so this binding has no Last Operation source.";

        public PlayerParticipationOperationResult LastParticipationOperation =>
            commandTrigger != null
                ? commandTrigger.LastParticipationResult
                : null;

        public LocalPlayerJoinResult LastJoinOperation => commandTrigger != null
            ? commandTrigger.LastJoinResult
            : null;

        public PlayerActorSelectionResult LastActorSelectionOperation =>
            commandTrigger != null
                ? commandTrigger.LastActorSelectionResult
                : null;

        public SessionPlayerLeaveResult LastLeaveOperation =>
            commandTrigger != null
                ? commandTrigger.LastLeaveResult
                : null;

        public string InitializationSummary =>
            DescribeInitialization(CurrentObservation);

        public string ActivitySummary => DescribeActivity(CurrentObservation);

        /// <summary>
        /// Reads the current public P2 observation through P1. It does not
        /// cache, subscribe to, mutate or reconcile Player state.
        /// </summary>
        public bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            observation = null;
            if (consumerAccessBinding == null ||
                !consumerAccessBinding.TryGetAccess(
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
        /// authority, executes a command or changes the current observation.
        /// </summary>
        public bool TryValidateConfiguration(out string issue)
        {
            if (consumerAccessBinding == null)
            {
                issue =
                    "Player Session Status requires an explicit Local Player Provisioning Consumer Access binding.";
                return false;
            }

            LocalPlayerProvisioningConsumerScope scope =
                consumerAccessBinding.Scope;
            if (scope != LocalPlayerProvisioningConsumerScope.Route &&
                scope != LocalPlayerProvisioningConsumerScope.Activity)
            {
                issue =
                    "Player Session Status requires a binding with an explicit Route or Activity scope.";
                return false;
            }

            if (commandTrigger != null && !ReferenceEquals(
                    commandTrigger.ConsumerAccessBinding,
                    consumerAccessBinding))
            {
                issue =
                    "The optional Player Session Command Trigger must use this same Consumer Access Binding; otherwise Last Operation could describe a different scope.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

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
            if (consumerAccessBinding == null)
            {
                return PlayerProvisioningStatusAvailability.MissingBinding;
            }

            if (consumerAccessBinding.BindingState ==
                LocalPlayerProvisioningConsumerBindingState.Released ||
                consumerAccessBinding.Snapshot.IsDisposed)
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
