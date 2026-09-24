using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Controls how UnityResetSubjectAdapter discovers local reset participants.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12B Unity reset participant discovery mode.")]
    public enum UnityResetParticipantDiscoveryMode
    {
        Unknown = 0,
        SameGameObject = 10,
        Children = 20
    }
}
