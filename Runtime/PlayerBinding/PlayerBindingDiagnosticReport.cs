using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable passive diagnostic report built from PlayerBindingReadinessSummary.
    /// This report is descriptive only. It does not bind views, activate cameras, activate input, bind controls,
    /// enable movement, spawn actors or own runtime lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49L passive Player binding diagnostic report.")]
    public sealed class PlayerBindingDiagnosticReport
    {
        private readonly PlayerBindingDiagnosticMessage[] _messages;

        internal PlayerBindingDiagnosticReport(
            PlayerBindingReadinessSummary summary,
            IEnumerable<PlayerBindingDiagnosticMessage> messages,
            string source,
            string reason)
        {
            Summary = summary;
            _messages = ToArray(messages);
            Source = source.NormalizeTextOrFallback(nameof(PlayerBindingDiagnosticReport));
            Reason = reason.NormalizeText();
        }

        public PlayerBindingReadinessSummary Summary { get; }

        public IReadOnlyList<PlayerBindingDiagnosticMessage> Messages => _messages;

        public string Source { get; }

        public string Reason { get; }

        public bool HasSummary => Summary != null;

        public int MessageCount => _messages.Length;

        public int ErrorCount => CountSeverity(PlayerBindingDiagnosticSeverity.Error);

        public int WarningCount => CountSeverity(PlayerBindingDiagnosticSeverity.Warning);

        public int InfoCount => CountSeverity(PlayerBindingDiagnosticSeverity.Info);

        public bool HasErrors => ErrorCount > 0;

        public bool HasWarnings => WarningCount > 0;

        public bool Succeeded => !HasErrors;

        public bool Failed => HasErrors;

        public bool IsReadyForViewBinding => Summary != null && Summary.IsReadyForViewBinding;

        public bool IsReadyForControlBinding => Summary != null && Summary.IsReadyForControlBinding;

        public bool IsReadyForFullBinding => Summary != null && Summary.IsReadyForFullBinding;

        public bool BindsView => false;

        public bool BindsControl => false;

        public bool ActivatesCamera => false;

        public bool ActivatesInput => false;

        public bool EnablesMovement => false;

        public bool SpawnsActor => false;

        public bool HasMessage(PlayerBindingDiagnosticMessageKind kind)
        {
            for (int i = 0; i < _messages.Length; i++)
            {
                if (_messages[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSeverity(PlayerBindingDiagnosticSeverity severity)
        {
            for (int i = 0; i < _messages.Length; i++)
            {
                if (_messages[i].Severity == severity)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasReadinessIssue(PlayerBindingReadinessIssueKind issueKind)
        {
            for (int i = 0; i < _messages.Length; i++)
            {
                if (_messages[i].ReadinessIssueKind == issueKind)
                {
                    return true;
                }
            }

            return false;
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("hasSummary='").Append(HasSummary).Append("'");
            builder.Append(" messages='").Append(MessageCount).Append("'");
            builder.Append(" errors='").Append(ErrorCount).Append("'");
            builder.Append(" warnings='").Append(WarningCount).Append("'");
            builder.Append(" info='").Append(InfoCount).Append("'");
            builder.Append(" readyForViewBinding='").Append(IsReadyForViewBinding).Append("'");
            builder.Append(" readyForControlBinding='").Append(IsReadyForControlBinding).Append("'");
            builder.Append(" readyForFullBinding='").Append(IsReadyForFullBinding).Append("'");
            builder.Append(" viewBinding='").Append(BindsView).Append("'");
            builder.Append(" controlBinding='").Append(BindsControl).Append("'");
            builder.Append(" cameraActivation='").Append(ActivatesCamera).Append("'");
            builder.Append(" inputActivation='").Append(ActivatesInput).Append("'");
            builder.Append(" movement='").Append(EnablesMovement).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            for (int i = 0; i < _messages.Length; i++)
            {
                builder.Append(" message[").Append(i).Append("]='").Append(_messages[i]).Append("'");
            }

            return builder.ToString();
        }

        private int CountSeverity(PlayerBindingDiagnosticSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < _messages.Length; i++)
            {
                if (_messages[i].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private static T[] ToArray<T>(IEnumerable<T> values)
        {
            if (values == null)
            {
                return Array.Empty<T>();
            }

            var list = new List<T>();
            foreach (T value in values)
            {
                list.Add(value);
            }

            return list.ToArray();
        }
    }
}
