using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Severity for object entry diagnostics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry issue severity introduced by F13A.")]
    public enum ObjectEntryIssueSeverity
    {
        Info = 0,
        Warning = 10,
        Error = 20
    }
}
