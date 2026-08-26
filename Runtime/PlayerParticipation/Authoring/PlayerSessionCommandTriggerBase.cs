using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal infrastructure shared by explicit Player Session command
    /// components. It owns no Session authority or Player state.
    /// </summary>
    [AddComponentMenu("")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "Internal shared infrastructure for explicit Player Session command triggers.")]
    public abstract class PlayerSessionCommandTriggerBase :
        PlayerSessionScopedAccessConsumer
    {
        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional diagnostic reason. The command name is used when left empty.")]
        private string reason;

        [NonSerialized]
        private string _lastDiagnostic =
            "No Player Session command has been invoked.";

        [NonSerialized]
        private int _invocationCount;

        [NonSerialized]
        private string _lastOutcome = "None";

        public string Reason => reason ?? string.Empty;
        public int InvocationCount => _invocationCount;
        public string LastDiagnostic => _lastDiagnostic;
        public string LastOutcome => _lastOutcome;
        public string ScopeBindingStatus => IsScopedAccessAvailable
            ? "Bound"
            : ScopedAccessState.ToString();
        public string ScopeBindingDiagnostic => ScopedAccessDiagnostic;

        /// <summary>
        /// Explicit UnityEvent entry point. It never runs automatically.
        /// </summary>
        public abstract void Invoke();

        /// <summary>
        /// Validates authored references and inputs only. It never resolves or
        /// changes runtime authority.
        /// </summary>
        public bool TryValidateConfiguration(out string issue)
        {
            return TryValidateScope(out issue) &&
                TryValidateCommandConfiguration(out issue);
        }

        protected abstract bool TryValidateCommandConfiguration(out string issue);

        protected string BeginInvocation(string commandName)
        {
            _invocationCount++;
            string resolvedReason = !string.IsNullOrWhiteSpace(reason)
                ? reason.Trim()
                : commandName;
            FrameworkLogger.Create(GetType()).Info(
                "Player Session command requested.",
                BuildFields(commandName, "Requested", string.Empty, resolvedReason));
            return resolvedReason;
        }

        protected void Complete(
            string commandName,
            string outcome,
            string diagnostic)
        {
            _lastOutcome = outcome ?? "Incomplete";
            _lastDiagnostic = string.IsNullOrWhiteSpace(diagnostic)
                ? "Player Session command returned no typed result."
                : diagnostic;
            FrameworkLogger logger = FrameworkLogger.Create(GetType());
            LogField[] fields = BuildFields(
                commandName,
                "Completed",
                _lastOutcome,
                _lastDiagnostic);
            if (string.Equals(_lastOutcome, "Succeeded", StringComparison.Ordinal) ||
                string.Equals(_lastOutcome, "IgnoredNoChange", StringComparison.Ordinal))
            {
                logger.Info("Player Session command completed.", fields);
                return;
            }

            logger.Warning("Player Session command completed without success.", fields);
        }

        protected static string Describe(
            PlayerParticipationOperationResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : "Player participation command returned no typed result.";
        }

        protected static string Describe(LocalPlayerJoinResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : "Local Player Join returned no typed result.";
        }

        protected static string Describe(PlayerActorSelectionResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : "Default Actor selection returned no typed result.";
        }

        protected static string Describe(SessionPlayerLeaveResult result)
        {
            return result != null
                ? result.ToDiagnosticString()
                : "Session Player Leave returned no typed result.";
        }

        protected static string Outcome(PlayerParticipationOperationResult result)
        {
            if (result == null) return "Missing";
            if (result.Succeeded) return "Succeeded";
            if (result.IgnoredNoChange) return "IgnoredNoChange";
            if (result.Failed) return "Failed";
            return result.Rejected ? "Rejected" : "Incomplete";
        }

        protected static string Outcome(LocalPlayerJoinResult result)
        {
            if (result == null) return "Missing";
            if (result.Succeeded) return "Succeeded";
            if (result.Failed) return "Failed";
            return result.Rejected ? "Rejected" : "Incomplete";
        }

        protected static string Outcome(PlayerActorSelectionResult result)
        {
            if (result == null) return "Missing";
            return result.Succeeded
                ? "Succeeded"
                : result.Rejected ? "Rejected" : "Incomplete";
        }

        protected static string Outcome(SessionPlayerLeaveResult result)
        {
            if (result == null) return "Missing";
            if (result.Succeeded) return "Succeeded";
            if (result.Failed) return "Failed";
            return result.Rejected ? "Rejected" : "Incomplete";
        }

        private LogField[] BuildFields(
            string commandName,
            string status,
            string outcome,
            string message)
        {
            return LogFields.Of(
                LogFields.Field("component", name),
                LogFields.Field("scene", gameObject.scene.name),
                LogFields.Field("command", commandName),
                LogFields.Field("status", status),
                LogFields.Field("outcome", outcome),
                LogFields.Field("invocation", _invocationCount),
                LogFields.Field("scope", Scope.IsDefinedScope() ? Scope.ToString() : "Missing"),
                LogFields.Field("bindingStatus", ScopeBindingStatus),
                LogFields.Field("message", message ?? string.Empty));
        }
    }
}
