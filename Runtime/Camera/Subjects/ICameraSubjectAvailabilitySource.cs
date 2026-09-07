using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Read-only Camera-domain observation seam for one scoped Subject availability
    /// authority. Consumers can observe immutable state but cannot publish or remove Subjects.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-E read-only Camera Subject availability source.")]
    public interface ICameraSubjectAvailabilitySource
    {
        string ContextId { get; }
        event Action<CameraSubjectAvailabilitySnapshot> AvailabilityChanged;
        CameraSubjectAvailabilitySnapshot CreateSnapshot();
    }
}
