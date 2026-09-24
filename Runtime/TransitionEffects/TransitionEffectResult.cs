using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive result for one Transition Effect request.
    /// The result records adapter-facing diagnostics and does not execute, release or fallback any visual effect.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B passive Transition Effect result; diagnostics only.")]
    public readonly struct TransitionEffectResult : IEquatable<TransitionEffectResult>
    {
        private readonly string[] _issues;

        public TransitionEffectResult(
            TransitionEffectRequest request,
            TransitionEffectStatus status,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Transition Effect result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(TransitionEffectStatus), status) || status == TransitionEffectStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition Effect result status must be explicit.");
            }

            Request = request;
            Status = status;
            Message = Normalize(message);
            _issues = CopyIssues(issues);
        }

        public TransitionEffectRequest Request { get; }

        public TransitionEffectId EffectId => Request.EffectId;

        public TransitionEffectKind EffectKind => Request.EffectKind;

        public TransitionEffectRequiredness Requiredness => Request.Requiredness;

        public TransitionOperationId OperationId => Request.OperationId;

        public TransitionKind TransitionKind => Request.TransitionKind;

        public TransitionPhase Phase => Request.Phase;

        public TransitionEffectStatus Status { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => _issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool IsRequired => Request.IsRequired;

        public bool IsOptional => Request.IsOptional;

        public bool Succeeded => Status == TransitionEffectStatus.Succeeded;

        public bool CompletedWithWarnings => Status == TransitionEffectStatus.CompletedWithWarnings;

        public bool Skipped => Status == TransitionEffectStatus.Skipped;

        public bool Failed => Status == TransitionEffectStatus.Failed;

        public bool Rejected => Status == TransitionEffectStatus.Rejected;

        public bool MissingAdapter => Status == TransitionEffectStatus.MissingAdapter;

        public bool Completed => Succeeded || CompletedWithWarnings || Skipped;

        public bool BlocksTransition => IsRequired && (Failed || Rejected || MissingAdapter);

        public bool IsValid => Request.IsValid && Status != TransitionEffectStatus.Unknown;

        public bool Equals(TransitionEffectResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                IReadOnlyList<string> issueItems = Issues;
                for (int i = 0; i < issueItems.Count; i++)
                {
                    hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(issueItems[i] ?? string.Empty);
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            string messageText = Message.ToDiagnosticText();
            builder.Append($"effect='{EffectId.StableText}' effectKind='{EffectKind}' requiredness='{Requiredness}' operation='{OperationId.StableText}' transitionKind='{TransitionKind}' phase='{Phase}' status='{Status}' completed='{Completed}' blocksTransition='{BlocksTransition}' issues='{IssueCount}' message='{messageText}'");

            if (HasIssues)
            {
                builder.Append(" issues=[");
                IReadOnlyList<string> issueItems = Issues;
                for (int i = 0; i < issueItems.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(issueItems[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static TransitionEffectResult SucceededResult(TransitionEffectRequest request, string message)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Succeeded, message, Array.Empty<string>());
        }

        public static TransitionEffectResult CompletedWithWarningsResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.CompletedWithWarnings, message, issues);
        }

        public static TransitionEffectResult SkippedResult(TransitionEffectRequest request, string message)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Skipped, message, Array.Empty<string>());
        }

        public static TransitionEffectResult FailedResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Failed, message, issues);
        }

        public static TransitionEffectResult RejectedResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Rejected, message, issues);
        }

        public static TransitionEffectResult MissingAdapterResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.MissingAdapter, message, issues);
        }

        public static bool operator ==(TransitionEffectResult left, TransitionEffectResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionEffectResult left, TransitionEffectResult right)
        {
            return !left.Equals(right);
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = Normalize(source[i]);
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }

    /// <summary>
    /// API status: Experimental. Named evidence for one Transition Effect adapter execution.
    /// This is Transition-specific diagnostics, not a generic adapter result contract.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F55 Transition Effect adapter evidence; domain-specific diagnostics.")]
    internal readonly struct TransitionEffectAdapterEvidence : IEquatable<TransitionEffectAdapterEvidence>
    {
        public TransitionEffectAdapterEvidence(
            string adapterName,
            TransitionEffectStatus status,
            int issueCount,
            int blockingIssueCount,
            string message)
        {
            if (!Enum.IsDefined(typeof(TransitionEffectStatus), status) || status == TransitionEffectStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition Effect adapter evidence status must be explicit.");
            }

            AdapterName = adapterName.NormalizeTextOrFallback("Transition Effect Adapter");
            Status = status;
            IssueCount = Math.Max(0, issueCount);
            BlockingIssueCount = Math.Max(0, blockingIssueCount);
            Message = message.NormalizeText();
        }

        public string AdapterName { get; }

        public TransitionEffectStatus Status { get; }

        public int IssueCount { get; }

        public int BlockingIssueCount { get; }

        public string Message { get; }

        public bool Applied => Status == TransitionEffectStatus.Succeeded
            || Status == TransitionEffectStatus.CompletedWithWarnings;

        public bool Skipped => Status == TransitionEffectStatus.Skipped;

        public bool Failed => Status == TransitionEffectStatus.Failed
            || Status == TransitionEffectStatus.Rejected
            || Status == TransitionEffectStatus.MissingAdapter;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool Equals(TransitionEffectAdapterEvidence other)
        {
            return string.Equals(AdapterName, other.AdapterName, StringComparison.Ordinal)
                && Status == other.Status
                && IssueCount == other.IssueCount
                && BlockingIssueCount == other.BlockingIssueCount
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectAdapterEvidence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(AdapterName ?? string.Empty);
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ BlockingIssueCount;
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
            string messageText = Message.ToDiagnosticText();
            return $"adapter='{AdapterName}' status='{Status}' applied='{Applied}' skipped='{Skipped}' failed='{Failed}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' message='{messageText}'";
        }

        public static TransitionEffectAdapterEvidence FromResult(
            string adapterName,
            TransitionEffectResult result)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException("Transition Effect adapter evidence requires a valid result.", nameof(result));
            }

            int blockingIssueCount = result.BlocksTransition
                ? Math.Max(1, result.IssueCount)
                : 0;
            return new TransitionEffectAdapterEvidence(
                adapterName,
                result.Status,
                result.IssueCount,
                blockingIssueCount,
                result.Message);
        }

        public static TransitionEffectAdapterEvidence MissingAdapter(
            string adapterName,
            int issueCount,
            int blockingIssueCount,
            string message)
        {
            return new TransitionEffectAdapterEvidence(
                adapterName,
                TransitionEffectStatus.MissingAdapter,
                issueCount,
                Math.Max(1, blockingIssueCount),
                message);
        }

        public static bool operator ==(TransitionEffectAdapterEvidence left, TransitionEffectAdapterEvidence right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionEffectAdapterEvidence left, TransitionEffectAdapterEvidence right)
        {
            return !left.Equals(right);
        }
    }
}
