namespace Immersive.Framework.PlayerParticipation
{
    public enum LocalPlayerProvisioningIssueKind
    {
        None = 0,
        MissingRequiredSurface = 10,
        DuplicateSurface = 20,
        MissingPlayerInputManager = 30,
        DivergentPlayerInputManager = 40,
        MissingPlayerPrefab = 50,
        InvalidPlayerHost = 60,
        InvalidCapacity = 70,
        ManualJoinRequired = 80,
        CSharpEventsRequired = 90
    }
}
