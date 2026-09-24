using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Internal physical-application port used by CameraOutputSession.
    /// Winner/default policy remains owned by the Session and Context.
    /// </summary>
    internal interface ICameraOutputApplication
    {
        CameraOutputBinding Binding { get; }
        CinemachineCamera AppliedCamera { get; }

        CameraOutputApplyResult Apply(
            CameraOutputContext context,
            CameraRigReference defaultRig,
            bool forceDefault);

        CameraOutputApplyResult Clear();
    }
}
