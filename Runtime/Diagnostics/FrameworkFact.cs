using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Experimental. Minimal structured fact emitted or collected by framework diagnostics.
    /// This is not a human log line, telemetry event, service locator or public recorder.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal structured diagnostics fact introduced by F1C.")]
    public readonly struct FrameworkFact
    {
        public FrameworkFact(
            FrameworkFactCode code,
            FrameworkFactScope scope,
            FrameworkFactSeverity severity,
            string source = null,
            string subject = null,
            string reason = null,
            string details = null)
        {
            if (!code.IsValid)
            {
                throw new ArgumentException("Framework fact code must be valid.", nameof(code));
            }

            Code = code;
            Scope = scope;
            Severity = severity;
            Source = Normalize(source);
            Subject = Normalize(subject);
            Reason = Normalize(reason);
            Details = Normalize(details);
        }

        public FrameworkFactCode Code { get; }

        public FrameworkFactScope Scope { get; }

        public FrameworkFactSeverity Severity { get; }

        public string Source { get; }

        public string Subject { get; }

        public string Reason { get; }

        public string Details { get; }

        public bool IsValid => Code.IsValid;

        public override string ToString()
        {
            return $"code='{Code}' scope='{Scope}' severity='{Severity}' source='{Source}' subject='{Subject}' reason='{Reason}' details='{Details}'";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
