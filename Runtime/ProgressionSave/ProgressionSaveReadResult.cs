using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Backend-agnostic result for reading one Progression Save slot record.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save slot read result primitive.")]
    public readonly struct ProgressionSaveReadResult : IEquatable<ProgressionSaveReadResult>
    {
        public ProgressionSaveReadResult(ProgressionSaveReadStatus status, ProgressionSaveSlotId slotId, ProgressionSaveSlotRecord record, string message)
        {
            ValidateStatus(status);

            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save read result requires a valid slot id.", nameof(slotId));
            }

            if (status == ProgressionSaveReadStatus.Found && !record.IsValid)
            {
                throw new ArgumentException("Progression Save found read result requires a valid record.", nameof(record));
            }

            if (status != ProgressionSaveReadStatus.Found && record.IsValid)
            {
                throw new ArgumentException("Progression Save non-found read result cannot carry a valid record.", nameof(record));
            }

            if (record.IsValid && record.SlotId != slotId)
            {
                throw new ArgumentException("Progression Save read result record slot id must match the requested slot id.", nameof(record));
            }

            Status = status;
            SlotId = slotId;
            Record = record;
            Message = Normalize(message);
        }

        public ProgressionSaveReadStatus Status { get; }

        public ProgressionSaveSlotId SlotId { get; }

        public ProgressionSaveSlotRecord Record { get; }

        public string Message { get; }

        public bool HasRecord => Status == ProgressionSaveReadStatus.Found && Record.IsValid;

        public bool Completed => Status is ProgressionSaveReadStatus.Found or ProgressionSaveReadStatus.Missing;

        public bool Failed => Status is ProgressionSaveReadStatus.Corrupt or ProgressionSaveReadStatus.BackendUnavailable or ProgressionSaveReadStatus.Failed or ProgressionSaveReadStatus.Rejected;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ProgressionSaveReadResult other)
        {
            return Status == other.Status
                && SlotId.Equals(other.SlotId)
                && Record.Equals(other.Record)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveReadResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ SlotId.GetHashCode();
                hashCode = hashCode * 397 ^ Record.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string messageText = HasMessage ? Message : "<none>";
            return $"status='{Status}' slot='{SlotId.StableText}' hasRecord='{HasRecord}' message='{messageText}'";
        }

        public static ProgressionSaveReadResult Found(ProgressionSaveSlotRecord record, string message)
        {
            return new ProgressionSaveReadResult(ProgressionSaveReadStatus.Found, record.SlotId, record, message);
        }

        public static ProgressionSaveReadResult Missing(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveReadResult(ProgressionSaveReadStatus.Missing, slotId, default, message);
        }

        public static ProgressionSaveReadResult Rejected(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveReadResult(ProgressionSaveReadStatus.Rejected, slotId, default, message);
        }

        public static ProgressionSaveReadResult Corrupt(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveReadResult(ProgressionSaveReadStatus.Corrupt, slotId, default, message);
        }

        public static ProgressionSaveReadResult BackendUnavailable(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveReadResult(ProgressionSaveReadStatus.BackendUnavailable, slotId, default, message);
        }

        public static ProgressionSaveReadResult FailedResult(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveReadResult(ProgressionSaveReadStatus.Failed, slotId, default, message);
        }

        private static void ValidateStatus(ProgressionSaveReadStatus status)
        {
            if (!Enum.IsDefined(typeof(ProgressionSaveReadStatus), status) || status == ProgressionSaveReadStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Progression Save read status must be explicit.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
