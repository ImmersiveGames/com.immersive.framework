using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Stable technical host for one local Player. PlayerInputManager may provision it, or a
    /// Scene-Provided Local Player authoring may reference an externally owned scene instance.
    /// It owns PlayerInput evidence, an explicit Actor Mount and typed Slot admission evidence.
    /// It is not an Actor, does not select an ActorProfile and does not execute gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [AddComponentMenu("Immersive Framework/Player/Local Player Host")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable Local Player Host surface. Scene-authored admission is Stable; manager provisioning and Session-Persistent remain Experimental.")]
    public sealed class LocalPlayerHostAuthoring : MonoBehaviour
    {
        private enum AdmissionState
        {
            None = 0,
            Staged = 10,
            Joined = 20,
            ReleaseStaged = 30,
            ReleaseFailed = 40
        }

        [Header("Technical Host")]
        [SerializeField]
        [Tooltip("Explicit PlayerInput owned by this technical host.")]
        private PlayerInput playerInput;

        [SerializeField]
        [Tooltip("Explicit child transform where a contextual Player Actor Runtime Host is materialized or scene-authored.")]
        private Transform actorMount;

        [Header("Actor Runtime Foundation")]
        [SerializeField]
        [Tooltip("Generic Player Actor Runtime Host prefab provided by this Local Player Host composition. Manager-Provisioned Local Players materialize it after Actor selection; Scene-Provided Local Players use the same shape as composition evidence.")]
        private PlayerActorRuntimeHost playerActorRuntimeHostPrefab;

        [NonSerialized] private AdmissionState _admissionState;
        [NonSerialized] private PlayerSlotId _joinedPlayerSlotId;
        [NonSerialized] private int _joinedConfiguredIndex = -1;
        [NonSerialized] private string _admissionSource = string.Empty;
        [NonSerialized] private string _admissionReason = string.Empty;

        public PlayerInput PlayerInput => playerInput;
        public Transform ActorMount => actorMount;
        public PlayerActorRuntimeHost PlayerActorRuntimeHostPrefab => playerActorRuntimeHostPrefab;
        public bool HasPlayerInputEvidence => playerInput != null;
        public bool HasActorMount => actorMount != null;
        public bool HasPlayerActorRuntimeHostPrefab => playerActorRuntimeHostPrefab != null;
        public bool IsAdmissionStaged => _admissionState == AdmissionState.Staged;
        public bool IsJoined => _admissionState == AdmissionState.Joined;
        public bool IsReleaseStaged => _admissionState == AdmissionState.ReleaseStaged;
        public bool IsReleaseFailed => _admissionState == AdmissionState.ReleaseFailed;
        internal bool IsAdmissionReleased => _admissionState == AdmissionState.None;
        public bool HasJoinedSlot => IsJoined && _joinedPlayerSlotId.IsValid;
        public PlayerSlotId JoinedPlayerSlotId => HasJoinedSlot ? _joinedPlayerSlotId : default;
        public int JoinedConfiguredIndex => IsJoined ? _joinedConfiguredIndex : -1;
        public string AdmissionSource => _admissionSource.NormalizeText();
        public string AdmissionReason => _admissionReason.NormalizeText();
        public bool HasPlayerActorRuntime =>
            actorMount != null &&
            actorMount.GetComponentInChildren<ActorDeclaration>(true) != null;

        /// <summary>
        /// Validates the reusable PlayerInputManager provisioning shape. The Actor Mount must be
        /// empty because the contextual Player Actor Runtime is materialized after join.
        /// </summary>
        public bool TryValidateConfiguration(out string issue)
        {
            return TryValidateConfiguration(
                requireEmptyActorMount: true,
                expectedSceneRuntimeHost: null,
                out issue);
        }

        /// <summary>
        /// Validates the Scene-Provided Local Player shape without changing runtime state.
        /// </summary>
        public bool TryValidateAdmissionConfiguration(
            PlayerActorRuntimeHost scenePlayerActorRuntimeHost,
            bool allowExistingActorRuntime,
            out string issue)
        {
            return TryValidateConfiguration(
                requireEmptyActorMount: !allowExistingActorRuntime,
                expectedSceneRuntimeHost: allowExistingActorRuntime
                    ? scenePlayerActorRuntimeHost
                    : null,
                out issue);
        }

        internal bool TryStageAdmission(
            PlayerSlotRuntimeSnapshot reservedSlot,
            string source,
            string reason,
            out string issue)
        {
            return TryStageAdmission(
                reservedSlot,
                source,
                reason,
                allowExistingActorRuntime: false,
                expectedSceneRuntimeHost: null,
                out issue);
        }

        internal bool TryStageAdmission(
            PlayerSlotRuntimeSnapshot reservedSlot,
            string source,
            string reason,
            bool allowExistingActorRuntime,
            PlayerActorRuntimeHost expectedSceneRuntimeHost,
            out string issue)
        {
            issue = string.Empty;

            if (!reservedSlot.IsValid || !reservedSlot.IsReserved)
            {
                issue = "Local Player Host admission staging requires a valid Reserved Player Slot snapshot.";
                return false;
            }

            if (!TryValidateConfiguration(
                    requireEmptyActorMount: !allowExistingActorRuntime,
                    expectedSceneRuntimeHost: expectedSceneRuntimeHost,
                    out issue))
            {
                return false;
            }

            if (_admissionState != AdmissionState.None)
            {
                issue = _admissionState == AdmissionState.Joined
                    ? $"Local Player Host is already joined to Slot '{_joinedPlayerSlotId.StableText}'."
                    : $"Local Player Host cannot stage admission from state '{_admissionState}'.";
                return false;
            }

            _joinedPlayerSlotId = reservedSlot.PlayerSlotId;
            _joinedConfiguredIndex = reservedSlot.ConfiguredIndex;
            _admissionSource = source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring));
            _admissionReason = reason.NormalizeTextOrFallback("local-player-host-admission");
            _admissionState = AdmissionState.Staged;
            return true;
        }

        internal void CommitStagedAdmission(
            PlayerSlotRuntimeSnapshot joinedSlot,
            string source,
            string reason)
        {
            if (_admissionState != AdmissionState.Staged || !_joinedPlayerSlotId.IsValid)
            {
                throw new InvalidOperationException(
                    "Local Player Host has no staged admission to commit.");
            }

            if (!joinedSlot.IsValid || !joinedSlot.IsJoined)
            {
                throw new InvalidOperationException(
                    "Local Player Host admission commit requires a valid Joined Player Slot snapshot.");
            }

            if (joinedSlot.PlayerSlotId != _joinedPlayerSlotId ||
                joinedSlot.ConfiguredIndex != _joinedConfiguredIndex)
            {
                throw new InvalidOperationException(
                    "Local Player Host admission commit does not match the staged Player Slot identity.");
            }

            _admissionSource = source.NormalizeTextOrFallback(_admissionSource);
            _admissionReason = reason.NormalizeTextOrFallback(_admissionReason);
            _admissionState = AdmissionState.Joined;
        }

        internal void RollbackStagedAdmission(string source, string reason)
        {
            if (_admissionState != AdmissionState.Staged)
            {
                return;
            }

            ClearAdmission(
                source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring)),
                reason.NormalizeTextOrFallback("local-player-host-admission-rollback"));
        }

        internal bool TryValidateCommittedAdmissionRelease(
            PlayerSlotId expectedPlayerSlotId,
            out string issue)
        {
            issue = string.Empty;

            if (_admissionState == AdmissionState.None)
            {
                return true;
            }

            if (_admissionState != AdmissionState.Joined &&
                _admissionState != AdmissionState.ReleaseFailed)
            {
                issue = $"Local Player Host cannot release admission from state '{_admissionState}'.";
                return false;
            }

            if (!expectedPlayerSlotId.IsValid ||
                !_joinedPlayerSlotId.IsValid ||
                expectedPlayerSlotId != _joinedPlayerSlotId)
            {
                issue = "Local Player Host admission release rejected a foreign or stale Player Slot identity.";
                return false;
            }

            return true;
        }

        internal bool TryReleaseCommittedAdmission(
            PlayerSlotId expectedPlayerSlotId,
            string source,
            string reason,
            out string issue)
        {
            if (!TryValidateCommittedAdmissionRelease(expectedPlayerSlotId, out issue))
            {
                return false;
            }

            if (_admissionState == AdmissionState.None)
            {
                return true;
            }

            _admissionState = AdmissionState.ReleaseStaged;
            try
            {
                ClearAdmission(
                    source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring)),
                    reason.NormalizeTextOrFallback("local-player-host-admission-release"));
                return true;
            }
            catch (Exception exception)
            {
                _admissionState = AdmissionState.ReleaseFailed;
                _admissionSource = source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring));
                _admissionReason = reason.NormalizeTextOrFallback("local-player-host-admission-release-failed");
                issue = $"Local Player Host admission release failed. {exception.Message}";
                return false;
            }
        }

        internal bool TryRestoreCommittedAdmission(
            PlayerSlotRuntimeSnapshot joinedSlot,
            string source,
            string reason,
            bool allowExistingActorRuntime,
            PlayerActorRuntimeHost expectedSceneRuntimeHost,
            out string issue)
        {
            issue = string.Empty;

            if (!joinedSlot.IsValid || !joinedSlot.IsJoined)
            {
                issue = "Local Player Host admission restore requires a valid Joined Player Slot snapshot.";
                return false;
            }

            if (!TryValidateConfiguration(
                    requireEmptyActorMount: !allowExistingActorRuntime,
                    expectedSceneRuntimeHost: expectedSceneRuntimeHost,
                    out issue))
            {
                return false;
            }

            if (_admissionState == AdmissionState.Joined)
            {
                if (_joinedPlayerSlotId == joinedSlot.PlayerSlotId &&
                    _joinedConfiguredIndex == joinedSlot.ConfiguredIndex)
                {
                    return true;
                }

                issue = "Local Player Host admission restore conflicts with another Joined Player Slot.";
                return false;
            }

            if (_admissionState != AdmissionState.None &&
                _admissionState != AdmissionState.ReleaseFailed)
            {
                issue = $"Local Player Host cannot restore admission from state '{_admissionState}'.";
                return false;
            }

            _joinedPlayerSlotId = joinedSlot.PlayerSlotId;
            _joinedConfiguredIndex = joinedSlot.ConfiguredIndex;
            _admissionSource = source.NormalizeTextOrFallback(nameof(LocalPlayerHostAuthoring));
            _admissionReason = reason.NormalizeTextOrFallback("local-player-host-admission-restore");
            _admissionState = AdmissionState.Joined;
            return true;
        }

        private bool TryValidateConfiguration(
            bool requireEmptyActorMount,
            PlayerActorRuntimeHost expectedSceneRuntimeHost,
            out string issue)
        {
            issue = string.Empty;

            if (playerInput == null)
            {
                issue = "Local Player Host requires an explicit PlayerInput reference.";
                return false;
            }

            if (playerInput.gameObject != gameObject)
            {
                issue = "Local Player Host PlayerInput must exist on the same GameObject as LocalPlayerHostAuthoring.";
                return false;
            }

            PlayerInput[] playerInputs = GetComponentsInChildren<PlayerInput>(true);
            if (playerInputs.Length != 1 || playerInputs[0] != playerInput)
            {
                issue = $"Local Player Host requires exactly one PlayerInput in its hierarchy. Found '{playerInputs.Length}'.";
                return false;
            }

            if (actorMount == null)
            {
                issue = "Local Player Host requires an explicit Actor Mount child transform.";
                return false;
            }

            if (actorMount == transform || !actorMount.IsChildOf(transform))
            {
                issue = "Local Player Host Actor Mount must be a child of the technical host root.";
                return false;
            }

            if (actorMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                issue = "Local Player Host Actor Mount must not contain a second PlayerInput.";
                return false;
            }

            if (requireEmptyActorMount)
            {
                if (actorMount.GetComponentInChildren<ActorDeclaration>(true) != null ||
                    actorMount.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null)
                {
                    issue = "Local Player provisioning host must not contain Player Actor Runtime infrastructure. Actor runtime is materialized contextually after Actor selection.";
                    return false;
                }

                return true;
            }

            if (expectedSceneRuntimeHost == null)
            {
                issue = "Scene-Provided Local Player admission requires an explicit Player Actor Runtime Host.";
                return false;
            }

            if (expectedSceneRuntimeHost.transform != actorMount &&
                !expectedSceneRuntimeHost.transform.IsChildOf(actorMount))
            {
                issue = "Scene-Provided Local Player Actor Runtime Host must exist under the exact Local Player Host Actor Mount.";
                return false;
            }

            PlayerActorRuntimeHost[] runtimeHosts =
                actorMount.GetComponentsInChildren<PlayerActorRuntimeHost>(true);
            if (runtimeHosts.Length != 1 || runtimeHosts[0] != expectedSceneRuntimeHost)
            {
                issue = $"Scene-Provided Local Player admission requires exactly one PlayerActorRuntimeHost under Actor Mount. Found '{runtimeHosts.Length}'.";
                return false;
            }

            if (!expectedSceneRuntimeHost.TryValidateConfiguration(out issue))
            {
                return false;
            }

            PlayerActorDeclaration expectedSceneActor =
                expectedSceneRuntimeHost.PlayerActorDeclaration;
            ActorDeclaration[] actorDeclarations =
                actorMount.GetComponentsInChildren<ActorDeclaration>(true);
            if (actorDeclarations.Length != 1 ||
                actorDeclarations[0] != expectedSceneActor)
            {
                issue = $"Scene-Provided Local Player admission requires one canonical PlayerActorDeclaration and no additional ActorDeclaration. Found '{actorDeclarations.Length}'.";
                return false;
            }

            return true;
        }

        private void ClearAdmission(string source, string reason)
        {
            _joinedPlayerSlotId = default;
            _joinedConfiguredIndex = -1;
            _admissionSource = source ?? string.Empty;
            _admissionReason = reason ?? string.Empty;
            _admissionState = AdmissionState.None;
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
