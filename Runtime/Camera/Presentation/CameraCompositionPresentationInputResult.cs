using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit result of projecting current Composition membership.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E Composition presentation-input projection result.")]
    public sealed class CameraCompositionPresentationInputResult
    {
        internal CameraCompositionPresentationInputResult(
            CameraCompositionPresentationInputStatus status,
            CameraCompositionPresentationInput input,
            string message)
        {
            Status = status;
            Input = input;
            Message = message ?? string.Empty;
        }

        public CameraCompositionPresentationInputStatus Status { get; }
        public CameraCompositionPresentationInput Input { get; }
        public string Message { get; }
        public bool Succeeded =>
            Status == CameraCompositionPresentationInputStatus.Succeeded &&
            Input != null &&
            Input.IsValid;
    }
}
