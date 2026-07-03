using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Internal. Default explicit participant source used until authoring/discovery is introduced.
    /// It intentionally returns no participants and performs no scene search or Unity side effect.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F10K default empty Activity Content Execution participant source; no discovery or Unity side effects.")]
    internal sealed class EmptyActivityContentExecutionParticipantSource : IActivityContentExecutionParticipantSource
    {
        internal static readonly EmptyActivityContentExecutionParticipantSource Instance = new EmptyActivityContentExecutionParticipantSource();

        private EmptyActivityContentExecutionParticipantSource()
        {
        }

        public ActivityContentExecutionParticipantSourceResult ResolveActivityContentExecutionParticipants(ActivityContentExecutionParticipantSourceRequest request)
        {
            return ActivityContentExecutionParticipantSourceResult.SucceededNoParticipants(
                request,
                request.Source,
                request.Reason,
                "No Activity Content Execution participant source is configured; lifecycle execution continues with an empty participant collection.");
        }
    }
}
