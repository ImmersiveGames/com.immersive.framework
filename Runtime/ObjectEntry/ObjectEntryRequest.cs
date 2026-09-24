using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Request envelope for introducing a logical object entry into a lifecycle-aware flow.
    /// F13A does not execute the request; it defines the contract future object entry integration will consume.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry request envelope introduced by F13A.")]
    public readonly struct ObjectEntryRequest : IEquatable<ObjectEntryRequest>
    {
        public ObjectEntryRequest(ObjectEntryDescriptor descriptor, string source, string reason)
        {
            Descriptor = descriptor;
            Source = source.NormalizeTextOrFallback("ObjectEntry");
            Reason = reason.NormalizeTextOrFallback("object-entry");
        }

        public ObjectEntryDescriptor Descriptor { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Equals(ObjectEntryRequest other)
        {
            return Descriptor.Equals(other.Descriptor)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectEntryRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Descriptor.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"source='{Source}' reason='{Reason}' {Descriptor}";
        }
    }
}
