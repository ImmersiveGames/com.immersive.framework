using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Immutable logical record stored behind a Progression Save slot.
    /// It carries payload bytes but does not define the backend, path, serializer or save runtime request path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save slot record primitive; backend-agnostic.")]
    public readonly struct ProgressionSaveSlotRecord : IEquatable<ProgressionSaveSlotRecord>
    {
        public ProgressionSaveSlotRecord(
            ProgressionSaveSlotId slotId,
            ProgressionSaveRecordId recordId,
            ProgressionSavePayload payload,
            long createdUtcTicks,
            long updatedUtcTicks,
            string displayName,
            string source,
            string reason)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save slot record requires a valid slot id.", nameof(slotId));
            }

            if (!recordId.IsValid)
            {
                throw new ArgumentException("Progression Save slot record requires a valid record id.", nameof(recordId));
            }

            if (!payload.IsValid)
            {
                throw new ArgumentException("Progression Save slot record requires a valid payload. Use ProgressionSavePayload.Empty for explicit empty records.", nameof(payload));
            }

            ValidateTicks(createdUtcTicks, nameof(createdUtcTicks));
            ValidateTicks(updatedUtcTicks, nameof(updatedUtcTicks));

            if (updatedUtcTicks < createdUtcTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(updatedUtcTicks), updatedUtcTicks, "Progression Save updated UTC ticks cannot be earlier than created UTC ticks.");
            }

            SlotId = slotId;
            RecordId = recordId;
            Payload = payload;
            CreatedUtcTicks = createdUtcTicks;
            UpdatedUtcTicks = updatedUtcTicks;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ProgressionSaveSlotId SlotId { get; }

        public ProgressionSaveRecordId RecordId { get; }

        public ProgressionSavePayload Payload { get; }

        public long CreatedUtcTicks { get; }

        public long UpdatedUtcTicks { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public DateTime CreatedUtc => new DateTime(CreatedUtcTicks, DateTimeKind.Utc);

        public DateTime UpdatedUtc => new DateTime(UpdatedUtcTicks, DateTimeKind.Utc);

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => SlotId.IsValid
            && RecordId.IsValid
            && Payload.IsValid
            && IsValidTicks(CreatedUtcTicks)
            && IsValidTicks(UpdatedUtcTicks)
            && UpdatedUtcTicks >= CreatedUtcTicks;

        public ProgressionSaveManifestEntry ToManifestEntry()
        {
            return new ProgressionSaveManifestEntry(
                SlotId,
                RecordId,
                DisplayName,
                CreatedUtcTicks,
                UpdatedUtcTicks,
                Payload.Format,
                Payload.ByteCount,
                Source,
                Reason);
        }

        public bool Equals(ProgressionSaveSlotRecord other)
        {
            return SlotId.Equals(other.SlotId)
                && RecordId.Equals(other.RecordId)
                && Payload.Equals(other.Payload)
                && CreatedUtcTicks == other.CreatedUtcTicks
                && UpdatedUtcTicks == other.UpdatedUtcTicks
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveSlotRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SlotId.GetHashCode();
                hashCode = hashCode * 397 ^ RecordId.GetHashCode();
                hashCode = hashCode * 397 ^ Payload.GetHashCode();
                hashCode = hashCode * 397 ^ CreatedUtcTicks.GetHashCode();
                hashCode = hashCode * 397 ^ UpdatedUtcTicks.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string displayNameText = HasDisplayName ? DisplayName : "<none>";
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"slot='{SlotId.StableText}' record='{RecordId.StableText}' createdUtcTicks='{CreatedUtcTicks}' updatedUtcTicks='{UpdatedUtcTicks}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}' payload=({Payload.ToDiagnosticString()})";
        }

        public static bool operator ==(ProgressionSaveSlotRecord left, ProgressionSaveSlotRecord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveSlotRecord left, ProgressionSaveSlotRecord right)
        {
            return !left.Equals(right);
        }

        private static void ValidateTicks(long ticks, string name)
        {
            if (!IsValidTicks(ticks))
            {
                throw new ArgumentOutOfRangeException(name, ticks, "Progression Save UTC ticks must be a valid positive DateTime tick value.");
            }
        }

        private static bool IsValidTicks(long ticks)
        {
            return ticks > 0 && ticks <= DateTime.MaxValue.Ticks;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
