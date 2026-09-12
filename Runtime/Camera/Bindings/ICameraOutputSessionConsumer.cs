
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    internal interface ICameraOutputDefinitionConsumer
    {
        CameraAuthoring.CameraOutputDefinition OutputDefinition { get; }
    }

    /// <summary>
    /// Internal injection boundary for route-scoped camera request sources.
    /// It intentionally has no static lookup or global registration path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Explicit Camera Output dependency by stable Output ID.")]
    public interface ICameraOutputSessionConsumer
    {
        CameraOutputId RequestedOutputId { get; }
        void AttachOutputSession(CameraOutputAuthoring binding);
        void DetachOutputSession(string reason);
    }
}
