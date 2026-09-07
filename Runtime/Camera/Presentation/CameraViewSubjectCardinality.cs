using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit logical cardinality of resolved Subjects in one View input.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C resolved Camera View Subject cardinality.")]
    public enum CameraViewSubjectCardinality
    {
        Zero = 0,
        One = 10,
        Many = 20
    }
}
