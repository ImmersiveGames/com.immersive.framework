using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Explicit source boundary for supplying Cycle Reset participants to the framework runtime.
    /// Implementations must not use global scene searches, service locator access, gameplay-specific assumptions, materialization, release, reload, snapshot restore or pool return.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11C Cycle Reset participant source boundary; explicit input only.")]
    public interface ICycleResetParticipantSource
    {
        /// <summary>
        /// Resolves known participants for one Route or Activity Cycle Reset request.
        /// This method must only return already-known participants; it must not create, destroy, reset, reload, save, restore or return pooled objects.
        /// </summary>
        IReadOnlyList<ICycleResetParticipant> ResolveCycleResetParticipants(CycleResetRequest request);
    }
}
