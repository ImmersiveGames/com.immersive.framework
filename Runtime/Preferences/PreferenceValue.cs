using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Immutable typed value carried by the Preferences boundary.
    /// It is not a Snapshot payload and does not imply a progression slot or persistence engine.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences typed value primitive.")]
    public readonly struct PreferenceValue : IEquatable<PreferenceValue>
    {
        private readonly string _stringValue;
        private readonly int _intValue;
        private readonly float _floatValue;
        private readonly bool _boolValue;

        private PreferenceValue(
            PreferenceValueKind kind,
            string stringValue,
            int intValue,
            float floatValue,
            bool boolValue)
        {
            if (!Enum.IsDefined(typeof(PreferenceValueKind), kind) || kind == PreferenceValueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Preference value kind must be explicit.");
            }

            Kind = kind;
            _stringValue = Normalize(stringValue);
            _intValue = intValue;
            _floatValue = floatValue;
            _boolValue = boolValue;
        }

        public PreferenceValueKind Kind { get; }

        public bool IsValid => Kind != PreferenceValueKind.Unknown;

        public string StringValue
        {
            get
            {
                EnsureKind(PreferenceValueKind.String);
                return _stringValue;
            }
        }

        public int IntValue
        {
            get
            {
                EnsureKind(PreferenceValueKind.Int);
                return _intValue;
            }
        }

        public float FloatValue
        {
            get
            {
                EnsureKind(PreferenceValueKind.Float);
                return _floatValue;
            }
        }

        public bool BoolValue
        {
            get
            {
                EnsureKind(PreferenceValueKind.Bool);
                return _boolValue;
            }
        }

        public bool TryGetString(out string value)
        {
            value = Kind == PreferenceValueKind.String ? _stringValue : string.Empty;
            return Kind == PreferenceValueKind.String;
        }

        public bool TryGetInt(out int value)
        {
            value = Kind == PreferenceValueKind.Int ? _intValue : 0;
            return Kind == PreferenceValueKind.Int;
        }

        public bool TryGetFloat(out float value)
        {
            value = Kind == PreferenceValueKind.Float ? _floatValue : 0f;
            return Kind == PreferenceValueKind.Float;
        }

        public bool TryGetBool(out bool value)
        {
            value = Kind == PreferenceValueKind.Bool && _boolValue;
            return Kind == PreferenceValueKind.Bool;
        }

        public bool Equals(PreferenceValue other)
        {
            return Kind == other.Kind
                && string.Equals(_stringValue, other._stringValue, StringComparison.Ordinal)
                && _intValue == other._intValue
                && _floatValue.Equals(other._floatValue)
                && _boolValue == other._boolValue;
        }

        public override bool Equals(object obj)
        {
            return obj is PreferenceValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(_stringValue ?? string.Empty);
                hashCode = hashCode * 397 ^ _intValue;
                hashCode = hashCode * 397 ^ _floatValue.GetHashCode();
                hashCode = hashCode * 397 ^ _boolValue.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            if (Kind == PreferenceValueKind.String)
            {
                return $"kind='{Kind}' value='{_stringValue}'";
            }

            if (Kind == PreferenceValueKind.Int)
            {
                return $"kind='{Kind}' value='{_intValue}'";
            }

            if (Kind == PreferenceValueKind.Float)
            {
                return $"kind='{Kind}' value='{_floatValue}'";
            }

            if (Kind == PreferenceValueKind.Bool)
            {
                return $"kind='{Kind}' value='{_boolValue}'";
            }

            return "kind='Unknown' value='<invalid>'";
        }

        public static PreferenceValue FromString(string value)
        {
            return new PreferenceValue(PreferenceValueKind.String, value, 0, 0f, false);
        }

        public static PreferenceValue FromInt(int value)
        {
            return new PreferenceValue(PreferenceValueKind.Int, string.Empty, value, 0f, false);
        }

        public static PreferenceValue FromFloat(float value)
        {
            return new PreferenceValue(PreferenceValueKind.Float, string.Empty, 0, value, false);
        }

        public static PreferenceValue FromBool(bool value)
        {
            return new PreferenceValue(PreferenceValueKind.Bool, string.Empty, 0, 0f, value);
        }

        public static bool operator ==(PreferenceValue left, PreferenceValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreferenceValue left, PreferenceValue right)
        {
            return !left.Equals(right);
        }

        private void EnsureKind(PreferenceValueKind expectedKind)
        {
            if (Kind != expectedKind)
            {
                throw new InvalidOperationException($"Preference value kind is '{Kind}', not '{expectedKind}'.");
            }
        }

        private static string Normalize(string value)
        {
            return value ?? string.Empty;
        }
    }
}
