using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Internal. Minimal request-admission evaluator used by F17C to route existing
    /// in-flight lifecycle/request guards through Gate decisions without creating a global Gate manager.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F17C request admission helper; no registry, queue, UI or lifecycle ownership.")]
    internal static class GateRequestAdmission
    {
        internal const string PolicySource = "F17C.RequestAdmissionGate";

        internal static GateEvaluationResult EvaluateLifecycleRequest(
            string subject,
            string source,
            string reason,
            bool routeRequestInFlight,
            bool activityRequestInFlight,
            bool cycleResetRequestInFlight,
            bool objectResetRequestInFlight)
        {
            IReadOnlyList<GateBlocker> blockers = BuildLifecycleRequestBlockers(
                routeRequestInFlight,
                activityRequestInFlight,
                cycleResetRequestInFlight,
                objectResetRequestInFlight);

            var snapshot = new GateSnapshot(blockers);
            return snapshot.Evaluate(
                GateScope.GameFlow,
                GateDomain.LifecycleRequest,
                default,
                NormalizeSubject(subject),
                source,
                reason,
                PolicySource);
        }

        internal static string FormatBlockedMessage(string requestName, GateEvaluationResult evaluation)
        {
            string resolvedRequestName = requestName.NormalizeTextOrFallback("Framework Request");

            return $"{resolvedRequestName} ignored because Gate blocked lifecycle request admission. {evaluation.ToDiagnosticString()}";
        }

        private static IReadOnlyList<GateBlocker> BuildLifecycleRequestBlockers(
            bool routeRequestInFlight,
            bool activityRequestInFlight,
            bool cycleResetRequestInFlight,
            bool objectResetRequestInFlight)
        {
            var blockers = new List<GateBlocker>(4);

            if (routeRequestInFlight)
            {
                blockers.Add(CreateLifecycleBlocker(
                    "route-request-in-flight",
                    "GameFlowRuntime",
                    "Route request is already in flight."));
            }

            if (activityRequestInFlight)
            {
                blockers.Add(CreateLifecycleBlocker(
                    "activity-request-in-flight",
                    "GameFlowRuntime",
                    "Activity request is already in flight."));
            }

            if (cycleResetRequestInFlight)
            {
                blockers.Add(CreateLifecycleBlocker(
                    "cycle-reset-request-in-flight",
                    "GameFlowRuntime",
                    "Cycle Reset request is already in flight."));
            }

            if (objectResetRequestInFlight)
            {
                blockers.Add(CreateLifecycleBlocker(
                    "object-reset-request-in-flight",
                    "FrameworkRuntimeHost",
                    "Object Reset request is already in flight."));
            }

            return blockers;
        }

        private static GateBlocker CreateLifecycleBlocker(string blockerId, string source, string reason)
        {
            return GateBlocker.ForAnyOwner(
                blockerId,
                GateScope.GameFlow,
                GateDomain.LifecycleRequest,
                source,
                reason,
                PolicySource);
        }

        private static string NormalizeSubject(string subject)
        {
            return subject.NormalizeTextOrFallback("FrameworkRequest");
        }
    }
}
