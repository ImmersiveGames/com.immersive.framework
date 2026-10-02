using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>How a semantic Reset target is addressed.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 separates target kind from addressing mode.")]
    public enum ResetReferenceMode
    {
        Direct = 0,
        Stable = 10
    }
}
