using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Declares whether a content declaration must resolve for its owning lifecycle to continue.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum FrameworkContentRequiredness
    {
        Optional = 0,
        Required = 10
    }
}
