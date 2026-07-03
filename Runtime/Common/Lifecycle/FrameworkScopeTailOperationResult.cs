using System;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Common
{
    internal readonly struct FrameworkScopeTailOperationResult
    {
        public FrameworkScopeTailOperationResult(
            RuntimeContentOwner currentOwner,
            RuntimeContentOwner previousOwner,
            RuntimeScopeLifecycleResult scopeResult,
            ContentAnchorBindingLifecycleResult bindingCleanupResult,
            RuntimeRootRegistryOperationResult previousScopeRootRemoveResult,
            bool bindingCleanupInvoked,
            bool bindingCleanupSkipped,
            bool previousScopeRootRemoveInvoked,
            bool previousScopeRootRemoveSkipped)
        {
            if (!currentOwner.IsValid)
            {
                throw new ArgumentException("Scope tail operation current owner must be valid.", nameof(currentOwner));
            }

            if (!scopeResult.HasOwner)
            {
                throw new ArgumentException("Scope tail operation scope result must have an owner.", nameof(scopeResult));
            }

            if (!scopeResult.Executed)
            {
                throw new ArgumentException("Scope tail operation scope result must be executed.", nameof(scopeResult));
            }

            if (bindingCleanupInvoked == bindingCleanupSkipped)
            {
                throw new ArgumentException("Scope tail operation binding cleanup state must be either invoked or skipped.", nameof(bindingCleanupInvoked));
            }

            if (previousScopeRootRemoveInvoked == previousScopeRootRemoveSkipped)
            {
                throw new ArgumentException("Scope tail operation previous scope root remove state must be either invoked or skipped.", nameof(previousScopeRootRemoveInvoked));
            }

            if (bindingCleanupInvoked != previousScopeRootRemoveInvoked)
            {
                throw new ArgumentException("Scope tail operation binding cleanup and previous scope root removal must use the same execution state.", nameof(previousScopeRootRemoveInvoked));
            }

            if (bindingCleanupInvoked && !bindingCleanupResult.Executed)
            {
                throw new ArgumentException("Scope tail operation binding cleanup result must be executed when cleanup is invoked.", nameof(bindingCleanupResult));
            }

            if (!bindingCleanupInvoked && bindingCleanupResult.Executed)
            {
                throw new ArgumentException("Scope tail operation binding cleanup result must be default when cleanup is skipped.", nameof(bindingCleanupResult));
            }

            if (previousScopeRootRemoveInvoked && previousScopeRootRemoveResult == null)
            {
                throw new ArgumentNullException(nameof(previousScopeRootRemoveResult));
            }

            if (!previousScopeRootRemoveInvoked && previousScopeRootRemoveResult != null)
            {
                throw new ArgumentException("Scope tail operation previous scope root remove result must be null when removal is skipped.", nameof(previousScopeRootRemoveResult));
            }

            CurrentOwner = currentOwner;
            PreviousOwner = previousOwner;
            ScopeResult = scopeResult;
            BindingCleanupResult = bindingCleanupResult;
            PreviousScopeRootRemoveResult = previousScopeRootRemoveResult;
            BindingCleanupInvoked = bindingCleanupInvoked;
            BindingCleanupSkipped = bindingCleanupSkipped;
            PreviousScopeRootRemoveInvoked = previousScopeRootRemoveInvoked;
            PreviousScopeRootRemoveSkipped = previousScopeRootRemoveSkipped;
        }

        public RuntimeContentOwner CurrentOwner { get; }

        public RuntimeContentOwner PreviousOwner { get; }

        public RuntimeScopeLifecycleResult ScopeResult { get; }

        public ContentAnchorBindingLifecycleResult BindingCleanupResult { get; }

        public RuntimeRootRegistryOperationResult PreviousScopeRootRemoveResult { get; }

        public bool BindingCleanupInvoked { get; }

        public bool BindingCleanupSkipped { get; }

        public bool PreviousScopeRootRemoveInvoked { get; }

        public bool PreviousScopeRootRemoveSkipped { get; }

        public bool HasPreviousScopeRootRemoveResult => PreviousScopeRootRemoveResult != null;

        public bool BindingCleanupSucceeded => BindingCleanupInvoked && BindingCleanupResult.Succeeded;

        public bool BindingCleanupRejected => BindingCleanupInvoked && BindingCleanupResult.Executed && !BindingCleanupResult.Succeeded;

        public bool PreviousScopeRootRemoveRejected => PreviousScopeRootRemoveResult != null && PreviousScopeRootRemoveResult.Rejected;

        public bool HasBlockingIssues => BindingCleanupRejected || PreviousScopeRootRemoveRejected || ScopeResult.Rejected;

        public RuntimeContentScope Scope => CurrentOwner.Scope;

        public bool HasPreviousOwner => PreviousOwner.IsValid;

        public string Source => ScopeResult.Source;

        public string Reason => ScopeResult.Reason;

        public string BindingCleanupStatus => BindingCleanupInvoked ? BindingCleanupResult.Status.ToString() : "None";

        public string PreviousScopeRootRemoveStatus => PreviousScopeRootRemoveResult != null ? PreviousScopeRootRemoveResult.Status.ToString() : "None";

        public string DiagnosticStatus
        {
            get
            {
                if (HasBlockingIssues)
                {
                    return "Rejected";
                }

                if (BindingCleanupInvoked || PreviousScopeRootRemoveInvoked || ScopeResult.Applied)
                {
                    return "Applied";
                }

                return "Observed";
            }
        }

        public string ToDiagnosticString()
        {
            string bindingCleanupText = BindingCleanupInvoked ? BindingCleanupResult.ToDiagnosticString() : "<none>";
            string removeText = HasPreviousScopeRootRemoveResult ? PreviousScopeRootRemoveResult.ToDiagnosticString() : "<none>";
            string currentOwnerText = CurrentOwner.StableText;
            string previousOwnerText = HasPreviousOwner ? PreviousOwner.StableText : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();

            return $"status='{DiagnosticStatus}' bindingCleanupInvoked='{BindingCleanupInvoked}' bindingCleanupSkipped='{BindingCleanupSkipped}' bindingCleanupStatus='{BindingCleanupStatus}' previousScopeRootRemoveInvoked='{PreviousScopeRootRemoveInvoked}' previousScopeRootRemoveSkipped='{PreviousScopeRootRemoveSkipped}' previousScopeRootRemoveStatus='{PreviousScopeRootRemoveStatus}' scope='{Scope}' currentOwner='{currentOwnerText}' previousOwner='{previousOwnerText}' scopeResult='{ScopeResult.DiagnosticStatus}' rootCount='{ScopeResult.RootCount}' source='{sourceText}' reason='{reasonText}' bindingCleanupResult='{bindingCleanupText}' previousScopeRootRemoveResult='{removeText}'";
        }
    }
}
