using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Experimental. Scope domain for structured framework facts.
    /// Scopes keep Application, Route, Activity, Content Anchor, QA and validation diagnostics separated.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal structured diagnostics scope introduced by F1C.")]
    public enum FrameworkFactScope
    {
        Application = 0,
        Session = 1,
        Route = 2,
        Activity = 3,
        Content = 4,
        Local = 5,
        ContentAnchor = 6,
        Runtime = 7,
        Validation = 8,
        Qa = 9
    }
}
