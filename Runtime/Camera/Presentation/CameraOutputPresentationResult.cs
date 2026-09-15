using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "CAMERA-028-C deterministic Output presentation operation result.")]
    internal readonly struct CameraOutputPresentationResult
    {
        internal CameraOutputPresentationResult(
            CameraOutputPresentationStatus status,
            CameraOutputPresentationSnapshot snapshot,
            string diagnostic)
        {
            Status = status;
            Snapshot = snapshot;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal CameraOutputPresentationStatus Status { get; }
        internal CameraOutputPresentationSnapshot Snapshot { get; }
        internal string Diagnostic { get; }
        internal bool Succeeded =>
            Status == CameraOutputPresentationStatus.Applied ||
            Status == CameraOutputPresentationStatus.Replaced ||
            Status == CameraOutputPresentationStatus.Cleared ||
            Status == CameraOutputPresentationStatus.NoChange;
        internal bool Changed =>
            Status == CameraOutputPresentationStatus.Applied ||
            Status == CameraOutputPresentationStatus.Replaced ||
            Status == CameraOutputPresentationStatus.Cleared;
    }
}
