using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Diagnostic item emitted by camera product contracts, target resolution and materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraIssue
    {
        public CameraIssue(CameraIssueSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code.NormalizeTextOrFallback("camera.issue");
            Message = message.NormalizeTextOrFallback(Code);
        }

        public CameraIssueSeverity Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public bool IsBlocking => Severity == CameraIssueSeverity.Blocking;

        public static CameraIssue Info(string code, string message)
        {
            return new CameraIssue(CameraIssueSeverity.Info, code, message);
        }

        public static CameraIssue Warning(string code, string message)
        {
            return new CameraIssue(CameraIssueSeverity.Warning, code, message);
        }

        public static CameraIssue Blocking(string code, string message)
        {
            return new CameraIssue(CameraIssueSeverity.Blocking, code, message);
        }
    }
}
