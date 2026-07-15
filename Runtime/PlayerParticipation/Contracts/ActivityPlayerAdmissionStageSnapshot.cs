using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B immutable staged Activity Player admission snapshot.")]
    public sealed class ActivityPlayerAdmissionStageSnapshot
    {
        internal ActivityPlayerAdmissionStageSnapshot(
            ActivityPlayerAdmissionStageToken token,
            ActivityPlayerAdmissionStageState state,
            ActivityPlayerAdmissionFlowDecision decision,
            RuntimeContentOwner stagedOwner,
            bool scopeCreated,
            bool resolverCompleted,
            bool resolverRolledBack,
            bool scopeReleased,
            string source,
            string reason,
            string message)
        {
            Token = token;
            State = state;
            Decision = decision;
            StagedOwner = stagedOwner;
            ScopeCreated = scopeCreated;
            ResolverCompleted = resolverCompleted;
            ResolverRolledBack = resolverRolledBack;
            ScopeReleased = scopeReleased;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ActivityPlayerAdmissionStageToken Token { get; }
        public ActivityPlayerAdmissionStageState State { get; }
        public ActivityPlayerAdmissionFlowDecision Decision { get; }
        public RuntimeContentOwner StagedOwner { get; }
        public bool ScopeCreated { get; }
        public bool ResolverCompleted { get; }
        public bool ResolverRolledBack { get; }
        public bool ScopeReleased { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsReadyToCommit =>
            State == ActivityPlayerAdmissionStageState.ReadyToCommit &&
            Token.IsValid &&
            Decision != null &&
            Decision.CanProceed;

        public bool IsCommitted =>
            State == ActivityPlayerAdmissionStageState.Committed;

        public bool IsRolledBack =>
            State == ActivityPlayerAdmissionStageState.RolledBack;

        public bool IsRollbackFailed =>
            State == ActivityPlayerAdmissionStageState.RollbackFailed;

        public string ToDiagnosticString()
        {
            return
                $"stage='{Token.StableText}' state='{State}' " +
                $"owner='{(StagedOwner.IsValid ? StagedOwner.ToString() : string.Empty)}' " +
                $"scopeCreated='{ScopeCreated}' resolverCompleted='{ResolverCompleted}' " +
                $"resolverRolledBack='{ResolverRolledBack}' scopeReleased='{ScopeReleased}' " +
                $"decision=[{(Decision != null ? Decision.ToDiagnosticString() : string.Empty)}] " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }

        internal static ActivityPlayerAdmissionStageSnapshot Empty(
            string source,
            string reason,
            string message)
        {
            return new ActivityPlayerAdmissionStageSnapshot(
                default,
                ActivityPlayerAdmissionStageState.None,
                null,
                default,
                false,
                false,
                false,
                false,
                source,
                reason,
                message);
        }
    }
}
