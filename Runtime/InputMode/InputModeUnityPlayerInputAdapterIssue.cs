using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue emitted by the explicit Unity PlayerInput adapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32D Unity PlayerInput adapter application issue.")]
    public sealed class InputModeUnityPlayerInputAdapterIssue
    {
        public InputModeUnityPlayerInputAdapterIssue(
            InputModeUnityPlayerInputAdapterIssueKind kind,
            InputModeKind requestedMode,
            UnityInputActionMapName actionMapName,
            bool blocking,
            string source,
            string message)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityPlayerInputAdapterIssueKind), kind) || kind == InputModeUnityPlayerInputAdapterIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unity PlayerInput adapter issue kind must be explicit.");
            }

            Kind = kind;
            RequestedMode = requestedMode;
            ActionMapName = actionMapName;
            Blocking = blocking;
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputAdapterIssue));
            Message = message.NormalizeText();
        }

        public InputModeUnityPlayerInputAdapterIssueKind Kind { get; }

        public InputModeKind RequestedMode { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public bool Blocking { get; }

        public string Source { get; }

        public string Message { get; }

        public override string ToString()
        {
            return $"{Kind}:{RequestedMode}:{ActionMapName}:{Message}";
        }

        public static InputModeUnityPlayerInputAdapterIssue BlockingIssue(
            InputModeUnityPlayerInputAdapterIssueKind kind,
            InputModeKind requestedMode,
            UnityInputActionMapName actionMapName,
            string source,
            string message)
        {
            return new InputModeUnityPlayerInputAdapterIssue(kind, requestedMode, actionMapName, true, source, message);
        }
    }
}
