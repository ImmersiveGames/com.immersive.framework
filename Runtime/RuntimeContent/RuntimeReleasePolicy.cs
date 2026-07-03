using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Logical release policy applied after a release operation succeeds.
    /// It controls handle state/registry cleanup only; it does not destroy objects, unload scenes, return pools or release Addressables handles.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8J logical runtime release policy; no physical cleanup implementation in RuntimeContent core.")]
    public enum RuntimeReleasePolicy
    {
        /// <summary>
        /// Invalid default value. Runtime release requests must always use an explicit policy.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Mark the handle as Released but keep it registered in the logical scope root for later diagnostics or owner-specific cleanup.
        /// </summary>
        MarkReleasedOnly = 10,

        /// <summary>
        /// Mark the handle as Released and unregister it from the logical scope root so the root can be removed.
        /// </summary>
        MarkReleasedAndUnregister = 20
    }
}
