using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Optional metadata for IUnityResettable components discovered by UnityResetSubjectAdapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12J optional metadata for gameplay component reset participation.")]
    public interface IUnityResettableMetadata
    {
        ResetParticipantRequiredness ResetRequiredness { get; }

        int ResetOrder { get; }

        string ResetDisplayName { get; }

        string ResetSource { get; }

        string ResetReason { get; }
    }
}
