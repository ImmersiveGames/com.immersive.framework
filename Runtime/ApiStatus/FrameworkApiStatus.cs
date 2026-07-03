namespace Immersive.Framework.ApiStatus
{
    /// <summary>
    /// API status: Stable. Canonical metadata categories used to classify public and semi-public framework surfaces.
    /// </summary>
    [FrameworkApiStatus(Stable, "Canonical API maturity metadata introduced by F1B.")]
    public enum FrameworkApiStatus
    {
        /// <summary>Surface may be consumed by games and external modules. Changes require ADR/migration.</summary>
        Stable = 0,

        /// <summary>Surface is available for controlled development use, but can change without compatibility guarantees.</summary>
        Experimental = 1,

        /// <summary>Surface is framework implementation detail and should not be consumed by game code.</summary>
        Internal = 2,

        /// <summary>Surface is retained as planning/frozen source and is not part of the active baseline.</summary>
        Deferred = 3,

        /// <summary>Surface is QA/editor/development tooling, not product API.</summary>
        DevelopmentTooling = 4,

        /// <summary>Surface has been removed or is scheduled for removal.</summary>
        Removed = 5
    }
}
