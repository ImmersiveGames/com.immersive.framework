using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Unity-facing adapter for <see cref="PlayerControl"/>.
    /// This component exposes passive control evidence on authored GameObjects without owning PlayerInputManager,
    /// InputAction routing, action map switching, movement, gameplay control or ControlBinding lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Controls/Player Control")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49I Unity adapter for passive PlayerControl evidence.")]
    public sealed class PlayerControlBehaviour : MonoBehaviour, IPlayerControl
    {
        [Header("Identity Evidence")]
        [Tooltip("Optional PlayerSlot declaration evidence. If present, it defines the PlayerSlot identity.")]
        [SerializeField] private PlayerSlotDeclaration playerSlotDeclaration;
        [Tooltip("Fallback stable PlayerSlot id used only when no PlayerSlot declaration is assigned.")]
        [SerializeField] private string playerSlotId = "player.1";

        [Header("PlayerEntry Evidence")]
        [Tooltip("Optional PlayerEntry evidence. It is sampled when RebuildControl or RefreshPlayerEntryFromBehaviour is called.")]
        [SerializeField] private PlayerEntryBehaviour playerEntryBehaviour;

        [Header("Control Evidence")]
        [Tooltip("Optional authored control target evidence. This is not moved or controlled by this component.")]
        [SerializeField] private UnityEngine.Transform controlTarget;
        [Tooltip("Optional input source descriptor. This is diagnostic metadata only and does not bind InputActions.")]
        [SerializeField] private string inputSourceId = "input.local.intent";

        [Header("Initial PlayerControl")]
        [Tooltip("Initial passive PlayerControl state for RebuildControl.")]
        [SerializeField] private PlayerControlState initialState = PlayerControlState.Declared;
        [Tooltip("Initial suspension reason. Required only when the initial state is Suspended.")]
        [SerializeField] private string initialSuspensionReason = string.Empty;
        [Tooltip("Initial diagnostic reason/source for passive PlayerControl evidence.")]
        [SerializeField] private string initialReason = "player-control.behaviour.initial";
        [Tooltip("If true, RebuildControl is called during Awake. Invalid configuration fails explicitly.")]
        [SerializeField] private bool rebuildOnAwake = true;

        private PlayerControl _control;
        private bool _hasControl;

        public bool HasControl => _hasControl;

        public PlayerSlotId PlayerSlotId => EnsureControl().PlayerSlotId;

        public PlayerControlState State => EnsureControl().State;

        public bool HasPlayerEntryEvidence => EnsureControl().HasPlayerEntryEvidence;

        public PlayerEntryState PlayerEntryState => EnsureControl().PlayerEntryState;

        public bool IsPlayerEntryReadyForControl => EnsureControl().IsPlayerEntryReadyForControl;

        public bool HasControlTarget => EnsureControl().HasControlTarget;

        public string ControlTargetName => EnsureControl().ControlTargetName;

        public string InputSourceId => EnsureControl().InputSourceId;

        public bool IsEligibleForBoundControl => EnsureControl().IsEligibleForBoundControl;

        public bool IsEligibleForActiveControl => EnsureControl().IsEligibleForActiveControl;

        public string SuspensionReason => EnsureControl().SuspensionReason;

        public string Reason => EnsureControl().Reason;

        private void Awake()
        {
            if (rebuildOnAwake)
            {
                RebuildControl();
            }
        }

        public PlayerControlSnapshot CreateSnapshot()
        {
            return EnsureControl().CreateSnapshot();
        }

        public PlayerControl RebuildControl(string reasonOverride = null)
        {
            PlayerSlotId resolvedSlotId = ResolvePlayerSlotId();
            string reason = reasonOverride.NormalizeTextOrFallback(initialReason);
            bool hasTarget = controlTarget != null;
            string targetName = hasTarget ? controlTarget.name : string.Empty;
            bool hasPlayerEntryEvidence = false;
            PlayerEntryState resolvedPlayerEntryState = PlayerEntryState.Configured;
            bool resolvedPlayerEntryReadyForControl = false;

            if (playerEntryBehaviour != null)
            {
                PlayerEntrySnapshot playerEntry = playerEntryBehaviour.CreateSnapshot();
                if (playerEntry.PlayerSlotId != resolvedSlotId)
                {
                    throw new InvalidOperationException($"PlayerControlBehaviour PlayerEntry evidence has a different PlayerSlot. Control='{resolvedSlotId.StableText}' PlayerEntry='{playerEntry.PlayerSlotId.StableText}'.");
                }

                hasPlayerEntryEvidence = true;
                resolvedPlayerEntryState = playerEntry.State;
                resolvedPlayerEntryReadyForControl = playerEntry.IsActorReadyForControl;
            }

            _control = new PlayerControl(
                resolvedSlotId,
                initialState,
                hasPlayerEntryEvidence,
                resolvedPlayerEntryState,
                resolvedPlayerEntryReadyForControl,
                hasTarget,
                targetName,
                inputSourceId,
                initialSuspensionReason,
                reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl SetState(PlayerControlState state, string reason = null)
        {
            _control = EnsureControl().WithState(state, reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl SetPlayerEntryEvidence(PlayerEntrySnapshot playerEntry, string reason = null)
        {
            _control = EnsureControl().WithPlayerEntryEvidence(playerEntry, reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl ClearPlayerEntryEvidence(string reason = null)
        {
            _control = EnsureControl().WithoutPlayerEntryEvidence(reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl RefreshPlayerEntryFromBehaviour(string reason = null)
        {
            if (playerEntryBehaviour == null)
            {
                throw new InvalidOperationException("PlayerControlBehaviour cannot refresh PlayerEntry evidence because PlayerEntryBehaviour is not assigned.");
            }

            return SetPlayerEntryEvidence(playerEntryBehaviour.CreateSnapshot(), reason);
        }

        public PlayerControl SetControlTarget(UnityEngine.Transform target, string reason = null)
        {
            controlTarget = target;
            string targetName = target != null ? target.name : string.Empty;
            _control = EnsureControl().WithControlTarget(target != null, targetName, reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl SetInputSource(string sourceId, string reason = null)
        {
            inputSourceId = sourceId.NormalizeText();
            _control = EnsureControl().WithInputSource(inputSourceId, reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl SetSuspension(string suspensionReason, string reason = null)
        {
            _control = EnsureControl().WithSuspension(suspensionReason, reason);
            _hasControl = true;
            return _control;
        }

        public PlayerControl Release(string reason = null)
        {
            _control = EnsureControl().Released(reason);
            _hasControl = true;
            return _control;
        }

        [ContextMenu("Player Control/Rebuild Control")]
        private void ContextRebuildControl()
        {
            RebuildControl("player-control.behaviour.context.rebuild");
        }

        [ContextMenu("Player Control/Refresh PlayerEntry Evidence")]
        private void ContextRefreshPlayerEntry()
        {
            RefreshPlayerEntryFromBehaviour("player-control.behaviour.context.refresh-player-entry");
        }

        [ContextMenu("Player Control/Set Bound")]
        private void ContextSetBound()
        {
            SetState(PlayerControlState.Bound, "player-control.behaviour.context.bound");
        }

        [ContextMenu("Player Control/Set Active")]
        private void ContextSetActive()
        {
            SetState(PlayerControlState.Active, "player-control.behaviour.context.active");
        }

        [ContextMenu("Player Control/Release")]
        private void ContextRelease()
        {
            Release("player-control.behaviour.context.release");
        }

        private PlayerControl EnsureControl()
        {
            return _hasControl && _control != null
                ? _control
                : RebuildControl("player-control.behaviour.ensure-control");
        }

        private PlayerSlotId ResolvePlayerSlotId()
        {
            if (playerSlotDeclaration != null)
            {
                return playerSlotDeclaration.PlayerSlotId;
            }

            string normalizedSlotId = playerSlotId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedSlotId))
            {
                throw new InvalidOperationException("PlayerControlBehaviour requires a PlayerSlotDeclaration or a non-empty PlayerSlotId.");
            }

            return new PlayerSlotId(normalizedSlotId);
        }

        private void Reset()
        {
            if (playerSlotDeclaration == null)
            {
                playerSlotDeclaration = GetComponent<PlayerSlotDeclaration>();
            }

            if (playerEntryBehaviour == null)
            {
                playerEntryBehaviour = GetComponent<PlayerEntryBehaviour>();
            }

            if (controlTarget == null)
            {
                controlTarget = transform;
            }
        }
    }
}
