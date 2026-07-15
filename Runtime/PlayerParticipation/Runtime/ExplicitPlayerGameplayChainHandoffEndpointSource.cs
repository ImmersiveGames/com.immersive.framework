using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit same-host endpoint source. It starts from the exact current P3J preparation
    /// token and only inspects components beneath that typed physical Actor/host evidence.
    /// It performs no global, name, tag or scene lookup.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7D explicit runtime-host endpoint source for reversible gameplay handoff.")]
    internal sealed class ExplicitPlayerGameplayChainHandoffEndpointSource :
        IPlayerGameplayChainHandoffEndpointSource
    {
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly UnityPlayerInputGateAdapter gateAdapter;
        private readonly CameraOutputSessionBinding outputSession;
        private readonly PlayerGameplayCameraRequiredness missingCameraRequiredness;

        internal ExplicitPlayerGameplayChainHandoffEndpointSource(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            UnityPlayerInputGateAdapter gateAdapter,
            CameraOutputSessionBinding outputSession,
            PlayerGameplayCameraRequiredness missingCameraRequiredness =
                PlayerGameplayCameraRequiredness.Optional)
        {
            this.preparationModule = preparationModule ??
                throw new ArgumentNullException(nameof(preparationModule));
            this.gateAdapter = gateAdapter != null
                ? gateAdapter
                : throw new ArgumentNullException(nameof(gateAdapter));
            this.outputSession = outputSession;
            this.missingCameraRequiredness = missingCameraRequiredness;
        }

        public bool TryResolveGameplayEndpoints(
            PlayerActorPreparationSummary preparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out UnityPlayerInputGateAdapter resolvedGateAdapter,
            out PlayerGameplayCameraAuthoring cameraAuthoring,
            out PlayerGameplayCameraRequiredness cameraRequiredness,
            out CameraOutputSessionBinding resolvedOutputSession,
            out string issue)
        {
            host = null;
            actorDeclaration = null;
            resolvedGateAdapter = null;
            cameraAuthoring = null;
            cameraRequiredness = missingCameraRequiredness;
            resolvedOutputSession = outputSession;
            issue = string.Empty;

            if (!preparation.IsValid || !preparation.IsPrepared ||
                !preparation.Token.IsValid)
            {
                issue = "Gameplay handoff endpoints require exact current prepared P3J evidence.";
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

            if (gateAdapter == null ||
                !ReferenceEquals(gateAdapter.PlayerInput, host.PlayerInput))
            {
                issue = "Gameplay handoff Gate adapter does not target the exact stable-host PlayerInput.";
                return false;
            }

            PlayerGameplayCameraAuthoring[] cameras =
                actorDeclaration.GetComponentsInChildren<PlayerGameplayCameraAuthoring>(true);
            if (cameras.Length > 1)
            {
                issue =
                    $"Current prepared Actor requires at most one PlayerGameplayCameraAuthoring. Found '{cameras.Length}'.";
                return false;
            }

            cameraAuthoring = cameras.Length == 1 ? cameras[0] : null;
            cameraRequiredness = cameraAuthoring != null
                ? cameraAuthoring.Requiredness
                : missingCameraRequiredness;
            resolvedGateAdapter = gateAdapter;
            return true;
        }
    }
}
