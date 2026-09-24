using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    internal readonly struct ActivityActivationGateResult
    {
        private ActivityActivationGateResult(
            bool canActivate,
            string source,
            string reason,
            string message)
        {
            CanActivate = canActivate;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        internal bool CanActivate { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }

        internal static ActivityActivationGateResult Allowed(
            string source,
            string reason,
            string message) =>
            new ActivityActivationGateResult(
                true,
                source,
                reason,
                message);

        internal static ActivityActivationGateResult Blocked(
            string source,
            string reason,
            string message) =>
            new ActivityActivationGateResult(
                false,
                source,
                reason,
                message);

        internal string ToDiagnosticString() =>
            $"allowed='{CanActivate}' source='{Source}' reason='{Reason}' message='{Message}'";
    }
}
