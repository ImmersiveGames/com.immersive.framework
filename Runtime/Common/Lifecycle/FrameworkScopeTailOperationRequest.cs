using System;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Common
{
    internal readonly struct FrameworkScopeTailOperationRequest
    {
        public FrameworkScopeTailOperationRequest(
            RuntimeContentOwner currentOwner,
            RuntimeContentOwner previousOwner,
            RuntimeRootRegistryOperationResult enterRootResult,
            RuntimeScopeContext context,
            int rootCount,
            string source,
            string reason,
            Func<int> rootCountProvider = null)
        {
            bool hasCurrentOwner = currentOwner.IsValid;
            bool hasPreviousOwner = previousOwner.IsValid;

            if (!hasCurrentOwner && !hasPreviousOwner)
            {
                throw new ArgumentException("Scope tail operation requires a current or previous owner.", nameof(currentOwner));
            }

            if (hasCurrentOwner)
            {
                if (enterRootResult == null)
                {
                    throw new ArgumentNullException(nameof(enterRootResult));
                }

                if (!enterRootResult.Owner.Equals(currentOwner))
                {
                    throw new ArgumentException("Scope tail operation enter result must belong to the current owner.", nameof(enterRootResult));
                }

                if (!context.IsValid)
                {
                    throw new ArgumentException("Scope tail operation context must be valid.", nameof(context));
                }

                if (!context.Owner.Equals(currentOwner))
                {
                    throw new ArgumentException("Scope tail operation context must belong to the current owner.", nameof(context));
                }
            }
            else
            {
                if (enterRootResult != null)
                {
                    throw new ArgumentException("Scope tail operation enter result must be absent when no current owner is available.", nameof(enterRootResult));
                }

                if (context.IsValid)
                {
                    throw new ArgumentException("Scope tail operation context must be empty when no current owner is available.", nameof(context));
                }
            }

            if (rootCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rootCount), rootCount, "Scope tail operation root count cannot be negative.");
            }

            CurrentOwner = currentOwner;
            PreviousOwner = previousOwner;
            EnterRootResult = enterRootResult;
            Context = context;
            _rootCount = rootCount;
            _rootCountProvider = rootCountProvider;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        private readonly int _rootCount;

        private readonly Func<int> _rootCountProvider;

        public RuntimeContentOwner CurrentOwner { get; }

        public RuntimeContentOwner PreviousOwner { get; }

        public RuntimeRootRegistryOperationResult EnterRootResult { get; }

        public RuntimeScopeContext Context { get; }

        public int RootCount => _rootCountProvider != null ? _rootCountProvider() : _rootCount;

        public bool HasCurrentOwner => CurrentOwner.IsValid;

        public string Source { get; }

        public string Reason { get; }

        public RuntimeContentScope Scope => HasCurrentOwner ? CurrentOwner.Scope : PreviousOwner.Scope;

        public bool HasPreviousOwner => PreviousOwner.IsValid;

        public bool HasDistinctPreviousOwner => HasPreviousOwner && (!HasCurrentOwner || !PreviousOwner.Equals(CurrentOwner));

        public string ToDiagnosticString()
        {
            string currentOwnerText = HasCurrentOwner ? CurrentOwner.StableText : "<none>";
            string previousOwnerText = HasPreviousOwner ? PreviousOwner.StableText : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();

            return $"scope='{Scope}' currentOwner='{currentOwnerText}' previousOwner='{previousOwnerText}' rootCount='{RootCount}' source='{sourceText}' reason='{reasonText}'";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
