using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Experimental. Immutable description of one active Gate blocker.
    /// A blocker is passive data. It does not own lifecycle, input, UI, transition, pause or gameplay execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F17B passive Gate blocker primitive; no runtime manager or service locator.")]
    public readonly struct GateBlocker : IEquatable<GateBlocker>
    {
        private readonly FrameworkIdentityValue _blockerId;

        public GateBlocker(
            FrameworkIdentityValue blockerId,
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string source,
            string reason,
            string policySource)
        {
            if (!blockerId.IsValid)
            {
                throw new ArgumentException("Gate blocker id must be valid.", nameof(blockerId));
            }

            if (!Enum.IsDefined(typeof(GateScope), scope) || scope == GateScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Gate blocker scope must be explicit.");
            }

            if (!Enum.IsDefined(typeof(GateDomain), domain) || domain == GateDomain.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(domain), domain, "Gate blocker domain must be explicit.");
            }

            _blockerId = blockerId;
            Scope = scope;
            Domain = domain;
            Owner = owner;
            Source = Normalize(source);
            Reason = Normalize(reason);
            PolicySource = Normalize(policySource);
        }

        public FrameworkIdentityValue BlockerId => _blockerId;

        public GateScope Scope { get; }

        public GateDomain Domain { get; }

        public FrameworkIdentityKey Owner { get; }

        public string Source { get; }

        public string Reason { get; }

        public string PolicySource { get; }

        public bool IsValid => _blockerId.IsValid
            && Scope != GateScope.Unknown
            && Domain != GateDomain.Unknown;

        public bool HasOwner => Owner.IsValid;

        public string OwnerStableText => HasOwner ? Owner.StableText : string.Empty;

        public string StableText => HasOwner
            ? $"{Scope}:{Domain}:{Owner.StableText}:{_blockerId.Value}"
            : $"{Scope}:{Domain}:<any-owner>:{_blockerId.Value}";

        public bool Blocks(GateScope scope, GateDomain domain)
        {
            return Blocks(scope, domain, default);
        }

        public bool Blocks(GateScope scope, GateDomain domain, FrameworkIdentityKey owner)
        {
            if (!IsValid || scope == GateScope.Unknown || domain == GateDomain.Unknown)
            {
                return false;
            }

            if (Scope != scope || Domain != domain)
            {
                return false;
            }

            return !HasOwner || owner.IsValid && Owner.Equals(owner);
        }

        public bool Equals(GateBlocker other)
        {
            return _blockerId.Equals(other._blockerId)
                && Scope == other.Scope
                && Domain == other.Domain
                && Owner.Equals(other.Owner)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(PolicySource, other.PolicySource, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GateBlocker other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _blockerId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)Domain;
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
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
            string ownerText = HasOwner ? Owner.StableText : "<any-owner>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string policySourceText = PolicySource.ToDiagnosticText();
            return $"blocker='{_blockerId.Value}' scope='{Scope}' domain='{Domain}' owner='{ownerText}' source='{sourceText}' reason='{reasonText}' policySource='{policySourceText}'";
        }

        public static GateBlocker ForAnyOwner(
            string blockerId,
            GateScope scope,
            GateDomain domain,
            string source,
            string reason,
            string policySource)
        {
            return new GateBlocker(
                new FrameworkIdentityValue(blockerId),
                scope,
                domain,
                default,
                source,
                reason,
                policySource);
        }

        public static GateBlocker ForOwner(
            string blockerId,
            GateScope scope,
            GateDomain domain,
            FrameworkIdentityKey owner,
            string source,
            string reason,
            string policySource)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Owner-scoped Gate blocker requires a valid owner identity.", nameof(owner));
            }

            return new GateBlocker(
                new FrameworkIdentityValue(blockerId),
                scope,
                domain,
                owner,
                source,
                reason,
                policySource);
        }

        public static bool operator ==(GateBlocker left, GateBlocker right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GateBlocker left, GateBlocker right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
