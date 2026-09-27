using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Pause-side projection of the Session physical Local Player Host lifetime.
    /// It observes canonical Player host evidence and binds the co-located
    /// PlayerPauseInput without owning PlayerInput, InputUser or devices.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-005 Session-host lifetime integration for physical Pause input.")]
    internal sealed class PauseSessionPlayerInputBindingRuntimeHostModule :
        MonoBehaviour
    {
        private FrameworkRuntimeHost _runtimeHost;
        private PlayerActorPreparationRuntimeHostModule _playerRuntime;
        private IPauseProductBindingPort _bindingPort;
        private PlayerSlotId _activeSlot;
        private LocalPlayerHostAuthoring _activeHost;
        private PlayerPauseInput _activeBinding;
        private FrameworkLogger _logger;
        private bool _shuttingDown;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            PlayerActorPreparationRuntimeHostModule playerRuntime,
            IPauseProductBindingPort bindingPort,
            out PauseSessionPlayerInputBindingRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;
            if (runtimeHost == null || playerRuntime == null || bindingPort == null)
            {
                issue =
                    "Pause Session PlayerInput binding requires explicit Framework host, Player host evidence and Pause binding port.";
                return false;
            }

            module =
                runtimeHost.GetComponent<
                    PauseSessionPlayerInputBindingRuntimeHostModule>();
            if (module == null)
            {
                module =
                    runtimeHost.gameObject.AddComponent<
                        PauseSessionPlayerInputBindingRuntimeHostModule>();
            }

            return module.TryInitialize(
                runtimeHost,
                playerRuntime,
                bindingPort,
                out issue);
        }

        private bool TryInitialize(
            FrameworkRuntimeHost runtimeHost,
            PlayerActorPreparationRuntimeHostModule playerRuntime,
            IPauseProductBindingPort bindingPort,
            out string issue)
        {
            issue = string.Empty;
            if (_runtimeHost != null)
            {
                if (ReferenceEquals(_runtimeHost, runtimeHost) &&
                    ReferenceEquals(_playerRuntime, playerRuntime) &&
                    ReferenceEquals(_bindingPort, bindingPort))
                {
                    return true;
                }

                issue =
                    "Pause Session PlayerInput binding is already initialized with different runtime evidence.";
                return false;
            }

            _runtimeHost = runtimeHost;
            _playerRuntime = playerRuntime;
            _bindingPort = bindingPort;
            _logger =
                FrameworkLogger.Create<
                    PauseSessionPlayerInputBindingRuntimeHostModule>();
            _playerRuntime.SessionPhysicalHostChanged +=
                OnSessionPhysicalHostChanged;

            ReconcileCurrentSessionHosts("initialization");
            return true;
        }

        private void OnSessionPhysicalHostChanged(
            PlayerSlotId playerSlotId)
        {
            if (_shuttingDown)
            {
                return;
            }

            if (_activeSlot.IsValid &&
                playerSlotId == _activeSlot &&
                (!_playerRuntime.TryGetCurrentSessionPhysicalHost(
                    playerSlotId,
                    out LocalPlayerHostAuthoring currentHost,
                    out _) ||
                 !ReferenceEquals(currentHost, _activeHost)))
            {
                ReleaseActive("session-physical-host-released");
            }

            if (_activeBinding == null)
            {
                TryBindSlot(playerSlotId, "session-physical-host-changed");
            }
        }

        private void ReconcileCurrentSessionHosts(
            string reason)
        {
            if (_runtimeHost == null ||
                !_runtimeHost.TryGetPlayerParticipationSnapshot(
                    out PlayerParticipationSnapshot snapshot) ||
                snapshot == null)
            {
                return;
            }

            for (int index = 0;
                 index < snapshot.Slots.Count && _activeBinding == null;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined)
                {
                    continue;
                }

                TryBindSlot(slot.PlayerSlotId, reason);
            }
        }

        private void TryBindSlot(
            PlayerSlotId playerSlotId,
            string reason)
        {
            if (_activeBinding != null ||
                !playerSlotId.IsValid ||
                !_playerRuntime.TryGetCurrentSessionPhysicalHost(
                    playerSlotId,
                    out LocalPlayerHostAuthoring host,
                    out _))
            {
                return;
            }

            PlayerPauseInput[] bindings =
                host.GetComponents<PlayerPauseInput>();
            if (bindings.Length != 1 || bindings[0] == null)
            {
                LogWarning(
                    playerSlotId,
                    host,
                    "Pause Session Host requires exactly one co-located PlayerPauseInput.",
                    reason);
                return;
            }

            PlayerPauseInput binding = bindings[0];
            if (!binding.TryInjectBindingPort(
                    _bindingPort,
                    out string diagnostic))
            {
                LogWarning(
                    playerSlotId,
                    host,
                    diagnostic,
                    reason);
                return;
            }

            _activeSlot = playerSlotId;
            _activeHost = host;
            _activeBinding = binding;
        }

        private void ReleaseActive(
            string reason)
        {
            PlayerPauseInput binding = _activeBinding;
            _activeSlot = default;
            _activeHost = null;
            _activeBinding = null;

            if (binding == null)
            {
                return;
            }

            if (!binding.TryReleaseBinding(
                    reason,
                    out string diagnostic))
            {
                _logger?.Warning(
                    "Pause Session PlayerInput binding release failed.",
                    LogFields.Of(
                        LogFields.Field("reason", reason),
                        LogFields.Field("issue", diagnostic)));
            }
        }

        private void LogWarning(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring host,
            string issue,
            string reason)
        {
            _logger?.Warning(
                "Pause Session PlayerInput binding could not be composed.",
                LogFields.Of(
                    LogFields.Field(
                        "playerSlot",
                        playerSlotId.StableText),
                    LogFields.Field(
                        "host",
                        host != null ? host.name : "<missing>"),
                    LogFields.Field("reason", reason),
                    LogFields.Field("issue", issue)));
        }

        private void OnDestroy()
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            if (_playerRuntime != null)
            {
                _playerRuntime.SessionPhysicalHostChanged -=
                    OnSessionPhysicalHostChanged;
            }

            ReleaseActive("runtime-host-shutdown");
            _bindingPort = null;
            _playerRuntime = null;
            _runtimeHost = null;
        }
    }
}
