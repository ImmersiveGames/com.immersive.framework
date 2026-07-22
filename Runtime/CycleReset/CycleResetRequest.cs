using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive request for one Route or Activity Cycle Reset.
    /// It carries active lifecycle context only; it does not reset Unity objects, reload scenes, release content, restore snapshots or return pooled objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset request contract; Route/Activity cycle only, no physical reset.")]
    public readonly struct CycleResetRequest : IEquatable<CycleResetRequest>
    {
        public CycleResetRequest(
            CycleResetScope scope,
            RouteAsset activeRoute,
            ActivityAsset activeActivity,
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(CycleResetScope), scope) || scope == CycleResetScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Cycle Reset request scope must be Route or Activity.");
            }

            if (activeRoute == null)
            {
                throw new ArgumentNullException(nameof(activeRoute), "Cycle Reset request requires an active Route.");
            }

            if (scope == CycleResetScope.Activity && activeActivity == null)
            {
                throw new ArgumentNullException(nameof(activeActivity), "Activity Cycle Reset request requires an active Activity.");
            }

            if (!policy.IsValid)
            {
                throw new ArgumentException("Cycle Reset request requires a defined policy.", nameof(policy));
            }

            Scope = scope;
            ActiveRoute = activeRoute;
            ActiveActivity = activeActivity;
            Policy = policy;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public CycleResetScope Scope { get; }

        public RouteAsset ActiveRoute { get; }

        public ActivityAsset ActiveActivity { get; }

        public CycleResetPolicy Policy { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRouteReset => Scope == CycleResetScope.Route;

        public bool IsActivityReset => Scope == CycleResetScope.Activity;

        public bool HasActiveRoute => ActiveRoute != null;

        public bool HasActiveActivity => ActiveActivity != null;

        public bool IncludesActiveActivity => IsActivityReset || IsRouteReset && Policy.IncludeActiveActivity && HasActiveActivity;

        public bool AllowsNoParticipants => Policy.AllowNoParticipants;

        public string ActiveRouteName => ActiveRoute != null ? ActiveRoute.RouteName : string.Empty;

        public string ActiveActivityName => ActiveActivity != null ? ActiveActivity.ActivityName : string.Empty;

        public bool IsValid => Scope != CycleResetScope.Unknown
            && ActiveRoute != null
            && (Scope != CycleResetScope.Activity || ActiveActivity != null)
            && Policy.IsValid;

        public bool AcceptsParticipantScope(CycleResetScope participantScope)
        {
            if (participantScope == CycleResetScope.Route)
            {
                return IsRouteReset;
            }

            if (participantScope == CycleResetScope.Activity)
            {
                return IsActivityReset || IsRouteReset && IncludesActiveActivity;
            }

            return false;
        }

        public bool Equals(CycleResetRequest other)
        {
            return Scope == other.Scope
                && SameRoute(ActiveRoute, other.ActiveRoute)
                && SameActivity(ActiveActivity, other.ActiveActivity)
                && Policy.Equals(other.Policy)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Scope;
                hashCode = hashCode * 397 ^ (ActiveRoute != null && ActiveRoute.HasValidRouteId ? ActiveRoute.RouteId.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (ActiveActivity != null && ActiveActivity.HasValidActivityId ? ActiveActivity.ActivityId.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ Policy.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        private static bool SameRoute(RouteAsset left, RouteAsset right) =>
            left == null ? right == null : left.HasSameIdentity(right);

        private static bool SameActivity(ActivityAsset left, ActivityAsset right) =>
            left == null ? right == null : left.HasSameIdentity(right);

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string activeActivityText = ActiveActivityName.ToDiagnosticText();
            return $"scope='{Scope}' activeRoute='{ActiveRouteName}' activeActivity='{activeActivityText}' includeActiveActivity='{IncludesActiveActivity}' {Policy.ToDiagnosticString()} source='{sourceText}' reason='{reasonText}'";
        }

        public static CycleResetRequest Route(
            RouteAsset activeRoute,
            ActivityAsset activeActivity,
            string source,
            string reason)
        {
            return new CycleResetRequest(
                CycleResetScope.Route,
                activeRoute,
                activeActivity,
                CycleResetPolicy.RouteDefault(),
                source,
                reason);
        }

        public static CycleResetRequest Route(
            RouteAsset activeRoute,
            ActivityAsset activeActivity,
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            return new CycleResetRequest(
                CycleResetScope.Route,
                activeRoute,
                activeActivity,
                policy,
                source,
                reason);
        }

        public static CycleResetRequest Activity(
            RouteAsset activeRoute,
            ActivityAsset activeActivity,
            string source,
            string reason)
        {
            return new CycleResetRequest(
                CycleResetScope.Activity,
                activeRoute,
                activeActivity,
                CycleResetPolicy.ActivityDefault(),
                source,
                reason);
        }

        public static bool operator ==(CycleResetRequest left, CycleResetRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetRequest left, CycleResetRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
