using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Typed identity for one runtime-created content instance.
    /// The value is functional identity, not a GameObject name, prefab path or display label.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B typed runtime content id primitive.")]
    public readonly struct RuntimeContentId : IFrameworkIdentity, IEquatable<RuntimeContentId>
    {
        private readonly FrameworkIdentityValue _value;

        public RuntimeContentId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public RuntimeContentId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Runtime content id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Runtime;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(RuntimeContentId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeContentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static RuntimeContentId From(string value)
        {
            return new RuntimeContentId(value);
        }

        public static bool operator ==(RuntimeContentId left, RuntimeContentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeContentId left, RuntimeContentId right)
        {
            return !left.Equals(right);
        }
    }
}
