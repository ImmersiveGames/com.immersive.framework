using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit result of selecting one View from a logical Assignment snapshot.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C View presentation-input projection result.")]
    public sealed class CameraViewPresentationInputResult
    {
        internal CameraViewPresentationInputResult(
            CameraViewPresentationInputStatus status,
            CameraViewPresentationInput input,
            string message)
        {
            Status = status;
            Input = input;
            Message = message ?? string.Empty;
        }

        public CameraViewPresentationInputStatus Status { get; }
        public CameraViewPresentationInput Input { get; }
        public string Message { get; }
        public bool Succeeded =>
            Status == CameraViewPresentationInputStatus.Succeeded &&
            Input != null &&
            Input.IsValid;
    }
}
