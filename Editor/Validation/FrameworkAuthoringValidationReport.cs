using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Validation
{
    internal sealed class FrameworkAuthoringValidationReport
    {
        private readonly List<FrameworkAuthoringValidationIssue> _issues = new List<FrameworkAuthoringValidationIssue>();

        internal FrameworkAuthoringValidationReport()
            : this(FrameworkValidationMode.Standard)
        {
        }

        internal FrameworkAuthoringValidationReport(FrameworkValidationMode validationMode)
        {
            ValidationMode = validationMode;
        }

        internal FrameworkValidationMode ValidationMode { get; }

        internal IReadOnlyList<FrameworkAuthoringValidationIssue> Issues => _issues;

        internal int ErrorCount { get; private set; }

        internal int WarningCount { get; private set; }

        internal int InfoCount { get; private set; }

        internal int OptionalSkipCount { get; private set; }

        internal int TotalIssueCount => _issues.Count;

        internal bool IsValid => ErrorCount == 0;

        internal bool HasIssues => _issues.Count > 0;

        internal void AddError(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Error, message, context);
        }

        internal void AddWarning(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Warning, message, context);
        }

        internal void AddInfo(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Info, message, context);
        }

        internal void AddOptionalSkip(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Info, message, context, true);
        }

        internal void AddRange(FrameworkAuthoringValidationReport other)
        {
            if (other == null)
            {
                return;
            }

            IReadOnlyList<FrameworkAuthoringValidationIssue> issues = other.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                Add(issue.Severity, issue.Message, issue.Context, issue.IsOptionalSkip);
            }
        }

        private void Add(FrameworkAuthoringValidationSeverity severity, string message, Object context)
        {
            Add(severity, message, context, false);
        }

        private void Add(
            FrameworkAuthoringValidationSeverity severity,
            string message,
            Object context,
            bool isOptionalSkip)
        {
            if (isOptionalSkip)
            {
                OptionalSkipCount++;
            }

            if (severity == FrameworkAuthoringValidationSeverity.Info &&
                !FrameworkValidationModePolicy.IncludeInfoDiagnostics(ValidationMode))
            {
                return;
            }

            var resolvedSeverity = ResolveSeverity(severity);
            _issues.Add(new FrameworkAuthoringValidationIssue(resolvedSeverity, message, context, isOptionalSkip));

            switch (resolvedSeverity)
            {
                case FrameworkAuthoringValidationSeverity.Error:
                    ErrorCount++;
                    break;
                case FrameworkAuthoringValidationSeverity.Warning:
                    WarningCount++;
                    break;
                default:
                    InfoCount++;
                    break;
            }
        }

        private FrameworkAuthoringValidationSeverity ResolveSeverity(FrameworkAuthoringValidationSeverity severity)
        {
            if (severity == FrameworkAuthoringValidationSeverity.Warning &&
                FrameworkValidationModePolicy.TreatWarningsAsErrors(ValidationMode))
            {
                return FrameworkAuthoringValidationSeverity.Error;
            }

            return severity;
        }
    }
}
