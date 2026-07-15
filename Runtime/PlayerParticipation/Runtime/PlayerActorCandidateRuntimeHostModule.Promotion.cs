
namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorCandidateRuntimeHostModule
    {
        internal bool TryBeginCandidatePromotion(
            PlayerActorCandidateStageToken expectedCandidate,
            string source,
            string reason,
            out PlayerActorCandidatePromotionHandle handle,
            out string issue)
        {
            if (candidateContext == null)
            {
                handle = null;
                issue = diagnostic;
                return false;
            }

            bool succeeded = candidateContext.TryBeginPromotion(
                expectedCandidate,
                source,
                reason,
                out handle,
                out issue);
            diagnostic = succeeded
                ? $"Candidate promotion reserved. candidate='{expectedCandidate.StableText}'."
                : issue;
            return succeeded;
        }

        internal bool WasCandidatePromoted(PlayerActorCandidateStageToken token) =>
            candidateContext != null && candidateContext.WasPromoted(token);
    }
}
