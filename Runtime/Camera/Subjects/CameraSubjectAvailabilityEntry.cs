using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Immutable current availability evidence for one Camera Subject.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A current Camera Subject availability evidence.")]
    public readonly struct CameraSubjectAvailabilityEntry
    {
        internal CameraSubjectAvailabilityEntry(CameraSubject subject, CameraSubjectAvailabilityOwnerId ownerId, CameraSubjectAvailabilityToken token)
        {
            Subject = subject;
            OwnerId = ownerId;
            Token = token;
        }

        public CameraSubject Subject { get; }
        public CameraSubjectAvailabilityOwnerId OwnerId { get; }
        public CameraSubjectAvailabilityToken Token { get; }
        public bool IsValid => Subject.IsValid && OwnerId.IsValid && Token.IsValid && Token.SubjectId == Subject.SubjectId && Token.OwnerId == OwnerId;
    }
}
