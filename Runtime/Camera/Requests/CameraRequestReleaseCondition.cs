namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares why a request must be released.
    /// This contract does not perform the release.
    /// </summary>
    public enum CameraRequestReleaseCondition
    {
        Undefined = 0,
        ExplicitRelease = 1,
        OwnerLifetimeEnded = 2,
        ScopeExited = 3,
        EligibilityLost = 4,
        SessionEnded = 5
    }
}
