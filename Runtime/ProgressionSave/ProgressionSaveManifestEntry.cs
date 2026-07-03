using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Immutable manifest row for one logical Progression Save slot.
    /// It contains metadata only and does not carry saved gameplay payload bytes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save manifest entry primitive; metadata only.")]
    public readonly struct ProgressionSaveManifestEntry : IEquatable<ProgressionSaveManifestEntry>
    {
        public ProgressionSaveManifestEntry(
            ProgressionSaveSlotId slotId,
            ProgressionSaveRecordId recordId,
            string displayName,
            long createdUtcTicks,
            long updatedUtcTicks,
            ProgressionSavePayloadFormat payloadFormat,
            int payloadByteCount,
            string source,
            string reason)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save manifest entry requires a valid slot id.", nameof(slotId));
            }

            if (!recordId.IsValid)
            {
                throw new ArgumentException("Progression Save manifest entry requires a valid record id.", nameof(recordId));
            }

            ValidateTicks(createdUtcTicks, nameof(createdUtcTicks));
            ValidateTicks(updatedUtcTicks, nameof(updatedUtcTicks));

            if (updatedUtcTicks < createdUtcTicks)
            {
                throw new ArgumentOutOfRangeException(nameof(updatedUtcTicks), updatedUtcTicks, "Progression Save manifest updated UTC ticks cannot be earlier than created UTC ticks.");
            }

            if (!Enum.IsDefined(typeof(ProgressionSavePayloadFormat), payloadFormat) || payloadFormat == ProgressionSavePayloadFormat.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadFormat), payloadFormat, "Progression Save manifest entry requires an explicit payload format.");
            }

            if (payloadByteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadByteCount), payloadByteCount, "Progression Save manifest payload byte count cannot be negative.");
            }

            if (payloadFormat == ProgressionSavePayloadFormat.Empty && payloadByteCount != 0)
            {
                throw new ArgumentException("Progression Save manifest empty payload entries must have zero payload bytes.", nameof(payloadByteCount));
            }

            if (payloadFormat != ProgressionSavePayloadFormat.Empty && payloadByteCount == 0)
            {
                throw new ArgumentException("Progression Save manifest non-empty payload entries must have a positive payload byte count.", nameof(payloadByteCount));
            }

            SlotId = slotId;
            RecordId = recordId;
            DisplayName = Normalize(displayName);
            CreatedUtcTicks = createdUtcTicks;
            UpdatedUtcTicks = updatedUtcTicks;
            PayloadFormat = payloadFormat;
            PayloadByteCount = payloadByteCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ProgressionSaveSlotId SlotId { get; }

        public ProgressionSaveRecordId RecordId { get; }

        public string DisplayName { get; }

        public long CreatedUtcTicks { get; }

        public long UpdatedUtcTicks { get; }

        public ProgressionSavePayloadFormat PayloadFormat { get; }

        public int PayloadByteCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public DateTime CreatedUtc => new DateTime(CreatedUtcTicks, DateTimeKind.Utc);

        public DateTime UpdatedUtc => new DateTime(UpdatedUtcTicks, DateTimeKind.Utc);

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => SlotId.IsValid
            && RecordId.IsValid
            && IsValidTicks(CreatedUtcTicks)
            && IsValidTicks(UpdatedUtcTicks)
            && UpdatedUtcTicks >= CreatedUtcTicks
            && PayloadFormat != ProgressionSavePayloadFormat.Unknown
            && PayloadByteCount >= 0
            && (PayloadFormat == ProgressionSavePayloadFormat.Empty && PayloadByteCount == 0
                || PayloadFormat != ProgressionSavePayloadFormat.Empty && PayloadByteCount > 0);

        public bool MatchesSlot(ProgressionSaveSlotId slotId)
        {
            return SlotId == slotId;
        }

        public bool Equals(ProgressionSaveManifestEntry other)
        {
            return SlotId.Equals(other.SlotId)
                && RecordId.Equals(other.RecordId)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && CreatedUtcTicks == other.CreatedUtcTicks
                && UpdatedUtcTicks == other.UpdatedUtcTicks
                && PayloadFormat == other.PayloadFormat
                && PayloadByteCount == other.PayloadByteCount
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveManifestEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SlotId.GetHashCode();
                hashCode = hashCode * 397 ^ RecordId.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ CreatedUtcTicks.GetHashCode();
                hashCode = hashCode * 397 ^ UpdatedUtcTicks.GetHashCode();
                hashCode = hashCode * 397 ^ (int)PayloadFormat;
                hashCode = hashCode * 397 ^ PayloadByteCount;
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
            return $"slot='{SlotId.StableText}' record='{RecordId.StableText}' displayName='{displayNameText}' createdUtcTicks='{CreatedUtcTicks}' updatedUtcTicks='{UpdatedUtcTicks}' payloadFormat='{PayloadFormat}' payloadBytes='{PayloadByteCount}' source='{sourceText}' reason='{reasonText}'";
        }

        public static bool operator ==(ProgressionSaveManifestEntry left, ProgressionSaveManifestEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveManifestEntry left, ProgressionSaveManifestEntry right)
        {
            return !left.Equals(right);
        }

        private static void ValidateTicks(long ticks, string name)
        {
            if (!IsValidTicks(ticks))
            {
                throw new ArgumentOutOfRangeException(name, ticks, "Progression Save manifest UTC ticks must be a valid positive DateTime tick value.");
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
