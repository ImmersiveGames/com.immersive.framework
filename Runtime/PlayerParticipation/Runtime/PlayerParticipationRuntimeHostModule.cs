using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
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
        "P3F/P3H/P3J host-scoped composition adapter for Session Player participation, Actor selection and preparation runtime.")]
    internal sealed class PlayerParticipationRuntimeHostModule : MonoBehaviour
    {
        private PlayerParticipationRuntimeContext _runtimeContext;
        private EffectivePlayerSessionConfiguration _effectiveConfiguration;
        private PlayerParticipationOperationResult _initializationResult;

        internal bool IsInitialized => _runtimeContext != null &&
            _initializationResult != null &&
            _initializationResult.Succeeded;

        internal PlayerParticipationOperationResult InitializationResult => _initializationResult;

        internal EffectivePlayerSessionConfiguration EffectiveConfiguration => _effectiveConfiguration;

        internal PlayerActorSelectionDuplicatePolicy ActorSelectionDuplicatePolicy =>
            _runtimeContext != null
                ? _runtimeContext.CreateSnapshot().ActorSelectionDuplicatePolicy
                : PlayerActorSelectionDuplicatePolicy.Unspecified;

        internal static PlayerParticipationRuntimeHostModule Attach(
            FrameworkRuntimeHost runtimeHost,
            EffectivePlayerSessionConfiguration effectiveConfiguration,
            PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy,
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

            result = module.Initialize(
                effectiveConfiguration,
                actorSelectionDuplicatePolicy,
                source,
                reason);
            return module;
        }

        internal PlayerParticipationOperationResult Initialize(
            EffectivePlayerSessionConfiguration targetEffectiveConfiguration,
            PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy,
            string source,
            string reason)
        {
            if (_initializationResult != null)
            {
                return _initializationResult;
            }

            if (targetEffectiveConfiguration == null)
            {
                _runtimeContext = null;
                _initializationResult = PlayerParticipationRuntimeContext.TryCreateWithEffectiveConfiguration(
                    null,
                    actorSelectionDuplicatePolicy,
                    source,
                    reason,
                    out _);
                return _initializationResult;
            }

            _initializationResult = PlayerParticipationRuntimeContext.TryCreateWithEffectiveConfiguration(
                targetEffectiveConfiguration,
                actorSelectionDuplicatePolicy,
                source,
                reason,
                out _runtimeContext);
            if (_initializationResult.Succeeded)
            {
                _effectiveConfiguration = targetEffectiveConfiguration;
            }

            return _initializationResult;
        }

        internal bool TryGetRuntimeContext(out PlayerParticipationRuntimeContext context)
        {
            context = _runtimeContext;
            return context != null;
        }

        internal bool TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
        {
            if (_runtimeContext == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    _initializationResult != null
                        ? _initializationResult.Status
                        : PlayerParticipationOperationStatus.None,
                    _initializationResult != null
                        ? _initializationResult.Message
                        : "Player participation runtime module is not initialized.");
                return false;
            }

            snapshot = _runtimeContext.CreateSnapshot();
            return true;
        }

        internal bool TryGetSlotSnapshot(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            if (_runtimeContext == null)
            {
                snapshot = default;
                return false;
            }

            return _runtimeContext.TryGetSlotSnapshot(playerSlotId, out snapshot);
        }

        internal bool TryGetActorSelection(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            if (_runtimeContext == null)
            {
                snapshot = default;
                return false;
            }

            return _runtimeContext.TryGetActorSelection(playerSlotId, out snapshot);
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return TryGetPreparationRuntime(out PlayerActorPreparationRuntimeHostModule preparation)
                ? preparation.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    "Player Actor preparation runtime module is not initialized.");
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return TryGetPreparationRuntime(out PlayerActorPreparationRuntimeHostModule preparation)
                ? preparation.TryReplaceActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection",
                    request,
                    "Player Actor preparation runtime module is not initialized.");
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return TryGetPreparationRuntime(out PlayerActorPreparationRuntimeHostModule preparation)
                ? preparation.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    "Player Actor preparation runtime module is not initialized.");
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            if (TryGetPreparationRuntime(out PlayerActorPreparationRuntimeHostModule preparation))
            {
                return preparation.TrySelectDefaultActor(
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
                "Player Actor preparation runtime module is not initialized.");
        }

        private bool TryGetPreparationRuntime(
            out PlayerActorPreparationRuntimeHostModule preparation)
        {
            preparation = GetComponent<PlayerActorPreparationRuntimeHostModule>();
            return preparation != null && preparation.IsReady;
        }

        private void OnDestroy()
        {
            _runtimeContext = null;
            _effectiveConfiguration = null;
            _initializationResult = null;
        }
    }

    /// <summary>
    /// Narrow typed access that requires an explicit FrameworkRuntimeHost reference.
    /// This is local same-object composition, not a global service locator.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3F/P3H/P3J typed access from FrameworkRuntimeHost to its scoped Player participation module.")]
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
