using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Immutable Activity-owned declaration that product Pause binding is required.
    /// It carries no Player, host, binding, token, or runtime authority reference.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P2.1A immutable Pause Activity binding intent; materialization is deferred.")]
    public readonly struct PauseActivityBindingIntent : IEquatable<PauseActivityBindingIntent>
    {
        public PauseActivityBindingIntent(
            PauseActivityBindingRequiredness requiredness,
            string source)
        {
            if (!Enum.IsDefined(typeof(PauseActivityBindingRequiredness), requiredness) ||
                requiredness == PauseActivityBindingRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredness),
                    requiredness,
                    "Pause Activity binding intent requiredness must be explicit.");
            }

            string normalizedSource = source.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedSource))
            {
                throw new ArgumentException(
                    "Pause Activity binding intent requires a diagnostic source.",
                    nameof(source));
            }

            Requiredness = requiredness;
            Source = normalizedSource;
        }

        public PauseActivityBindingRequiredness Requiredness { get; }

        public string Source { get; }

        public bool IsRequired => Requiredness == PauseActivityBindingRequiredness.Required;

        public bool IsValid =>
            Enum.IsDefined(typeof(PauseActivityBindingRequiredness), Requiredness) &&
            Requiredness != PauseActivityBindingRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public bool Equals(PauseActivityBindingIntent other)
        {
            return Requiredness == other.Requiredness &&
                string.Equals(Source, other.Source, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseActivityBindingIntent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Requiredness;
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return $"requiredness='{Requiredness}' source='{Source}'";
        }
    }
}
