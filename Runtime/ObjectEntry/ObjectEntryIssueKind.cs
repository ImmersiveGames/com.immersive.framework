using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds for object entry planning and validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry issue kind introduced by F13A.")]
    public enum ObjectEntryIssueKind
    {
        Unknown = 0,
        InvalidRequest = 10,
        InvalidIdentity = 20,
        InvalidScope = 30,
        MissingOwner = 40,
        DuplicateIdentity = 50,
        UnsupportedSourceKind = 60,
        Exception = 70
    }
}
