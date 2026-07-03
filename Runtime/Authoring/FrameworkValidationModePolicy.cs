using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Minimal F1 policy for how ValidationMode affects framework diagnostics.
    /// Required configuration still fails in every mode; this policy only controls diagnostic strictness/noise.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal ValidationMode semantics introduced by F1D.")]
    public static class FrameworkValidationModePolicy
    {
        public static bool RequiredConfigurationFails(FrameworkValidationMode mode)
        {
            return true;
        }

        public static bool TreatWarningsAsErrors(FrameworkValidationMode mode)
        {
            return mode == FrameworkValidationMode.Strict;
        }

        public static bool IncludeInfoDiagnostics(FrameworkValidationMode mode)
        {
            return mode != FrameworkValidationMode.Release;
        }

        public static bool IsKnown(FrameworkValidationMode mode)
        {
            return mode is FrameworkValidationMode.Strict or FrameworkValidationMode.Standard or FrameworkValidationMode.Release;
        }

        public static string GetSummary(FrameworkValidationMode mode)
        {
            switch (mode)
            {
                case FrameworkValidationMode.Strict:
                    return "Strict: required configuration fails, warnings are promoted to errors, and info diagnostics are included.";
                case FrameworkValidationMode.Standard:
                    return "Standard: required configuration fails, warnings remain warnings, and info diagnostics are included.";
                case FrameworkValidationMode.Release:
                    return "Release: required configuration fails, warnings remain warnings, and info diagnostics are suppressed.";
                default:
                    return "Unknown ValidationMode: required configuration fails and diagnostics should be treated as Strict until the asset is corrected.";
            }
        }
    }
}
