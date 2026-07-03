using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue emitted while building a Unity Input dry-run application plan.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32C InputMode Unity application dry-run plan issue.")]
    public readonly struct InputModeUnityApplicationPlanIssue : IEquatable<InputModeUnityApplicationPlanIssue>
    {
        public InputModeUnityApplicationPlanIssue(
            InputModeUnityApplicationPlanIssueKind kind,
            InputModeKind inputMode,
            UnityInputActionMapName actionMapName,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityApplicationPlanIssueKind), kind) || kind == InputModeUnityApplicationPlanIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "InputMode Unity application plan issue kind must be explicit.");
            }

            Kind = kind;
            InputMode = inputMode;
            ActionMapName = actionMapName;
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPlanIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public InputModeUnityApplicationPlanIssueKind Kind { get; }

        public InputModeKind InputMode { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(InputModeUnityApplicationPlanIssue other)
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
            return obj is InputModeUnityApplicationPlanIssue other && Equals(other);
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

        internal static InputModeUnityApplicationPlanIssue BlockingIssue(
            InputModeUnityApplicationPlanIssueKind kind,
            InputModeKind inputMode,
            UnityInputActionMapName actionMapName,
            string source,
            string message)
        {
            return new InputModeUnityApplicationPlanIssue(kind, inputMode, actionMapName, source, message, true);
        }
    }
}
