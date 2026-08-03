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
            return route != null && Route != null && Route.HasSameIdentity(route);
        }

        internal bool OwnsActivity(ActivityAsset activity)
        {
            return activity != null &&
                Occurrence.IsValid &&
                Occurrence.Activity != null &&
                Occurrence.Activity.HasSameIdentity(activity);
        }

        internal void RequestCancellation(string reason)
        {
            WaitScope.Cancel(reason);
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
