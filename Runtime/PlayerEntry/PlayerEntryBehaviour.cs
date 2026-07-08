using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerEntry
{
    /// <summary>
    /// API status: Experimental. Unity-facing adapter for <see cref="PlayerEntry"/>.
    /// This component exposes passive PlayerEntry evidence on authored GameObjects without owning
    /// join, spawn, view binding, control binding, PlayerInputManager or gameplay movement.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Entry/Player Entry")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49E Unity adapter for passive PlayerEntry evidence.")]
    public sealed class PlayerEntryBehaviour : MonoBehaviour, IPlayerEntry
    {
        [Header("Identity Evidence")]
        [Tooltip("Optional PlayerSlot declaration evidence. If present, it defines the PlayerSlot identity.")]
        [SerializeField] private PlayerSlotDeclaration playerSlotDeclaration;
        [Tooltip("Fallback stable PlayerSlot id used only when no PlayerSlot declaration is assigned.")]
        [SerializeField] private string playerSlotId = "player.1";
        [Tooltip("Optional generic Actor declaration evidence. If present, it defines the Actor identity.")]
        [SerializeField] private ActorDeclaration actorDeclaration;
        [Tooltip("Optional PlayerActor declaration evidence. If present, it defines the Actor identity.")]
        [SerializeField] private PlayerActorDeclaration playerActorDeclaration;
        [Tooltip("Fallback stable Actor id used only when no Actor declaration is assigned.")]
        [SerializeField] private string actorId = "qa.player-entry.actor";

        [Header("Readiness Evidence")]
        [Tooltip("Optional Unity Actor readiness evidence. If present, RebuildEntry uses this component snapshot.")]
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [Tooltip("Fallback Actor readiness state used only when no ActorReadinessBehaviour is assigned.")]
        [SerializeField] private ActorReadinessState initialActorReadinessState = ActorReadinessState.NotReady;
        [Tooltip("Fallback Actor readiness diagnostic reason used only when no ActorReadinessBehaviour is assigned.")]
        [SerializeField] private string initialActorReadinessReason = string.Empty;

        [Header("Initial PlayerEntry")]
        [Tooltip("Initial passive PlayerEntry state for RebuildEntry.")]
        [SerializeField] private PlayerEntryState initialState = PlayerEntryState.Configured;
        [Tooltip("Initial suspension reason. Required only when the initial state is Suspended.")]
        [SerializeField] private string initialSuspensionReason = string.Empty;
        [Tooltip("Initial diagnostic reason/source for the passive PlayerEntry evidence.")]
        [SerializeField] private string initialReason = "player-entry.behaviour.initial";
        [Tooltip("If true, RebuildEntry is called during Awake. Invalid configuration fails explicitly.")]
        [SerializeField] private bool rebuildOnAwake = true;

        private PlayerEntry _entry;
        private bool _hasEntry;

        public bool HasEntry => _hasEntry;

        public PlayerSlotId PlayerSlotId => EnsureEntry().PlayerSlotId;

        public ActorId ActorId => EnsureEntry().ActorId;

        public PlayerEntryState State => EnsureEntry().State;

        public ActorReadinessSnapshot ActorReadiness => EnsureEntry().ActorReadiness;

        public bool IsActorReadyForView => EnsureEntry().IsActorReadyForView;

        public bool IsActorReadyForControl => EnsureEntry().IsActorReadyForControl;

        public string SuspensionReason => EnsureEntry().SuspensionReason;

        public string Reason => EnsureEntry().Reason;

        private void Awake()
        {
            if (rebuildOnAwake)
            {
                RebuildEntry();
            }
        }

        public PlayerEntrySnapshot CreateSnapshot()
        {
            return EnsureEntry().CreateSnapshot();
        }

        public PlayerEntry RebuildEntry(string reasonOverride = null)
        {
            PlayerSlotId resolvedSlotId = ResolvePlayerSlotId();
            ActorId resolvedActorId = ResolveActorId();
            ActorReadinessSnapshot readiness = ResolveActorReadiness();
            string reason = reasonOverride.NormalizeTextOrFallback(initialReason);

            _entry = new PlayerEntry(
                resolvedSlotId,
                resolvedActorId,
                initialState,
                readiness,
                initialSuspensionReason,
                reason);
            _hasEntry = true;
            return _entry;
        }

        public PlayerEntry SetState(PlayerEntryState state, string reason = null)
        {
            _entry = EnsureEntry().WithState(state, reason);
            _hasEntry = true;
            return _entry;
        }

        public PlayerEntry SetActorReadiness(ActorReadinessSnapshot actorReadiness, string reason = null)
        {
            _entry = EnsureEntry().WithActorReadiness(actorReadiness, reason);
            _hasEntry = true;
            return _entry;
        }

        public PlayerEntry SetActorReadiness(ActorReadinessState actorReadinessState, string actorReadinessReason, string reason = null)
        {
            return SetActorReadiness(new ActorReadinessSnapshot(actorReadinessState, actorReadinessReason), reason);
        }

        public PlayerEntry RefreshActorReadinessFromBehaviour(string reason = null)
        {
            if (actorReadinessBehaviour == null)
            {
                throw new InvalidOperationException("PlayerEntryBehaviour cannot refresh Actor readiness because ActorReadinessBehaviour is not assigned.");
            }

            return SetActorReadiness(actorReadinessBehaviour.CreateSnapshot(), reason);
        }

        public PlayerEntry SetSuspension(string suspensionReason, string reason = null)
        {
            _entry = EnsureEntry().WithSuspension(suspensionReason, reason);
            _hasEntry = true;
            return _entry;
        }

        public PlayerEntry Release(string reason = null)
        {
            _entry = EnsureEntry().Released(reason);
            _hasEntry = true;
            return _entry;
        }

        [ContextMenu("Player Entry/Rebuild Entry")]
        private void ContextRebuildEntry()
        {
            RebuildEntry("player-entry.behaviour.context.rebuild");
        }

        [ContextMenu("Player Entry/Refresh Actor Readiness")]
        private void ContextRefreshActorReadiness()
        {
            RefreshActorReadinessFromBehaviour("player-entry.behaviour.context.refresh-actor-readiness");
        }

        [ContextMenu("Player Entry/Set ActorReady")]
        private void ContextSetActorReady()
        {
            SetState(PlayerEntryState.ActorReady, "player-entry.behaviour.context.actor-ready");
        }

        [ContextMenu("Player Entry/Set Active")]
        private void ContextSetActive()
        {
            SetState(PlayerEntryState.Active, "player-entry.behaviour.context.active");
        }

        [ContextMenu("Player Entry/Release")]
        private void ContextRelease()
        {
            Release("player-entry.behaviour.context.release");
        }

        private PlayerEntry EnsureEntry()
        {
            return _hasEntry && _entry != null
                ? _entry
                : RebuildEntry("player-entry.behaviour.ensure-entry");
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
                throw new InvalidOperationException("PlayerEntryBehaviour requires a PlayerSlotDeclaration or a non-empty PlayerSlotId.");
            }

            return new PlayerSlotId(normalizedSlotId);
        }

        private ActorId ResolveActorId()
        {
            bool hasResolvedActorId = false;
            ActorId resolvedActorId = default;

            ResolveActorDeclaration(actorDeclaration, nameof(ActorDeclaration), ref hasResolvedActorId, ref resolvedActorId);
            ResolveActorDeclaration(playerActorDeclaration, nameof(PlayerActorDeclaration), ref hasResolvedActorId, ref resolvedActorId);

            if (hasResolvedActorId)
            {
                return resolvedActorId;
            }

            string normalizedActorId = actorId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                throw new InvalidOperationException("PlayerEntryBehaviour requires an ActorDeclaration, PlayerActorDeclaration or a non-empty ActorId.");
            }

            return new ActorId(normalizedActorId);
        }

        private static void ResolveActorDeclaration(
            IActor actor,
            string sourceLabel,
            ref bool hasResolvedActorId,
            ref ActorId resolvedActorId)
        {
            if (actor == null)
            {
                return;
            }

            ActorId candidateActorId = actor.ActorId;
            if (!hasResolvedActorId)
            {
                resolvedActorId = candidateActorId;
                hasResolvedActorId = true;
                return;
            }

            if (resolvedActorId != candidateActorId)
            {
                throw new InvalidOperationException(
                    $"PlayerEntryBehaviour has conflicting Actor identity evidence. Existing='{resolvedActorId.StableText}' {sourceLabel}='{candidateActorId.StableText}'.");
            }
        }

        private ActorReadinessSnapshot ResolveActorReadiness()
        {
            return actorReadinessBehaviour != null
                ? actorReadinessBehaviour.CreateSnapshot()
                : new ActorReadinessSnapshot(initialActorReadinessState, initialActorReadinessReason);
        }

        private void Reset()
        {
            if (playerSlotDeclaration == null)
            {
                playerSlotDeclaration = GetComponent<PlayerSlotDeclaration>();
            }

            if (actorDeclaration == null)
            {
                actorDeclaration = GetComponent<ActorDeclaration>();
            }

            if (playerActorDeclaration == null)
            {
                playerActorDeclaration = GetComponent<PlayerActorDeclaration>();
            }

            if (actorReadinessBehaviour == null)
            {
                actorReadinessBehaviour = GetComponent<ActorReadinessBehaviour>();
            }
        }
    }
}
