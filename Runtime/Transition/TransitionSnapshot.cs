using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Passive immutable diagnostics snapshot for one Transition operation.
    /// This snapshot is not a registry, manager, visual progress model or lifecycle owner.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B passive Transition snapshot primitive; diagnostics only.")]
    public readonly struct TransitionSnapshot : IEquatable<TransitionSnapshot>
    {
        private readonly TransitionStep[] _observedSteps;
        private readonly string[] _facts;

        public TransitionSnapshot(
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionPhase currentPhase,
            TransitionStatus status,
            string source,
            string reason,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> facts)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition snapshot requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), kind) || kind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Transition snapshot kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionPhase), currentPhase) || currentPhase == TransitionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPhase), currentPhase, "Transition snapshot phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionStatus), status) || status == TransitionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition snapshot status must be explicit.");
            }

            OperationId = operationId;
            Kind = kind;
            CurrentPhase = currentPhase;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            _observedSteps = CopySteps(observedSteps);
            _facts = CopyFacts(facts);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind Kind { get; }

        public TransitionPhase CurrentPhase { get; }

        public TransitionStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<TransitionStep> ObservedSteps => _observedSteps ?? Array.Empty<TransitionStep>();

        public IReadOnlyList<string> Facts => _facts ?? Array.Empty<string>();

        public int ObservedStepCount => ObservedSteps.Count;

        public int FactCount => Facts.Count;

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

        public bool HasFacts => FactCount > 0;

        public bool IsValid => OperationId.IsValid
            && Kind != TransitionKind.Unknown
            && CurrentPhase != TransitionPhase.Unknown
            && Status != TransitionStatus.Unknown;

        public bool Equals(TransitionSnapshot other)
        {
            return OperationId.Equals(other.OperationId)
                && Kind == other.Kind
                && CurrentPhase == other.CurrentPhase
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SequenceEquals(ObservedSteps, other.ObservedSteps)
                && SequenceEquals(Facts, other.Facts);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ (int)CurrentPhase;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);

                IReadOnlyList<TransitionStep> steps = ObservedSteps;
                for (int i = 0; i < steps.Count; i++)
                {
                    hashCode = hashCode * 397 ^ steps[i].GetHashCode();
                }

                IReadOnlyList<string> factItems = Facts;
                for (int i = 0; i < factItems.Count; i++)
                {
                    hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(factItems[i] ?? string.Empty);
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
            builder.Append($"operation='{OperationId.StableText}' kind='{Kind}' phase='{CurrentPhase}' status='{Status}' source='{sourceText}' reason='{reasonText}' observedSteps='{ObservedStepCount}' facts='{FactCount}' blockingIssues='{BlockingIssueCount}'");

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

            if (HasFacts)
            {
                builder.Append(" facts=[");
                IReadOnlyList<string> factItems = Facts;
                for (int i = 0; i < factItems.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(factItems[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static TransitionSnapshot FromPlan(TransitionPlan plan, TransitionPhase currentPhase, TransitionStatus status, IReadOnlyList<string> facts)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition snapshot requires a valid plan.", nameof(plan));
            }

            return new TransitionSnapshot(
                plan.OperationId,
                plan.Kind,
                currentPhase,
                status,
                plan.Source,
                plan.Reason,
                plan.Steps,
                facts);
        }

        public static TransitionSnapshot FromResult(TransitionResult result, TransitionPhase currentPhase, IReadOnlyList<string> facts)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException("Transition snapshot requires a valid result.", nameof(result));
            }

            return new TransitionSnapshot(
                result.OperationId,
                result.Kind,
                currentPhase,
                result.Status,
                result.Source,
                result.Reason,
                result.ObservedSteps,
                facts);
        }

        public static bool operator ==(TransitionSnapshot left, TransitionSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionSnapshot left, TransitionSnapshot right)
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
                    throw new ArgumentException("Transition snapshot cannot contain invalid observed steps.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static string[] CopyFacts(IReadOnlyList<string> source)
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

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
