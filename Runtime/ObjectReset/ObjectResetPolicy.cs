using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Policy attached to a logical Object Reset request.
    /// F14H keeps policy limited to current-snapshot targeting and no-participant handling.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14H Object Reset policy primitive for current-snapshot targeting and no-participant handling.")]
    public readonly struct ObjectResetPolicy : IEquatable<ObjectResetPolicy>
    {
        private readonly bool _defined;

        public ObjectResetPolicy(bool requireCurrentSnapshot, bool allowNoParticipants)
        {
            if (!requireCurrentSnapshot)
            {
                throw new ArgumentException("Object Reset policy must require the current Object Entry snapshot.", nameof(requireCurrentSnapshot));
            }

            _defined = true;
            RequireCurrentSnapshot = requireCurrentSnapshot;
            AllowNoParticipants = allowNoParticipants;
        }

        public bool RequireCurrentSnapshot { get; }

        public bool AllowNoParticipants { get; }

        public bool IsValid => _defined && RequireCurrentSnapshot;

        public bool Equals(ObjectResetPolicy other)
        {
            return _defined == other._defined
                && RequireCurrentSnapshot == other.RequireCurrentSnapshot
                && AllowNoParticipants == other.AllowNoParticipants;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _defined ? 1 : 0;
                hashCode = hashCode * 397 ^ (RequireCurrentSnapshot ? 1 : 0);
                hashCode = hashCode * 397 ^ (AllowNoParticipants ? 1 : 0);
                return hashCode;
            }
        }

        public string ToDiagnosticString()
        {
            return $"requireCurrentSnapshot='{RequireCurrentSnapshot}' allowNoParticipants='{AllowNoParticipants}'";
        }

        public static ObjectResetPolicy Default()
        {
            return new ObjectResetPolicy(requireCurrentSnapshot: true, allowNoParticipants: true);
        }

        public static bool operator ==(ObjectResetPolicy left, ObjectResetPolicy right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetPolicy left, ObjectResetPolicy right)
        {
            return !left.Equals(right);
        }
    }
}
