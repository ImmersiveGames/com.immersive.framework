using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue emitted by passive InputMode request evaluation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A InputMode request diagnostic issue.")]
    public readonly struct InputModeRequestIssue : IEquatable<InputModeRequestIssue>
    {
        public InputModeRequestIssue(InputModeRequestIssueKind kind, InputModeKind mode, string source, string message, bool blocking)
        {
            if (!Enum.IsDefined(typeof(InputModeRequestIssueKind), kind) || kind == InputModeRequestIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "InputMode request issue kind must be explicit.");
            }

            Kind = kind;
            Mode = mode;
            Source = source.NormalizeTextOrFallback(nameof(InputModeRequestIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public InputModeRequestIssueKind Kind { get; }

        public InputModeKind Mode { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(InputModeRequestIssue other)
        {
            return Kind == other.Kind
                && Mode == other.Mode
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeRequestIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (int)Mode;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{Mode}:{Message}";
        }

        public static InputModeRequestIssue BlockingIssue(InputModeRequestIssueKind kind, InputModeKind mode, string source, string message)
        {
            return new InputModeRequestIssue(kind, mode, source, message, true);
        }
    }
}
