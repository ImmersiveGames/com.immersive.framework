using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Request to reset one logical ObjectEntry target.
    /// It carries identity/owner context only; it does not reference Unity objects or participants directly.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14H Object Reset request primitive for logical target execution.")]
    public readonly struct ObjectResetRequest : IEquatable<ObjectResetRequest>
    {
        public ObjectResetRequest(
            ObjectResetTarget target,
            ObjectResetPolicy policy,
            string source,
            string reason)
        {
            if (!target.IsValid)
            {
                throw new ArgumentException("Object Reset request requires a valid target.", nameof(target));
            }

            if (!policy.IsValid)
            {
                throw new ArgumentException("Object Reset request requires a valid policy.", nameof(policy));
            }

            Target = target;
            Policy = policy;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ObjectResetTarget Target { get; }

        public ObjectResetPolicy Policy { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Target.IsValid && Policy.IsValid;

        public bool AllowsNoParticipants => Policy.AllowNoParticipants;

        public bool Equals(ObjectResetRequest other)
        {
            return Target.Equals(other.Target)
                && Policy.Equals(other.Policy)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Target.GetHashCode();
                hashCode = hashCode * 397 ^ Policy.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"{Target.ToDiagnosticString()} {Policy.ToDiagnosticString()} source='{sourceText}' reason='{reasonText}'";
        }

        public static ObjectResetRequest ForTarget(
            ObjectResetTarget target,
            string source,
            string reason)
        {
            return new ObjectResetRequest(target, ObjectResetPolicy.Default(), source, reason);
        }

        public static bool operator ==(ObjectResetRequest left, ObjectResetRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetRequest left, ObjectResetRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
