using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Identity
{
    /// <summary>
    /// API status: Stable. Minimal contract for future domain-specific framework identity wrappers.
    /// It intentionally exposes only domain and value; lifecycle objects must not use it as a service lookup contract.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable boundary identity primitive. Breaking changes require ADR/migration.")]
    public interface IFrameworkIdentity
    {
        FrameworkIdentityDomain Domain { get; }

        FrameworkIdentityValue Value { get; }
    }
}
