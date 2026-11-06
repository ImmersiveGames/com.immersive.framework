using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ObjectEntry
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C1 contribution close outcome.")]
    public enum ObjectEntryCompositionContributionCloseStatus
    {
        Closed = 10,
        NoChange = 20,
        Stale = 30,
        Cancelled = 40,
        Faulted = 50
    }

    /// <summary>Framework-owned result for closing one consumer contribution's intent.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C1 consumer contribution close result.")]
    public sealed class ObjectEntryCompositionContributionCloseResult
    {
        internal ObjectEntryCompositionContributionCloseResult(
            ObjectEntryCompositionContributionCloseStatus status,
            string issue)
        {
            Status = status;
            Issue = issue ?? string.Empty;
        }

        public ObjectEntryCompositionContributionCloseStatus Status { get; }

        public string Issue { get; }

        public bool IsSuccessful => Status is ObjectEntryCompositionContributionCloseStatus.Closed or
            ObjectEntryCompositionContributionCloseStatus.NoChange;
    }

    /// <summary>One independent consumer intent opened against an exact Object Entry occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C1 Framework-owned contribution handle.")]
    public sealed class ObjectEntryCompositionContribution
    {
        private readonly ObjectEntryCompositionAccessGrant _grant;
        private readonly ObjectEntryCompositionRuntimeBinding _runtime;
        private readonly CompositionContribution _composerContribution;

        internal ObjectEntryCompositionContribution(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionRuntimeBinding runtime,
            CompositionContribution composerContribution,
            int priority)
        {
            _grant = grant;
            _runtime = runtime;
            _composerContribution = composerContribution;
            Priority = priority;
        }

        public int Priority { get; }

        /// <summary>Closes this consumer's contribution intent; Framework owner release remains responsible for physical cleanup.</summary>
        /// <remarks>Call on Unity's main thread. C1 contributions are empty and do not materialize parts.</remarks>
        public Task<ObjectEntryCompositionContributionCloseResult> CloseAsync(
            CancellationToken cancellationToken = default)
        {
            return _grant.CloseContributionAsync(this, cancellationToken);
        }

        internal bool IsFrom(ObjectEntryCompositionAccessGrant grant) => ReferenceEquals(_grant, grant);

        internal bool IsFromRuntime(ObjectEntryCompositionRuntimeBinding runtime) => ReferenceEquals(_runtime, runtime);

        internal CompositionContribution ComposerContribution => _composerContribution;
    }
}
