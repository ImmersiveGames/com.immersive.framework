using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7C immutable non-physical contextual Player Actor candidate evidence.")]
    public sealed class PlayerActorCandidateStageSnapshot
    {
        internal PlayerActorCandidateStageSnapshot(
            PlayerActorCandidateStageToken token,
            PlayerActorCandidateStageState state,
            PlayerActorMaterializationSnapshot materialization,
            PlayerActorPreparationToken currentPreparationToken,
            ActorId currentActorId,
            RuntimeContentOwner currentOwner,
            string source,
            string reason,
            string message)
        {
            Token = token;
            State = state;
            Materialization = materialization;
            CurrentPreparationToken = currentPreparationToken;
            CurrentActorId = currentActorId;
            CurrentOwner = currentOwner;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PlayerActorCandidateStageToken Token { get; }
        public PlayerActorCandidateStageState State { get; }
        public PlayerActorMaterializationSnapshot Materialization { get; }
        public PlayerActorPreparationToken CurrentPreparationToken { get; }
        public ActorId CurrentActorId { get; }
        public RuntimeContentOwner CurrentOwner { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool HasCurrentPreparation =>
            CurrentPreparationToken.IsValid &&
            CurrentActorId.IsValid &&
            CurrentOwner.IsValid;

        public bool IsStagedInactive =>
            State == PlayerActorCandidateStageState.StagedInactive &&
            Token.IsValid &&
            Materialization.IsStaged;

        public bool IsRollbackFailed =>
            State == PlayerActorCandidateStageState.RollbackFailed;

        public bool IsRolledBack =>
            State == PlayerActorCandidateStageState.RolledBack;

        public string ToDiagnosticString()
        {
            return
                $"candidate='{Token.StableText}' state='{State}' " +
                $"materialization='{(Materialization.IsValid ? Materialization.ToDiagnosticString() : string.Empty)}' " +
                $"currentPreparation='{CurrentPreparationToken.StableText}' " +
                $"currentActor='{(CurrentActorId.IsValid ? CurrentActorId.StableText : string.Empty)}' " +
                $"currentOwner='{(CurrentOwner.IsValid ? CurrentOwner.StableText : string.Empty)}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        internal static PlayerActorCandidateStageSnapshot Empty(
            string source,
            string reason,
            string message)
        {
            return new PlayerActorCandidateStageSnapshot(
                default,
                PlayerActorCandidateStageState.None,
                default,
                default,
                default,
                default,
                source,
                reason,
                message);
        }
    }
}
