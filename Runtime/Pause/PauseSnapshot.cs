using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive immutable snapshot of logical Pause state.
    /// The snapshot may be logged or inspected, but it does not own UI, input, Time.timeScale or Gate execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B passive Pause snapshot primitive; diagnostics only.")]
    public readonly struct PauseSnapshot : IEquatable<PauseSnapshot>
    {
        private readonly PauseIssue[] _issues;
        private readonly string[] _facts;

        public PauseSnapshot(
            PauseState state,
            PauseRequestId lastRequestId,
            string source,
            string reason,
            IReadOnlyList<PauseIssue> issues,
            IReadOnlyList<string> facts)
        {
            if (!Enum.IsDefined(typeof(PauseState), state) || state == PauseState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Pause snapshot state must be explicit.");
            }

            State = state;
            LastRequestId = lastRequestId;
            Source = Normalize(source);
            Reason = Normalize(reason);
            _issues = CopyIssues(issues);
            _facts = CopyFacts(facts);
        }

        public PauseState State { get; }

        public PauseRequestId LastRequestId { get; }

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<PauseIssue> Issues => _issues ?? Array.Empty<PauseIssue>();

        public IReadOnlyList<string> Facts => _facts ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public int FactCount => Facts.Count;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<PauseIssue> items = Issues;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].BlocksRequest)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool IsPaused => State == PauseState.Paused;

        public bool IsRunning => State == PauseState.Running;

        public bool HasLastRequest => LastRequestId.IsValid;

        public bool HasIssues => IssueCount > 0;

        public bool HasFacts => FactCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool IsValid => State != PauseState.Unknown;

        public bool Equals(PauseSnapshot other)
        {
            return State == other.State
                && LastRequestId.Equals(other.LastRequestId)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues)
                && SequenceEquals(Facts, other.Facts);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)State;
                hashCode = hashCode * 397 ^ LastRequestId.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);

                IReadOnlyList<PauseIssue> issueItems = Issues;
                for (int i = 0; i < issueItems.Count; i++)
                {
                    hashCode = hashCode * 397 ^ issueItems[i].GetHashCode();
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
            string requestText = HasLastRequest ? LastRequestId.StableText : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            builder.Append($"state='{State}' paused='{IsPaused}' running='{IsRunning}' lastRequest='{requestText}' source='{sourceText}' reason='{reasonText}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' facts='{FactCount}'");

            if (HasIssues)
            {
                builder.Append(" issues=[");
                IReadOnlyList<PauseIssue> issueItems = Issues;
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

        public static PauseSnapshot FromState(PauseState state, string source, string reason, IReadOnlyList<string> facts)
        {
            return new PauseSnapshot(state, default, source, reason, Array.Empty<PauseIssue>(), facts);
        }

        public static PauseSnapshot FromResult(PauseResult result, IReadOnlyList<string> facts)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException("Pause snapshot requires a valid result.", nameof(result));
            }

            return new PauseSnapshot(result.CurrentState, result.RequestId, result.Request.Source, result.Request.Reason, result.Issues, facts);
        }

        public static bool operator ==(PauseSnapshot left, PauseSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseSnapshot left, PauseSnapshot right)
        {
            return !left.Equals(right);
        }

        private static PauseIssue[] CopyIssues(IReadOnlyList<PauseIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<PauseIssue>();
            }

            var copy = new PauseIssue[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Pause snapshot cannot contain invalid issues.", nameof(source));
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

        private static bool SequenceEquals(IReadOnlyList<PauseIssue> left, IReadOnlyList<PauseIssue> right)
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
