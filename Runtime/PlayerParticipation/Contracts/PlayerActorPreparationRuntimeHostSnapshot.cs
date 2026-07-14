using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical diagnostics for FrameworkRuntimeHost Player Actor preparation composition.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.5 runtime-host Player Actor preparation diagnostics snapshot.")]
    public sealed class PlayerActorPreparationRuntimeHostSnapshot
    {
        internal PlayerActorPreparationRuntimeHostSnapshot(
            bool initialized,
            string sessionContextId,
            int registeredHostCount,
            int joinRequestCount,
            int preparationRequestCount,
            LocalPlayerJoinStatus lastJoinStatus,
            PlayerActorPreparationSnapshot preparation,
            string diagnostic)
        {
            IsInitialized = initialized;
            SessionContextId = sessionContextId ?? string.Empty;
            RegisteredHostCount = registeredHostCount;
            JoinRequestCount = joinRequestCount;
            PreparationRequestCount = preparationRequestCount;
            LastJoinStatus = lastJoinStatus;
            Preparation = preparation;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool IsInitialized { get; }
        public string SessionContextId { get; }
        public int RegisteredHostCount { get; }
        public int JoinRequestCount { get; }
        public int PreparationRequestCount { get; }
        public LocalPlayerJoinStatus LastJoinStatus { get; }
        public PlayerActorPreparationSnapshot Preparation { get; }
        public string Diagnostic { get; }

        public int PreparedCount => Preparation?.PreparedCount ?? 0;
        public int ReleaseFailedCount => Preparation?.ReleaseFailedCount ?? 0;
        public int RetainedReleaseFailureCount => Preparation?.RetainedReleaseFailureCount ?? 0;

        internal static PlayerActorPreparationRuntimeHostSnapshot Unavailable(
            string diagnostic)
        {
            string resolved = string.IsNullOrWhiteSpace(diagnostic)
                ? "Player Actor preparation runtime host is unavailable."
                : diagnostic.Trim();
            return new PlayerActorPreparationRuntimeHostSnapshot(
                false,
                string.Empty,
                0,
                0,
                0,
                LocalPlayerJoinStatus.None,
                new PlayerActorPreparationSnapshot(
                    string.Empty,
                    0,
                    Array.Empty<PlayerActorPreparationSummary>(),
                    Array.Empty<PlayerActorMaterializationSnapshot>(),
                    PlayerActorPreparationStatus.RejectedRuntimeUnavailable,
                    resolved),
                resolved);
        }
    }
}
