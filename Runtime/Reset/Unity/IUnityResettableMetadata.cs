using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>Optional execution metadata for custom capabilities inside a Resettable boundary.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 optional metadata for custom Resettable capabilities.")]
    public interface IUnityResettableMetadata
    {
        ResetParticipantRequiredness ResetRequiredness { get; }

        int ResetOrder { get; }

        string ResetDisplayName { get; }
    }
}
