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

        public static GateSnapshot CreateRunningSnapshot(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason)
        {
            return new GateSnapshot(new[]
            {
                CreateLifecycleRequestBlocker(operationId, kind, source, reason)
            });
        }

        public static GateSnapshot CreateReleasedSnapshot()
        {
            return GateSnapshot.Empty();
        }
    }
}
