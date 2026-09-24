using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive diagnostics snapshot for Transition Effect planning/results.
    /// This is not an effect registry, loading progress model, visual surface or adapter owner.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B passive Transition Effect snapshot; diagnostics only.")]
    public readonly struct TransitionEffectSnapshot : IEquatable<TransitionEffectSnapshot>
    {
        private readonly TransitionEffectRequest[] _plannedRequests;
        private readonly TransitionEffectResult[] _observedResults;
        private readonly string[] _facts;

        public TransitionEffectSnapshot(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            TransitionPhase currentPhase,
            TransitionEffectStatus status,
            string source,
            string reason,
            IReadOnlyList<TransitionEffectRequest> plannedRequests,
            IReadOnlyList<TransitionEffectResult> observedResults,
            IReadOnlyList<string> facts)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Effect snapshot requires a valid operation id.", nameof(operationId));
            }

            FrameworkEnumValidation.ThrowIfUndefinedOr(transitionKind, TransitionKind.Unknown, nameof(transitionKind), "Transition Effect snapshot transition kind must be explicit.");

            FrameworkEnumValidation.ThrowIfUndefinedOr(currentPhase, TransitionPhase.Unknown, nameof(currentPhase), "Transition Effect snapshot phase must be explicit.");

            FrameworkEnumValidation.ThrowIfUndefinedOr(status, TransitionEffectStatus.Unknown, nameof(status), "Transition Effect snapshot status must be explicit.");

            OperationId = operationId;
            TransitionKind = transitionKind;
            CurrentPhase = currentPhase;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            _plannedRequests = CopyRequests(operationId, transitionKind, plannedRequests);
            _observedResults = CopyResults(operationId, transitionKind, observedResults);
            _facts = CopyFacts(facts);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind TransitionKind { get; }

        public TransitionPhase CurrentPhase { get; }

        public TransitionEffectStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<TransitionEffectRequest> PlannedRequests => _plannedRequests ?? Array.Empty<TransitionEffectRequest>();

        public IReadOnlyList<TransitionEffectResult> ObservedResults => _observedResults ?? Array.Empty<TransitionEffectResult>();

        public IReadOnlyList<string> Facts => _facts ?? Array.Empty<string>();

        public int PlannedRequestCount => PlannedRequests.Count;

        public int ObservedResultCount => ObservedResults.Count;

        public int FactCount => Facts.Count;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<TransitionEffectResult> items = ObservedResults;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].BlocksTransition)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasPlannedRequests => PlannedRequestCount > 0;

        public bool HasObservedResults => ObservedResultCount > 0;

        public bool HasFacts => FactCount > 0;

        public bool IsValid => OperationId.IsValid
            && FrameworkEnumValidation.IsDefinedAndNot(TransitionKind, TransitionKind.Unknown)
            && FrameworkEnumValidation.IsDefinedAndNot(CurrentPhase, TransitionPhase.Unknown)
            && FrameworkEnumValidation.IsDefinedAndNot(Status, TransitionEffectStatus.Unknown);

        public bool Equals(TransitionEffectSnapshot other)
        {
            return OperationId.Equals(other.OperationId)
                && TransitionKind == other.TransitionKind
                && CurrentPhase == other.CurrentPhase
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SequenceEquals(PlannedRequests, other.PlannedRequests)
                && SequenceEquals(ObservedResults, other.ObservedResults)
                && SequenceEquals(Facts, other.Facts);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)TransitionKind;
                hashCode = hashCode * 397 ^ (int)CurrentPhase;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);

                IReadOnlyList<TransitionEffectRequest> requests = PlannedRequests;
                for (int i = 0; i < requests.Count; i++)
                {
                    hashCode = hashCode * 397 ^ requests[i].GetHashCode();
                }

                IReadOnlyList<TransitionEffectResult> results = ObservedResults;
                for (int i = 0; i < results.Count; i++)
                {
                    hashCode = hashCode * 397 ^ results[i].GetHashCode();
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
            builder.Append($"operation='{OperationId.StableText}' transitionKind='{TransitionKind}' phase='{CurrentPhase}' status='{Status}' source='{sourceText}' reason='{reasonText}' planned='{PlannedRequestCount}' observed='{ObservedResultCount}' facts='{FactCount}' blockingIssues='{BlockingIssueCount}'");

            if (HasPlannedRequests)
            {
                builder.Append(" plannedRequests=[");
                IReadOnlyList<TransitionEffectRequest> requests = PlannedRequests;
                for (int i = 0; i < requests.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(requests[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            if (HasObservedResults)
            {
                builder.Append(" observedResults=[");
                IReadOnlyList<TransitionEffectResult> results = ObservedResults;
                for (int i = 0; i < results.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(results[i].ToDiagnosticString());
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

        public static TransitionEffectSnapshot FromPlan(
            TransitionEffectPlan plan,
            TransitionPhase currentPhase,
            TransitionEffectStatus status,
            IReadOnlyList<string> facts)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition Effect snapshot requires a valid plan.", nameof(plan));
            }

            return new TransitionEffectSnapshot(
                plan.OperationId,
                plan.TransitionKind,
                currentPhase,
                status,
                plan.Source,
                plan.Reason,
                plan.Requests,
                Array.Empty<TransitionEffectResult>(),
                facts);
        }

        public static TransitionEffectSnapshot FromResults(
            TransitionEffectPlan plan,
            TransitionPhase currentPhase,
            TransitionEffectStatus status,
            IReadOnlyList<TransitionEffectResult> results,
            IReadOnlyList<string> facts)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition Effect snapshot requires a valid plan.", nameof(plan));
            }

            return new TransitionEffectSnapshot(
                plan.OperationId,
                plan.TransitionKind,
                currentPhase,
                status,
                plan.Source,
                plan.Reason,
                plan.Requests,
                results,
                facts);
        }

        private static TransitionEffectRequest[] CopyRequests(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            IReadOnlyList<TransitionEffectRequest> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectRequest>();
            }

            var copy = new TransitionEffectRequest[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain invalid planned requests.", nameof(source));
                }

                if (source[i].OperationId != operationId || source[i].TransitionKind != transitionKind)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain planned requests from another Transition.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static TransitionEffectResult[] CopyResults(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            IReadOnlyList<TransitionEffectResult> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectResult>();
            }

            var copy = new TransitionEffectResult[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain invalid observed results.", nameof(source));
                }

                if (source[i].OperationId != operationId || source[i].TransitionKind != transitionKind)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain observed results from another Transition.", nameof(source));
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

            string[] copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = Normalize(source[i]);
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<TransitionEffectRequest> left, IReadOnlyList<TransitionEffectRequest> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SequenceEquals(IReadOnlyList<TransitionEffectResult> left, IReadOnlyList<TransitionEffectResult> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
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
}
