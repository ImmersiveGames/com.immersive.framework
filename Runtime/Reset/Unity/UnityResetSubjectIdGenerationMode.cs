using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Selects whether a Unity reset subject uses a stable authored id or a runtime-generated instance id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12B Unity reset subject id generation mode.")]
    public enum UnityResetSubjectIdGenerationMode
    {
        Unknown = 0,
        AuthoredStableId = 10,
        RuntimeInstanceId = 20
    }
}
