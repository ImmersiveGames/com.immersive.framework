using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ACCESS-2 immutable scoped Player Session access diagnostic.")]
    public sealed class PlayerSessionScopedAccessSnapshot
    {
        internal PlayerSessionScopedAccessSnapshot(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            bool isAvailable,
            bool isDisposed,
            bool hasJoinCapability,
            string diagnostic)
        {
            Scope = scope;
            Owner = owner;
            IsAvailable = isAvailable;
            IsDisposed = isDisposed;
            HasJoinCapability = hasJoinCapability;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public LocalPlayerProvisioningConsumerScope Scope { get; }
        public RuntimeContentOwner Owner { get; }
        public bool IsAvailable { get; }
        public bool IsDisposed { get; }
        public bool HasJoinCapability { get; }
        public string Diagnostic { get; }

        internal static PlayerSessionScopedAccessSnapshot Unavailable(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            string diagnostic)
        {
            return new PlayerSessionScopedAccessSnapshot(
                scope, owner, false, false, false, diagnostic);
        }
    }
}
