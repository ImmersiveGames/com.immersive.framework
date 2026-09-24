using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Declares the lifecycle owner that may receive a Local Player provisioning
    /// consumer endpoint. It is an access lifetime, never Player configuration.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-03 explicit consumer access lifetime.")]
    public enum LocalPlayerProvisioningConsumerScope
    {
        Unspecified = 0,
        Route = 10,
        Activity = 20
    }

    internal static class LocalPlayerProvisioningConsumerScopeExtensions
    {
        internal static bool IsDefinedScope(
            this LocalPlayerProvisioningConsumerScope scope)
        {
            return scope == LocalPlayerProvisioningConsumerScope.Route ||
                scope == LocalPlayerProvisioningConsumerScope.Activity;
        }
    }
}
