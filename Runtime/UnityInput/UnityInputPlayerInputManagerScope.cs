using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Declares the framework scope where Unity's official PlayerInputManager is expected.
    /// This is evidence vocabulary only; it does not create a custom input manager.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31B PlayerInputManager session scope vocabulary.")]
    public enum UnityInputPlayerInputManagerScope
    {
        Unknown = 0,
        Session = 10
    }
}
