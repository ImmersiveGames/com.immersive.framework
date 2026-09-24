using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Backend-agnostic result for reading a Progression Save manifest.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save manifest read result primitive.")]
    public readonly struct ProgressionSaveManifestReadResult : IEquatable<ProgressionSaveManifestReadResult>
    {
        public ProgressionSaveManifestReadResult(ProgressionSaveReadStatus status, ProgressionSaveManifest manifest, string message)
        {
            ValidateStatus(status);

            if (status == ProgressionSaveReadStatus.Found && !manifest.IsValid)
            {
                throw new ArgumentException("Progression Save found manifest read result requires a valid manifest.", nameof(manifest));
            }

            if (status != ProgressionSaveReadStatus.Found && manifest.IsValid)
            {
                throw new ArgumentException("Progression Save non-found manifest read result cannot carry a valid manifest.", nameof(manifest));
            }

            Status = status;
            Manifest = manifest;
            Message = Normalize(message);
        }

        public ProgressionSaveReadStatus Status { get; }

        public ProgressionSaveManifest Manifest { get; }

        public string Message { get; }

        public bool HasManifest => Status == ProgressionSaveReadStatus.Found && Manifest.IsValid;

        public bool Completed => Status is ProgressionSaveReadStatus.Found or ProgressionSaveReadStatus.Missing;

        public bool Failed => Status is ProgressionSaveReadStatus.Corrupt or ProgressionSaveReadStatus.BackendUnavailable or ProgressionSaveReadStatus.Failed or ProgressionSaveReadStatus.Rejected;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ProgressionSaveManifestReadResult other)
        {
            return Status == other.Status
                && Manifest.Equals(other.Manifest)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveManifestReadResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ Manifest.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string messageText = HasMessage ? Message : "<none>";
            int count = HasManifest ? Manifest.Count : 0;
            return $"status='{Status}' hasManifest='{HasManifest}' entries='{count}' message='{messageText}'";
        }

        public static ProgressionSaveManifestReadResult Found(ProgressionSaveManifest manifest, string message)
        {
            return new ProgressionSaveManifestReadResult(ProgressionSaveReadStatus.Found, manifest, message);
        }

        public static ProgressionSaveManifestReadResult Missing(string message)
        {
            return new ProgressionSaveManifestReadResult(ProgressionSaveReadStatus.Missing, default, message);
        }

        public static ProgressionSaveManifestReadResult Rejected(string message)
        {
            return new ProgressionSaveManifestReadResult(ProgressionSaveReadStatus.Rejected, default, message);
        }

        public static ProgressionSaveManifestReadResult Corrupt(string message)
        {
            return new ProgressionSaveManifestReadResult(ProgressionSaveReadStatus.Corrupt, default, message);
        }

        public static ProgressionSaveManifestReadResult BackendUnavailable(string message)
        {
            return new ProgressionSaveManifestReadResult(ProgressionSaveReadStatus.BackendUnavailable, default, message);
        }

        public static ProgressionSaveManifestReadResult FailedResult(string message)
        {
            return new ProgressionSaveManifestReadResult(ProgressionSaveReadStatus.Failed, default, message);
        }

        private static void ValidateStatus(ProgressionSaveReadStatus status)
        {
            if (!Enum.IsDefined(typeof(ProgressionSaveReadStatus), status) || status == ProgressionSaveReadStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Progression Save manifest read status must be explicit.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
