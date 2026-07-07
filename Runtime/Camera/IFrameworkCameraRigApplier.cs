using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Optional adapter contract for concrete camera packages that apply priority and targets.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public interface IFrameworkCameraRigApplier
    {
        bool Supports(FrameworkCameraRigDescriptor descriptor);

        void Apply(FrameworkCameraRigDescriptor descriptor);
    }
}
