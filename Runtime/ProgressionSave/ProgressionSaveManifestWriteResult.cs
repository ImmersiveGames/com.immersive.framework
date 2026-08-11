using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Internal. Result used by the built-in backend while maintaining
    /// its physical manifest. Manifest mutation is not part of the public backend
    /// contract.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR018-A internal built-in-backend manifest write result.")]
    internal readonly struct ProgressionSaveManifestWriteResult : IEquatable<ProgressionSaveManifestWriteResult>
    {
        internal ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus status, string message)
        {
            ValidateStatus(status);

            Status = status;
            Message = Normalize(message);
        }

        internal ProgressionSaveWriteStatus Status { get; }

        internal string Message { get; }

        internal bool Written => Status == ProgressionSaveWriteStatus.Written;

        internal bool Failed => Status is ProgressionSaveWriteStatus.BackendUnavailable
            or ProgressionSaveWriteStatus.Failed
            or ProgressionSaveWriteStatus.Rejected;

        internal bool HasMessage => !string.IsNullOrWhiteSpace(Message);

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

        internal static ProgressionSaveManifestWriteResult WrittenResult(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.Written, message);
        }

        internal static ProgressionSaveManifestWriteResult Rejected(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.Rejected, message);
        }

        internal static ProgressionSaveManifestWriteResult BackendUnavailable(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.BackendUnavailable, message);
        }

        internal static ProgressionSaveManifestWriteResult FailedResult(string message)
        {
            return new ProgressionSaveManifestWriteResult(ProgressionSaveWriteStatus.Failed, message);
        }

        private static void ValidateStatus(ProgressionSaveWriteStatus status)
        {
            if (!Enum.IsDefined(typeof(ProgressionSaveWriteStatus), status)
                || status == ProgressionSaveWriteStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Progression Save internal manifest write status must be explicit.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
