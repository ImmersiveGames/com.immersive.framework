using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Explicit passive context for runtime-created content owned by one scope owner.
    /// This context carries owner/source/reason data only; it does not create roots, materialize prefabs, destroy objects or bind anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8E explicit runtime scope context; passive owner context only, no root creation or materialization behavior.")]
    public readonly struct RuntimeScopeContext : IEquatable<RuntimeScopeContext>
    {
        public RuntimeScopeContext(RuntimeContentOwner owner, string source, string reason)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime scope context owner must be valid.", nameof(owner));
            }

            Owner = owner;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeContentOwner Owner { get; }

        public RuntimeContentScope Scope => Owner.Scope;

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Owner.IsValid;

        public string StableText => Owner.StableText;

        public RuntimeContentIdentity CreateIdentity(RuntimeContentId contentId)
        {
            return new RuntimeContentIdentity(Owner, contentId);
        }

        public RuntimeContentIdentity CreateIdentity(string contentId)
        {
            return RuntimeContentIdentity.From(Owner, contentId);
        }

        public bool Equals(RuntimeScopeContext other)
        {
            return Owner.Equals(other.Owner)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeScopeContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Owner.GetHashCode();
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
            return $"owner='{Owner.StableText}' scope='{Scope}' source='{sourceText}' reason='{reasonText}'";
        }

        public static bool operator ==(RuntimeScopeContext left, RuntimeScopeContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeScopeContext left, RuntimeScopeContext right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
