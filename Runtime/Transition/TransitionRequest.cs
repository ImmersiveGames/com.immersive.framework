using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Minimal request passed through transition orchestration before/after framework flow changes.
    /// The request describes Route/Activity intent; it does not execute scene, Activity, UI or visual behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24B Transition contract request; no visual or lifecycle ownership.")]
    public readonly struct TransitionRequest : IEquatable<TransitionRequest>
    {
        public TransitionRequest(
            TransitionOperationId operationId,
            TransitionScope scope,
            TransitionPhase phase,
            string source,
            string reason,
            RouteAsset fromRoute,
            RouteAsset toRoute,
            ActivityAsset fromActivity,
            ActivityAsset toActivity)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition request requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionScope), scope) || scope == TransitionScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Transition request scope must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionPhase), phase) || phase == TransitionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Transition request phase must be explicit.");
            }

            OperationId = operationId;
            Scope = scope;
            Phase = phase;
            Source = Normalize(source, "Unknown");
            Reason = Normalize(reason, "None");
            FromRoute = fromRoute;
            ToRoute = toRoute;
            FromActivity = fromActivity;
            ToActivity = toActivity;
        }

        public TransitionOperationId OperationId { get; }

        public TransitionScope Scope { get; }

        public TransitionPhase Phase { get; }

        public string Source { get; }

        public string Reason { get; }

        public RouteAsset FromRoute { get; }

        public RouteAsset ToRoute { get; }

        public ActivityAsset FromActivity { get; }

        public ActivityAsset ToActivity { get; }

        public TransitionKind Kind
        {
            get
            {
                switch (Scope)
                {
                    case TransitionScope.Startup:
                        return TransitionKind.RouteStartup;
                    case TransitionScope.Route:
                        return TransitionKind.RouteSwitch;
                    case TransitionScope.Activity:
                        return TransitionKind.ActivitySwitch;
                    case TransitionScope.ActivityClear:
                        return TransitionKind.ActivityClear;
                    default:
                        return TransitionKind.Unknown;
                }
            }
        }

        public bool IsValid => OperationId.IsValid
            && Scope != TransitionScope.Unknown
            && Phase != TransitionPhase.Unknown
            && Kind != TransitionKind.Unknown;

        public bool Equals(TransitionRequest other)
        {
            return OperationId.Equals(other.OperationId)
                && Scope == other.Scope
                && Phase == other.Phase
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SameRoute(FromRoute, other.FromRoute)
                && SameRoute(ToRoute, other.ToRoute)
                && SameActivity(FromActivity, other.FromActivity)
                && SameActivity(ToActivity, other.ToActivity);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)Phase;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ RouteHash(FromRoute);
                hashCode = hashCode * 397 ^ RouteHash(ToRoute);
                hashCode = hashCode * 397 ^ ActivityHash(FromActivity);
                hashCode = hashCode * 397 ^ ActivityHash(ToActivity);
                return hashCode;
            }
        }

        private static bool SameRoute(RouteAsset left, RouteAsset right) =>
            left == null ? right == null : left.HasSameIdentity(right);

        private static bool SameActivity(ActivityAsset left, ActivityAsset right) =>
            left == null ? right == null : left.HasSameIdentity(right);

        private static int RouteHash(RouteAsset route) =>
            route != null && route.HasValidRouteId ? route.RouteId.GetHashCode() : 0;

        private static int ActivityHash(ActivityAsset activity) =>
            activity != null && activity.HasValidActivityId ? activity.ActivityId.GetHashCode() : 0;

        public static TransitionRequest Before(
            TransitionOperationId operationId,
            TransitionScope scope,
            string source,
            string reason,
            RouteAsset fromRoute,
            RouteAsset toRoute,
            ActivityAsset fromActivity,
            ActivityAsset toActivity)
        {
            return new TransitionRequest(
                operationId,
                scope,
                TransitionPhase.OperationOpened,
                source,
                reason,
                fromRoute,
                toRoute,
                fromActivity,
                toActivity);
        }

        public static TransitionRequest After(
            TransitionOperationId operationId,
            TransitionScope scope,
            string source,
            string reason,
            RouteAsset fromRoute,
            RouteAsset toRoute,
            ActivityAsset fromActivity,
            ActivityAsset toActivity)
        {
            return new TransitionRequest(
                operationId,
                scope,
                TransitionPhase.OperationClosed,
                source,
                reason,
                fromRoute,
                toRoute,
                fromActivity,
                toActivity);
        }

        public string ToDiagnosticString()
        {
            return $"operation='{OperationId.StableText}' scope='{Scope}' kind='{Kind}' phase='{Phase}' source='{Source}' reason='{Reason}' fromRoute='{RouteName(FromRoute)}' toRoute='{RouteName(ToRoute)}' fromActivity='{ActivityName(FromActivity)}' toActivity='{ActivityName(ToActivity)}'";
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }

        private static string RouteName(RouteAsset route)
        {
            return route != null ? Normalize(route.RouteName, "<unnamed>") : "<none>";
        }

        private static string ActivityName(ActivityAsset activity)
        {
            return activity != null ? Normalize(activity.ActivityName, "<unnamed>") : "<none>";
        }
    }
}
