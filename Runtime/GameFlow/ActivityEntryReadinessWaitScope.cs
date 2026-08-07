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
        private ActivityEntryReadinessInterruptionReason _interruptionReason;
        private string _replacementRouteName = string.Empty;

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

        internal ActivityEntryReadinessInterruptionReason InterruptionReason
        {
            get
            {
                lock (_syncRoot)
                {
                    return _interruptionReason;
                }
            }
        }

        internal string CancellationDiagnostic
        {
            get
            {
                lock (_syncRoot)
                {
                    return string.IsNullOrWhiteSpace(_replacementRouteName)
                        ? _interruptionReason.ToString()
                        : $"{_interruptionReason} replacementRoute='{_replacementRouteName}'";
                }
            }
        }

        internal void Cancel(
            ActivityEntryReadinessInterruptionReason interruptionReason,
            string replacementRouteName = null)
        {
            lock (_syncRoot)
            {
                if (_disposed || _cancellationRequested)
                {
                    return;
                }

                _cancellationRequested = true;
                _interruptionReason = interruptionReason;
                _replacementRouteName = replacementRouteName ?? string.Empty;
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
