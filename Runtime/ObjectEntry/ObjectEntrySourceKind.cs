using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Describes how an object entry is supplied to the framework.
    /// It is diagnostic/contract metadata; it does not perform discovery or materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry source kind primitive introduced by F13A.")]
    public enum ObjectEntrySourceKind
    {
        Unspecified = 0,
        SceneAuthored = 10,
        RuntimeRegistered = 20,
        RuntimeMaterialized = 30,
        External = 40
    }
}
