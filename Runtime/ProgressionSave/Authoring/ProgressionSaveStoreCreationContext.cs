using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// Explicit application-scope context supplied when a Progression Save backend is
    /// materialized. It contains product identity, not backend-specific storage data.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C explicit Progression Save store materialization context.")]
    public readonly struct ProgressionSaveStoreCreationContext :
        IEquatable<ProgressionSaveStoreCreationContext>
    {
        public ProgressionSaveStoreCreationContext(string applicationName)
        {
            string normalized =
                applicationName.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException(
                    "Progression Save store creation requires a non-empty application name.",
                    nameof(applicationName));
            }

            ApplicationName = normalized;
        }

        public string ApplicationName { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ApplicationName);

        public bool Equals(
            ProgressionSaveStoreCreationContext other)
        {
            return string.Equals(
                ApplicationName,
                other.ApplicationName,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveStoreCreationContext other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(
                ApplicationName ?? string.Empty);
        }

        public override string ToString()
        {
            return $"application='{ApplicationName ?? string.Empty}'";
        }

        public static bool operator ==(
            ProgressionSaveStoreCreationContext left,
            ProgressionSaveStoreCreationContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ProgressionSaveStoreCreationContext left,
            ProgressionSaveStoreCreationContext right)
        {
            return !left.Equals(right);
        }
    }
}
