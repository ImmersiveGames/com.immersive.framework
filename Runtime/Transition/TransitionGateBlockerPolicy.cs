using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Passive relationship between a logical Transition operation and Gate blockers.
    /// This policy describes which blocker a running Transition would expose; it does not register, apply,
    /// release or own Gate state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18D passive Transition-to-Gate blocker policy; no runtime registry or flow mutation.")]
    public static class TransitionGateBlockerPolicy
    {
        public const string PolicySource = "F18D.TransitionGateBlocker";
        public const string LifecycleRequestBlockerId = "transition-operation-in-flight";
        public const string InputAcceptanceBlockerId = "transition-input-acceptance";
        public const string InteractionAcceptanceBlockerId = "transition-interaction-acceptance";
        public const string GameplayActionBlockerId = "transition-gameplay-action";

        public static GateBlocker CreateLifecycleRequestBlocker(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Gate blocker requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), kind) || kind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Transition Gate blocker kind must be explicit.");
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(TransitionGateBlockerPolicy));

            string normalizedReason = reason.NormalizeTextOrFallback($"Transition operation is in flight. operation='{operationId.StableText}' kind='{kind}'.");

            return GateBlocker.ForAnyOwner(
                LifecycleRequestBlockerId,
                GateScope.GameFlow,
                GateDomain.LifecycleRequest,
                normalizedSource,
                normalizedReason,
                PolicySource);
        }

        public static GateBlocker CreateInputAcceptanceBlocker(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason)
        {
            ValidateOperation(operationId, kind);

            return GateBlocker.ForAnyOwner(
                InputAcceptanceBlockerId,
                GateScope.Input,
                GateDomain.InputAcceptance,
                source.NormalizeTextOrFallback(nameof(TransitionGateBlockerPolicy)),
                reason.NormalizeTextOrFallback($"Transition blocks input acceptance. operation='{operationId.StableText}' kind='{kind}'."),
                PolicySource);
        }

        public static GateBlocker CreateInteractionAcceptanceBlocker(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason)
        {
            ValidateOperation(operationId, kind);

            return GateBlocker.ForAnyOwner(
                InteractionAcceptanceBlockerId,
                GateScope.Interaction,
                GateDomain.InteractionAcceptance,
                source.NormalizeTextOrFallback(nameof(TransitionGateBlockerPolicy)),
                reason.NormalizeTextOrFallback($"Transition blocks interaction acceptance. operation='{operationId.StableText}' kind='{kind}'."),
                PolicySource);
        }

        public static GateBlocker CreateGameplayActionBlocker(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason)
        {
            ValidateOperation(operationId, kind);

            return GateBlocker.ForAnyOwner(
                GameplayActionBlockerId,
                GateScope.Gameplay,
                GateDomain.GameplayAction,
                source.NormalizeTextOrFallback(nameof(TransitionGateBlockerPolicy)),
                reason.NormalizeTextOrFallback($"Transition blocks gameplay actions. operation='{operationId.StableText}' kind='{kind}'."),
                PolicySource);
        }

        public static GateSnapshot CreateRunningSnapshot(
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionGateMode mode,
            string source,
            string reason)
        {
            if (!BlocksAny(mode))
            {
                return GateSnapshot.Empty();
            }

            var blockers = new System.Collections.Generic.List<GateBlocker>(4);
            if (BlocksLifecycleRequests(mode))
            {
                blockers.Add(CreateLifecycleRequestBlocker(operationId, kind, source, reason));
            }

            if (BlocksInputAcceptance(mode))
            {
                blockers.Add(CreateInputAcceptanceBlocker(operationId, kind, source, reason));
            }

            if (BlocksInteractionAcceptance(mode))
            {
                blockers.Add(CreateInteractionAcceptanceBlocker(operationId, kind, source, reason));
            }

            if (BlocksGameplayAction(mode))
            {
                blockers.Add(CreateGameplayActionBlocker(operationId, kind, source, reason));
            }

            return new GateSnapshot(blockers);
        }

        public static bool BlocksAny(TransitionGateMode mode)
        {
            return mode is TransitionGateMode.LifecycleRequestsOnly
                or TransitionGateMode.InputAndInteraction
                or TransitionGateMode.InputInteractionAndGameplay;
        }

        public static bool BlocksLifecycleRequests(TransitionGateMode mode)
        {
            return BlocksAny(mode);
        }

        public static bool BlocksInputAcceptance(TransitionGateMode mode)
        {
            return mode is TransitionGateMode.InputAndInteraction
                or TransitionGateMode.InputInteractionAndGameplay;
        }

        public static bool BlocksInteractionAcceptance(TransitionGateMode mode)
        {
            return mode is TransitionGateMode.InputAndInteraction
                or TransitionGateMode.InputInteractionAndGameplay;
        }

        public static bool BlocksGameplayAction(TransitionGateMode mode)
        {
            return mode == TransitionGateMode.InputInteractionAndGameplay;
        }

        public static GateSnapshot CreateReleasedSnapshot()
        {
            return GateSnapshot.Empty();
        }

        private static void ValidateOperation(TransitionOperationId operationId, TransitionKind kind)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Gate blocker requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), kind) || kind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Transition Gate blocker kind must be explicit.");
            }
        }
    }
}
