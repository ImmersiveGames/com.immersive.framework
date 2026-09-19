using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit logical cardinality of resolved Camera Subjects.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E resolved Camera Subject cardinality.")]
    public enum CameraSubjectCardinality
    {
        Zero = 0,
        One = 10,
        Many = 20
    }
}
