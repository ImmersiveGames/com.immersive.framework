using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Backend-agnostic result for deleting one Progression Save slot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save delete result primitive.")]
    public readonly struct ProgressionSaveDeleteResult : IEquatable<ProgressionSaveDeleteResult>
    {
        public ProgressionSaveDeleteResult(ProgressionSaveDeleteStatus status, ProgressionSaveSlotId slotId, string message)
        {
            ValidateStatus(status);

            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save delete result requires a valid slot id.", nameof(slotId));
            }

            Status = status;
            SlotId = slotId;
            Message = Normalize(message);
        }

        public ProgressionSaveDeleteStatus Status { get; }

        public ProgressionSaveSlotId SlotId { get; }

        public string Message { get; }

        public bool Completed => Status is ProgressionSaveDeleteStatus.Deleted or ProgressionSaveDeleteStatus.Missing;

        public bool Failed => Status is ProgressionSaveDeleteStatus.BackendUnavailable or ProgressionSaveDeleteStatus.Failed or ProgressionSaveDeleteStatus.Rejected;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ProgressionSaveDeleteResult other)
        {
            return Status == other.Status
                && SlotId.Equals(other.SlotId)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveDeleteResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ SlotId.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string messageText = HasMessage ? Message : "<none>";
            return $"status='{Status}' slot='{SlotId.StableText}' message='{messageText}'";
        }

        public static ProgressionSaveDeleteResult Deleted(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveDeleteResult(ProgressionSaveDeleteStatus.Deleted, slotId, message);
        }

        public static ProgressionSaveDeleteResult Missing(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveDeleteResult(ProgressionSaveDeleteStatus.Missing, slotId, message);
        }

        public static ProgressionSaveDeleteResult Rejected(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveDeleteResult(ProgressionSaveDeleteStatus.Rejected, slotId, message);
        }

        public static ProgressionSaveDeleteResult BackendUnavailable(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveDeleteResult(ProgressionSaveDeleteStatus.BackendUnavailable, slotId, message);
        }

        public static ProgressionSaveDeleteResult FailedResult(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveDeleteResult(ProgressionSaveDeleteStatus.Failed, slotId, message);
        }

        private static void ValidateStatus(ProgressionSaveDeleteStatus status)
        {
            if (!Enum.IsDefined(typeof(ProgressionSaveDeleteStatus), status) || status == ProgressionSaveDeleteStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Progression Save delete status must be explicit.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
