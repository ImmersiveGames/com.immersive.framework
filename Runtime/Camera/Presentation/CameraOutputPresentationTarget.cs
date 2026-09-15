using UnityEngine;

namespace Immersive.Framework.Camera
{
    internal sealed class CameraOutputPresentationTarget
    {
        private readonly UnityEngine.Camera _camera;

        internal CameraOutputPresentationTarget(UnityEngine.Camera camera)
        {
            _camera = camera;
        }

        internal bool IsAvailable => _camera != null;
        internal Rect CaptureRect() => _camera.rect;
        internal void Apply(CameraOutputPresentationRect rect) =>
            _camera.rect = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        internal void Restore(Rect baseline)
        {
            if (_camera != null) _camera.rect = baseline;
        }
    }
}
