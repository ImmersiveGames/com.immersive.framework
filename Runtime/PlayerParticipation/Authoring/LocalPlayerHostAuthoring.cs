using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Stable technical host provisioned by PlayerInputManager for one local Player.
    /// It owns PlayerInput evidence, an explicit Actor mount and the joined Slot binding.
    /// It is not an Actor, does not select an ActorProfile and does not execute gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [AddComponentMenu("Immersive Framework/Player/Local Player Host Authoring")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.2 stable Local Player technical host and staged Slot admission evidence.")]
    public sealed class LocalPlayerHostAuthoring : MonoBehaviour
    {
        private enum AdmissionState
        {
            None = 0,
            Staged = 10,
            Joined = 20
        }

        [Header("Technical Host")]
        [SerializeField]
        [Tooltip("Explicit PlayerInput owned by this technical host.")]
        private PlayerInput playerInput;

        [SerializeField]
        [Tooltip("Explicit child transform where a contextual Logical Actor Host may be materialized later.")]
        private Transform actorMount;

        [NonSerialized] private AdmissionState admissionState;
        [NonSerialized] private PlayerSlotId joinedPlayerSlotId;
        [NonSerialized] private int joinedConfiguredIndex = -1;
        [NonSerialized] private string admissionSource = string.Empty;
        [NonSerialized] private string admissionReason = string.Empty;

        public PlayerInput PlayerInput => playerInput;

        public Transform ActorMount => actorMount;

        public bool HasPlayerInputEvidence => playerInput != null;

        public bool HasActorMount => actorMount != null;

        public bool IsAdmissionStaged => admissionState == AdmissionState.Staged;

        public bool IsJoined => admissionState == AdmissionState.Joined;

        public bool HasJoinedSlot => IsJoined && joinedPlayerSlotId.IsValid;

        public PlayerSlotId JoinedPlayerSlotId => HasJoinedSlot
            ? joinedPlayerSlotId
            : default;

        public int JoinedConfiguredIndex => IsJoined ? joinedConfiguredIndex : -1;

        public string AdmissionSource => admissionSource.NormalizeText();

        public string AdmissionReason => admissionReason.NormalizeText();

        public bool HasLogicalActor =>
            actorMount != null &&
            actorMount.GetComponentInChildren<ActorDeclaration>(true) != null;

        /// <summary>
        /// Validates the reusable provisioning-host shape without mutating it.
        /// A provisioning host must not contain a Logical Actor or a pre-authored Slot identity.
        /// </summary>
        public bool TryValidateConfiguration(out string issue)
        {
            return TryValidateConfiguration(requireEmptyActorMount: true, out issue);
        }

        internal bool TryStageAdmission(
            PlayerSlotRuntimeSnapshot reservedSlot,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;

            if (!reservedSlot.IsValid || !reservedSlot.IsReserved)
            {
                issue = "Local Player Host admission staging requires a valid Reserved Player Slot snapshot.";
                return false;
            }

            if (!TryValidateConfiguration(requireEmptyActorMount: true, out issue))
            {
                return false;
            }

            if (admissionState != AdmissionState.None)
            {
                issue = admissionState == AdmissionState.Joined
                    ? $"Local Player Host is already joined to Slot '{joinedPlayerSlotId.StableText}'."
                    : "Local Player Host already has an admission staging operation.";
                return false;
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring));
            string normalizedReason = reason.NormalizeTextOrFallback("local-player-host-admission");
            joinedPlayerSlotId = reservedSlot.PlayerSlotId;
            joinedConfiguredIndex = reservedSlot.ConfiguredIndex;
            admissionSource = normalizedSource;
            admissionReason = normalizedReason;
            admissionState = AdmissionState.Staged;
            return true;
        }

        internal void CommitStagedAdmission(
            PlayerSlotRuntimeSnapshot joinedSlot,
            string source,
            string reason)
        {
            if (admissionState != AdmissionState.Staged || !joinedPlayerSlotId.IsValid)
            {
                throw new InvalidOperationException(
                    "Local Player Host has no staged admission to commit.");
            }

            if (!joinedSlot.IsValid || !joinedSlot.IsJoined)
            {
                throw new InvalidOperationException(
                    "Local Player Host admission commit requires a valid Joined Player Slot snapshot.");
            }

            if (joinedSlot.PlayerSlotId != joinedPlayerSlotId ||
                joinedSlot.ConfiguredIndex != joinedConfiguredIndex)
            {
                throw new InvalidOperationException(
                    "Local Player Host admission commit does not match the staged Player Slot identity.");
            }

            admissionSource = source.NormalizeTextOrFallback(admissionSource);
            admissionReason = reason.NormalizeTextOrFallback(admissionReason);
            admissionState = AdmissionState.Joined;
        }

        internal void RollbackStagedAdmission(string source, string reason)
        {
            if (admissionState != AdmissionState.Staged)
            {
                return;
            }

            joinedPlayerSlotId = default;
            joinedConfiguredIndex = -1;
            admissionSource = source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring));
            admissionReason = reason.NormalizeTextOrFallback("local-player-host-admission-rollback");
            admissionState = AdmissionState.None;
        }

        private bool TryValidateConfiguration(
            bool requireEmptyActorMount,
            out string issue)
        {
            issue = string.Empty;

            if (playerInput == null)
            {
                issue = "Local Player Host requires an explicit PlayerInput reference.";
                return false;
            }

            if (!ReferenceEquals(playerInput.gameObject, gameObject))
            {
                issue = "Local Player Host PlayerInput must exist on the same GameObject as LocalPlayerHostAuthoring.";
                return false;
            }

            PlayerInput[] playerInputs = GetComponentsInChildren<PlayerInput>(true);
            if (playerInputs.Length != 1 || !ReferenceEquals(playerInputs[0], playerInput))
            {
                issue = $"Local Player Host requires exactly one PlayerInput in its hierarchy. Found '{playerInputs.Length}'.";
                return false;
            }

            if (actorMount == null)
            {
                issue = "Local Player Host requires an explicit Actor Mount child transform.";
                return false;
            }

            if (ReferenceEquals(actorMount, transform) || !actorMount.IsChildOf(transform))
            {
                issue = "Local Player Host Actor Mount must be a child of the technical host root.";
                return false;
            }

            if (actorMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Local Player Host Actor Mount must not contain a second PlayerInput.";
                return false;
            }

            if (requireEmptyActorMount &&
                GetComponentInChildren<ActorDeclaration>(true) != null)
            {
                issue = "Local Player provisioning host must not contain an ActorDeclaration. Logical Actors are materialized contextually after join.";
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            playerInput = GetComponent<PlayerInput>();
        }

        private void OnValidate()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }
        }
#endif
    }
}
