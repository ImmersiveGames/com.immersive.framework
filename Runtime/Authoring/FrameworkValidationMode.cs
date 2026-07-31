using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Stable. Public authoring mode that controls validation and diagnostics severity.
    /// F1D defines the minimal semantics: required configuration fails in every mode; Strict promotes warnings to errors;
    /// Standard keeps warnings as warnings; Release suppresses info diagnostics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public enum FrameworkValidationMode
    {
        /// <summary>Required configuration fails; warning diagnostics are promoted to errors; info diagnostics are included.</summary>
        Strict = 0,

        /// <summary>Required configuration fails; warning diagnostics remain warnings; info diagnostics are included.</summary>
        Standard = 1,

        /// <summary>Required configuration fails; warning diagnostics remain warnings; info diagnostics are suppressed.</summary>
        Release = 2
    }
}
