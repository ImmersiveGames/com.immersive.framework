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
        ILocalPlayerProvisioningBackend
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
            private readonly PlayerInputManager manager;
            private readonly Action<PlayerInput> listener;
            private readonly List<PendingJoinedPlayer> pendingPlayers = new();
            private bool sceneLoadedSubscribed;
            private bool disposed;

            internal JoinedSubscription(
                PlayerInputManager manager,
                Action<PlayerInput> listener)
            {
                this.manager = manager != null
                    ? manager
                    : throw new ArgumentNullException(nameof(manager));
                this.listener = listener ??
                    throw new ArgumentNullException(nameof(listener));
                manager.onPlayerJoined += HandlePlayerJoined;
            }

            internal bool Matches(Action<PlayerInput> candidate)
            {
                return listener == candidate;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                manager.onPlayerJoined -= HandlePlayerJoined;
                ReleaseSceneLoadedSubscription();
                pendingPlayers.Clear();
            }

            private void HandlePlayerJoined(PlayerInput playerInput)
            {
                if (disposed)
                {
                    return;
                }

                if (ReferenceEquals(playerInput, null) || playerInput == null)
                {
                    listener(playerInput);
                    return;
                }

                Scene scene = playerInput.gameObject.scene;
                if (!scene.IsValid() || scene.isLoaded)
                {
                    listener(playerInput);
                    return;
                }

                for (int index = 0; index < pendingPlayers.Count; index++)
                {
                    if (ReferenceEquals(
                            pendingPlayers[index].PlayerInput,
                            playerInput))
                    {
                        return;
                    }
                }

                pendingPlayers.Add(new PendingJoinedPlayer(
                    playerInput,
                    scene.handle.GetRawData()));
                EnsureSceneLoadedSubscription();
            }

            private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (disposed)
                {
                    return;
                }

                for (int index = pendingPlayers.Count - 1; index >= 0; index--)
                {
                    PendingJoinedPlayer pending = pendingPlayers[index];
                    if (pending.SourceSceneHandle != scene.handle.GetRawData())
                    {
                        continue;
                    }

                    pendingPlayers.RemoveAt(index);
                    listener(pending.PlayerInput);
                }

                if (pendingPlayers.Count == 0)
                {
                    ReleaseSceneLoadedSubscription();
                }
            }

            private void EnsureSceneLoadedSubscription()
            {
                if (sceneLoadedSubscribed)
                {
                    return;
                }

                SceneManager.sceneLoaded += HandleSceneLoaded;
                sceneLoadedSubscribed = true;
            }

            private void ReleaseSceneLoadedSubscription()
            {
                if (!sceneLoadedSubscribed)
                {
                    return;
                }

                SceneManager.sceneLoaded -= HandleSceneLoaded;
                sceneLoadedSubscribed = false;
            }
        }

        private readonly PlayerInputManager manager;
        private readonly List<JoinedSubscription> joinedSubscriptions = new();

        internal UnityLocalPlayerProvisioningBackend(PlayerInputManager manager)
        {
            this.manager = manager;
        }

        public bool IsAvailable => manager != null;

        public bool UsesManualJoin =>
            manager != null &&
            manager.joinBehavior == PlayerJoinBehavior.JoinPlayersManually;

        public GameObject PlayerPrefab => manager != null
            ? manager.playerPrefab
            : null;

        public int CurrentPlayerCount => manager != null
            ? manager.playerCount
            : 0;

        public int TechnicalMaxPlayerCount => manager != null
            ? manager.maxPlayerCount
            : 0;

        public event Action<PlayerInput> PlayerJoined
        {
            add
            {
                if (manager == null || value == null)
                {
                    return;
                }

                joinedSubscriptions.Add(new JoinedSubscription(manager, value));
            }
            remove
            {
                if (manager == null || value == null)
                {
                    return;
                }

                for (int index = joinedSubscriptions.Count - 1;
                     index >= 0;
                     index--)
                {
                    JoinedSubscription subscription = joinedSubscriptions[index];
                    if (!subscription.Matches(value))
                    {
                        continue;
                    }

                    subscription.Dispose();
                    joinedSubscriptions.RemoveAt(index);
                    return;
                }
            }
        }

        public PlayerInput JoinPlayer(LocalPlayerJoinRequest request)
        {
            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Local Player provisioning backend has no PlayerInputManager.");
            }

            if (!request.TryValidate(out string issue))
            {
                throw new ArgumentException(issue, nameof(request));
            }

            return manager.JoinPlayer(
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
            if (playerInput == null)
            {
                return;
            }

            playerInput.DeactivateInput();
            UnityEngine.Object.Destroy(playerInput.gameObject);
        }
    }
}
