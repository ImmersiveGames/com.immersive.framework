using System.Collections.Generic;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class FrameworkAuthoringValidationGui
    {
        internal static void DrawSummary(
            FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                EditorGUILayout.LabelField("Result", "Not run");
                return;
            }

            string result = report.ErrorCount > 0
                ? $"Invalid — {report.ErrorCount} error(s), {report.WarningCount} warning(s)"
                : report.WarningCount > 0
                    ? $"Valid with {report.WarningCount} warning(s)"
                    : "Valid";

            EditorGUILayout.LabelField("Result", result);
        }

        internal static void DrawIssues(
            FrameworkAuthoringValidationReport report,
            bool includeInfo)
        {
            if (report == null)
            {
                return;
            }

            IReadOnlyList<FrameworkAuthoringValidationIssue> issues =
                report.Issues;

            for (int i = 0; i < issues.Count; i++)
            {
                FrameworkAuthoringValidationIssue issue = issues[i];

                if (!includeInfo &&
                    issue.Severity ==
                    FrameworkAuthoringValidationSeverity.Info)
                {
                    continue;
                }

                if (issue.Severity ==
                    FrameworkAuthoringValidationSeverity.Info)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            issue.Message,
                            EditorStyles.wordWrappedMiniLabel);

                        DrawSelectButton(issue.Context, 22f);
                    }

                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(
                        issue.Message,
                        ToMessageType(issue.Severity));

                    DrawSelectButton(issue.Context, 38f);
                }
            }
        }

        internal static void LogReport(
            string title,
            FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            var logger =
                FrameworkLogger.Create(
                    typeof(FrameworkAuthoringValidationGui));

            string summary =
                "Authoring Validation completed.";

            LogField[] summaryFields = LogFields.Of(
                LogFields.Field("scope", title),
                LogFields.Field("mode", report.ValidationMode),
                LogFields.Field(
                    "totalIssues",
                    report.TotalIssueCount),
                LogFields.Field(
                    "errors",
                    report.ErrorCount),
                LogFields.Field(
                    "warnings",
                    report.WarningCount),
                LogFields.Field(
                    "info",
                    report.InfoCount),
                LogFields.Field(
                    "optionalSkips",
                    report.OptionalSkipCount));

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

            IReadOnlyList<FrameworkAuthoringValidationIssue> issues =
                report.Issues;

            for (int i = 0; i < issues.Count; i++)
            {
                FrameworkAuthoringValidationIssue issue =
                    issues[i];

                string contextName =
                    issue.Context != null
                        ? issue.Context.name
                        : "<none>";

                string message =
                    $"Authoring Validation issue. scope='{title}' severity='{issue.Severity}' context='{contextName}' message='{issue.Message}'.";

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

        private static void DrawSelectButton(
            Object context,
            float height)
        {
            using (new EditorGUI.DisabledScope(context == null))
            {
                if (GUILayout.Button(
                        "Select",
                        GUILayout.Width(58f),
                        GUILayout.Height(height)))
                {
                    Selection.activeObject = context;
                    EditorGUIUtility.PingObject(context);
                }
            }
        }

        private static MessageType ToMessageType(
            FrameworkAuthoringValidationSeverity severity)
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
