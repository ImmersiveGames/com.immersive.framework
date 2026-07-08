using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Unity-facing adapter for <see cref="PlayerView"/>.
    /// This component exposes passive PlayerView evidence without activating cameras, selecting CameraDirector priority,
    /// binding input, binding control or owning gameplay movement.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Views/Player View")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49G Unity adapter for passive PlayerView evidence.")]
    public sealed class PlayerViewBehaviour : MonoBehaviour, IPlayerView
    {
        [Header("Identity Evidence")]
        [Tooltip("Optional PlayerSlot declaration evidence. If present, it defines the PlayerSlot identity.")]
        [SerializeField] private PlayerSlotDeclaration playerSlotDeclaration;
        [Tooltip("Fallback stable PlayerSlot id used only when no PlayerSlot declaration is assigned.")]
        [SerializeField] private string playerSlotId = "player.1";

        [Header("View Evidence")]
        [Tooltip("Optional camera evidence. This component does not activate or prioritize the camera.")]
        [SerializeField] private UnityEngine.Camera viewCamera;
        [Tooltip("Optional target evidence. This component does not bind or move the target.")]
        [SerializeField] private Transform viewTarget;

        [Header("PlayerEntry Evidence")]
        [Tooltip("Optional passive PlayerEntry evidence. Bound and Active views require this evidence in ViewBound or Active state.")]
        [SerializeField] private PlayerEntryBehaviour playerEntryBehaviour;

        [Header("Initial PlayerView")]
        [Tooltip("Initial passive PlayerView state for RebuildView.")]
        [SerializeField] private PlayerViewState initialState = PlayerViewState.Declared;
        [Tooltip("Initial diagnostic reason/source for passive PlayerView evidence.")]
        [SerializeField] private string initialReason = "player-view.behaviour.initial";
        [Tooltip("If true, RebuildView is called during Awake. Invalid configuration fails explicitly.")]
        [SerializeField] private bool rebuildOnAwake = true;

        private PlayerView _view;
        private bool _hasView;

        public bool HasView => _hasView;

        public PlayerSlotId PlayerSlotId => EnsureView().PlayerSlotId;

        public PlayerViewState State => EnsureView().State;

        public bool HasCameraEvidence => EnsureView().HasCameraEvidence;

        public bool HasTargetEvidence => EnsureView().HasTargetEvidence;

        public bool HasPlayerEntryEvidence => EnsureView().HasPlayerEntryEvidence;

        public bool IsEligibleForActiveView => EnsureView().IsEligibleForActiveView;

        public UnityEngine.Camera ViewCamera => viewCamera;

        public Transform ViewTarget => viewTarget;

        public PlayerEntryBehaviour PlayerEntryBehaviour => playerEntryBehaviour;

        private void Awake()
        {
            if (rebuildOnAwake)
            {
                RebuildView();
            }
        }

        public PlayerViewSnapshot CreateSnapshot()
        {
            return EnsureView().CreateSnapshot();
        }

        public PlayerView RebuildView(string reasonOverride = null)
        {
            PlayerSlotId resolvedSlotId = ResolvePlayerSlotId();
            string reason = reasonOverride.NormalizeTextOrFallback(initialReason);
            bool hasPlayerEntryEvidence = false;
            PlayerEntryState playerEntryState = PlayerEntryState.Configured;

            if (playerEntryBehaviour != null)
            {
                PlayerEntrySnapshot entrySnapshot = playerEntryBehaviour.CreateSnapshot();
                if (entrySnapshot.PlayerSlotId != resolvedSlotId)
                {
                    throw new InvalidOperationException(
                        $"PlayerViewBehaviour PlayerEntry evidence must match PlayerSlotId. PlayerView='{resolvedSlotId.StableText}' PlayerEntry='{entrySnapshot.PlayerSlotId.StableText}'.");
                }

                hasPlayerEntryEvidence = true;
                playerEntryState = entrySnapshot.State;
            }

            _view = new PlayerView(
                resolvedSlotId,
                initialState,
                viewCamera != null,
                viewTarget != null,
                hasPlayerEntryEvidence,
                playerEntryState,
                viewCamera != null ? viewCamera.name : string.Empty,
                viewTarget != null ? viewTarget.name : string.Empty,
                reason);
            _hasView = true;
            return _view;
        }

        public PlayerView RefreshPlayerEntryEvidence(string reason = null)
        {
            if (playerEntryBehaviour == null)
            {
                throw new InvalidOperationException("PlayerViewBehaviour cannot refresh PlayerEntry evidence because PlayerEntryBehaviour is not assigned.");
            }

            PlayerEntrySnapshot entrySnapshot = playerEntryBehaviour.CreateSnapshot();
            _view = EnsureView().WithPlayerEntryEvidence(entrySnapshot, reason);
            _hasView = true;
            return _view;
        }

        public PlayerView SetState(PlayerViewState state, string reason = null)
        {
            _view = EnsureView().WithState(state, reason);
            _hasView = true;
            return _view;
        }

        public PlayerView Release(string reason = null)
        {
            _view = EnsureView().Released(reason);
            _hasView = true;
            return _view;
        }

        [ContextMenu("Player View/Rebuild View")]
        private void ContextRebuildView()
        {
            RebuildView("player-view.behaviour.context.rebuild");
        }

        [ContextMenu("Player View/Refresh PlayerEntry Evidence")]
        private void ContextRefreshPlayerEntryEvidence()
        {
            RefreshPlayerEntryEvidence("player-view.behaviour.context.refresh-player-entry");
        }

        [ContextMenu("Player View/Set Bound")]
        private void ContextSetBound()
        {
            SetState(PlayerViewState.Bound, "player-view.behaviour.context.bound");
        }

        [ContextMenu("Player View/Set Active")]
        private void ContextSetActive()
        {
            SetState(PlayerViewState.Active, "player-view.behaviour.context.active");
        }

        [ContextMenu("Player View/Release")]
        private void ContextRelease()
        {
            Release("player-view.behaviour.context.release");
        }

        private PlayerView EnsureView()
        {
            return _hasView && _view != null
                ? _view
                : RebuildView("player-view.behaviour.ensure-view");
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
                throw new InvalidOperationException("PlayerViewBehaviour requires a PlayerSlotDeclaration or a non-empty PlayerSlotId.");
            }

            return new PlayerSlotId(normalizedSlotId);
        }

        private void Reset()
        {
            if (playerSlotDeclaration == null)
            {
                playerSlotDeclaration = GetComponent<PlayerSlotDeclaration>();
            }

            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<UnityEngine.Camera>();
            }

            if (viewTarget == null)
            {
                viewTarget = transform;
            }

            if (playerEntryBehaviour == null)
            {
                playerEntryBehaviour = GetComponent<PlayerEntryBehaviour>();
            }
        }
    }
}
