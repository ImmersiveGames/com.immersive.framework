using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed reference to one concrete materialized Camera rig.
    ///
    /// CameraRigComposer is the sole framework authority for rig targets,
    /// requirements, framing and Cinemachine materialization. Reusable authoring
    /// values belong to Unity Presets and do not participate in runtime requests.
    ///
    /// This reference carries evidence only and does not activate the rig.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public readonly struct CameraRigReference
    {
        public CameraRigReference(
            CameraRigComposer composer)
        {
            Composer = composer;
        }

        public CameraRigComposer Composer { get; }

        public bool HasComposer =>
            Composer != null;

        public bool IsValid =>
            HasComposer;

        public static CameraRigReference FromComposer(
            CameraRigComposer composer)
        {
            return new CameraRigReference(
                composer);
        }

        public static CameraRigReference From(
            CameraRigComposer composer)
        {
            return FromComposer(
                composer);
        }
    }
}
