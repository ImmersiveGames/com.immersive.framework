using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Explicit source boundary for supplying Activity Content Execution participants to ActivityFlow.
    /// Implementations must not rely on global scene searches, service locator access or gameplay-specific assumptions.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10K Activity Content Execution participant source boundary; explicit input only.")]
    public interface IActivityContentExecutionParticipantSource
    {
        /// <summary>
        /// Resolves participants for the supplied Activity transition context.
        /// This method must only return known participants; it must not materialize, destroy, place, reset or mutate gameplay objects.
        /// </summary>
        ActivityContentExecutionParticipantSourceResult ResolveActivityContentExecutionParticipants(ActivityContentExecutionParticipantSourceRequest request);
    }
}
