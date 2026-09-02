using System;
using System.Collections.Generic;
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
        IPlayerGameplayCurrentContextEndpointSource
    {
        private readonly FrameworkRuntimeHost _runtimeHost;
        private readonly PlayerActorPreparationRuntimeHostModule _preparationModule;
        private readonly PlayerGameplayCameraRequiredness _missingCameraRequiredness;

        internal HostScopedPlayerGameplayChainEndpointSource(
            FrameworkRuntimeHost runtimeHost,
            PlayerActorPreparationRuntimeHostModule preparationModule,
            PlayerGameplayCameraRequiredness missingCameraRequiredness =
                PlayerGameplayCameraRequiredness.Optional)
        {
            this._runtimeHost = runtimeHost ??
                throw new ArgumentNullException(nameof(runtimeHost));
            this._preparationModule = preparationModule ??
                throw new ArgumentNullException(nameof(preparationModule));
            this._missingCameraRequiredness = missingCameraRequiredness;
        }

        public bool TryResolveGameplayEndpoints(
            PlayerActorPreparationSummary preparation,
            out LocalPlayerHostAuthoring host,
            out PlayerActorDeclaration actorDeclaration,
            out UnityPlayerInputGateAdapter gateAdapter,
            out PlayerGameplayInputReader gameplayInputReader,
            out PlayerGameplayCameraAuthoring cameraAuthoring,
            out PlayerGameplayCameraRequiredness cameraRequiredness,
            out CameraOutputAuthoring outputSession,
            out string issue)
        {
            host = null;
            actorDeclaration = null;
            gateAdapter = null;
            gameplayInputReader = null;
            cameraAuthoring = null;
            cameraRequiredness = _missingCameraRequiredness;
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

            if (!_preparationModule.TryGetPreparedPhysicalEvidence(
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

            IReadOnlyList<UnityPlayerInputGateAdapter> gateAdapters =
                ResolveHostOwnedGateAdapters(host);
            if (gateAdapters.Count != 1 ||
                gateAdapters[0] == null ||
                !ReferenceEquals(gateAdapters[0].PlayerInput, host.PlayerInput))
            {
                issue =
                    $"Stable Local Player Host '{host.name}' requires exactly one host-owned UnityPlayerInputGateAdapter targeting its own PlayerInput. Found '{gateAdapters.Count}'.";
                return false;
            }

            gateAdapter = gateAdapters[0];
            if (!gateAdapter.TryBindInputGateRuntime(
                    _runtimeHost,
                    out issue))
            {
                issue =
                    $"Stable Local Player Host '{host.name}' Gate adapter could not bind to the canonical FrameworkRuntimeHost Gate authority. {issue}";
                gateAdapter = null;
                return false;
            }

            PlayerGameplayInputReader[] gameplayInputReaders =
                actorDeclaration.GetComponentsInChildren<PlayerGameplayInputReader>(
                    true);
            if (gameplayInputReaders.Length > 1)
            {
                issue =
                    $"Prepared Actor '{actorDeclaration.ActorId.StableText}' requires at most one PlayerGameplayInputReader. Found '{gameplayInputReaders.Length}'.";
                return false;
            }

            gameplayInputReader = gameplayInputReaders.Length == 1
                ? gameplayInputReaders[0]
                : null;

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
                : _missingCameraRequiredness;

            if (cameraAuthoring != null &&
                !_runtimeHost.TryGetPlayerGameplayCameraOutputSession(
                    out outputSession,
                    out issue))
            {
                return false;
            }

            if (cameraAuthoring == null)
            {
                _runtimeHost.TryGetPlayerGameplayCameraOutputSession(
                    out outputSession,
                    out _);
            }

            return true;
        }

        private static IReadOnlyList<UnityPlayerInputGateAdapter>
            ResolveHostOwnedGateAdapters(LocalPlayerHostAuthoring host)
        {
            var ownedGateAdapters =
                new List<UnityPlayerInputGateAdapter>();
            UnityPlayerInputGateAdapter[] hierarchyGateAdapters =
                host.GetComponentsInChildren<UnityPlayerInputGateAdapter>(
                    true);

            for (int index = 0;
                 index < hierarchyGateAdapters.Length;
                 index++)
            {
                UnityPlayerInputGateAdapter gateAdapter =
                    hierarchyGateAdapters[index];
                if (gateAdapter == null ||
                    !ReferenceEquals(
                        gateAdapter.GetComponentInParent<
                            LocalPlayerHostAuthoring>(true),
                        host))
                {
                    continue;
                }

                ownedGateAdapters.Add(gateAdapter);
            }

            return ownedGateAdapters;
        }
    }
}
