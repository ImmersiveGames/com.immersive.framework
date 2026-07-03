using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.TransitionEffects;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Passive immutable result for a transition operation.
    /// The result aggregates observed steps and issues; it does not mutate lifecycle or release Gate blockers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B passive Transition result primitive; diagnostics only.")]
    public readonly struct TransitionResult : IEquatable<TransitionResult>
    {
        private readonly TransitionStep[] _observedSteps;
        private readonly string[] _issues;
        private readonly TransitionEffectKind _effectKind;
        private readonly TransitionEffectStatus _effectStatus;
        private readonly int _effectAdapterCount;
        private readonly string _visualText;
        private readonly int _effectBlockingIssueCount;
        private readonly TransitionEffectAdapterEvidence[] _effectAdapterEvidence;

        public TransitionResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionStatus status,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
            : this(
                operationId,
                kind,
                status,
                source,
                reason,
                message,
                observedSteps,
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount,
                null)
        {
        }

        private TransitionResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionStatus status,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind,
            TransitionEffectStatus effectStatus,
            int effectAdapterCount,
            string visualText,
            int effectBlockingIssueCount,
            IReadOnlyList<TransitionEffectAdapterEvidence> effectAdapterEvidence)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition result requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), kind) || kind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Transition result kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionStatus), status) || status == TransitionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition result status must be explicit.");
            }

            OperationId = operationId;
            Kind = kind;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            _observedSteps = CopySteps(observedSteps);
            _issues = CopyIssues(issues);
            _effectKind = effectKind;
            _effectStatus = effectStatus;
            _effectAdapterCount = Math.Max(0, effectAdapterCount);
            _visualText = NormalizeVisual(visualText);
            _effectBlockingIssueCount = Math.Max(0, effectBlockingIssueCount);
            _effectAdapterEvidence = CopyAdapterEvidence(effectAdapterEvidence);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind Kind { get; }

        public TransitionStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public IReadOnlyList<TransitionStep> ObservedSteps => _observedSteps ?? Array.Empty<TransitionStep>();

        public IReadOnlyList<string> Issues => _issues ?? Array.Empty<string>();

        public int ObservedStepCount => ObservedSteps.Count;

        public int IssueCount => Issues.Count;

        public TransitionEffectKind EffectKind => _effectKind;

        public TransitionEffectStatus EffectStatus => _effectStatus;

        public int EffectAdapterCount => _effectAdapterCount;

        public string VisualText => _visualText;

        public int EffectBlockingIssueCount => _effectBlockingIssueCount;

        internal IReadOnlyList<TransitionEffectAdapterEvidence> EffectAdapterEvidence =>
            _effectAdapterEvidence ?? Array.Empty<TransitionEffectAdapterEvidence>();

        internal int EffectAdapterEvidenceCount => EffectAdapterEvidence.Count;

        internal int AppliedEffectAdapterEvidenceCount => CountEffectAdapterEvidenceApplied();

        internal int SkippedEffectAdapterEvidenceCount => CountEffectAdapterEvidenceSkipped();

        internal int FailedEffectAdapterEvidenceCount => CountEffectAdapterEvidenceFailed();

        internal int EffectAdapterEvidenceBlockingIssueCount => CountEffectAdapterEvidenceBlockingIssues();

        internal bool HasEffectAdapterEvidence => EffectAdapterEvidenceCount > 0;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<TransitionStep> items = ObservedSteps;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].BlockingIssue)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasObservedSteps => ObservedStepCount > 0;

        public bool HasIssues => IssueCount > 0;

        public bool HasEffectDiagnostics => EffectKind != TransitionEffectKind.Unknown
            || EffectStatus != TransitionEffectStatus.Skipped
            || EffectAdapterCount > 0
            || EffectAdapterEvidenceCount > 0
            || EffectBlockingIssueCount > 0
            || !string.Equals(VisualText, "NoneConfigured", StringComparison.Ordinal);

        public bool IsValid => OperationId.IsValid && Kind != TransitionKind.Unknown && Status != TransitionStatus.Unknown;

        public bool Succeeded => Status == TransitionStatus.Succeeded;

        public bool CompletedWithWarnings => Status == TransitionStatus.CompletedWithWarnings;

        public bool Failed => Status == TransitionStatus.Failed;

        public bool Rejected => Status == TransitionStatus.Rejected;

        public bool Cancelled => Status == TransitionStatus.Cancelled;

        public bool Completed => Succeeded || CompletedWithWarnings;

        public bool Equals(TransitionResult other)
        {
            return OperationId.Equals(other.OperationId)
                && Kind == other.Kind
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(ObservedSteps, other.ObservedSteps)
                && SequenceEquals(Issues, other.Issues)
                && EffectKind == other.EffectKind
                && EffectStatus == other.EffectStatus
                && EffectAdapterCount == other.EffectAdapterCount
                && string.Equals(VisualText, other.VisualText, StringComparison.Ordinal)
                && EffectBlockingIssueCount == other.EffectBlockingIssueCount
                && SequenceEquals(EffectAdapterEvidence, other.EffectAdapterEvidence);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hashCode = hashCode * 397 ^ (int)EffectKind;
                hashCode = hashCode * 397 ^ (int)EffectStatus;
                hashCode = hashCode * 397 ^ EffectAdapterCount;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(VisualText ?? string.Empty);
                hashCode = hashCode * 397 ^ EffectBlockingIssueCount;
                IReadOnlyList<TransitionEffectAdapterEvidence> adapterEvidence = EffectAdapterEvidence;
                for (int i = 0; i < adapterEvidence.Count; i++)
                {
                    hashCode = hashCode * 397 ^ adapterEvidence[i].GetHashCode();
                }

                IReadOnlyList<TransitionStep> steps = ObservedSteps;
                for (int i = 0; i < steps.Count; i++)
                {
                    hashCode = hashCode * 397 ^ steps[i].GetHashCode();
                }

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
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string effectText = EffectKind != TransitionEffectKind.Unknown ? EffectKind.ToString() : VisualText;
            builder.Append(
                $"operation='{OperationId.StableText}' kind='{Kind}' status='{Status}' source='{sourceText}' reason='{reasonText}' observedSteps='{ObservedStepCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' message='{messageText}' effectKind='{effectText}' effectStatus='{EffectStatus}' effectAdapters='{EffectAdapterCount}' visual='{VisualText}' effectBlockingIssues='{EffectBlockingIssueCount}' effectAdapterEvidence='{EffectAdapterEvidenceCount}' effectAdapterEvidenceApplied='{AppliedEffectAdapterEvidenceCount}' effectAdapterEvidenceSkipped='{SkippedEffectAdapterEvidenceCount}' effectAdapterEvidenceFailed='{FailedEffectAdapterEvidenceCount}' effectAdapterEvidenceBlockingIssues='{EffectAdapterEvidenceBlockingIssueCount}'");

            if (HasObservedSteps)
            {
                builder.Append(" observed=[");
                IReadOnlyList<TransitionStep> steps = ObservedSteps;
                for (int i = 0; i < steps.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(steps[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

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

            if (HasEffectAdapterEvidence)
            {
                builder.Append(" effectAdapterEvidence=[");
                IReadOnlyList<TransitionEffectAdapterEvidence> adapterEvidence = EffectAdapterEvidence;
                for (int i = 0; i < adapterEvidence.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(adapterEvidence[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static TransitionResult SucceededResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Succeeded,
                source,
                reason,
                message,
                observedSteps,
                Array.Empty<string>(),
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }


        public static TransitionResult SkippedResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Skipped,
                source,
                reason,
                message,
                observedSteps,
                Array.Empty<string>(),
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static TransitionResult CompletedWithWarningsResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.CompletedWithWarnings,
                source,
                reason,
                message,
                observedSteps,
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static TransitionResult FailedResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Failed,
                source,
                reason,
                message,
                observedSteps,
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static TransitionResult RejectedResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Rejected,
                source,
                reason,
                message,
                Array.Empty<TransitionStep>(),
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        internal TransitionResult WithEffectAdapterEvidence(
            IReadOnlyList<TransitionEffectAdapterEvidence> effectAdapterEvidence)
        {
            return new TransitionResult(
                OperationId,
                Kind,
                Status,
                Source,
                Reason,
                Message,
                ObservedSteps,
                Issues,
                EffectKind,
                EffectStatus,
                EffectAdapterCount,
                VisualText,
                EffectBlockingIssueCount,
                effectAdapterEvidence);
        }

        public static bool operator ==(TransitionResult left, TransitionResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionResult left, TransitionResult right)
        {
            return !left.Equals(right);
        }

        private static TransitionStep[] CopySteps(IReadOnlyList<TransitionStep> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionStep>();
            }

            var copy = new TransitionStep[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition result cannot contain invalid observed steps.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new List<string>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i]))
                {
                    continue;
                }

                copy.Add(source[i].Trim());
            }

            return copy.Count == 0 ? Array.Empty<string>() : copy.ToArray();
        }

        private static TransitionEffectAdapterEvidence[] CopyAdapterEvidence(
            IReadOnlyList<TransitionEffectAdapterEvidence> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectAdapterEvidence>();
            }

            var copy = new TransitionEffectAdapterEvidence[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static bool SequenceEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < left.Count; i++)
            {
                if (!comparer.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private int CountEffectAdapterEvidenceApplied()
        {
            int count = 0;
            IReadOnlyList<TransitionEffectAdapterEvidence> adapterEvidence = EffectAdapterEvidence;
            for (int i = 0; i < adapterEvidence.Count; i++)
            {
                if (adapterEvidence[i].Applied)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountEffectAdapterEvidenceSkipped()
        {
            int count = 0;
            IReadOnlyList<TransitionEffectAdapterEvidence> adapterEvidence = EffectAdapterEvidence;
            for (int i = 0; i < adapterEvidence.Count; i++)
            {
                if (adapterEvidence[i].Skipped)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountEffectAdapterEvidenceFailed()
        {
            int count = 0;
            IReadOnlyList<TransitionEffectAdapterEvidence> adapterEvidence = EffectAdapterEvidence;
            for (int i = 0; i < adapterEvidence.Count; i++)
            {
                if (adapterEvidence[i].Failed)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountEffectAdapterEvidenceBlockingIssues()
        {
            int count = 0;
            IReadOnlyList<TransitionEffectAdapterEvidence> adapterEvidence = EffectAdapterEvidence;
            for (int i = 0; i < adapterEvidence.Count; i++)
            {
                count += adapterEvidence[i].BlockingIssueCount;
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }

        private static string NormalizeVisual(string value)
        {
            return value.NormalizeTextOrFallback("NoneConfigured");
        }
    }
}
