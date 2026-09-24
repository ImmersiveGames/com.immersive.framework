using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Expected local contribution declaration used by F5G validators.
    /// This is policy data only; it does not materialize, load, unload or hold a runtime object reference.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Expected local contribution declaration introduced by F5G validators; no runtime materialization behavior.")]
    internal readonly struct LocalContributionRequirement : IEquatable<LocalContributionRequirement>
    {
        public LocalContributionRequirement(
            LocalContentIdentity identity,
            FrameworkContentRequiredness requiredness,
            string diagnosticLabel = null)
        {
            Identity = identity;
            Requiredness = NormalizeRequiredness(requiredness);
            DiagnosticLabel = diagnosticLabel.NormalizeText();
        }

        public LocalContentIdentity Identity { get; }

        public FrameworkContentRequiredness Requiredness { get; }

        public string DiagnosticLabel { get; }

        public bool IsValid => Identity.IsValid;

        public bool IsRequired => Requiredness == FrameworkContentRequiredness.Required;

        public bool IsOptional => Requiredness == FrameworkContentRequiredness.Optional;

        public bool Equals(LocalContributionRequirement other)
        {
            return Identity.Equals(other.Identity)
                && Requiredness == other.Requiredness
                && string.Equals(DiagnosticLabel, other.DiagnosticLabel, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalContributionRequirement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Identity.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DiagnosticLabel ?? string.Empty);
                return hashCode;
            }
        }

        public string ToDiagnosticString()
        {
            string label = string.IsNullOrWhiteSpace(DiagnosticLabel)
                ? string.Empty
                : $" label='{FormatValue(DiagnosticLabel)}'";

            return $"identity='{FormatValue(Identity.StableText)}' requiredness='{Requiredness}'{label}";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static bool operator ==(LocalContributionRequirement left, LocalContributionRequirement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalContributionRequirement left, LocalContributionRequirement right)
        {
            return !left.Equals(right);
        }

        private static FrameworkContentRequiredness NormalizeRequiredness(FrameworkContentRequiredness requiredness)
        {
            return requiredness == FrameworkContentRequiredness.Required
                ? FrameworkContentRequiredness.Required
                : FrameworkContentRequiredness.Optional;
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}
