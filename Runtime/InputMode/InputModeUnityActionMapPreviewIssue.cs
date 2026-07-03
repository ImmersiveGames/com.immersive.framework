
using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue emitted while previewing InputMode-to-action-map evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B InputMode Unity action map preview issue.")]
    public readonly struct InputModeUnityActionMapPreviewIssue : IEquatable<InputModeUnityActionMapPreviewIssue>
    {
        public InputModeUnityActionMapPreviewIssue(
            InputModeUnityActionMapPreviewIssueKind kind,
            InputModeKind inputMode,
            UnityInputActionMapName actionMapName,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityActionMapPreviewIssueKind), kind) || kind == InputModeUnityActionMapPreviewIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "InputMode Unity action map preview issue kind must be explicit.");
            }

            Kind = kind;
            InputMode = inputMode;
            ActionMapName = actionMapName;
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityActionMapPreviewIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public InputModeUnityActionMapPreviewIssueKind Kind { get; }

        public InputModeKind InputMode { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(InputModeUnityActionMapPreviewIssue other)
        {
            return Kind == other.Kind
                && InputMode == other.InputMode
                && ActionMapName == other.ActionMapName
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeUnityActionMapPreviewIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (int)InputMode;
                hash = (hash * 397) ^ ActionMapName.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{InputMode}:{ActionMapName}";
        }

        internal static InputModeUnityActionMapPreviewIssue BlockingIssue(
            InputModeUnityActionMapPreviewIssueKind kind,
            InputModeKind inputMode,
            UnityInputActionMapName actionMapName,
            string source,
            string message)
        {
            return new InputModeUnityActionMapPreviewIssue(kind, inputMode, actionMapName, source, message, true);
        }
    }
}
