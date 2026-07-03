using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Identity
{
    /// <summary>
    /// API status: Experimental. Minimal contract for future domain-specific framework identity wrappers.
    /// It intentionally exposes only domain and value; lifecycle objects must not use it as a service lookup contract.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal typed identity contract introduced by F1E.")]
    public interface IFrameworkIdentity
    {
        FrameworkIdentityDomain Domain { get; }

        FrameworkIdentityValue Value { get; }
    }
}
