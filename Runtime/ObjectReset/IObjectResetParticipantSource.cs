using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Explicit source boundary for supplying Object Reset participants to the framework runtime.
    /// Implementations must not use global scene searches, service locator access, gameplay-specific assumptions, materialization, release, reload, snapshot restore or pool return.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant source boundary; explicit input only.")]
    public interface IObjectResetParticipantSource
    {
        /// <summary>
        /// Resolves known participants for one already-resolved Object Reset target.
        /// This method must only return already-known participants; it must not create, destroy, reset, reload, save, restore or return pooled objects.
        /// </summary>
        IReadOnlyList<IObjectResetParticipant> ResolveObjectResetParticipants(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget);
    }
}
