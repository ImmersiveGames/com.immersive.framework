using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable evidence that a PlayerView binding was applied to a target.
    /// This is not camera activation, Cinemachine priority, input routing, control binding, movement or spawning.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A PlayerView binding target snapshot.")]
    public readonly struct PlayerViewBindingSnapshot : IEquatable<PlayerViewBindingSnapshot>
    {
        private readonly string _cameraName;
        private readonly string _viewTargetName;
        private readonly string _bindingTargetName;
        private readonly string _source;
        private readonly string _reason;

        public PlayerViewBindingSnapshot(
            PlayerSlotId playerSlotId,
            PlayerViewState playerViewState,
            PlayerEntryState playerEntryState,
            string cameraName,
            string viewTargetName,
            string bindingTargetName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerView binding snapshot requires a valid PlayerSlotId.", nameof(playerSlotId));
            }

            if (!Enum.IsDefined(typeof(PlayerViewState), playerViewState))
            {
                throw new ArgumentOutOfRangeException(nameof(playerViewState), playerViewState, "PlayerView state is not defined.");
            }

            if (!Enum.IsDefined(typeof(PlayerEntryState), playerEntryState))
            {
                throw new ArgumentOutOfRangeException(nameof(playerEntryState), playerEntryState, "PlayerEntry state is not defined.");
            }

            PlayerSlotId = playerSlotId;
            PlayerViewState = playerViewState;
            PlayerEntryState = playerEntryState;
            _cameraName = cameraName.NormalizeText();
            _viewTargetName = viewTargetName.NormalizeText();
            _bindingTargetName = bindingTargetName.NormalizeText();
            _source = source.NormalizeTextOrFallback(nameof(PlayerViewBindingSnapshot));
            _reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerViewState PlayerViewState { get; }

        public PlayerEntryState PlayerEntryState { get; }

        public string CameraName => _cameraName;

        public string ViewTargetName => _viewTargetName;

        public string BindingTargetName => _bindingTargetName;

        public string Source => _source;

        public string Reason => _reason;

        public bool IsActivePlayerView => PlayerViewState == PlayerViewState.Active;

        public bool BindsView => true;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool Equals(PlayerViewBindingSnapshot other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && PlayerViewState == other.PlayerViewState
                && PlayerEntryState == other.PlayerEntryState
                && string.Equals(_cameraName, other._cameraName, StringComparison.Ordinal)
                && string.Equals(_viewTargetName, other._viewTargetName, StringComparison.Ordinal)
                && string.Equals(_bindingTargetName, other._bindingTargetName, StringComparison.Ordinal)
                && string.Equals(_source, other._source, StringComparison.Ordinal)
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewBindingSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ (int)PlayerViewState;
                hash = (hash * 397) ^ (int)PlayerEntryState;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_cameraName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_viewTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_bindingTargetName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"playerSlot='{PlayerSlotId.StableText}' playerViewState='{PlayerViewState}' playerEntryState='{PlayerEntryState}' camera='{_cameraName.ToDiagnosticText()}' viewTarget='{_viewTargetName.ToDiagnosticText()}' bindingTarget='{_bindingTargetName.ToDiagnosticText()}' viewBinding='{BindsView}' controlBinding='{BindsControl}' cameraActivation='{ActivatesCamera}' inputActivation='{ActivatesInput}' movement='{EnablesMovement}' actorSpawning='{SpawnsActor}' reason='{_reason.ToDiagnosticText()}'";
        }

        public static PlayerViewBindingSnapshot FromPlayerView(
            PlayerViewSnapshot playerView,
            string bindingTargetName,
            string source,
            string reason)
        {
            return new PlayerViewBindingSnapshot(
                playerView.PlayerSlotId,
                playerView.State,
                playerView.PlayerEntryState,
                playerView.CameraName,
                playerView.TargetName,
                bindingTargetName,
                source,
                reason);
        }
    }
}
