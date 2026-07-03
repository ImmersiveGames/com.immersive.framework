using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive result of evaluating effect requiredness and authoring guardrails.
    /// This is not an effect registry, adapter owner, Transition owner or fallback path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19E Transition Effect policy evaluation; no adapter registry or runtime execution.")]
    public readonly struct TransitionEffectPolicyEvaluation : IEquatable<TransitionEffectPolicyEvaluation>
    {
        private readonly TransitionEffectPolicyIssue[] _issues;

        public TransitionEffectPolicyEvaluation(
            TransitionEffectPlan plan,
            string policySource,
            int adapterCount,
            int matchedRequestCount,
            int requiredAdapterMissingCount,
            int optionalAdapterMissingCount,
            int duplicateEffectIdCount,
            IReadOnlyList<TransitionEffectPolicyIssue> issues)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition Effect policy evaluation requires a valid plan.", nameof(plan));
            }

            Plan = plan;
            PolicySource = policySource.NormalizeText();
            AdapterCount = Math.Max(0, adapterCount);
            MatchedRequestCount = Math.Max(0, matchedRequestCount);
            RequiredAdapterMissingCount = Math.Max(0, requiredAdapterMissingCount);
            OptionalAdapterMissingCount = Math.Max(0, optionalAdapterMissingCount);
            DuplicateEffectIdCount = Math.Max(0, duplicateEffectIdCount);
            _issues = CopyIssues(issues);
        }

        public TransitionEffectPlan Plan { get; }

        public string PolicySource { get; }

        public int AdapterCount { get; }

        public int MatchedRequestCount { get; }

        public int RequiredAdapterMissingCount { get; }

        public int OptionalAdapterMissingCount { get; }

        public int DuplicateEffectIdCount { get; }

        public IReadOnlyList<TransitionEffectPolicyIssue> Issues => _issues ?? Array.Empty<TransitionEffectPolicyIssue>();

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<TransitionEffectPolicyIssue> items = Issues;
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

        public bool HasIssues => IssueCount > 0;

        public bool BlocksTransition => BlockingIssueCount > 0;

        public bool IsAllowed => !BlocksTransition;

        public bool IsValid => Plan.IsValid;

        public bool Equals(TransitionEffectPolicyEvaluation other)
        {
            return Plan.Equals(other.Plan)
                && string.Equals(PolicySource, other.PolicySource, StringComparison.Ordinal)
                && AdapterCount == other.AdapterCount
                && MatchedRequestCount == other.MatchedRequestCount
                && RequiredAdapterMissingCount == other.RequiredAdapterMissingCount
                && OptionalAdapterMissingCount == other.OptionalAdapterMissingCount
                && DuplicateEffectIdCount == other.DuplicateEffectIdCount
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectPolicyEvaluation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Plan.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(PolicySource ?? string.Empty);
                hashCode = hashCode * 397 ^ AdapterCount;
                hashCode = hashCode * 397 ^ MatchedRequestCount;
                hashCode = hashCode * 397 ^ RequiredAdapterMissingCount;
                hashCode = hashCode * 397 ^ OptionalAdapterMissingCount;
                hashCode = hashCode * 397 ^ DuplicateEffectIdCount;

                IReadOnlyList<TransitionEffectPolicyIssue> issueItems = Issues;
                for (int i = 0; i < issueItems.Count; i++)
                {
                    hashCode = hashCode * 397 ^ issueItems[i].GetHashCode();
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
            string sourceText = PolicySource.ToDiagnosticText();
            builder.Append($"operation='{Plan.OperationId.StableText}' transitionKind='{Plan.TransitionKind}' policySource='{sourceText}' allowed='{IsAllowed}' blocksTransition='{BlocksTransition}' adapters='{AdapterCount}' requests='{Plan.RequestCount}' matched='{MatchedRequestCount}' requiredMissing='{RequiredAdapterMissingCount}' optionalMissing='{OptionalAdapterMissingCount}' duplicates='{DuplicateEffectIdCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}'");

            if (HasIssues)
            {
                builder.Append(" policyIssues=[");
                IReadOnlyList<TransitionEffectPolicyIssue> issueItems = Issues;
                for (int i = 0; i < issueItems.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(issueItems[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        private static TransitionEffectPolicyIssue[] CopyIssues(IReadOnlyList<TransitionEffectPolicyIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectPolicyIssue>();
            }

            var copy = new TransitionEffectPolicyIssue[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition Effect policy evaluation cannot contain invalid issues.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<TransitionEffectPolicyIssue> left, IReadOnlyList<TransitionEffectPolicyIssue> right)
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
    }
}
