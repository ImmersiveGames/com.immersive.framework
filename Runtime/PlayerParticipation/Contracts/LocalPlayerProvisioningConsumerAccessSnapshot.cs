using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-03 immutable scoped consumer access diagnostic.")]
    public sealed class LocalPlayerProvisioningConsumerAccessSnapshot
    {
        internal LocalPlayerProvisioningConsumerAccessSnapshot(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            bool isAvailable,
            bool isDisposed,
            string diagnostic)
        {
            Scope = scope;
            Owner = owner;
            IsAvailable = isAvailable;
            IsDisposed = isDisposed;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public LocalPlayerProvisioningConsumerScope Scope { get; }

        public RuntimeContentOwner Owner { get; }

        public bool IsAvailable { get; }

        public bool IsDisposed { get; }

        public string Diagnostic { get; }

        internal static LocalPlayerProvisioningConsumerAccessSnapshot Unavailable(
            LocalPlayerProvisioningConsumerScope scope,
            RuntimeContentOwner owner,
            string diagnostic)
        {
            return new LocalPlayerProvisioningConsumerAccessSnapshot(
                scope,
                owner,
                false,
                false,
                diagnostic);
        }
    }
}
