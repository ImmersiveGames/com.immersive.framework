using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Backend-agnostic result for writing a Progression Save slot record or manifest.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save write result primitive.")]
    public readonly struct ProgressionSaveWriteResult : IEquatable<ProgressionSaveWriteResult>
    {
        public ProgressionSaveWriteResult(ProgressionSaveWriteStatus status, ProgressionSaveSlotId slotId, ProgressionSaveRecordId recordId, string message)
        {
            ValidateStatus(status);
            ValidateSlotForStatus(status, slotId);
            ValidateRecordForStatus(status, recordId);

            Status = status;
            SlotId = slotId;
            RecordId = recordId;
            Message = Normalize(message);
        }

        public ProgressionSaveWriteStatus Status { get; }

        public ProgressionSaveSlotId SlotId { get; }

        public ProgressionSaveRecordId RecordId { get; }

        public string Message { get; }

        public bool Written => Status == ProgressionSaveWriteStatus.Written;

        public bool Failed => Status is ProgressionSaveWriteStatus.BackendUnavailable or ProgressionSaveWriteStatus.Failed or ProgressionSaveWriteStatus.Rejected;

        public bool HasSlot => SlotId.IsValid;

        public bool HasRecord => RecordId.IsValid;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ProgressionSaveWriteResult other)
        {
            return Status == other.Status
                && SlotId.Equals(other.SlotId)
                && RecordId.Equals(other.RecordId)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveWriteResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ SlotId.GetHashCode();
                hashCode = hashCode * 397 ^ RecordId.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string slotText = HasSlot ? SlotId.StableText : "<none>";
            string recordText = HasRecord ? RecordId.StableText : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"status='{Status}' slot='{slotText}' record='{recordText}' message='{messageText}'";
        }

        public static ProgressionSaveWriteResult SlotWritten(ProgressionSaveSlotRecord record, string message)
        {
            if (!record.IsValid)
            {
                throw new ArgumentException("Progression Save slot write result requires a valid record.", nameof(record));
            }

            return new ProgressionSaveWriteResult(ProgressionSaveWriteStatus.Written, record.SlotId, record.RecordId, message);
        }


        public static ProgressionSaveWriteResult Rejected(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveWriteResult(ProgressionSaveWriteStatus.Rejected, slotId, default, message);
        }

        public static ProgressionSaveWriteResult BackendUnavailable(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveWriteResult(ProgressionSaveWriteStatus.BackendUnavailable, slotId, default, message);
        }

        public static ProgressionSaveWriteResult FailedResult(ProgressionSaveSlotId slotId, string message)
        {
            return new ProgressionSaveWriteResult(ProgressionSaveWriteStatus.Failed, slotId, default, message);
        }

        private static void ValidateStatus(ProgressionSaveWriteStatus status)
        {
            if (!Enum.IsDefined(typeof(ProgressionSaveWriteStatus), status) || status == ProgressionSaveWriteStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Progression Save write status must be explicit.");
            }
        }

        private static void ValidateSlotForStatus(ProgressionSaveWriteStatus status, ProgressionSaveSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save slot write result requires a valid slot id.", nameof(slotId));
            }
        }

        private static void ValidateRecordForStatus(ProgressionSaveWriteStatus status, ProgressionSaveRecordId recordId)
        {
            if (status == ProgressionSaveWriteStatus.Written && !recordId.IsValid)
            {
                throw new ArgumentException("Progression Save written slot result requires a valid record id.", nameof(recordId));
            }

            if (status != ProgressionSaveWriteStatus.Written && recordId.IsValid)
            {
                throw new ArgumentException("Progression Save non-written slot result cannot carry a record id.", nameof(recordId));
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
