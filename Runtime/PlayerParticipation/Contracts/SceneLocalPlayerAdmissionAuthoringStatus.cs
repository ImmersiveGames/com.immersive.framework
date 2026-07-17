using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Authoring-only status. Runtime reservation, admission and release use separate typed results.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4 scene local Player admission authoring status.")]
    public enum SceneLocalPlayerAdmissionAuthoringStatus
    {
        NotValidated = 0,
        Valid = 10,
        InvalidReferences = 100,
        InvalidHost = 110,
        InvalidSlotProfile = 120,
        InvalidActorProfile = 130,
        InvalidActorHierarchy = 140,
        InvalidActorShape = 150,
        MissingProfileEvidence = 160,
        IncompatibleProfileEvidence = 170
    }
}
