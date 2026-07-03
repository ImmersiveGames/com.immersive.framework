using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Coarse payload representation stored by the Progression Save port.
    /// This is not a backend selection, file extension or JSON declaration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save payload format primitive; backend-agnostic.")]
    public enum ProgressionSavePayloadFormat
    {
        Unknown = 0,
        Empty = 10,
        Binary = 20,
        Text = 30,
        Structured = 40
    }
}
