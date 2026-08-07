using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    internal sealed class ActivityEntryReadinessActiveOperation : IDisposable
    {
        private readonly TaskCompletionSource<bool> _unwindCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _syncRoot = new object();
        private bool _completed;
        private bool _disposed;

        internal ActivityEntryReadinessActiveOperation(
            TransitionOperationId operationId,
            ActivityReadinessOccurrence occurrence,
            RouteAsset route)
        {
            OperationId = operationId;
            Occurrence = occurrence;
            Route = route;
            WaitScope = new ActivityEntryReadinessWaitScope(operationId, occurrence);
        }

        internal TransitionOperationId OperationId { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal RouteAsset Route { get; }
        internal ActivityEntryReadinessWaitScope WaitScope { get; }
        internal Task Unwound => _unwindCompletion.Task;

        internal bool OwnsRoute(RouteAsset route)
        {
            // IF-ID-03: wait ownership is the exact Route definition reference.
            return route != null && Route != null && ReferenceEquals(Route, route);
        }

        internal bool OwnsActivity(ActivityAsset activity)
        {
            // IF-ID-04: wait ownership is the exact Activity definition reference
            // (occurrence sequence remains on Occurrence / WaitScope).
            return activity != null &&
                Occurrence.IsValid &&
                Occurrence.Activity != null &&
                ReferenceEquals(Occurrence.Activity, activity);
        }

        internal void RequestCancellation(
            ActivityEntryReadinessInterruptionReason interruptionReason,
            string replacementRouteName = null)
        {
            WaitScope.Cancel(interruptionReason, replacementRouteName);
        }

        internal void CompleteUnwind()
        {
            lock (_syncRoot)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
            }

            WaitScope.Dispose();
            _unwindCompletion.TrySetResult(true);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            CompleteUnwind();
        }
    }
}
