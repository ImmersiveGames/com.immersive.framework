using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Explicit result of one Preferences write/delete operation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences write result.")]
    public readonly struct PreferenceWriteResult : IEquatable<PreferenceWriteResult>
    {
        public PreferenceWriteResult(
            PreferenceKey key,
            PreferenceWriteStatus status,
            PreferenceValueKind valueKind,
            string message)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("Preference write result requires a valid key.", nameof(key));
            }

            if (!Enum.IsDefined(typeof(PreferenceWriteStatus), status) || status == PreferenceWriteStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Preference write status must be explicit.");
            }

            if (valueKind != PreferenceValueKind.Unknown && !Enum.IsDefined(typeof(PreferenceValueKind), valueKind))
            {
                throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, "Preference value kind must be known or Unknown for delete/missing results.");
            }

            Key = key;
            Status = status;
            ValueKind = valueKind;
            Message = Normalize(message);
        }

        public PreferenceKey Key { get; }

        public PreferenceWriteStatus Status { get; }

        public PreferenceValueKind ValueKind { get; }

        public string Message { get; }

        public bool Written => Status == PreferenceWriteStatus.Written;

        public bool Deleted => Status == PreferenceWriteStatus.Deleted;

        public bool Missing => Status == PreferenceWriteStatus.Missing;

        public bool Failed => Status == PreferenceWriteStatus.Failed;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool IsValid => Key.IsValid && Status != PreferenceWriteStatus.Unknown;

        public bool Equals(PreferenceWriteResult other)
        {
            return Key.Equals(other.Key)
                && Status == other.Status
                && ValueKind == other.ValueKind
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PreferenceWriteResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Key.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ (int)ValueKind;
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
            string messageText = HasMessage ? Message : "<none>";
            return $"key='{Key.StableText}' status='{Status}' valueKind='{ValueKind}' message='{messageText}'";
        }

        public static PreferenceWriteResult WrittenResult(PreferenceKey key, PreferenceValueKind valueKind, string message)
        {
            if (valueKind == PreferenceValueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, "Written preference result requires a concrete value kind.");
            }

            return new PreferenceWriteResult(key, PreferenceWriteStatus.Written, valueKind, message);
        }

        public static PreferenceWriteResult DeletedResult(PreferenceKey key, string message)
        {
            return new PreferenceWriteResult(key, PreferenceWriteStatus.Deleted, PreferenceValueKind.Unknown, message);
        }

        public static PreferenceWriteResult MissingResult(PreferenceKey key, string message)
        {
            return new PreferenceWriteResult(key, PreferenceWriteStatus.Missing, PreferenceValueKind.Unknown, message);
        }

        public static PreferenceWriteResult FailedResult(PreferenceKey key, PreferenceValueKind valueKind, string message)
        {
            return new PreferenceWriteResult(key, PreferenceWriteStatus.Failed, valueKind, message);
        }

        public static bool operator ==(PreferenceWriteResult left, PreferenceWriteResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreferenceWriteResult left, PreferenceWriteResult right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
