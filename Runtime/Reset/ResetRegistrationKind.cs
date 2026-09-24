using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Classifies the opaque reset registration handle payload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset registration handle kind.")]
    public enum ResetRegistrationKind
    {
        Unknown = 0,
        Subject = 10,
        Participant = 20
    }
}
