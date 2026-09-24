using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Diagnostic origin of a reset subject.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset subject origin.")]
    public enum ResetSubjectOrigin
    {
        Unknown = 0,
        SceneAuthored = 10,
        RuntimeRegistered = 20
    }
}
