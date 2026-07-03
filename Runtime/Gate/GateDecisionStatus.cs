using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Experimental. Canonical outcome of one Gate admission decision.
    /// The status is intentionally richer than bool so diagnostics can distinguish block, queue and rejection cases.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F17B Gate decision status primitive; no bool-only canonical contract.")]
    public enum GateDecisionStatus
    {
        /// <summary>Invalid default value.</summary>
        Unknown = 0,

        /// <summary>The requested operation can proceed.</summary>
        Allowed = 10,

        /// <summary>The requested operation is valid but currently blocked by at least one active blocker.</summary>
        Blocked = 20,

        /// <summary>The requested operation is valid but was deferred by policy. F17B does not implement a queue runtime.</summary>
        Queued = 30,

        /// <summary>The request shape is invalid.</summary>
        RejectedInvalidRequest = 40,

        /// <summary>The request scope is invalid or missing.</summary>
        RejectedInvalidScope = 50,

        /// <summary>The request domain is invalid or missing.</summary>
        RejectedInvalidDomain = 60,

        /// <summary>The request is stale for the current lifecycle generation.</summary>
        RejectedStale = 70,

        /// <summary>The request belongs to a different owner/scope than the active context.</summary>
        RejectedForeign = 80,

        /// <summary>A required policy or blocker source is missing.</summary>
        RejectedPolicyMissing = 90
    }
}
