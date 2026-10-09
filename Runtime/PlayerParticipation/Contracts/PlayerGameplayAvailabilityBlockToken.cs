using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Opaque ownership evidence for one consumer gameplay-availability block.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "IF-ADR-044 occurrence-scoped gameplay-availability block token.")]
    public readonly struct PlayerGameplayAvailabilityBlockToken : IEquatable<PlayerGameplayAvailabilityBlockToken>
    {
        private readonly string _sessionContextId;
        private readonly PlayerOccurrenceId _playerOccurrenceId;
        private readonly int _sequence;

        internal PlayerGameplayAvailabilityBlockToken(
            string sessionContextId,
            PlayerOccurrenceId playerOccurrenceId,
            int sequence)
        {
            _sessionContextId = sessionContextId.NormalizeText();
            _playerOccurrenceId = playerOccurrenceId;
            _sequence = sequence;
        }

        public bool IsValid =>
            !string.IsNullOrEmpty(_sessionContextId) &&
            _playerOccurrenceId.IsValid &&
            _sequence > 0;

        internal string SessionContextId => _sessionContextId;
        internal PlayerOccurrenceId PlayerOccurrenceId => _playerOccurrenceId;
        internal int Sequence => _sequence;

        public bool Equals(PlayerGameplayAvailabilityBlockToken other) =>
            string.Equals(_sessionContextId, other._sessionContextId, StringComparison.Ordinal) &&
            _playerOccurrenceId == other._playerOccurrenceId &&
            _sequence == other._sequence;

        public override bool Equals(object obj) =>
            obj is PlayerGameplayAvailabilityBlockToken other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(_sessionContextId ?? string.Empty);
                hash = hash * 397 ^ _playerOccurrenceId.GetHashCode();
                hash = hash * 397 ^ _sequence;
                return hash;
            }
        }

        public override string ToString() =>
            IsValid ? nameof(PlayerGameplayAvailabilityBlockToken) : string.Empty;

        public static bool operator ==(
            PlayerGameplayAvailabilityBlockToken left,
            PlayerGameplayAvailabilityBlockToken right) => left.Equals(right);

        public static bool operator !=(
            PlayerGameplayAvailabilityBlockToken left,
            PlayerGameplayAvailabilityBlockToken right) => !left.Equals(right);
    }
}
