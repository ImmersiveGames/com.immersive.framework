using System.Threading;
using System.Threading.Tasks;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        internal Task<ActivityEntryReadinessWaitResult>
            WaitForActivityEntryReadinessAsync(
                ActivityReadinessOccurrence occurrence)
        {
            return WaitForActivityEntryReadinessAsync(
                occurrence,
                CancellationToken.None);
        }

        internal Task<ActivityEntryReadinessWaitResult>
            WaitForActivityEntryReadinessAsync(
                ActivityReadinessOccurrence occurrence,
                CancellationToken cancellationToken)
        {
            if (!TryResolveEntryReadinessOccurrenceState(
                    occurrence,
                    out ActivityReadinessOccurrenceState occurrenceState))
            {
                return Task.FromResult(
                    ActivityEntryReadinessWaitResult.Invalidation(
                        occurrence,
                        default,
                        "OccurrenceUnavailable",
                        1));
            }

            var waiter = new ActivityEntryReadinessWaiter(
                occurrenceState,
                cancellationToken);
            return waiter.Completion;
        }

        private bool TryResolveEntryReadinessOccurrenceState(
            ActivityReadinessOccurrence occurrence,
            out ActivityReadinessOccurrenceState occurrenceState)
        {
            occurrenceState = null;
            if (!occurrence.IsValid)
            {
                return false;
            }

            ActivityReadinessOccurrenceState current =
                _currentAuthorableReadinessState;
            if (current != null &&
                !current.IsInvalidated &&
                current.Occurrence.Matches(
                    occurrence.Activity,
                    occurrence.TransitionSequence))
            {
                occurrenceState = current;
                return true;
            }

            ActivityReadinessOccurrenceState pending =
                _pendingAuthorableReadinessState;
            if (pending == null ||
                pending.IsInvalidated ||
                !pending.Occurrence.Matches(
                    occurrence.Activity,
                    occurrence.TransitionSequence))
            {
                return false;
            }

            occurrenceState = pending;
            return true;
        }
    }
}
