using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored consumer that receives the Session-scoped Camera Subject availability
    /// authority through explicit Session injection. It mirrors the existing
    /// Camera Output consumer scene-injection convention: no Find-based polling,
    /// no static/global lookup, no service locator. The Session decides when a scoped
    /// authority exists; consumers only receive what Session explicitly hands them.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-E scene-injection contract for Camera Subject availability consumers.")]
    public interface ICameraSubjectAvailabilityConsumer
    {
        void AttachCameraSubjectAvailability(ICameraSubjectAvailabilitySource availabilitySource);
    }
}
