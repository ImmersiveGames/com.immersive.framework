using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7H exact Activity Player lifecycle admission token, including flow and Route identity when applicable.")]
    public readonly struct ActivityPlayerLifecycleAdmissionToken :
        IEquatable<ActivityPlayerLifecycleAdmissionToken>
    {
        internal ActivityPlayerLifecycleAdmissionToken(
            string sessionContextId,
            RuntimeContentOwner previousOwner,
            RuntimeContentOwner targetOwner,
            ActivityPlayerLifecycleAdmissionFlowKind flowKind,
            RouteId previousRouteId,
            RouteId targetRouteId,
            int sequence)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PreviousOwner = previousOwner;
            TargetOwner = targetOwner;
            FlowKind = flowKind;
            PreviousRouteId = previousRouteId;
            TargetRouteId = targetRouteId;
            Sequence = sequence;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner PreviousOwner { get; }
        public RuntimeContentOwner TargetOwner { get; }
        public ActivityPlayerLifecycleAdmissionFlowKind FlowKind { get; }
        public RouteId PreviousRouteId { get; }
        public RouteId TargetRouteId { get; }
        public int Sequence { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PreviousOwner.IsValid &&
            PreviousOwner.Scope == RuntimeContentScope.Activity &&
            TargetOwner.IsValid &&
            TargetOwner.Scope == RuntimeContentScope.Activity &&
            PreviousOwner != TargetOwner &&
            FlowKind != ActivityPlayerLifecycleAdmissionFlowKind.None &&
            HasCoherentRouteIdentity() &&
            Sequence > 0;

        public string StableText
        {
            get
            {
                if (!IsValid)
                {
                    return string.Empty;
                }

                string routeText = FlowKind ==
                        ActivityPlayerLifecycleAdmissionFlowKind
                            .RouteStartupActivitySwitch
                    ? $":{PreviousRouteId.StableText}->{TargetRouteId.StableText}"
                    : string.Empty;
                return
                    $"{SessionContextId}:{FlowKind}{routeText}:" +
                    $"{PreviousOwner.StableText}->{TargetOwner.StableText}:{Sequence}";
            }
        }

        private bool HasCoherentRouteIdentity()
        {
            if (FlowKind ==
                ActivityPlayerLifecycleAdmissionFlowKind
                    .SameRouteActivitySwitch)
            {
                return !PreviousRouteId.IsValid &&
                    !TargetRouteId.IsValid;
            }

            if (FlowKind ==
                ActivityPlayerLifecycleAdmissionFlowKind
                    .RouteStartupActivitySwitch)
            {
                return PreviousRouteId.IsValid &&
                    TargetRouteId.IsValid &&
                    PreviousRouteId != TargetRouteId;
            }

            return false;
        }

        public bool Equals(ActivityPlayerLifecycleAdmissionToken other) =>
            string.Equals(
                SessionContextId,
                other.SessionContextId,
                StringComparison.Ordinal) &&
            PreviousOwner == other.PreviousOwner &&
            TargetOwner == other.TargetOwner &&
            FlowKind == other.FlowKind &&
            PreviousRouteId == other.PreviousRouteId &&
            TargetRouteId == other.TargetRouteId &&
            Sequence == other.Sequence;

        public override bool Equals(object obj) =>
            obj is ActivityPlayerLifecycleAdmissionToken other &&
            Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(
                    SessionContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ PreviousOwner.GetHashCode();
                hashCode = hashCode * 397 ^ TargetOwner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)FlowKind;
                hashCode = hashCode * 397 ^ PreviousRouteId.GetHashCode();
                hashCode = hashCode * 397 ^ TargetRouteId.GetHashCode();
                hashCode = hashCode * 397 ^ Sequence;
                return hashCode;
            }
        }

        public static bool operator ==(
            ActivityPlayerLifecycleAdmissionToken left,
            ActivityPlayerLifecycleAdmissionToken right) =>
            left.Equals(right);

        public static bool operator !=(
            ActivityPlayerLifecycleAdmissionToken left,
            ActivityPlayerLifecycleAdmissionToken right) =>
            !left.Equals(right);

        public override string ToString() => StableText;
    }
}
