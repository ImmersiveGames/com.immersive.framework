using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue emitted while previewing a logical InputMode request against Unity Input evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32A InputMode Unity application preview issue.")]
    public readonly struct InputModeUnityApplicationPreviewIssue : IEquatable<InputModeUnityApplicationPreviewIssue>
    {
        public InputModeUnityApplicationPreviewIssue(
            InputModeUnityApplicationPreviewIssueKind kind,
            InputModeKind inputMode,
            UnityInputTargetRole targetRole,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityApplicationPreviewIssueKind), kind) || kind == InputModeUnityApplicationPreviewIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "InputMode Unity application preview issue kind must be explicit.");
            }

            Kind = kind;
            InputMode = inputMode;
            TargetRole = targetRole;
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPreviewIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public InputModeUnityApplicationPreviewIssueKind Kind { get; }

        public InputModeKind InputMode { get; }

        public UnityInputTargetRole TargetRole { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(InputModeUnityApplicationPreviewIssue other)
        {
            return Kind == other.Kind
                && InputMode == other.InputMode
                && TargetRole == other.TargetRole
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeUnityApplicationPreviewIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (int)InputMode;
                hash = (hash * 397) ^ (int)TargetRole;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{InputMode}:{TargetRole}";
        }

        internal static InputModeUnityApplicationPreviewIssue BlockingIssue(
            InputModeUnityApplicationPreviewIssueKind kind,
            InputModeKind inputMode,
            UnityInputTargetRole targetRole,
            string source,
            string message)
        {
            return new InputModeUnityApplicationPreviewIssue(kind, inputMode, targetRole, source, message, true);
        }
    }
}
