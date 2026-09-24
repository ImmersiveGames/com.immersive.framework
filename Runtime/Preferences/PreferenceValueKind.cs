using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Canonical value kinds supported by the Preferences boundary.
    /// This is not a progression save schema, Snapshot payload format or backend selection.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences value kind primitive; independent from Snapshot and Progression Save.")]
    public enum PreferenceValueKind
    {
        Unknown = 0,
        String = 10,
        Int = 20,
        Float = 30,
        Bool = 40
    }
}
