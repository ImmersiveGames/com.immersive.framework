using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Synchronous local participant contract for the Reset module.
    /// Awaitable orchestration belongs to ResetExecutor, not to individual participants.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A synchronous Reset participant contract.")]
    public interface IResetParticipant
    {
        bool TryCreateResetParticipantDescriptor(
            ResetSubject subject,
            out ResetParticipantDescriptor descriptor,
            out ResetIssue issue);

        ResetParticipantResult Reset(ResetContext context);
    }
}
