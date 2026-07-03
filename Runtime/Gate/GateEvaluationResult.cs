using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Experimental. Aggregate result of evaluating one Gate decision against blocker/fact context.
    /// This is passive data; it does not execute the admitted operation, queue work or mutate lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F17B Gate evaluation result primitive with explicit blockers and facts.")]
    public readonly struct GateEvaluationResult : IEquatable<GateEvaluationResult>
    {
        private readonly GateBlocker[] _blockingBlockers;
        private readonly string[] _facts;

        public GateEvaluationResult(
            GateDecision decision,
            IReadOnlyList<GateBlocker> blockingBlockers,
            IReadOnlyList<string> facts)
        {
            if (!decision.IsValid)
            {
                throw new ArgumentException("Gate evaluation result requires a valid decision.", nameof(decision));
            }

            Decision = decision;
            _blockingBlockers = CopyBlockers(blockingBlockers);
            _facts = CopyFacts(facts);
        }

        public GateDecision Decision { get; }

        public IReadOnlyList<GateBlocker> BlockingBlockers => _blockingBlockers ?? Array.Empty<GateBlocker>();

        public IReadOnlyList<string> Facts => _facts ?? Array.Empty<string>();

        public GateDecisionStatus Status => Decision.Status;

        public GateScope Scope => Decision.Scope;

        public GateDomain Domain => Decision.Domain;

        public bool IsAllowed => Decision.IsAllowed;

        public bool IsBlocked => Decision.IsBlocked;

        public bool IsQueued => Decision.IsQueued;

        public bool IsRejected => Decision.IsRejected;

        public int BlockingBlockerCount => FrameworkIssueCounting.Count(BlockingBlockers);

        public int FactCount => FrameworkIssueCounting.Count(Facts);

        public bool HasBlockingBlockers => FrameworkIssueCounting.HasAny(BlockingBlockers);

        public bool HasFacts => FrameworkIssueCounting.HasAny(Facts);

        public bool IsValid => Decision.IsValid;

        public bool Equals(GateEvaluationResult other)
        {
            return Decision.Equals(other.Decision)
                && SequenceEquals(BlockingBlockers, other.BlockingBlockers)
                && SequenceEquals(Facts, other.Facts);
        }

        public override bool Equals(object obj)
        {
            return obj is GateEvaluationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Decision.GetHashCode();
                IReadOnlyList<GateBlocker> blockers = BlockingBlockers;
                for (int i = 0; i < blockers.Count; i++)
                {
                    hashCode = hashCode * 397 ^ blockers[i].GetHashCode();
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
            builder.Append(Decision.ToDiagnosticString());
            builder.Append(" blockers='");
            builder.Append(BlockingBlockerCount);
            builder.Append("' facts='");
            builder.Append(FactCount);
            builder.Append("'");

            if (HasBlockingBlockers)
            {
                builder.Append(" blocking=[");
                IReadOnlyList<GateBlocker> blockers = BlockingBlockers;
                for (int i = 0; i < blockers.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(blockers[i].ToDiagnosticString());
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

        public static GateEvaluationResult FromDecision(GateDecision decision)
        {
            return new GateEvaluationResult(decision, Array.Empty<GateBlocker>(), Array.Empty<string>());
        }

        public static GateEvaluationResult Allowed(GateDecision decision, IReadOnlyList<string> facts)
        {
            if (!decision.IsAllowed)
            {
                throw new ArgumentException("Allowed Gate evaluation result requires an allowed decision.", nameof(decision));
            }

            return new GateEvaluationResult(decision, Array.Empty<GateBlocker>(), facts);
        }

        public static GateEvaluationResult Blocked(
            GateDecision decision,
            IReadOnlyList<GateBlocker> blockers,
            IReadOnlyList<string> facts)
        {
            if (!decision.IsBlocked)
            {
                throw new ArgumentException("Blocked Gate evaluation result requires a blocked decision.", nameof(decision));
            }

            if (blockers == null || blockers.Count == 0)
            {
                throw new ArgumentException("Blocked Gate evaluation result requires at least one blocker.", nameof(blockers));
            }

            return new GateEvaluationResult(decision, blockers, facts);
        }

        public static GateEvaluationResult Rejected(GateDecision decision, IReadOnlyList<string> facts)
        {
            if (!decision.IsRejected)
            {
                throw new ArgumentException("Rejected Gate evaluation result requires a rejected decision.", nameof(decision));
            }

            return new GateEvaluationResult(decision, Array.Empty<GateBlocker>(), facts);
        }

        public static bool operator ==(GateEvaluationResult left, GateEvaluationResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GateEvaluationResult left, GateEvaluationResult right)
        {
            return !left.Equals(right);
        }

        private static GateBlocker[] CopyBlockers(IReadOnlyList<GateBlocker> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<GateBlocker>();
            }

            var copy = new GateBlocker[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Gate evaluation result cannot contain invalid blockers.", nameof(source));
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
    }
}
