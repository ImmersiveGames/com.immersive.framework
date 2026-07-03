using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Intent-level purpose for Pause content requirements.
    /// This enum does not create or bind overlay roots, menus or Content Anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23E Pause content requirement purpose vocabulary; no materialization.")]
    public enum PauseContentRequirementPurpose
    {
        Unknown = 0,
        PresentationRoot = 10,
        MenuRoot = 20,
        FocusRoot = 30,
        LoadingStatusRegion = 40,
        PreferencesRegion = 50,
        SaveStatusRegion = 60
    }
}
