using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Immutable diagnostic handle for one discovered local contribution.
    /// It is not a materialization handle, lifecycle owner, release token or runtime object reference.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Local contribution handle introduced by F5D and carrying requiredness metadata from F5F; not game-facing API.")]
    internal readonly struct LocalContributionHandle : IEquatable<LocalContributionHandle>
    {
        public LocalContributionHandle(
            LocalContentIdentity identity,
            LocalContributionSourceKind sourceKind,
            FrameworkContentRequiredness requiredness,
            string sceneName,
            string objectName,
            string componentType)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Local contribution handle requires a valid local content identity.", nameof(identity));
            }

            if (!Enum.IsDefined(typeof(LocalContributionSourceKind), sourceKind) || sourceKind == LocalContributionSourceKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Local contribution source kind must be explicit.");
            }

            Identity = identity;
            SourceKind = sourceKind;
            Requiredness = NormalizeRequiredness(requiredness);
            SceneName = Normalize(sceneName, "<no-scene>");
            ObjectName = Normalize(objectName, "<missing>");
            ComponentType = Normalize(componentType, "<unknown>");
        }

        public LocalContentIdentity Identity { get; }

        public LocalContributionSourceKind SourceKind { get; }

        public FrameworkContentRequiredness Requiredness { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string ComponentType { get; }

        public bool IsValid => Identity.IsValid && SourceKind != LocalContributionSourceKind.Unknown;

        public bool Equals(LocalContributionHandle other)
        {
            return Identity.Equals(other.Identity)
                && SourceKind == other.SourceKind
                && Requiredness == other.Requiredness
                && string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(ComponentType, other.ComponentType, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalContributionHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Identity.GetHashCode();
                hashCode = hashCode * 397 ^ (int)SourceKind;
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(SceneName);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ObjectName);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ComponentType);
                return hashCode;
            }
        }

        public string ToDiagnosticString()
        {
            return $"identity='{FormatValue(Identity.StableText)}' scope='{Identity.ContentScope}' owner='{FormatValue(Identity.ScopeOwner.Value.Value)}' localId='{FormatValue(Identity.LocalId.StableText)}' localScopeKind='{Identity.LocalScopeKind}' source='{SourceKind}' requiredness='{Requiredness}' scene='{FormatValue(SceneName)}' object='{FormatValue(ObjectName)}' component='{FormatValue(ComponentType)}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static bool operator ==(LocalContributionHandle left, LocalContributionHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalContributionHandle left, LocalContributionHandle right)
        {
            return !left.Equals(right);
        }

        private static FrameworkContentRequiredness NormalizeRequiredness(FrameworkContentRequiredness requiredness)
        {
            return requiredness == FrameworkContentRequiredness.Required
                ? FrameworkContentRequiredness.Required
                : FrameworkContentRequiredness.Optional;
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\'");
        }
    }
}
