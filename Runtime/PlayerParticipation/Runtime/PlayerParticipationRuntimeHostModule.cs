using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Host-scoped composition adapter for the plain C# PlayerParticipationRuntimeContext.
    /// The component shares the FrameworkRuntimeHost GameObject and lifetime; it is not
    /// the domain authority and exposes no static/global lookup.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3F/P3H host-scoped composition adapter for Session Player participation and Actor selection runtime.")]
    internal sealed class PlayerParticipationRuntimeHostModule : MonoBehaviour
    {
        private PlayerParticipationRuntimeContext runtimeContext;
        private PlayerParticipationOperationResult initializationResult;

        internal bool IsInitialized => runtimeContext != null &&
            initializationResult != null &&
            initializationResult.Succeeded;

        internal PlayerParticipationOperationResult InitializationResult => initializationResult;

        internal PlayerActorSelectionPolicyProfile ActorSelectionPolicyProfile =>
            runtimeContext != null
                ? runtimeContext.CreateSnapshot().ActorSelectionPolicyProfile
                : null;

        internal static PlayerParticipationRuntimeHostModule Attach(
            FrameworkRuntimeHost runtimeHost,
            GameApplicationAsset gameApplication,
            string source,
            string reason,
            out PlayerParticipationOperationResult result)
        {
            if (runtimeHost == null)
            {
                throw new ArgumentNullException(nameof(runtimeHost));
            }

            PlayerParticipationRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerParticipationRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<PlayerParticipationRuntimeHostModule>();
            }

            result = module.Initialize(gameApplication, source, reason);
            return module;
        }

        internal PlayerParticipationOperationResult Initialize(
            GameApplicationAsset gameApplication,
            string source,
            string reason)
        {
            if (initializationResult != null)
            {
                return initializationResult;
            }

            if (gameApplication == null)
            {
                runtimeContext = null;
                initializationResult = PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy(
                    null,
                    0,
                    false,
                    null,
                    source,
                    reason,
                    out _);
                return initializationResult;
            }

            initializationResult = PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy(
                gameApplication.LocalPlayerSlots,
                gameApplication.LocalPlayerSlotCount,
                initialJoiningOpen: false,
                gameApplication.PlayerActorSelectionPolicyProfile,
                source,
                reason,
                out runtimeContext);
            return initializationResult;
        }

        internal bool TryGetRuntimeContext(out PlayerParticipationRuntimeContext context)
        {
            context = runtimeContext;
            return context != null;
        }

        internal bool TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
        {
            if (runtimeContext == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    initializationResult != null
                        ? initializationResult.Status
                        : PlayerParticipationOperationStatus.None,
                    initializationResult != null
                        ? initializationResult.Message
                        : "Player participation runtime module is not initialized.");
                return false;
            }

            snapshot = runtimeContext.CreateSnapshot();
            return true;
        }

        internal bool TryGetSlotSnapshot(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            if (runtimeContext == null)
            {
                snapshot = default;
                return false;
            }

            return runtimeContext.TryGetSlotSnapshot(playerSlotId, out snapshot);
        }

        internal bool TryGetActorSelection(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            if (runtimeContext == null)
            {
                snapshot = default;
                return false;
            }

            return runtimeContext.TryGetActorSelection(playerSlotId, out snapshot);
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return runtimeContext != null
                ? runtimeContext.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    "Player participation runtime module is not initialized.");
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return runtimeContext != null
                ? runtimeContext.TryReplaceActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection",
                    request,
                    "Player participation runtime module is not initialized.");
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return runtimeContext != null
                ? runtimeContext.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    "Player participation runtime module is not initialized.");
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            if (runtimeContext != null)
            {
                return runtimeContext.TrySelectDefaultActor(
                    playerSlotId,
                    expectedSelectionRevision,
                    source,
                    reason);
            }

            return PlayerActorSelectionResult.RuntimeUnavailable(
                "SelectDefaultActor",
                new PlayerActorSelectionRequest(
                    playerSlotId,
                    null,
                    source,
                    reason,
                    expectedSelectionRevision),
                "Player participation runtime module is not initialized.");
        }

        private void OnDestroy()
        {
            runtimeContext = null;
            initializationResult = null;
        }
    }

    /// <summary>
    /// Narrow typed access that requires an explicit FrameworkRuntimeHost reference.
    /// This is local same-object composition, not a global service locator.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3F/P3H typed access from FrameworkRuntimeHost to its scoped Player participation module.")]
    internal static class FrameworkRuntimeHostPlayerParticipationExtensions
    {
        internal static bool TryGetPlayerParticipationRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerParticipationRuntimeContext runtimeContext)
        {
            runtimeContext = null;
            if (runtimeHost == null)
            {
                return false;
            }

            PlayerParticipationRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerParticipationRuntimeHostModule>();
            return module != null && module.TryGetRuntimeContext(out runtimeContext);
        }

        internal static bool TryGetPlayerParticipationSnapshot(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerParticipationSnapshot snapshot)
        {
            if (runtimeHost == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.None,
                    "FrameworkRuntimeHost is missing.");
                return false;
            }

            PlayerParticipationRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerParticipationRuntimeHostModule>();
            if (module == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.None,
                    "FrameworkRuntimeHost has no Player participation runtime module.");
                return false;
            }

            return module.TryGetSnapshot(out snapshot);
        }
    }
}
