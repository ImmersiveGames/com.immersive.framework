using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. High-level result for one Progression Save runtime request.
    /// It maps store results without exposing JSON files, paths or backend implementation details as canonical API.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save runtime request result primitive.")]
    public readonly struct ProgressionSaveRequestResult : IEquatable<ProgressionSaveRequestResult>
    {
        public ProgressionSaveRequestResult(
            ProgressionSaveRequest request,
            ProgressionSaveRequestStatus status,
            ProgressionSaveBackendId backendId,
            ProgressionSaveSlotRecord record,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Progression Save request result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(ProgressionSaveRequestStatus), status) || status == ProgressionSaveRequestStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Progression Save request result status must be explicit.");
            }

            if (!backendId.IsValid)
            {
                throw new ArgumentException("Progression Save request result requires a valid backend id.", nameof(backendId));
            }

            if (status is ProgressionSaveRequestStatus.Saved or ProgressionSaveRequestStatus.Loaded && !record.IsValid)
            {
                throw new ArgumentException("Progression Save saved/loaded request result requires a valid record.", nameof(record));
            }

            if (status != ProgressionSaveRequestStatus.Saved && status != ProgressionSaveRequestStatus.Loaded && record.IsValid)
            {
                throw new ArgumentException("Progression Save request result cannot carry a record unless the status is Saved or Loaded.", nameof(record));
            }

            if (record.IsValid && record.SlotId != request.SlotId)
            {
                throw new ArgumentException("Progression Save request result record slot must match request slot.", nameof(record));
            }

            Request = request;
            Status = status;
            BackendId = backendId;
            Record = record;
            Message = Normalize(message);
        }

        public ProgressionSaveRequest Request { get; }

        public ProgressionSaveRequestId RequestId => Request.RequestId;

        public ProgressionSaveRequestKind Kind => Request.Kind;

        public ProgressionSaveSlotId SlotId => Request.SlotId;

        public ProgressionSaveMoment Moment => Request.Moment;

        public ProgressionSaveRequestStatus Status { get; }

        public ProgressionSaveBackendId BackendId { get; }

        public ProgressionSaveSlotRecord Record { get; }

        public string Message { get; }

        public bool HasRecord => Record.IsValid;

        public bool Completed => Status is ProgressionSaveRequestStatus.Saved or ProgressionSaveRequestStatus.Loaded or ProgressionSaveRequestStatus.Deleted or ProgressionSaveRequestStatus.Missing;

        public bool Failed => Status is ProgressionSaveRequestStatus.Rejected or ProgressionSaveRequestStatus.BackendUnavailable or ProgressionSaveRequestStatus.Corrupt or ProgressionSaveRequestStatus.Failed;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool IsValid => Request.IsValid
            && Status != ProgressionSaveRequestStatus.Unknown
            && BackendId.IsValid
            && (HasRecord && Status is ProgressionSaveRequestStatus.Saved or ProgressionSaveRequestStatus.Loaded
                || !HasRecord && Status != ProgressionSaveRequestStatus.Saved && Status != ProgressionSaveRequestStatus.Loaded);

        public bool Equals(ProgressionSaveRequestResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && BackendId.Equals(other.BackendId)
                && Record.Equals(other.Record)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveRequestResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ BackendId.GetHashCode();
                hashCode = hashCode * 397 ^ Record.GetHashCode();
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
            string recordText = HasRecord ? Record.RecordId.StableText : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"request='{RequestId.StableText}' kind='{Kind}' slot='{SlotId.StableText}' status='{Status}' completed='{Completed}' failed='{Failed}' backend='{BackendId.StableText}' record='{recordText}' momentKind='{Moment.Kind}' message='{messageText}'";
        }

        public static ProgressionSaveRequestResult Saved(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            ProgressionSaveSlotRecord record,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Saved, backendId, record, message);
        }

        public static ProgressionSaveRequestResult Loaded(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            ProgressionSaveSlotRecord record,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Loaded, backendId, record, message);
        }

        public static ProgressionSaveRequestResult Deleted(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Deleted, backendId, default, message);
        }

        public static ProgressionSaveRequestResult Missing(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Missing, backendId, default, message);
        }

        public static ProgressionSaveRequestResult Rejected(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Rejected, backendId, default, message);
        }

        public static ProgressionSaveRequestResult BackendUnavailable(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.BackendUnavailable, backendId, default, message);
        }

        public static ProgressionSaveRequestResult Corrupt(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Corrupt, backendId, default, message);
        }

        public static ProgressionSaveRequestResult FailedResult(
            ProgressionSaveRequest request,
            ProgressionSaveBackendId backendId,
            string message)
        {
            return new ProgressionSaveRequestResult(request, ProgressionSaveRequestStatus.Failed, backendId, default, message);
        }

        public static bool operator ==(ProgressionSaveRequestResult left, ProgressionSaveRequestResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveRequestResult left, ProgressionSaveRequestResult right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
