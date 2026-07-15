using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7B internal ownership handoff for one committed staged Activity admission.")]
    internal sealed class ActivityPlayerAdmissionStageCommit
    {
        private readonly IActivityPlayerAdmissionStageScopeRuntime scopeRuntime;
        private readonly IActivityPlayerAdmissionStageResolver resolver;
        private readonly ActivityPlayerAdmissionStageScope scope;
        private readonly ActivityPlayerAdmissionStageResolution resolution;
        private bool completed;
        private bool rolledBack;
        private bool released;

        internal ActivityPlayerAdmissionStageCommit(
            ActivityPlayerAdmissionStageToken token,
            ActivityPlayerAdmissionFlowDecision decision,
            IActivityPlayerAdmissionStageScopeRuntime scopeRuntime,
            IActivityPlayerAdmissionStageResolver resolver,
            ActivityPlayerAdmissionStageScope scope,
            ActivityPlayerAdmissionStageResolution resolution)
        {
            Token = token;
            Decision = decision;
            this.scopeRuntime = scopeRuntime;
            this.resolver = resolver;
            this.scope = scope;
            this.resolution = resolution;
        }

        internal ActivityPlayerAdmissionStageToken Token { get; }
        internal ActivityPlayerAdmissionFlowDecision Decision { get; }
        internal ActivityPlayerAdmissionStageScope Scope => scope;
        internal bool IsCompleted => completed;
        internal bool IsRolledBack => rolledBack;
        internal bool IsReleased => released;

        internal bool TryComplete(out string issue)
        {
            issue = string.Empty;
            if (rolledBack)
            {
                issue = "Committed Activity Player admission stage was already rolled back.";
                return false;
            }

            if (released)
            {
                issue = "Committed Activity Player admission stage was already released.";
                return false;
            }

            if (completed)
            {
                return true;
            }

            completed = true;
            return true;
        }

        internal bool TryRelease(
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (rolledBack)
            {
                issue = "Committed Activity Player admission stage was rolled back before activation.";
                return false;
            }

            if (!completed)
            {
                issue = "Committed Activity Player admission stage cannot be released before ownership completion.";
                return false;
            }

            if (released)
            {
                return true;
            }

            if (!TryReleaseParts(source, reason, out issue))
            {
                return false;
            }

            released = true;
            return true;
        }

        internal bool TryRollback(
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (completed)
            {
                issue = "Committed Activity Player admission stage ownership was already completed.";
                return false;
            }

            if (rolledBack)
            {
                return true;
            }

            if (!TryReleaseParts(source, reason, out issue))
            {
                return false;
            }

            rolledBack = true;
            return true;
        }

        private bool TryReleaseParts(
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            string resolverIssue = string.Empty;
            try
            {
                if (!resolver.TryRollback(
                        resolution,
                        source,
                        reason,
                        out resolverIssue))
                {
                    issue = string.IsNullOrWhiteSpace(resolverIssue)
                        ? "Committed Activity Player stage resolver release failed."
                        : resolverIssue.Trim();
                    return false;
                }
            }
            catch (System.Exception exception)
            {
                issue =
                    $"Committed Activity Player stage resolver release threw '{exception.GetType().Name}'. {exception.Message}";
                return false;
            }

            string scopeIssue = string.Empty;
            try
            {
                if (!scopeRuntime.TryRelease(
                        scope,
                        source,
                        reason,
                        out scopeIssue))
                {
                    issue = string.IsNullOrWhiteSpace(scopeIssue)
                        ? "Committed Activity Player stage scope release failed."
                        : scopeIssue.Trim();
                    return false;
                }
            }
            catch (System.Exception exception)
            {
                issue =
                    $"Committed Activity Player stage scope release threw '{exception.GetType().Name}'. {exception.Message}";
                return false;
            }

            return true;
        }
    }
}
