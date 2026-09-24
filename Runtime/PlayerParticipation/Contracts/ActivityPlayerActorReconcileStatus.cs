using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Typed result status for one explicit active-Activity Player lifecycle reconcile pass.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-M07-10 explicit delta reconcile status.")]
    public enum ActivityPlayerActorReconcileStatus
    {
        None = 0,
        SucceededNoChange = 10,
        SucceededProgressed = 20,
        SucceededCompleted = 30,
        RejectedInvalidRequest = 100,
        RejectedNoActiveActivity = 110,
        RejectedForeignOrStaleActivity = 120,
        RejectedForeignOrStaleOwner = 130,
        RejectedForeignOrStaleOccurrence = 140,
        FailedProjection = 200,
        FailedHostEvidence = 210,
        FailedSelection = 220,
        FailedPreparation = 230,
        FailedGameplayAdmission = 240,
        FailedRollback = 250
    }
}
