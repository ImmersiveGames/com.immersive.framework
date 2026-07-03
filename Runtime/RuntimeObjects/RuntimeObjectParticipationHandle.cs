using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeObjects
{
    /// <summary>
    /// API status: Experimental. Opaque runtime handle for one registered runtime object participation record.
    /// It is not a GameObject id, save id, actor id, pool handle or lifecycle owner.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F44 runtime object participation handle for dynamically registered ObjectEntry records.")]
    public readonly struct RuntimeObjectParticipationHandle : IEquatable<RuntimeObjectParticipationHandle>
    {
        public RuntimeObjectParticipationHandle(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public bool Equals(RuntimeObjectParticipationHandle other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeObjectParticipationHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return IsValid ? Value.ToString() : "<invalid>";
        }

        public static bool operator ==(RuntimeObjectParticipationHandle left, RuntimeObjectParticipationHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeObjectParticipationHandle left, RuntimeObjectParticipationHandle right)
        {
            return !left.Equals(right);
        }
    }
}
