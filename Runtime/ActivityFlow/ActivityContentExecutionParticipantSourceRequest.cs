using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive request used by ActivityFlow to ask an explicit source for Activity Content Execution participants.
    /// It carries lifecycle context only; it does not discover scene objects, execute participants or mutate gameplay state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10K Activity Content Execution participant source request; lifecycle context only.")]
    public readonly struct ActivityContentExecutionParticipantSourceRequest : IEquatable<ActivityContentExecutionParticipantSourceRequest>
    {
        public ActivityContentExecutionParticipantSourceRequest(
            RouteAsset route,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            Route = route;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        private RouteAsset Route { get; }

        private ActivityAsset PreviousActivity { get; }

        private ActivityAsset NextActivity { get; }

        public string Source { get; }

        public string Reason { get; }

        private bool HasPreviousActivity => PreviousActivity != null;

        private bool HasNextActivity => NextActivity != null;

        private bool HasActivityTransition => HasPreviousActivity || HasNextActivity;

        public bool IsValid => HasActivityTransition;

        private string RouteName => Route != null ? Route.RouteName : string.Empty;

        private string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        private string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public bool Equals(ActivityContentExecutionParticipantSourceRequest other)
        {
            return SameRoute(Route, other.Route)
                && SameActivity(PreviousActivity, other.PreviousActivity)
                && SameActivity(NextActivity, other.NextActivity)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantSourceRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Route != null && Route.HasValidRouteId ? Route.RouteId.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ (PreviousActivity != null && PreviousActivity.HasValidActivityId ? PreviousActivity.ActivityId.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (NextActivity != null && NextActivity.HasValidActivityId ? NextActivity.ActivityId.GetHashCode() : 0);
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
            string routeText = RouteName.ToDiagnosticText();
            string previousText = PreviousActivityName.ToDiagnosticText();
            string nextText = NextActivityName.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"route='{routeText}' previousActivity='{previousText}' nextActivity='{nextText}' valid='{IsValid}' source='{sourceText}' reason='{reasonText}'";
        }

        public static bool operator ==(ActivityContentExecutionParticipantSourceRequest left, ActivityContentExecutionParticipantSourceRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionParticipantSourceRequest left, ActivityContentExecutionParticipantSourceRequest right)
        {
            return !left.Equals(right);
        }
    }
}
