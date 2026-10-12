using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>Capability contract for a gameplay component discovered inside a Resettable boundary.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 custom Resettable capability.")]
    public interface IUnityResettable
    {
        /// <summary>
        /// Executes this component's reset synchronously. Awaitable orchestration remains owned by ResetExecutor.
        /// </summary>
        ResetParticipantResult Reset(ResetContext context);
    }
}
