using System;
using System.Threading;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    internal sealed class ActivityEntryReadinessWaitScope : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly CancellationTokenSource _cancellationSource =
            new CancellationTokenSource();
        private bool _cancellationRequested;
        private bool _disposed;
        private string _cancellationReason = string.Empty;

        internal ActivityEntryReadinessWaitScope(
            TransitionOperationId operationId,
            ActivityReadinessOccurrence occurrence)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException(
                    "Activity entry-readiness wait scope requires a valid operation id.",
                    nameof(operationId));
            }

            if (!occurrence.IsValid)
            {
                throw new ArgumentException(
                    "Activity entry-readiness wait scope requires a valid occurrence.",
                    nameof(occurrence));
            }

            OperationId = operationId;
            Occurrence = occurrence;
        }

        internal TransitionOperationId OperationId { get; }
        internal ActivityReadinessOccurrence Occurrence { get; }
        internal CancellationToken Token => _cancellationSource.Token;

        internal bool CancellationRequested
        {
            get
            {
                lock (_syncRoot)
                {
                    return _cancellationRequested;
                }
            }
        }

        internal string CancellationReason
        {
            get
            {
                lock (_syncRoot)
                {
                    return _cancellationReason;
                }
            }
        }

        internal void Cancel(string reason)
        {
            lock (_syncRoot)
            {
                if (_disposed || _cancellationRequested)
                {
                    return;
                }

                _cancellationRequested = true;
                _cancellationReason = string.IsNullOrWhiteSpace(reason)
                    ? "ActivityEntryReadinessWaitInvalidated"
                    : reason.Trim();
                _cancellationSource.Cancel();
            }
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
                _cancellationSource.Dispose();
            }
        }
    }
}
