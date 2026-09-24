using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Internal release action vocabulary. F6F only plans actions; physical execution starts later.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release action vocabulary; no physical release side effects in this cut.")]
    internal enum ContentReleaseAction
    {
        None = 0,
        UnloadScene = 10,
        DestroyRuntimeObject = 20,
        ReturnToPool = 30,
        CustomParticipantRelease = 40
    }
}
