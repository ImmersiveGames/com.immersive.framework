using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic state for the effective camera priority decision.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public readonly struct FrameworkCameraPriorityState
    {
        public FrameworkCameraPriorityState(
            FrameworkCameraRigRole role,
            int priority,
            string source,
            string reason)
        {
            Role = role;
            Priority = priority;
            Source = source.NormalizeTextOrFallback(nameof(FrameworkCameraPriorityState));
            Reason = reason.NormalizeText();
        }

        public FrameworkCameraRigRole Role { get; }

        public int Priority { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Role != FrameworkCameraRigRole.Unknown;

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string reasonText = Reason.ToDiagnosticText();
            return $"role='{Role}' priority='{Priority}' source='{Source}' reason='{reasonText}'";
        }
    }
}
