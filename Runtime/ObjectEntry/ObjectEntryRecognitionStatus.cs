using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>Explicit outcomes for read-only physical Object Entry recognition.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Outcomes for physical Object Entry recognition.")]
    public enum ObjectEntryRecognitionStatus
    {
        Invalid = 0,
        Unrecognized = 10,
        Resolved = 20,
        Unavailable = 30,
        Ambiguous = 40
    }
}
