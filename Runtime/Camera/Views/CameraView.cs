using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable logical Camera View description. The View exists for its context scope
    /// independently from Subject availability and contains no physical presentation data.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B logical Camera View description.")]
    public readonly struct CameraView
    {
        public CameraView(CameraViewId viewId, string description)
        {
            ViewId = viewId;
            Description = description.NormalizeText();
        }

        public CameraViewId ViewId { get; }
        public string Description { get; }
        public bool IsValid => ViewId.IsValid;
    }
}
