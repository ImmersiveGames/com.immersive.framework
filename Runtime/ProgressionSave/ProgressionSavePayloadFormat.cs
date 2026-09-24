using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Stable. Coarse payload representation stored by the
    /// Progression Save port. This is not a backend selection, file extension
    /// or JSON declaration.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Stable,
        "ADR018-A certified Progression Save payload format primitive.")]
    public enum ProgressionSavePayloadFormat
    {
        Unknown = 0,
        Empty = 10,
        Binary = 20,
        Text = 30,
        Structured = 40
    }
}
