namespace Immersive.Framework.Camera
{
    /// <summary>Consumer that requires an exact logical View-to-physical Output relation.</summary>
    internal interface ICameraViewOutputBindingConsumer
    {
        CameraViewId RequestedViewId { get; }
    }
}
