using System;
using System.Collections.Generic;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common
{
    internal static class ParticipantExecutor
    {
        internal static ParticipantExecutionResult Execute<TParticipant, TResult>(
            string source,
            string reason,
            IReadOnlyList<ParticipantExecutionEntry<TParticipant>> entries,
            Func<TParticipant, TResult> invoke,
            Func<ParticipantExecutionEntry<TParticipant>, TResult, bool> isResultValid,
            Func<ParticipantExecutionEntry<TParticipant>, TResult, bool> isResultSuccessful,
            Func<ParticipantExecutionEntry<TParticipant>, TResult, bool> isResultBlocking,
            Func<ParticipantExecutionEntry<TParticipant>, TResult, int> issueCountSelector,
            Func<ParticipantExecutionEntry<TParticipant>, Exception, ParticipantExecutionIssue> exceptionToIssue,
            Func<ParticipantExecutionEntry<TParticipant>, TResult, ParticipantExecutionIssue> invalidResultToIssue)
            where TParticipant : class
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (invoke == null)
            {
                throw new ArgumentNullException(nameof(invoke));
            }

            if (isResultValid == null)
            {
                throw new ArgumentNullException(nameof(isResultValid));
            }

            if (isResultSuccessful == null)
            {
                throw new ArgumentNullException(nameof(isResultSuccessful));
            }

            if (isResultBlocking == null)
            {
                throw new ArgumentNullException(nameof(isResultBlocking));
            }

            if (issueCountSelector == null)
            {
                throw new ArgumentNullException(nameof(issueCountSelector));
            }

            if (exceptionToIssue == null)
            {
                throw new ArgumentNullException(nameof(exceptionToIssue));
            }

            if (invalidResultToIssue == null)
            {
                throw new ArgumentNullException(nameof(invalidResultToIssue));
            }

            var orderedEntries = FrameworkCollectionCopy.ToArrayOrEmpty(entries);
            if (orderedEntries.Length == 0)
            {
                return new ParticipantExecutionResult(
                    source,
                    reason,
                    Array.Empty<string>(),
                    Array.Empty<ParticipantExecutionIssue>(),
                    0,
                    0,
                    0,
                    0,
                    0,
                    0);
            }

            ValidateEntries(orderedEntries);
            Array.Sort(orderedEntries, (left, right) => CompareEntries(left, right));

            var issues = new List<ParticipantExecutionIssue>(orderedEntries.Length);
            var executionOrder = new List<string>(orderedEntries.Length);
            int successfulCount = 0;
            int blockingFailureCount = 0;
            int optionalFailureCount = 0;
            int invalidResultCount = 0;
            int exceptionCount = 0;

            for (int i = 0; i < orderedEntries.Length; i++)
            {
                var entry = orderedEntries[i];
                executionOrder.Add(entry.Label);

                try
                {
                    TResult result = invoke(entry.Participant);
                    if (!isResultValid(entry, result))
                    {
                        issues.Add(invalidResultToIssue(entry, result));
                        invalidResultCount++;
                        continue;
                    }

                    int issueCount = Math.Max(0, issueCountSelector(entry, result));
                    if (!isResultSuccessful(entry, result))
                    {
                        bool blocking = isResultBlocking(entry, result);
                        issues.Add(CreateFailureIssue(entry, source, reason, issueCount, blocking));
                        if (blocking)
                        {
                            blockingFailureCount++;
                        }
                        else
                        {
                            optionalFailureCount++;
                        }

                        continue;
                    }

                    if (issueCount > 0)
                    {
                        issues.Add(CreateWarningIssue(entry, source, reason, issueCount));
                    }

                    successfulCount++;
                }
                catch (Exception exception)
                {
                    issues.Add(exceptionToIssue(entry, exception));
                    exceptionCount++;
                }
            }

            return new ParticipantExecutionResult(
                source,
                reason,
                executionOrder,
                issues,
                orderedEntries.Length,
                successfulCount,
                blockingFailureCount,
                optionalFailureCount,
                invalidResultCount,
                exceptionCount);
        }

        private static void ValidateEntries<TParticipant>(ParticipantExecutionEntry<TParticipant>[] entries)
            where TParticipant : class
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].IsValid)
                {
                    throw new ArgumentException("Participant executor requires valid entries.", nameof(entries));
                }
            }
        }

        private static int CompareEntries<TParticipant>(ParticipantExecutionEntry<TParticipant> left, ParticipantExecutionEntry<TParticipant> right)
            where TParticipant : class
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            int sourceIndexComparison = left.SourceIndex.CompareTo(right.SourceIndex);
            if (sourceIndexComparison != 0)
            {
                return sourceIndexComparison;
            }

            return StringComparer.Ordinal.Compare(left.Label ?? string.Empty, right.Label ?? string.Empty);
        }

        private static ParticipantExecutionIssue CreateFailureIssue<TParticipant>(
            ParticipantExecutionEntry<TParticipant> entry,
            string source,
            string reason,
            int issueCount,
            bool blocking)
            where TParticipant : class
        {
            return new ParticipantExecutionIssue(
                blocking ? ParticipantExecutionIssueSeverity.Error : ParticipantExecutionIssueSeverity.Warning,
                entry.Label,
                source,
                reason,
                blocking ? "Participant failure is blocking." : "Participant failure is non-blocking.",
                Math.Max(1, issueCount));
        }

        private static ParticipantExecutionIssue CreateWarningIssue<TParticipant>(
            ParticipantExecutionEntry<TParticipant> entry,
            string source,
            string reason,
            int issueCount)
            where TParticipant : class
        {
            return new ParticipantExecutionIssue(
                ParticipantExecutionIssueSeverity.Warning,
                entry.Label,
                source,
                reason,
                "Participant completed with non-blocking issues.",
                Math.Max(1, issueCount));
        }
    }
}
