using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for one Loading operation.
    /// This is not a scene path, Transition id, TransitionEffect id, UI label, Progression Save slot or backend key.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading operation identity primitive; observation/progress only.")]
    public readonly struct LoadingOperationId : IFrameworkIdentity, IEquatable<LoadingOperationId>
    {
        private readonly FrameworkIdentityValue _value;

        public LoadingOperationId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LoadingOperationId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Loading operation id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Loading;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(LoadingOperationId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingOperationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LoadingOperationId From(string value)
        {
            return new LoadingOperationId(value);
        }

        public static bool operator ==(LoadingOperationId left, LoadingOperationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingOperationId left, LoadingOperationId right)
        {
            return !left.Equals(right);
        }
    }
}
