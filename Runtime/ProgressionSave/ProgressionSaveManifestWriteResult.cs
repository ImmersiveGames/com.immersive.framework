using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Backend-agnostic result for writing a Progression Save manifest.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save manifest write result primitive.")]
    public readonly struct ProgressionSaveManifestWriteResult : IEquatable<ProgressionSaveManifestWriteResult>
    {
        public ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus status, string message)
        {
            ValidateStatus(status);

            Status = status;
            Message = Normalize(message);
        }

        public ProgressionSaveWriteStatus Status { get; }

        public string Message { get; }

        public bool Written => Status == ProgressionSaveWriteStatus.Written;

        public bool Failed => Status is ProgressionSaveWriteStatus.BackendUnavailable or ProgressionSaveWriteStatus.Failed or ProgressionSaveWriteStatus.Rejected;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ProgressionSaveManifestWriteResult other)
        {
            return Status == other.Status
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveManifestWriteResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string messageText = HasMessage ? Message : "<none>";
            return $"status='{Status}' message='{messageText}'";
        }

        public static ProgressionSaveManifestWriteResult WrittenResult(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.Written, message);
        }

        public static ProgressionSaveManifestWriteResult Rejected(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.Rejected, message);
        }

        public static ProgressionSaveManifestWriteResult BackendUnavailable(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.BackendUnavailable, message);
        }

        public static ProgressionSaveManifestWriteResult FailedResult(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.Failed, message);
        }

        private static void ValidateStatus(ProgressionSaveWriteStatus status)
        {
            if (!Enum.IsDefined(typeof(ProgressionSaveWriteStatus), status) || status == ProgressionSaveWriteStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Progression Save manifest write status must be explicit.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
