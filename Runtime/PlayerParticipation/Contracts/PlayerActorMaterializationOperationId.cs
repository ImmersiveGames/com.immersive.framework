using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Framework-generated identity for one contextual Player Actor materialization attempt.
    /// Callers provide owner and Slot intent but never author this operation identity.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.3 contextual Player Actor materialization operation identity.")]
    public readonly struct PlayerActorMaterializationOperationId :
        IEquatable<PlayerActorMaterializationOperationId>
    {
        private readonly string value;

        private PlayerActorMaterializationOperationId(string value)
        {
            this.value = value.NormalizeText();
        }

        public bool IsValid => !string.IsNullOrEmpty(value);

        public string StableText => IsValid ? value : string.Empty;

        internal static bool TryCreate(
            string sessionContextId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            int sequence,
            out PlayerActorMaterializationOperationId operationId,
            out string issue)
        {
            string normalizedSessionContextId = sessionContextId.NormalizeText();
            if (string.IsNullOrEmpty(normalizedSessionContextId))
            {
                operationId = default;
                issue = "Player Actor materialization operation requires a non-empty Session context identity.";
                return false;
            }

            if (!owner.IsValid)
            {
                operationId = default;
                issue = "Player Actor materialization operation requires a valid Runtime Content owner.";
                return false;
            }

            if (!playerSlotId.IsValid)
            {
                operationId = default;
                issue = "Player Actor materialization operation requires a valid Player Slot identity.";
                return false;
            }

            if (sequence <= 0)
            {
                operationId = default;
                issue = "Player Actor materialization operation sequence must be greater than zero.";
                return false;
            }

            operationId = new PlayerActorMaterializationOperationId(
                $"player-actor-materialization:{normalizedSessionContextId}:" +
                $"{owner.Scope}:{owner.OwnerIdentity.Value.Value}:" +
                $"{playerSlotId.Value.Value}:{sequence}");
            issue = string.Empty;
            return true;
        }

        public bool Equals(PlayerActorMaterializationOperationId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerActorMaterializationOperationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
        }

        public override string ToString()
        {
            return StableText;
        }

        public static bool operator ==(
            PlayerActorMaterializationOperationId left,
            PlayerActorMaterializationOperationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            PlayerActorMaterializationOperationId left,
            PlayerActorMaterializationOperationId right)
        {
            return !left.Equals(right);
        }
    }
}
