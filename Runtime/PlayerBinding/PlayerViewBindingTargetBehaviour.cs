using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Unity target that stores explicit PlayerView binding evidence.
    /// It does not enable/disable cameras, change priorities, bind input/control, move objects or spawn actors.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player Binding/Player View Binding Target")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A Unity target for explicit PlayerView binding evidence.")]
    public sealed class PlayerViewBindingTargetBehaviour : MonoBehaviour, IPlayerViewBindingTarget
    {
        [Tooltip("Human-readable target name used in diagnostics only.")]
        [SerializeField] private string bindingTargetName = "Player View Binding Target";

        private PlayerViewBindingSnapshot _currentBinding;
        private bool _hasBinding;

        public string BindingTargetName => bindingTargetName.NormalizeTextOrFallback(name);

        public bool HasPlayerViewBinding => _hasBinding;

        public PlayerViewBindingSnapshot CurrentPlayerViewBinding
        {
            get
            {
                if (!_hasBinding)
                {
                    throw new InvalidOperationException("PlayerView binding target has no current binding.");
                }

                return _currentBinding;
            }
        }

        public bool ActivatesCamera => false;

        public bool BindsControl => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public PlayerViewBindingResult ApplyPlayerViewBinding(PlayerViewBindingSnapshot binding, string source = null, string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewBindingTargetBehaviour));
            if (!binding.PlayerSlotId.IsValid)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.InvalidPlayerView,
                    string.Empty,
                    BindingTargetName,
                    normalizedSource,
                    reason,
                    "PlayerView binding target rejected an invalid PlayerSlotId.");
            }

            _currentBinding = binding;
            _hasBinding = true;
            return PlayerViewBindingResult.Success(
                binding.PlayerSlotId,
                BindingTargetName,
                normalizedSource,
                reason,
                "PlayerView binding evidence stored on target.");
        }

        public PlayerViewBindingResult ClearPlayerViewBinding(PlayerSlotId playerSlotId, string source = null, string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewBindingTargetBehaviour));
            string playerSlotIdText = playerSlotId.IsValid ? playerSlotId.StableText : string.Empty;

            if (!_hasBinding)
            {
                return PlayerViewBindingResult.NoOperation(
                    PlayerViewBindingFailureKind.MissingExistingBinding,
                    playerSlotIdText,
                    BindingTargetName,
                    normalizedSource,
                    reason,
                    "PlayerView binding target had no binding to clear.");
            }

            if (_currentBinding.PlayerSlotId != playerSlotId)
            {
                return PlayerViewBindingResult.Failure(
                    PlayerViewBindingFailureKind.TargetPlayerSlotMismatch,
                    playerSlotIdText,
                    BindingTargetName,
                    normalizedSource,
                    reason,
                    $"PlayerView binding target is bound to '{_currentBinding.PlayerSlotId.StableText}' and cannot clear '{playerSlotIdText}'.");
            }

            _currentBinding = default;
            _hasBinding = false;
            return PlayerViewBindingResult.Success(
                playerSlotId,
                BindingTargetName,
                normalizedSource,
                reason,
                "PlayerView binding evidence cleared from target.",
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
