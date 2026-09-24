using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Immutable result and resulting snapshot for one availability mutation.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A Camera Subject availability mutation result.")]
    public sealed class CameraSubjectAvailabilityResult
    {
        internal CameraSubjectAvailabilityResult(CameraSubjectAvailabilityStatus status, CameraSubject subject, CameraSubjectAvailabilityToken token, CameraSubjectAvailabilitySnapshot snapshot, string message)
        {
            Status = status;
            Subject = subject;
            Token = token;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public CameraSubjectAvailabilityStatus Status { get; }
        public CameraSubject Subject { get; }
        public CameraSubjectAvailabilityToken Token { get; }
        public CameraSubjectAvailabilitySnapshot Snapshot { get; }
        public string Message { get; }
        public bool Succeeded => Status is CameraSubjectAvailabilityStatus.SucceededAvailable or CameraSubjectAvailabilityStatus.SucceededAlreadyAvailable or CameraSubjectAvailabilityStatus.SucceededUnavailable or CameraSubjectAvailabilityStatus.SucceededAlreadyUnavailable;
    }
}
