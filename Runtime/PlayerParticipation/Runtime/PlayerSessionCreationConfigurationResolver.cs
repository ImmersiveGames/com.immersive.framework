using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Selects and resolves the authored Player Session Profile for one
    /// creation attempt. The explicit Profile, when supplied, replaces the
    /// Game Application default as the complete source of configuration.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-016 creation-time Player Session Profile selection for FrameworkRuntimeHost.")]
    internal static class PlayerSessionCreationConfigurationResolver
    {
        /// <summary>
        /// Returns false only when Player Session is explicitly disabled by
        /// the Game Application. When enabled, a result is always returned;
        /// an absent or invalid selected Profile is represented by its typed
        /// initialization failure.
        /// </summary>
        internal static bool TryResolve(
            GameApplicationAsset gameApplication,
            PlayerSessionProfile explicitPlayerSessionProfile,
            out PlayerSessionInitializationResult result)
        {
            if (gameApplication == null)
            {
                throw new ArgumentNullException(nameof(gameApplication));
            }

            if (!gameApplication.PlayerSessionEnabled)
            {
                result = null;
                return false;
            }

            PlayerSessionProfile selectedProfile =
                explicitPlayerSessionProfile != null
                    ? explicitPlayerSessionProfile
                    : gameApplication.DefaultPlayerSessionProfile;
            result = PlayerSessionConfigurationResolver.Resolve(selectedProfile);
            return true;
        }
    }
}
