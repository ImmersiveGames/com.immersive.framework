using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Normalized, device-agnostic Pause input signal.
    /// A signal describes intent only; it does not poll input, bind actions, dispatch PauseRequest,
    /// own UI navigation, mutate Pause state or change Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23E Pause Input signal boundary; intent-only and no concrete input binding.")]
    public readonly struct PauseInputSignal : IEquatable<PauseInputSignal>
    {
        public PauseInputSignal(
            PauseInputActionId actionId,
            PauseInputCommandKind commandKind,
            PauseInputSourceKind sourceKind,
            string source,
            string reason)
        {
            if (!actionId.IsValid)
            {
                throw new ArgumentException("Pause input signal requires a valid action id.", nameof(actionId));
            }

            if (!Enum.IsDefined(typeof(PauseInputCommandKind), commandKind) || commandKind == PauseInputCommandKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(commandKind), commandKind, "Pause input command must be explicit.");
            }

            if (!Enum.IsDefined(typeof(PauseInputSourceKind), sourceKind) || sourceKind == PauseInputSourceKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Pause input source kind must be explicit.");
            }

            ActionId = actionId;
            CommandKind = commandKind;
            SourceKind = sourceKind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PauseInputActionId ActionId { get; }

        public PauseInputCommandKind CommandKind { get; }

        public PauseInputSourceKind SourceKind { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => ActionId.IsValid && CommandKind != PauseInputCommandKind.Unknown && SourceKind != PauseInputSourceKind.Unknown;

        public bool IsPauseStateCommand => CommandKind is PauseInputCommandKind.TogglePause or PauseInputCommandKind.Pause or PauseInputCommandKind.Resume;

        public bool IsMenuCommand => !IsPauseStateCommand;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(PauseInputSignal other)
        {
            return ActionId.Equals(other.ActionId)
                && CommandKind == other.CommandKind
                && SourceKind == other.SourceKind
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseInputSignal other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ActionId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)CommandKind;
                hashCode = hashCode * 397 ^ (int)SourceKind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"action='{ActionId.StableText}' command='{CommandKind}' sourceKind='{SourceKind}' stateCommand='{IsPauseStateCommand}' menuCommand='{IsMenuCommand}' source='{sourceText}' reason='{reasonText}'";
        }

        public static PauseInputSignal Toggle(PauseInputActionId actionId, PauseInputSourceKind sourceKind, string source, string reason)
        {
            return new PauseInputSignal(actionId, PauseInputCommandKind.TogglePause, sourceKind, source, reason);
        }

        public static PauseInputSignal Pause(PauseInputActionId actionId, PauseInputSourceKind sourceKind, string source, string reason)
        {
            return new PauseInputSignal(actionId, PauseInputCommandKind.Pause, sourceKind, source, reason);
        }

        public static PauseInputSignal Resume(PauseInputActionId actionId, PauseInputSourceKind sourceKind, string source, string reason)
        {
            return new PauseInputSignal(actionId, PauseInputCommandKind.Resume, sourceKind, source, reason);
        }

        public static PauseInputSignal MenuCommand(PauseInputActionId actionId, PauseInputCommandKind commandKind, PauseInputSourceKind sourceKind, string source, string reason)
        {
            return new PauseInputSignal(actionId, commandKind, sourceKind, source, reason);
        }

        public static bool operator ==(PauseInputSignal left, PauseInputSignal right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseInputSignal left, PauseInputSignal right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
