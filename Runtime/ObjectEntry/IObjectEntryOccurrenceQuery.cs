using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Read-only, point-in-time query for the current physical occurrence of an admitted Object Entry.
    /// A resolved GameObject establishes physical availability only; it grants no gameplay authorization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Owner-scoped read-only Object Entry occurrence query.")]
    public interface IObjectEntryOccurrenceQuery
    {
        /// <summary>
        /// Resolves the reference against committed Runtime Content. The result describes physical
        /// availability at this call and must not be used as a persistent physical identity.
        /// </summary>
        ObjectEntryOccurrenceResolution Resolve(StableObjectReference reference);
    }
}
