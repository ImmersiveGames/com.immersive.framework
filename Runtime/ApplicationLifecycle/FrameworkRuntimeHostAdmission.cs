using System;
using System.Threading.Tasks;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal enum FrameworkRuntimeHostPhase
    {
        Created = 0,
        Starting = 1,
        Operational = 2,
        Failed = 3,
        Stopping = 4,
        Finalizing = 5,
        Stopped = 6
    }

    internal sealed class FrameworkRuntimeHostAdmission
    {
        private readonly object _sync = new();
        private FrameworkRuntimeHostPhase _phase = FrameworkRuntimeHostPhase.Created;
        private int _activeOperations;
        private bool _stopRequestedDuringStart;
        private TaskCompletionSource<bool> _startupCompleted;
        private TaskCompletionSource<bool> _operationsDrained;

        internal FrameworkRuntimeHostPhase Phase
        {
            get
            {
                lock (_sync)
                {
                    return _phase;
                }
            }
        }

        internal int ActiveOperationCount
        {
            get
            {
                lock (_sync)
                {
                    return _activeOperations;
                }
            }
        }

        internal bool TryBeginStart()
        {
            lock (_sync)
            {
                if (_phase != FrameworkRuntimeHostPhase.Created)
                {
                    return false;
                }

                _phase = FrameworkRuntimeHostPhase.Starting;
                _stopRequestedDuringStart = false;
                _startupCompleted = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                return true;
            }
        }

        internal void CompleteStart(bool succeeded)
        {
            TaskCompletionSource<bool> startupCompleted;
            lock (_sync)
            {
                if (_phase != FrameworkRuntimeHostPhase.Starting &&
                    !(_phase == FrameworkRuntimeHostPhase.Stopping && _stopRequestedDuringStart))
                {
                    throw new InvalidOperationException(
                        "FrameworkRuntimeHost startup can complete only from Starting.");
                }

                if (_phase == FrameworkRuntimeHostPhase.Starting)
                {
                    _phase = _stopRequestedDuringStart
                        ? FrameworkRuntimeHostPhase.Stopping
                        : succeeded
                            ? FrameworkRuntimeHostPhase.Operational
                            : FrameworkRuntimeHostPhase.Failed;
                }

                startupCompleted = _startupCompleted;
                _startupCompleted = null;
                _stopRequestedDuringStart = false;
            }

            startupCompleted?.TrySetResult(true);
        }

        internal Task BeginStopAndWaitForOperationsAsync()
        {
            lock (_sync)
            {
                if (_phase == FrameworkRuntimeHostPhase.Stopped)
                {
                    return Task.CompletedTask;
                }

                if (_phase == FrameworkRuntimeHostPhase.Starting)
                {
                    _stopRequestedDuringStart = true;
                    return _startupCompleted != null
                        ? _startupCompleted.Task
                        : Task.CompletedTask;
                }

                _phase = FrameworkRuntimeHostPhase.Stopping;
                if (_activeOperations == 0)
                {
                    return Task.CompletedTask;
                }

                if (_operationsDrained == null || _operationsDrained.Task.IsCompleted)
                {
                    _operationsDrained = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }

                return _operationsDrained.Task;
            }
        }

        internal bool TryBeginStopFinalization()
        {
            lock (_sync)
            {
                if (_phase != FrameworkRuntimeHostPhase.Stopping || _activeOperations != 0)
                {
                    return false;
                }

                _phase = FrameworkRuntimeHostPhase.Finalizing;
                return true;
            }
        }

        internal void CompleteStop(bool succeeded)
        {
            lock (_sync)
            {
                if (_phase != FrameworkRuntimeHostPhase.Finalizing)
                {
                    throw new InvalidOperationException(
                        "FrameworkRuntimeHost stop can complete only from Finalizing.");
                }

                if (!succeeded)
                {
                    _phase = FrameworkRuntimeHostPhase.Stopping;
                    return;
                }

                if (_activeOperations != 0)
                {
                    throw new InvalidOperationException(
                        "FrameworkRuntimeHost cannot complete stop while operations remain active.");
                }

                _phase = FrameworkRuntimeHostPhase.Stopped;
                _operationsDrained = null;
            }
        }

        internal bool TryEnterOperation(bool cleanup, out IDisposable lease)
        {
            lock (_sync)
            {
                bool admitted = _phase == FrameworkRuntimeHostPhase.Operational ||
                    cleanup && _phase == FrameworkRuntimeHostPhase.Stopping;
                if (!admitted)
                {
                    lease = null;
                    return false;
                }

                if (_activeOperations == 0 && _phase == FrameworkRuntimeHostPhase.Stopping)
                {
                    _operationsDrained = null;
                }

                _activeOperations++;
                lease = new OperationLease(this);
                return true;
            }
        }

        private void ExitOperation()
        {
            TaskCompletionSource<bool> drained = null;
            lock (_sync)
            {
                if (_activeOperations <= 0)
                {
                    throw new InvalidOperationException(
                        "FrameworkRuntimeHost operation lease was released more than once.");
                }

                _activeOperations--;
                if (_activeOperations == 0 && _phase == FrameworkRuntimeHostPhase.Stopping)
                {
                    drained = _operationsDrained;
                }
            }

            drained?.TrySetResult(true);
        }

        private sealed class OperationLease : IDisposable
        {
            private FrameworkRuntimeHostAdmission _owner;

            internal OperationLease(FrameworkRuntimeHostAdmission owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                FrameworkRuntimeHostAdmission owner = _owner;
                if (owner == null)
                {
                    return;
                }

                _owner = null;
                owner.ExitOperation();
            }
        }
    }
}
