using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Identity of one camera capability for a prepared local Player.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3K.4 current Player camera capability token.")]
    public readonly struct PlayerGameplayCameraEligibilityToken : IEquatable<PlayerGameplayCameraEligibilityToken>
    {
        internal PlayerGameplayCameraEligibilityToken(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken preparationToken,
            string cameraOutputId,
            int cameraRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            PreparationToken = preparationToken;
            CameraOutputId = cameraOutputId.NormalizeText();
            CameraRevision = cameraRevision;
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public string CameraOutputId { get; }
        public int CameraRevision { get; }
        public int EligibilityRevision => CameraRevision;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) && PlayerSlotId.IsValid &&
            PreparationToken.IsValid && !string.IsNullOrEmpty(CameraOutputId) &&
            CameraRevision > 0 &&
            string.Equals(PreparationToken.SessionContextId, SessionContextId, StringComparison.Ordinal) &&
            PreparationToken.PlayerSlotId == PlayerSlotId;

        public string StableText => IsValid
            ? $"player-gameplay-camera:{SessionContextId}:{PlayerSlotId.Value.Value}:{PreparationToken.StableText}:{CameraOutputId}:{CameraRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayCameraEligibilityToken other) =>
            string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
            PlayerSlotId == other.PlayerSlotId && PreparationToken == other.PreparationToken &&
            string.Equals(CameraOutputId, other.CameraOutputId, StringComparison.Ordinal) &&
            CameraRevision == other.CameraRevision;
        public override bool Equals(object obj) => obj is PlayerGameplayCameraEligibilityToken other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SessionContextId, PlayerSlotId, PreparationToken, CameraOutputId, CameraRevision);
        public override string ToString() => StableText;
        public static bool operator ==(PlayerGameplayCameraEligibilityToken left, PlayerGameplayCameraEligibilityToken right) => left.Equals(right);
        public static bool operator !=(PlayerGameplayCameraEligibilityToken left, PlayerGameplayCameraEligibilityToken right) => !left.Equals(right);
    }
}
