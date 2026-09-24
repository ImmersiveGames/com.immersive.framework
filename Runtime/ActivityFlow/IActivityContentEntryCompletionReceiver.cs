using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Receives the deterministic completion of an Activity entry after its content
    /// transition has completed. Persistent Content composition attaches receivers explicitly;
    /// this is runtime lifecycle integration, not Activity authoring.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime lifecycle extension point for optional module coordination.")]
    public interface IActivityContentEntryCompletionReceiver
    {
        void OnActivityContentEntryCompleted();
    }
}
