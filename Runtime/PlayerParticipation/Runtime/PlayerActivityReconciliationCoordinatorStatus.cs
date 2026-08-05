namespace Immersive.Framework.PlayerParticipation
{
    internal enum PlayerActivityReconciliationCoordinatorStatus
    {
        None = 0,
        Ready = 10,
        SucceededNoActiveActivity = 20,
        SucceededWaitingForOccurrence = 30,
        SucceededReconciled = 40,
        FailedRuntimeUnavailable = 100,
        FailedInvalidTarget = 110,
        FailedReconcile = 120,
        FailedException = 130
    }
}
