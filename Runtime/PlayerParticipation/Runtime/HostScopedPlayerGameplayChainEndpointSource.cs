using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Camera;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Resolves one Slot's gameplay endpoints from exact P3J physical evidence.
    /// Gate adapters are resolved from the stable Local Player Host for that Slot,
    /// so this source supports multiple local Players without a fixed shared adapter.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7F host-scoped multi-Slot Player gameplay endpoint source.")]
    internal sealed class HostScopedPlayerGameplayChainEndpointSource :
        IPlayerGameplayChainHandoffEndpointSource
    {
        private readonly FrameworkRuntimeHost runtimeHost;
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly PlayerGameplayCameraRequiredness missingCameraRequiredness;

        internal HostScopedPlayerGameplayChainEndpointSource(
            FrameworkRuntimeHost runtimeHost,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerGameplayCameraRequiredness missingCameraRequiredness =
                PlayerGameplayCameraRequiredness.Optional)
        {
            this.runtimeHost = runtimeHost ??
                throw new ArgumentNullException(nameof(runtimeHost));
            this.preparationModule = preparationModule ??
                throw new ArgumentNullException(nameof(preparationModule));
            this.missingCameraRequiredness = missingCameraRequiredness;
        }

        public bool TryResolveGameplayEndpoints(
            PlayerActorPreparationSummary preparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out UnityPlayerInputGateAdapter gateAdapter,
            out PlayerGameplayCameraAuthoring cameraAuthoring,
            out PlayerGameplayCameraRequiredness cameraRequiredness,
            out CameraOutputSessionBinding outputSession,
            out string issue)
        {
            host = null;
            actorDeclaration = null;
            gateAdapter = null;
            cameraAuthoring = null;
            cameraRequiredness = missingCameraRequiredness;
            outputSession = null;
            issue = string.Empty;

            if (!preparation.IsValid ||
                !preparation.IsPrepared ||
                !preparation.Token.IsValid)
            {
                issue =
                    "Player gameplay endpoints require exact current prepared P3J evidence.";
                return false;
            }

            if (!preparationModule.TryGetPreparedPhysicalEvidence(
                    preparation.PlayerSlotId,
                    preparation.Token,
                    out host,
                    out _,
                    out actorDeclaration,
                    out _,
                    out issue))
            {
                return false;
            }

            UnityPlayerInputGateAdapter[] gateAdapters =
                host.GetComponents<UnityPlayerInputGateAdapter>();
            if (gateAdapters.Length != 1 ||
                gateAdapters[0] == null ||
                !ReferenceEquals(gateAdapters[0].PlayerInput, host.PlayerInput))
            {
                issue =
                    $"Stable Local Player Host '{host.name}' requires exactly one UnityPlayerInputGateAdapter targeting its own PlayerInput. Found '{gateAdapters.Length}'.";
                return false;
            }

            gateAdapter = gateAdapters[0];

            PlayerGameplayCameraAuthoring[] cameraAuthorings =
                actorDeclaration.GetComponentsInChildren<PlayerGameplayCameraAuthoring>(
                    true);
            if (cameraAuthorings.Length > 1)
            {
                issue =
                    $"Prepared Actor '{actorDeclaration.ActorId.StableText}' requires at most one PlayerGameplayCameraAuthoring. Found '{cameraAuthorings.Length}'.";
                return false;
            }

            cameraAuthoring =
                cameraAuthorings.Length == 1 ? cameraAuthorings[0] : null;
            cameraRequiredness = cameraAuthoring != null
                ? cameraAuthoring.Requiredness
                : missingCameraRequiredness;

            if (cameraAuthoring != null &&
                !runtimeHost.TryGetPlayerGameplayCameraOutputSession(
                    out outputSession,
                    out issue))
            {
                return false;
            }

            if (cameraAuthoring == null)
            {
                runtimeHost.TryGetPlayerGameplayCameraOutputSession(
                    out outputSession,
                    out _);
            }

            return true;
        }
    }
}
