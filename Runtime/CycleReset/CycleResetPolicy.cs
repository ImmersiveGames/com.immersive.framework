using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive policy for one Cycle Reset request.
    /// It controls only cycle-level reset orchestration; it does not define object, player, pool, save or gameplay behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset policy primitive; no physical reset or gameplay behavior.")]
    public readonly struct CycleResetPolicy : IEquatable<CycleResetPolicy>
    {
        private readonly bool _isDefined;

        public CycleResetPolicy(
            bool includeActiveActivity,
            bool allowNoParticipants,
            bool treatOptionalFailuresAsWarnings)
        {
            _isDefined = true;
            IncludeActiveActivity = includeActiveActivity;
            AllowNoParticipants = allowNoParticipants;
            TreatOptionalFailuresAsWarnings = treatOptionalFailuresAsWarnings;
        }

        public bool IncludeActiveActivity { get; }

        public bool AllowNoParticipants { get; }

        public bool TreatOptionalFailuresAsWarnings { get; }

        public bool IsValid => _isDefined;

        public bool Equals(CycleResetPolicy other)
        {
            return _isDefined == other._isDefined
                && IncludeActiveActivity == other.IncludeActiveActivity
                && AllowNoParticipants == other.AllowNoParticipants
                && TreatOptionalFailuresAsWarnings == other.TreatOptionalFailuresAsWarnings;
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _isDefined.GetHashCode();
                hashCode = hashCode * 397 ^ IncludeActiveActivity.GetHashCode();
                hashCode = hashCode * 397 ^ AllowNoParticipants.GetHashCode();
                hashCode = hashCode * 397 ^ TreatOptionalFailuresAsWarnings.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"includeActiveActivity='{IncludeActiveActivity}' allowNoParticipants='{AllowNoParticipants}' treatOptionalFailuresAsWarnings='{TreatOptionalFailuresAsWarnings}'";
        }

        public static CycleResetPolicy RouteDefault()
        {
            return new CycleResetPolicy(
                includeActiveActivity: true,
                allowNoParticipants: true,
                treatOptionalFailuresAsWarnings: true);
        }

        public static CycleResetPolicy ActivityDefault()
        {
            return new CycleResetPolicy(
                includeActiveActivity: false,
                allowNoParticipants: true,
                treatOptionalFailuresAsWarnings: true);
        }

        public static bool operator ==(CycleResetPolicy left, CycleResetPolicy right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetPolicy left, CycleResetPolicy right)
        {
            return !left.Equals(right);
        }
    }
}
