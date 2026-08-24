using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Unity Input System adapter for one explicit Session-authorized PlayerInputManager.
    /// It does not own Slot allocation or framework admission.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G.3/P3M5B Unity PlayerInputManager provisioning adapter with scene-load callback normalization.")]
    internal sealed class UnityLocalPlayerProvisioningBackend :
        ILocalPlayerProvisioningBackend,
        IAdmittedLocalPlayerReleaseBackend
    {
        private readonly struct PendingJoinedPlayer
        {
            internal PendingJoinedPlayer(
                PlayerInput playerInput,
                ulong sourceSceneHandle)
            {
                PlayerInput = playerInput;
                SourceSceneHandle = sourceSceneHandle;
            }

            internal PlayerInput PlayerInput { get; }

            internal ulong SourceSceneHandle { get; }
        }

        /// <summary>
        /// PlayerInput may report a scene-authored Player during scene activation, before
        /// Scene.isLoaded becomes true. Local Player provisioning must not classify that callback
        /// as an unauthorized manual join before the complete scene authoring graph is available.
        /// </summary>
        private sealed class JoinedSubscription : IDisposable
        {
            private readonly PlayerInputManager _manager;
            private readonly Action<PlayerInput> _listener;
            private readonly List<PendingJoinedPlayer> _pendingPlayers = new();
            private bool _sceneLoadedSubscribed;
            private bool _disposed;

            internal JoinedSubscription(
                PlayerInputManager manager,
                Action<PlayerInput> listener)
            {
                this._manager = manager != null
                    ? manager
                    : throw new ArgumentNullException(nameof(manager));
                this._listener = listener ??
                    throw new ArgumentNullException(nameof(listener));
                manager.onPlayerJoined += HandlePlayerJoined;
            }

            internal bool Matches(Action<PlayerInput> candidate)
            {
                return _listener == candidate;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _manager.onPlayerJoined -= HandlePlayerJoined;
                ReleaseSceneLoadedSubscription();
                _pendingPlayers.Clear();
            }

            private void HandlePlayerJoined(PlayerInput playerInput)
            {
                if (_disposed)
                {
                    return;
                }

                if (ReferenceEquals(playerInput, null) || playerInput == null)
                {
                    _listener(playerInput);
                    return;
                }

                Scene scene = playerInput.gameObject.scene;
                if (!scene.IsValid() || scene.isLoaded)
                {
                    _listener(playerInput);
                    return;
                }

                for (int index = 0; index < _pendingPlayers.Count; index++)
                {
                    if (ReferenceEquals(
                            _pendingPlayers[index].PlayerInput,
                            playerInput))
                    {
                        return;
                    }
                }

                _pendingPlayers.Add(new PendingJoinedPlayer(
                    playerInput,
                    scene.handle.GetRawData()));
                EnsureSceneLoadedSubscription();
            }

            private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (_disposed)
                {
                    return;
                }

                for (int index = _pendingPlayers.Count - 1; index >= 0; index--)
                {
                    PendingJoinedPlayer pending = _pendingPlayers[index];
                    if (pending.SourceSceneHandle != scene.handle.GetRawData())
                    {
                        continue;
                    }

                    _pendingPlayers.RemoveAt(index);
                    _listener(pending.PlayerInput);
                }

                if (_pendingPlayers.Count == 0)
                {
                    ReleaseSceneLoadedSubscription();
                }
            }

            private void EnsureSceneLoadedSubscription()
            {
                if (_sceneLoadedSubscribed)
                {
                    return;
                }

                SceneManager.sceneLoaded += HandleSceneLoaded;
                _sceneLoadedSubscribed = true;
            }

            private void ReleaseSceneLoadedSubscription()
            {
                if (!_sceneLoadedSubscribed)
                {
                    return;
                }

                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _sceneLoadedSubscribed = false;
            }
        }

        private readonly PlayerInputManager _manager;
        private readonly List<JoinedSubscription> _joinedSubscriptions = new();

        internal UnityLocalPlayerProvisioningBackend(PlayerInputManager manager)
        {
            this._manager = manager;
        }

        public bool IsAvailable => _manager != null;

        public bool UsesManualJoin =>
            _manager != null &&
            _manager.joinBehavior == PlayerJoinBehavior.JoinPlayersManually;

        public GameObject PlayerPrefab => _manager != null
            ? _manager.playerPrefab
            : null;

        public event Action<PlayerInput> PlayerJoined
        {
            add
            {
                if (_manager == null || value == null)
                {
                    return;
                }

                _joinedSubscriptions.Add(new JoinedSubscription(_manager, value));
            }
            remove
            {
                if (_manager == null || value == null)
                {
                    return;
                }

                for (int index = _joinedSubscriptions.Count - 1;
                     index >= 0;
                     index--)
                {
                    JoinedSubscription subscription = _joinedSubscriptions[index];
                    if (!subscription.Matches(value))
                    {
                        continue;
                    }

                    subscription.Dispose();
                    _joinedSubscriptions.RemoveAt(index);
                    return;
                }
            }
        }

        public PlayerInput JoinPlayer(LocalPlayerJoinRequest request)
        {
            if (_manager == null)
            {
                throw new InvalidOperationException(
                    "Local Player provisioning backend has no PlayerInputManager.");
            }

            if (!request.TryValidate(out string issue))
            {
                throw new ArgumentException(issue, nameof(request));
            }

            return _manager.JoinPlayer(
                playerIndex: -1,
                splitScreenIndex: -1,
                controlScheme: request.HasControlSchemeHint
                    ? request.ControlScheme
                    : null,
                pairWithDevice: request.PairWithDevice);
        }

        public void RejectPlayer(
            PlayerInput playerInput,
            string source,
            string reason)
        {
            _ = source;
            _ = reason;
            DestroyProvisionedPlayer(playerInput);
        }

        public void ReleaseAdmittedPlayer(
            PlayerInput playerInput,
            string source,
            string reason)
        {
            _ = source;
            _ = reason;
            DestroyProvisionedPlayer(playerInput);
        }

        private static void DestroyProvisionedPlayer(PlayerInput playerInput)
        {
            if (ReferenceEquals(playerInput, null) || playerInput == null)
            {
                return;
            }

            playerInput.DeactivateInput();
            UnityEngine.Object.Destroy(playerInput.gameObject);
        }
    }
}
