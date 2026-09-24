namespace Immersive.Framework.PlayerParticipation
{
    internal enum PlayerActivityReconciliationStableChangeKind
    {
        None = 0,
        SessionRevisionChanged = 10,
        ActivityOccurrenceChanged = 20,
        SessionRevisionAndActivityOccurrenceChanged = 30
    }
}
