using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7B internal ownership handoff for one committed staged Activity admission.")]
    internal sealed class ActivityPlayerAdmissionStageCommit
    {
        private readonly IActivityPlayerAdmissionStageScopeRuntime _scopeRuntime;
        private readonly IActivityPlayerAdmissionStageResolver _resolver;
        private readonly ActivityPlayerAdmissionStageScope _scope;
        private readonly ActivityPlayerAdmissionStageResolution _resolution;
        private bool _completed;
        private bool _rolledBack;
        private bool _released;

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
            this._scopeRuntime = scopeRuntime;
            this._resolver = resolver;
            this._scope = scope;
            this._resolution = resolution;
        }

        internal ActivityPlayerAdmissionStageToken Token { get; }
        internal ActivityPlayerAdmissionFlowDecision Decision { get; }
        internal ActivityPlayerAdmissionStageScope Scope => _scope;
        internal bool IsCompleted => _completed;
        internal bool IsRolledBack => _rolledBack;
        internal bool IsReleased => _released;

        internal bool TryComplete(out string issue)
        {
            issue = string.Empty;
            if (_rolledBack)
            {
                issue = "Committed Activity Player admission stage was already rolled back.";
                return false;
            }

            if (_released)
            {
                issue = "Committed Activity Player admission stage was already released.";
                return false;
            }

            if (_completed)
            {
                return true;
            }

            _completed = true;
            return true;
        }

        internal bool TryRelease(
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (_rolledBack)
            {
                issue = "Committed Activity Player admission stage was rolled back before activation.";
                return false;
            }

            if (!_completed)
            {
                issue = "Committed Activity Player admission stage cannot be released before ownership completion.";
                return false;
            }

            if (_released)
            {
                return true;
            }

            if (!TryReleaseParts(source, reason, out issue))
            {
                return false;
            }

            _released = true;
            return true;
        }

        internal bool TryRollback(
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (_completed)
            {
                issue = "Committed Activity Player admission stage ownership was already completed.";
                return false;
            }

            if (_rolledBack)
            {
                return true;
            }

            if (!TryReleaseParts(source, reason, out issue))
            {
                return false;
            }

            _rolledBack = true;
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
                if (!_resolver.TryRollback(
                        _resolution,
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
                if (!_scopeRuntime.TryRelease(
                        _scope,
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
