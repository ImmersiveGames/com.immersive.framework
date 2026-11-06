using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using System.Threading;
using System.Threading.Tasks;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>Permission to open contributions for one exact committed Object Entry occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C1 exact-occurrence composition access grant.")]
    public sealed class ObjectEntryCompositionAccessGrant
    {
        private readonly StableObjectBindingRegistry _registry;
        private readonly StableObjectBinding _binding;
        private readonly ObjectEntryCompositionRuntimeSlot _contextSlot;
        private readonly ObjectEntryCompositionRuntimeBinding _contextAtIssue;

        internal ObjectEntryCompositionAccessGrant(
            StableObjectBindingRegistry registry,
            StableObjectBinding binding,
            ObjectEntryCompositionRuntimeSlot contextSlot,
            ObjectEntryCompositionRuntimeBinding contextAtIssue)
        {
            _registry = registry;
            _binding = binding;
            _contextSlot = contextSlot;
            _contextAtIssue = contextAtIssue;
        }

        /// <summary>Opens an independent empty contribution on the exact occurrence captured by this grant.</summary>
        /// <remarks>Call on Unity's main thread. C1 does not expose dynamic part mutation or materialization.</remarks>
        public bool TryOpenContribution(
            int priority,
            out ObjectEntryCompositionContribution contribution,
            out string issue)
        {
            return _registry.TryOpenDynamicContribution(this, priority, out contribution, out issue);
        }

        internal bool TryValidate(out string issue) =>
            _registry.ValidateCompositionAccessGrant(this, out issue);

        internal bool IsBoundTo(
            StableObjectBindingRegistry registry,
            StableObjectBinding binding,
            ObjectEntryCompositionRuntimeSlot contextSlot)
        {
            return ReferenceEquals(_registry, registry) && ReferenceEquals(_binding, binding) &&
                ReferenceEquals(_contextSlot, contextSlot);
        }

        internal bool TryGetBinding(
            out StableObjectBindingRegistry registry,
            out StableObjectBinding binding,
            out ObjectEntryCompositionRuntimeSlot contextSlot)
        {
            registry = _registry;
            binding = _binding;
            contextSlot = _contextSlot;
            return registry != null && binding != null && contextSlot != null;
        }

        internal ObjectEntryCompositionRuntimeBinding ContextAtIssue => _contextAtIssue;

        internal void Invalidate() => _contextSlot?.Invalidate();

        internal Task<ObjectEntryCompositionContributionCloseResult> CloseContributionAsync(
            ObjectEntryCompositionContribution contribution,
            CancellationToken cancellationToken)
        {
            return _registry.CloseDynamicContributionAsync(this, contribution, cancellationToken);
        }
    }
}
