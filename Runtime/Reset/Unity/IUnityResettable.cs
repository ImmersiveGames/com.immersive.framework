using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Implement this on gameplay MonoBehaviours that should participate
    /// in a UnityResetSubjectAdapter reset without adding a separate UnityResetParticipantBehaviour.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12J gameplay component reset participation through UnityResetSubjectAdapter.")]
    public interface IUnityResettable
    {
        /// <summary>
        /// Stable participant id under the owning ResetSubject. It must be unique for that subject.
        /// </summary>
        string ResetParticipantId { get; }

        /// <summary>
        /// Executes this component's reset synchronously. Awaitable orchestration remains owned by ResetExecutor.
        /// </summary>
        ResetParticipantResult Reset(ResetContext context);
    }
}
