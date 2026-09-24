using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Backend-agnostic runtime request for Progression Save.
    /// It does not know file paths, JSON, PlayerPrefs, UI, Snapshot participants or Route/Activity lifecycle hooks.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save runtime request primitive; no lifecycle autosave hook.")]
    public readonly struct ProgressionSaveRequest : IEquatable<ProgressionSaveRequest>
    {
        public ProgressionSaveRequest(
            ProgressionSaveRequestId requestId,
            ProgressionSaveRequestKind kind,
            ProgressionSaveSlotId slotId,
            ProgressionSaveRecordId recordId,
            ProgressionSavePayload payload,
            string displayName,
            ProgressionSaveMoment moment,
            string source,
            string reason)
        {
            if (!requestId.IsValid)
            {
                throw new ArgumentException("Progression Save request requires a valid request id.", nameof(requestId));
            }

            if (!Enum.IsDefined(typeof(ProgressionSaveRequestKind), kind) || kind == ProgressionSaveRequestKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Progression Save request kind must be explicit.");
            }

            if (!slotId.IsValid)
            {
                throw new ArgumentException("Progression Save request requires a valid slot id.", nameof(slotId));
            }

            if (!moment.IsValid)
            {
                throw new ArgumentException("Progression Save request requires a valid moment descriptor.", nameof(moment));
            }

            if (kind == ProgressionSaveRequestKind.Save)
            {
                if (!recordId.IsValid)
                {
                    throw new ArgumentException("Progression Save save request requires a valid record id.", nameof(recordId));
                }

                if (!payload.IsValid)
                {
                    throw new ArgumentException("Progression Save save request requires a valid payload. Use ProgressionSavePayload.Empty for an explicit empty save.", nameof(payload));
                }
            }
            else
            {
                if (recordId.IsValid)
                {
                    throw new ArgumentException("Only Progression Save save requests can carry a record id.", nameof(recordId));
                }

                if (payload.IsValid)
                {
                    throw new ArgumentException("Only Progression Save save requests can carry a payload.", nameof(payload));
                }
            }

            RequestId = requestId;
            Kind = kind;
            SlotId = slotId;
            RecordId = recordId;
            Payload = payload;
            DisplayName = Normalize(displayName);
            Moment = moment;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ProgressionSaveRequestId RequestId { get; }

        public ProgressionSaveRequestKind Kind { get; }

        public ProgressionSaveSlotId SlotId { get; }

        public ProgressionSaveRecordId RecordId { get; }

        public ProgressionSavePayload Payload { get; }

        public string DisplayName { get; }

        public ProgressionSaveMoment Moment { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsSave => Kind == ProgressionSaveRequestKind.Save;

        public bool IsLoad => Kind == ProgressionSaveRequestKind.Load;

        public bool IsDelete => Kind == ProgressionSaveRequestKind.Delete;

        public bool HasRecord => RecordId.IsValid;

        public bool HasPayload => Payload.IsValid;

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => RequestId.IsValid
            && Kind != ProgressionSaveRequestKind.Unknown
            && SlotId.IsValid
            && Moment.IsValid
            && (IsSave && RecordId.IsValid && Payload.IsValid
                || !IsSave && !RecordId.IsValid && !Payload.IsValid);

        public bool Equals(ProgressionSaveRequest other)
        {
            return RequestId.Equals(other.RequestId)
                && Kind == other.Kind
                && SlotId.Equals(other.SlotId)
                && RecordId.Equals(other.RecordId)
                && Payload.Equals(other.Payload)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && Moment.Equals(other.Moment)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RequestId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ SlotId.GetHashCode();
                hashCode = hashCode * 397 ^ RecordId.GetHashCode();
                hashCode = hashCode * 397 ^ Payload.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ Moment.GetHashCode();
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
            string recordText = HasRecord ? RecordId.StableText : "<none>";
            string displayNameText = HasDisplayName ? DisplayName : "<none>";
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            string payloadText = HasPayload ? Payload.ToDiagnosticString() : "<none>";
            return $"request='{RequestId.StableText}' kind='{Kind}' slot='{SlotId.StableText}' record='{recordText}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}' moment=({Moment.ToDiagnosticString()}) payload=({payloadText})";
        }

        public static ProgressionSaveRequest Save(
            string requestId,
            ProgressionSaveSlotId slotId,
            ProgressionSaveRecordId recordId,
            ProgressionSavePayload payload,
            string displayName,
            ProgressionSaveMoment moment,
            string source,
            string reason)
        {
            return new ProgressionSaveRequest(
                ProgressionSaveRequestId.From(requestId),
                ProgressionSaveRequestKind.Save,
                slotId,
                recordId,
                payload,
                displayName,
                moment,
                source,
                reason);
        }

        public static ProgressionSaveRequest Load(
            string requestId,
            ProgressionSaveSlotId slotId,
            ProgressionSaveMoment moment,
            string source,
            string reason)
        {
            return new ProgressionSaveRequest(
                ProgressionSaveRequestId.From(requestId),
                ProgressionSaveRequestKind.Load,
                slotId,
                default,
                default,
                string.Empty,
                moment,
                source,
                reason);
        }

        public static ProgressionSaveRequest Delete(
            string requestId,
            ProgressionSaveSlotId slotId,
            ProgressionSaveMoment moment,
            string source,
            string reason)
        {
            return new ProgressionSaveRequest(
                ProgressionSaveRequestId.From(requestId),
                ProgressionSaveRequestKind.Delete,
                slotId,
                default,
                default,
                string.Empty,
                moment,
                source,
                reason);
        }

        public static bool operator ==(ProgressionSaveRequest left, ProgressionSaveRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveRequest left, ProgressionSaveRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
