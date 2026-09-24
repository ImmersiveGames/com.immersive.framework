using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Requiredness of an object entry for its owning lifecycle scope.
    /// Required entries are allowed to block validation in future integration cuts; F13A only records the contract.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry requiredness primitive introduced by F13A.")]
    public enum ObjectEntryRequiredness
    {
        Unspecified = 0,
        Required = 10,
        Optional = 20
    }
}
