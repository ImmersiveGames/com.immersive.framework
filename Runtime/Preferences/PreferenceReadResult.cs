using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Explicit result of one Preferences read operation.
    /// The result never represents a silent fallback value.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences read result; explicit missing/type mismatch/failure states.")]
    public readonly struct PreferenceReadResult : IEquatable<PreferenceReadResult>
    {
        public PreferenceReadResult(
            PreferenceKey key,
            PreferenceValueKind expectedKind,
            PreferenceReadStatus status,
            PreferenceValue value,
            string message)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("Preference read result requires a valid key.", nameof(key));
            }

            FrameworkEnumValidation.ThrowIfUndefinedOr(expectedKind, PreferenceValueKind.Unknown, nameof(expectedKind), "Preference read expected kind must be explicit.");

            FrameworkEnumValidation.ThrowIfUndefinedOr(status, PreferenceReadStatus.Unknown, nameof(status), "Preference read status must be explicit.");

            if (status == PreferenceReadStatus.Found)
            {
                if (!value.IsValid)
                {
                    throw new ArgumentException("Found preference read result requires a valid value.", nameof(value));
                }

                if (value.Kind != expectedKind)
                {
                    throw new ArgumentException("Found preference read result value kind must match expected kind.", nameof(value));
                }
            }
            else if (value.IsValid)
            {
                throw new ArgumentException("Only a found preference read result may carry a value.", nameof(value));
            }

            Key = key;
            ExpectedKind = expectedKind;
            Status = status;
            Value = value;
            Message = Normalize(message);
        }

        public PreferenceKey Key { get; }

        public PreferenceValueKind ExpectedKind { get; }

        public PreferenceReadStatus Status { get; }

        public PreferenceValue Value { get; }

        public string Message { get; }

        public bool Found => Status == PreferenceReadStatus.Found;

        public bool Missing => Status == PreferenceReadStatus.Missing;

        public bool TypeMismatch => Status == PreferenceReadStatus.TypeMismatch;

        public bool Failed => Status == PreferenceReadStatus.Failed;

        public bool HasValue => Value.IsValid;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool IsValid => Key.IsValid
            && FrameworkEnumValidation.IsDefinedAndNot(ExpectedKind, PreferenceValueKind.Unknown)
            && FrameworkEnumValidation.IsDefinedAndNot(Status, PreferenceReadStatus.Unknown)
            && (Found && Value.IsValid && Value.Kind == ExpectedKind || !Found && !Value.IsValid);

        public bool Equals(PreferenceReadResult other)
        {
            return Key.Equals(other.Key)
                && ExpectedKind == other.ExpectedKind
                && Status == other.Status
                && Value.Equals(other.Value)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PreferenceReadResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Key.GetHashCode();
                hashCode = hashCode * 397 ^ (int)ExpectedKind;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ Value.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string valueText = HasValue ? Value.ToDiagnosticString() : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"key='{Key.StableText}' expectedKind='{ExpectedKind}' status='{Status}' value=({valueText}) message='{messageText}'";
        }

        public static PreferenceReadResult FoundResult(PreferenceKey key, PreferenceValue value, string message)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Found preference read result requires a valid value.", nameof(value));
            }

            return new PreferenceReadResult(key, value.Kind, PreferenceReadStatus.Found, value, message);
        }

        public static PreferenceReadResult MissingResult(PreferenceKey key, PreferenceValueKind expectedKind, string message)
        {
            return new PreferenceReadResult(key, expectedKind, PreferenceReadStatus.Missing, default, message);
        }

        public static PreferenceReadResult TypeMismatchResult(PreferenceKey key, PreferenceValueKind expectedKind, string message)
        {
            return new PreferenceReadResult(key, expectedKind, PreferenceReadStatus.TypeMismatch, default, message);
        }

        public static PreferenceReadResult FailedResult(PreferenceKey key, PreferenceValueKind expectedKind, string message)
        {
            return new PreferenceReadResult(key, expectedKind, PreferenceReadStatus.Failed, default, message);
        }

        public static bool operator ==(PreferenceReadResult left, PreferenceReadResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreferenceReadResult left, PreferenceReadResult right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
