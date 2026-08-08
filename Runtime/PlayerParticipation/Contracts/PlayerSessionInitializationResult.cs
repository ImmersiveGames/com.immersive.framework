using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable outcome of resolving Player Session initialization
    /// configuration. A successful result is evidence for a later runtime
    /// integration cut; it does not initialize or mutate a Session.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 Player Session initialization configuration result.")]
    public sealed class PlayerSessionInitializationResult
    {
        private PlayerSessionInitializationResult(
            EffectivePlayerSessionConfiguration configuration,
            PlayerSessionInitializationFailure failure,
            string message)
        {
            Configuration = configuration;
            Failure = failure;
            Message = message ?? string.Empty;
        }

        public EffectivePlayerSessionConfiguration Configuration { get; }

        public PlayerSessionInitializationFailure Failure { get; }

        public string Message { get; }

        public bool Succeeded =>
            Configuration != null &&
            Failure == PlayerSessionInitializationFailure.None;

        public bool Failed => !Succeeded;

        public static PlayerSessionInitializationResult SucceededWith(
            EffectivePlayerSessionConfiguration configuration,
            string message)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return new PlayerSessionInitializationResult(
                configuration,
                PlayerSessionInitializationFailure.None,
                message);
        }

        public static PlayerSessionInitializationResult FailedWith(
            PlayerSessionInitializationFailure failure,
            string message)
        {
            if (!Enum.IsDefined(
                    typeof(PlayerSessionInitializationFailure),
                    failure) ||
                failure == PlayerSessionInitializationFailure.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failure),
                    failure,
                    "A failed Player Session initialization result requires a failure kind.");
            }

            return new PlayerSessionInitializationResult(
                null,
                failure,
                message);
        }
    }
}
