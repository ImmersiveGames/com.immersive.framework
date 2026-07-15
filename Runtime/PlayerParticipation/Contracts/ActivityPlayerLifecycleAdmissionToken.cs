using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
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
            string previousRouteName,
            string targetRouteName,
            int sequence)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PreviousOwner = previousOwner;
            TargetOwner = targetOwner;
            FlowKind = flowKind;
            PreviousRouteName = previousRouteName.NormalizeText();
            TargetRouteName = targetRouteName.NormalizeText();
            Sequence = sequence;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner PreviousOwner { get; }
        public RuntimeContentOwner TargetOwner { get; }
        public ActivityPlayerLifecycleAdmissionFlowKind FlowKind { get; }
        public string PreviousRouteName { get; }
        public string TargetRouteName { get; }
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
                    ? $":{PreviousRouteName}->{TargetRouteName}"
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
                return string.IsNullOrEmpty(PreviousRouteName) &&
                    string.IsNullOrEmpty(TargetRouteName);
            }

            if (FlowKind ==
                ActivityPlayerLifecycleAdmissionFlowKind
                    .RouteStartupActivitySwitch)
            {
                return !string.IsNullOrEmpty(PreviousRouteName) &&
                    !string.IsNullOrEmpty(TargetRouteName) &&
                    !string.Equals(
                        PreviousRouteName,
                        TargetRouteName,
                        StringComparison.Ordinal);
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
            string.Equals(
                PreviousRouteName,
                other.PreviousRouteName,
                StringComparison.Ordinal) &&
            string.Equals(
                TargetRouteName,
                other.TargetRouteName,
                StringComparison.Ordinal) &&
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
                hashCode = hashCode * 397 ^
                    StringComparer.Ordinal.GetHashCode(
                        PreviousRouteName ?? string.Empty);
                hashCode = hashCode * 397 ^
                    StringComparer.Ordinal.GetHashCode(
                        TargetRouteName ?? string.Empty);
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
