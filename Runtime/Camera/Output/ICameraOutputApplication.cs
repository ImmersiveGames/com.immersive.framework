using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Internal physical-application port used by CameraOutputSession.
    /// Legacy Request and Fallback application is owned by the Session/Context boundary.
    /// </summary>
    internal interface ICameraOutputApplication
    {
        CameraOutputBinding Binding { get; }
        CinemachineCamera AppliedCamera { get; }

        CameraOutputApplyResult Apply(
            CameraOutputContext context,
            CameraRigReference fallbackRig,
            bool coverWithFallback);

        CameraOutputApplyResult Clear();
    }
}
