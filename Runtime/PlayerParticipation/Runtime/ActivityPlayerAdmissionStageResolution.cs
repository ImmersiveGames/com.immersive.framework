namespace Immersive.Framework.PlayerParticipation
{
    public sealed class ActivityPlayerAdmissionStageResolution
    {
        public ActivityPlayerAdmissionStageResolution(
            bool succeeded,
            PlayerParticipationSnapshot participationSnapshot,
            PlayerActorPreparationSnapshot preparationSnapshot,
            PlayerGameplayAdmissionSnapshot gameplayAdmissionSnapshot,
            object resolverState,
            string message)
        {
            Succeeded = succeeded;
            ParticipationSnapshot = participationSnapshot;
            PreparationSnapshot = preparationSnapshot;
            GameplayAdmissionSnapshot = gameplayAdmissionSnapshot;
            ResolverState = resolverState;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public PlayerParticipationSnapshot ParticipationSnapshot { get; }
        public PlayerActorPreparationSnapshot PreparationSnapshot { get; }
        public PlayerGameplayAdmissionSnapshot GameplayAdmissionSnapshot { get; }
        public object ResolverState { get; }
        public string Message { get; }

        public static ActivityPlayerAdmissionStageResolution Failed(
            PlayerParticipationSnapshot participationSnapshot,
            PlayerActorPreparationSnapshot preparationSnapshot,
            PlayerGameplayAdmissionSnapshot gameplayAdmissionSnapshot,
            object resolverState,
            string message)
        {
            return new ActivityPlayerAdmissionStageResolution(
                false,
                participationSnapshot,
                preparationSnapshot,
                gameplayAdmissionSnapshot,
                resolverState,
                message);
        }
    }
}
