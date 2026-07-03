using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Per-target result inside an Object Reset Group request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F39A Object Reset Group per-target result.")]
    public readonly struct ObjectResetGroupTargetResult : IEquatable<ObjectResetGroupTargetResult>
    {
        public ObjectResetGroupTargetResult(
            int index,
            string objectEntryId,
            bool skipped,
            bool hasResetResult,
            ObjectResetResult resetResult,
            string message)
        {
            Index = index;
            ObjectEntryId = objectEntryId.NormalizeText();
            Skipped = skipped;
            HasResetResult = hasResetResult;
            ResetResult = resetResult;
            Message = message.NormalizeText();
        }

        public int Index { get; }

        public string ObjectEntryId { get; }

        public bool Skipped { get; }

        public bool HasResetResult { get; }

        public ObjectResetResult ResetResult { get; }

        public string Message { get; }

        public bool Succeeded => HasResetResult && ResetResult.Succeeded;

        public bool CompletedWithWarnings => HasResetResult && ResetResult.CompletedWithWarnings;

        public bool Failed => !Skipped && (!HasResetResult || ResetResult.Failed);

        public int ParticipantCount => HasResetResult ? ResetResult.ParticipantCount : 0;

        public int ParticipantSucceededCount => HasResetResult ? ResetResult.ParticipantSucceededCount : 0;

        public int ParticipantSkippedCount => HasResetResult ? ResetResult.ParticipantSkippedCount : 0;

        public int ParticipantFailedCount => HasResetResult ? ResetResult.ParticipantFailedCount : 0;

        public int BlockingIssueCount => HasResetResult ? ResetResult.BlockingIssueCount : (Failed ? 1 : 0);

        public int NonBlockingIssueCount => HasResetResult ? ResetResult.NonBlockingIssueCount : 0;

        public bool Equals(ObjectResetGroupTargetResult other)
        {
            return Index == other.Index
                && string.Equals(ObjectEntryId, other.ObjectEntryId, StringComparison.Ordinal)
                && Skipped == other.Skipped
                && HasResetResult == other.HasResetResult
                && ResetResult.Equals(other.ResetResult)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetGroupTargetResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Index;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ObjectEntryId ?? string.Empty);
                hashCode = hashCode * 397 ^ (Skipped ? 1 : 0);
                hashCode = hashCode * 397 ^ (HasResetResult ? 1 : 0);
                hashCode = hashCode * 397 ^ ResetResult.GetHashCode();
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
            return $"index='{Index}' objectEntry='{ObjectEntryId.ToDiagnosticText()}' skipped='{Skipped}' hasResetResult='{HasResetResult}' succeeded='{Succeeded}' warnings='{CompletedWithWarnings}' failed='{Failed}' participants='{ParticipantCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' message='{Message.ToDiagnosticText()}'";
        }

        public static ObjectResetGroupTargetResult SkippedTarget(int index, string objectEntryId, string message)
        {
            return new ObjectResetGroupTargetResult(index, objectEntryId, skipped: true, hasResetResult: false, default, message);
        }

        public static ObjectResetGroupTargetResult FailedTarget(int index, string objectEntryId, string message)
        {
            return new ObjectResetGroupTargetResult(index, objectEntryId, skipped: false, hasResetResult: false, default, message);
        }

        public static ObjectResetGroupTargetResult FromResetResult(int index, string objectEntryId, ObjectResetResult resetResult)
        {
            return new ObjectResetGroupTargetResult(index, objectEntryId, skipped: false, hasResetResult: true, resetResult, resetResult.Message);
        }
    }
}
