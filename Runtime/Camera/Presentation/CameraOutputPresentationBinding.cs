using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-028-C explicit Output-to-normalized-rect presentation intent.")]
    public readonly struct CameraOutputPresentationBinding
    {
        public CameraOutputPresentationBinding(
            CameraOutputId outputId,
            CameraOutputPresentationRect rect)
        {
            OutputId = outputId;
            Rect = rect;
        }

        public CameraOutputId OutputId { get; }
        public CameraOutputPresentationRect Rect { get; }
        public bool IsValid => OutputId.IsValid && Rect.IsValid;
    }
}
