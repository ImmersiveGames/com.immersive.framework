using Immersive.Framework.InputMode;

namespace Immersive.Framework.Pause
{
    internal interface IPauseProductBindingPort
    {
        bool TryRegister(PausePlayerInputBinding binding, out PauseProductBindingToken token, out string diagnostic);
        bool ReleaseBinding(PauseProductBindingToken token, string reason, out string diagnostic);
    }

    internal interface IPauseProductRequestPort
    {
        PauseProductRequestResult RequestPause(PauseRequest request);
        bool TryGetPauseSnapshot(out PauseSnapshot snapshot);
    }

    internal interface IPauseProductApplicationPort
    {
        bool TryApplyProductPause(PauseRequest request, out PauseResult result, out string diagnostic);
        bool TryRestorePauseSnapshot(PauseSnapshot snapshot, string reason, out string diagnostic);
        bool TryGetApplicationPauseSnapshot(out PauseSnapshot snapshot);
    }

    internal enum PauseProductBindingState
    {
        Unbound = 0,
        Binding = 10,
        Bound = 20,
        Unbinding = 30,
        Failed = 40
    }

    internal readonly struct PauseProductBindingToken
    {
        internal PauseProductBindingToken(long generation, int playerInstanceId)
        {
            Generation = generation;
            PlayerInstanceId = playerInstanceId;
        }

        internal long Generation { get; }
        internal int PlayerInstanceId { get; }
        internal bool IsValid => Generation > 0 && PlayerInstanceId != 0;
    }

    internal enum PauseProductRequestStatus
    {
        Unknown = 0,
        Applied = 10,
        Ignored = 20,
        BindingUnavailable = 30,
        Rejected = 40,
        Failed = 50
    }

    internal readonly struct PauseProductRequestResult
    {
        internal PauseProductRequestResult(PauseProductRequestStatus status, PauseResult pauseResult, InputModeRuntimeOperationResult inputModeResult, string diagnostic)
        {
            Status = status;
            PauseResult = pauseResult;
            InputModeResult = inputModeResult;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal PauseProductRequestStatus Status { get; }
        internal PauseResult PauseResult { get; }
        internal InputModeRuntimeOperationResult InputModeResult { get; }
        internal string Diagnostic { get; }
        internal bool Succeeded => Status == PauseProductRequestStatus.Applied;
        internal bool Ignored => Status == PauseProductRequestStatus.Ignored;
    }
}
