
using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Project-owned Unity Input action map name used as passive evidence.
    /// This is a validated name only; it does not switch action maps.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B Unity Input action map name value.")]
    public readonly struct UnityInputActionMapName : IEquatable<UnityInputActionMapName>
    {
        private UnityInputActionMapName(string name)
        {
            Name = name.NormalizeText();
        }

        public string Name { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Name);

        public bool Equals(UnityInputActionMapName other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityInputActionMapName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public static bool operator ==(UnityInputActionMapName left, UnityInputActionMapName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnityInputActionMapName left, UnityInputActionMapName right)
        {
            return !left.Equals(right);
        }

        public static UnityInputActionMapName From(string name)
        {
            return new UnityInputActionMapName(name);
        }
    }
}
