using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Validation
{
    internal readonly struct FrameworkAuthoringValidationIssue
    {
        internal FrameworkAuthoringValidationIssue(
            FrameworkAuthoringValidationSeverity severity,
            string message,
            Object context)
            : this(severity, message, context, false)
        {
        }

        internal FrameworkAuthoringValidationIssue(
            FrameworkAuthoringValidationSeverity severity,
            string message,
            Object context,
            bool isOptionalSkip)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Context = context;
            IsOptionalSkip = isOptionalSkip;
        }

        internal FrameworkAuthoringValidationSeverity Severity { get; }

        internal string Message { get; }

        internal Object Context { get; }

        internal bool IsOptionalSkip { get; }
    }
}
