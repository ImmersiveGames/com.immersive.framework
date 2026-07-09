using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Unity target that stores explicit PlayerControl binding evidence.
    /// It does not activate input, route InputActions, switch action maps, enable movement, control objects or spawn actors.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Binding/Player Control Binding Target")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A Unity target for explicit PlayerControl binding evidence.")]
    public sealed class PlayerControlBindingTargetBehaviour : MonoBehaviour, IPlayerControlBindingTarget
    {
        [Tooltip("Human-readable target name used in diagnostics only.")]
        [SerializeField] private string bindingTargetName = "Player Control Binding Target";

        private PlayerControlBindingSnapshot _currentBinding;
        private bool _hasBinding;

        public string BindingTargetName => bindingTargetName.NormalizeTextOrFallback(name);

        public bool HasPlayerControlBinding => _hasBinding;

        public PlayerControlBindingSnapshot CurrentPlayerControlBinding
        {
            get
            {
                if (!_hasBinding)
                {
                    throw new InvalidOperationException("PlayerControl binding target has no current binding.");
                }

                return _currentBinding;
            }
        }

        public bool BindsView => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public PlayerControlBindingResult ApplyPlayerControlBinding(PlayerControlBindingSnapshot binding, string source = null, string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerControlBindingTargetBehaviour));
            if (!binding.PlayerSlotId.IsValid)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.InvalidPlayerControl,
                    string.Empty,
                    BindingTargetName,
                    normalizedSource,
                    reason,
                    "PlayerControl binding target rejected an invalid PlayerSlotId.");
            }

            _currentBinding = binding;
            _hasBinding = true;
            return PlayerControlBindingResult.Success(
                binding.PlayerSlotId,
                BindingTargetName,
                normalizedSource,
                reason,
                "PlayerControl binding evidence stored on target.");
        }

        public PlayerControlBindingResult ClearPlayerControlBinding(PlayerSlotId playerSlotId, string source = null, string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerControlBindingTargetBehaviour));
            string playerSlotIdText = playerSlotId.IsValid ? playerSlotId.StableText : string.Empty;

            if (!_hasBinding)
            {
                return PlayerControlBindingResult.NoOperation(
                    PlayerControlBindingFailureKind.MissingExistingBinding,
                    playerSlotIdText,
                    BindingTargetName,
                    normalizedSource,
                    reason,
                    "PlayerControl binding target had no binding to clear.");
            }

            if (_currentBinding.PlayerSlotId != playerSlotId)
            {
                return PlayerControlBindingResult.Failure(
                    PlayerControlBindingFailureKind.TargetPlayerSlotMismatch,
                    playerSlotIdText,
                    BindingTargetName,
                    normalizedSource,
                    reason,
                    $"PlayerControl binding target is bound to '{_currentBinding.PlayerSlotId.StableText}' and cannot clear '{playerSlotIdText}'.");
            }

            _currentBinding = default;
            _hasBinding = false;
            return PlayerControlBindingResult.Success(
                playerSlotId,
                BindingTargetName,
                normalizedSource,
                reason,
                "PlayerControl binding evidence cleared from target.",
                false);
        }

        internal void ConfigureForDiagnostics(string targetName)
        {
            bindingTargetName = targetName.NormalizeTextOrFallback(name);
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(bindingTargetName))
            {
                bindingTargetName = name;
            }
        }
    }
}
