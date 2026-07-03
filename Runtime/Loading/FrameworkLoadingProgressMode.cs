using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Internal. Loading progress representation mode for framework diagnostics.
    /// It describes the shape of the loading progress contract, not the visual loading surface.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26B loading progress mode contract.")]
    internal enum FrameworkLoadingProgressMode
    {
        Unknown = 0,
        Indeterminate = 10,
        Determinate = 20
    }
}
