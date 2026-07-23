using System.Collections.Generic;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class FrameworkAuthoringValidationGui
    {
        internal static void DrawSummary(FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                EditorGUILayout.HelpBox("Authoring validation has not been run.", MessageType.None);
                return;
            }

            var messageType = report.ErrorCount > 0
                ? MessageType.Error
                : report.WarningCount > 0
                    ? MessageType.Warning
                    : MessageType.Info;

            EditorGUILayout.HelpBox(
                $"Authoring Validation: mode='{report.ValidationMode}' totalIssues='{report.TotalIssueCount}' errors='{report.ErrorCount}' warnings='{report.WarningCount}' info='{report.InfoCount}' optionalSkips='{report.OptionalSkipCount}'.",
                messageType);
        }

        internal static void DrawIssues(FrameworkAuthoringValidationReport report, bool includeInfo)
        {
            if (report == null)
            {
                return;
            }

            IReadOnlyList<FrameworkAuthoringValidationIssue> issues = report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                if (!includeInfo && issue.Severity == FrameworkAuthoringValidationSeverity.Info)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));

                    using (new EditorGUI.DisabledScope(issue.Context == null))
                    {
                        if (GUILayout.Button("Select", GUILayout.Width(64), GUILayout.Height(38)))
                        {
                            Selection.activeObject = issue.Context;
                            EditorGUIUtility.PingObject(issue.Context);
                        }
                    }
                }
            }
        }

        internal static void LogReport(string title, FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            var logger = FrameworkLogger.Create(typeof(FrameworkAuthoringValidationGui));
            string summary = "Authoring Validation completed.";
            LogField[] summaryFields = LogFields.Of(
                LogFields.Field("scope", title),
                LogFields.Field("mode", report.ValidationMode),
                LogFields.Field("totalIssues", report.TotalIssueCount),
                LogFields.Field("errors", report.ErrorCount),
                LogFields.Field("warnings", report.WarningCount),
                LogFields.Field("info", report.InfoCount),
                LogFields.Field("optionalSkips", report.OptionalSkipCount));

            if (report.ErrorCount > 0)
            {
                logger.Error(summary, summaryFields);
            }
            else if (report.WarningCount > 0)
            {
                logger.Warning(summary, summaryFields);
            }
            else
            {
                logger.Info(summary, summaryFields);
            }

            IReadOnlyList<FrameworkAuthoringValidationIssue> issues = report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                string contextName = issue.Context != null ? issue.Context.name : "<none>";
                string message = $"Authoring Validation issue. scope='{title}' severity='{issue.Severity}' context='{contextName}' message='{issue.Message}'.";

                switch (issue.Severity)
                {
                    case FrameworkAuthoringValidationSeverity.Error:
                        logger.Error(message);
                        break;
                    case FrameworkAuthoringValidationSeverity.Warning:
                        logger.Warning(message);
                        break;
                    default:
                        logger.Debug(message);
                        break;
                }
            }
        }

        private static MessageType ToMessageType(FrameworkAuthoringValidationSeverity severity)
        {
            switch (severity)
            {
                case FrameworkAuthoringValidationSeverity.Error:
                    return MessageType.Error;
                case FrameworkAuthoringValidationSeverity.Warning:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }
    }
}
