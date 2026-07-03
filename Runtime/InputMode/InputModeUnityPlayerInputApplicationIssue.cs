using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue emitted by F32E PlayerInput application.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32E Unity PlayerInput application issue.")]
    public readonly struct InputModeUnityPlayerInputApplicationIssue
    {
        private InputModeUnityPlayerInputApplicationIssue(
            InputModeUnityPlayerInputApplicationIssueKind kind,
            InputModeKind requestedMode,
            UnityInputActionMapName actionMapName,
            bool blocking,
            string source,
            string message)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityPlayerInputApplicationIssueKind), kind) || kind == InputModeUnityPlayerInputApplicationIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "PlayerInput application issue kind must be explicit.");
            }

            Kind = kind;
            RequestedMode = requestedMode;
            ActionMapName = actionMapName;
            Blocking = blocking;
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputApplicationIssue));
            Message = message.NormalizeText();
        }

        public InputModeUnityPlayerInputApplicationIssueKind Kind { get; }

        public InputModeKind RequestedMode { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public bool Blocking { get; }

        public string Source { get; }

        public string Message { get; }

        public bool IsValid => Kind != InputModeUnityPlayerInputApplicationIssueKind.None;

        public override string ToString()
        {
            return $"{Kind}:{RequestedMode}:{ActionMapName}:{Message}";
        }

        public static InputModeUnityPlayerInputApplicationIssue BlockingIssue(
            InputModeUnityPlayerInputApplicationIssueKind kind,
            InputModeKind requestedMode,
            UnityInputActionMapName actionMapName,
            string source,
            string message)
        {
            return new InputModeUnityPlayerInputApplicationIssue(kind, requestedMode, actionMapName, true, source, message);
        }
    }
}
