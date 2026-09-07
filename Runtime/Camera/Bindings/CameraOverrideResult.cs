
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraOverrideResult
    {
        internal CameraOverrideResult(
            CameraOverrideOperationKind operation,
            bool succeeded,
            bool isActive,
            string diagnostic)
        {
            Operation = operation;
            Succeeded = succeeded;
            IsActive = isActive;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public CameraOverrideOperationKind Operation { get; }
        public bool Succeeded { get; }
        public bool IsActive { get; }
        public string Diagnostic { get; }
    }
}
