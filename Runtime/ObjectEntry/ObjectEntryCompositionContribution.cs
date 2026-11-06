using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>One independent consumer intent opened against an exact Object Entry occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 Framework-owned contribution handle.")]
    public sealed class ObjectEntryCompositionContribution
    {
        private readonly ObjectEntryCompositionAccessGrant _grant;
        private readonly ObjectEntryCompositionRuntimeBinding _runtime;
        private readonly Immersive.Composition.CompositionContribution _composerContribution;

        internal ObjectEntryCompositionContribution(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionRuntimeBinding runtime,
            Immersive.Composition.CompositionContribution composerContribution,
            int priority)
        {
            _grant = grant;
            _runtime = runtime;
            _composerContribution = composerContribution;
            Priority = priority;
        }

        public int Priority { get; }

        /// <summary>Adds or replaces one part owned by this contribution.</summary>
        /// <remarks>Call on Unity's main thread. The exact committed Object Entry occurrence remains the mutation target.</remarks>
        public Task<ObjectEntryCompositionOperationResult> SetPartAsync(
            ObjectEntryCompositionPartDefinition part,
            CancellationToken cancellationToken = default)
        {
            return _grant.SetContributionPartAsync(this, part, cancellationToken);
        }

        /// <summary>Removes this contribution's intent for one part key.</summary>
        /// <remarks>Call on Unity's main thread. Composition recalculates the effective part and dependent graph.</remarks>
        public Task<ObjectEntryCompositionOperationResult> RemovePartAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            return _grant.RemoveContributionPartAsync(this, key, cancellationToken);
        }

        /// <summary>Closes this consumer's contribution intent; Framework owner release remains responsible for physical cleanup.</summary>
        /// <remarks>Call on Unity's main thread. Closing this handle does not close the Composer or its owner.</remarks>
        public Task<ObjectEntryCompositionOperationResult> CloseAsync(
            CancellationToken cancellationToken = default)
        {
            return _grant.CloseContributionAsync(this, cancellationToken);
        }

        internal bool IsFrom(ObjectEntryCompositionAccessGrant grant) => ReferenceEquals(_grant, grant);

        internal bool IsFromRuntime(ObjectEntryCompositionRuntimeBinding runtime) => ReferenceEquals(_runtime, runtime);

        internal ObjectEntryCompositionRuntimeBinding Runtime => _runtime;

        internal Immersive.Composition.CompositionContribution ComposerContribution => _composerContribution;
    }
}
