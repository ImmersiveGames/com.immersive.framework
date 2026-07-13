
using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped identity for one authorized local Player provisioning operation.
    /// The provisioning bridge creates this identity; callers do not choose it.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G.2 local Player join operation identity.")]
    public readonly struct LocalPlayerJoinOperationId : IEquatable<LocalPlayerJoinOperationId>
    {
        private readonly string value;

        private LocalPlayerJoinOperationId(string value)
        {
            this.value = value.NormalizeText();
        }

        public bool IsValid => !string.IsNullOrEmpty(value);

        public string StableText => IsValid ? value : string.Empty;

        internal static bool TryCreate(
            string sessionContextId,
            int sequence,
            out LocalPlayerJoinOperationId operationId,
            out string issue)
        {
            string normalizedContext = sessionContextId.NormalizeText();
            if (string.IsNullOrEmpty(normalizedContext))
            {
                operationId = default;
                issue = "Local Player join operation requires a non-empty Session context identity.";
                return false;
            }

            if (sequence <= 0)
            {
                operationId = default;
                issue = "Local Player join operation sequence must be greater than zero.";
                return false;
            }

            operationId = new LocalPlayerJoinOperationId(
                $"local-player-join:{normalizedContext}:{sequence}");
            issue = string.Empty;
            return true;
        }

        public bool Equals(LocalPlayerJoinOperationId other)
        {
            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalPlayerJoinOperationId other && Equals(other);
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
            LocalPlayerJoinOperationId left,
            LocalPlayerJoinOperationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            LocalPlayerJoinOperationId left,
            LocalPlayerJoinOperationId right)
        {
            return !left.Equals(right);
        }
    }
}
