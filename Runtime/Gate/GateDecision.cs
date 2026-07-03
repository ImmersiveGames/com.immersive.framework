using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Experimental. Immutable result of one Gate admission decision.
    /// The decision carries enough context for diagnostics; it is not a runtime queue, UI event or input binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F17B Gate decision primitive with explicit status/scope/domain context.")]
    public readonly struct GateDecision : IEquatable<GateDecision>
    {
        public GateDecision(
            GateDecisionStatus status,
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string subject,
            string source,
            string reason,
            string policySource)
        {
            if (!Enum.IsDefined(typeof(GateDecisionStatus), status) || status == GateDecisionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Gate decision status must be explicit.");
            }

            if (!Enum.IsDefined(typeof(GateScope), scope))
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Gate decision scope value is not defined.");
            }

            if (!Enum.IsDefined(typeof(GateDomain), domain))
            {
                throw new ArgumentOutOfRangeException(nameof(domain), domain, "Gate decision domain value is not defined.");
            }

            Status = status;
            Scope = scope;
            Domain = domain;
            Owner = owner;
            Subject = Normalize(subject);
            Source = Normalize(source);
            Reason = Normalize(reason);
            PolicySource = Normalize(policySource);
        }

        public GateDecisionStatus Status { get; }

        public GateScope Scope { get; }

        public GateDomain Domain { get; }

        public FrameworkIdentityKey Owner { get; }

        public string Subject { get; }

        public string Source { get; }

        public string Reason { get; }

        public string PolicySource { get; }

        public bool IsAllowed => Status == GateDecisionStatus.Allowed;

        public bool IsBlocked => Status == GateDecisionStatus.Blocked;

        public bool IsQueued => Status == GateDecisionStatus.Queued;

        public bool IsRejected => Status is GateDecisionStatus.RejectedInvalidRequest or GateDecisionStatus.RejectedInvalidScope or GateDecisionStatus.RejectedInvalidDomain or GateDecisionStatus.RejectedStale or GateDecisionStatus.RejectedForeign or GateDecisionStatus.RejectedPolicyMissing;

        public bool HasOwner => Owner.IsValid;

        public bool HasSubject => !string.IsNullOrWhiteSpace(Subject);

        public bool HasPolicySource => !string.IsNullOrWhiteSpace(PolicySource);

        public bool HasExplicitScope => Scope != GateScope.Unknown;

        public bool HasExplicitDomain => Domain != GateDomain.Unknown;

        public bool IsValid => Status != GateDecisionStatus.Unknown
            && (Status == GateDecisionStatus.RejectedInvalidScope || HasExplicitScope)
            && (Status == GateDecisionStatus.RejectedInvalidDomain || HasExplicitDomain);

        public bool Equals(GateDecision other)
        {
            return Status == other.Status
                && Scope == other.Scope
                && Domain == other.Domain
                && Owner.Equals(other.Owner)
                && string.Equals(Subject, other.Subject, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(PolicySource, other.PolicySource, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GateDecision other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)Domain;
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Subject ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(PolicySource ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string ownerText = HasOwner ? Owner.StableText : "<none>";
            string subjectText = Subject.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string policySourceText = PolicySource.ToDiagnosticText();
            return $"status='{Status}' scope='{Scope}' domain='{Domain}' owner='{ownerText}' subject='{subjectText}' source='{sourceText}' reason='{reasonText}' policySource='{policySourceText}'";
        }

        public static GateDecision Allowed(
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string subject,
            string source,
            string reason,
            string policySource)
        {
            return new GateDecision(
                GateDecisionStatus.Allowed,
                scope,
                domain,
                owner,
                subject,
                source,
                reason,
                policySource);
        }

        public static GateDecision Blocked(
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string subject,
            string source,
            string reason,
            string policySource)
        {
            return new GateDecision(
                GateDecisionStatus.Blocked,
                scope,
                domain,
                owner,
                subject,
                source,
                reason,
                policySource);
        }

        public static GateDecision Queued(
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string subject,
            string source,
            string reason,
            string policySource)
        {
            return new GateDecision(
                GateDecisionStatus.Queued,
                scope,
                domain,
                owner,
                subject,
                source,
                reason,
                policySource);
        }

        public static GateDecision Rejected(
            GateDecisionStatus status,
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string subject,
            string source,
            string reason,
            string policySource)
        {
            if (status is GateDecisionStatus.Allowed or GateDecisionStatus.Blocked or GateDecisionStatus.Queued or GateDecisionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Rejected Gate decision requires a rejected status.");
            }

            return new GateDecision(
                status,
                scope,
                domain,
                owner,
                subject,
                source,
                reason,
                policySource);
        }

        public static bool operator ==(GateDecision left, GateDecision right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GateDecision left, GateDecision right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
