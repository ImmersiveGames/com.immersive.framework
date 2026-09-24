using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal diagnostic snapshot for lifecycle-driven runtime root/context changes.
    /// It records logical root enter/exit operations only; it does not materialize, destroy or release objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8F lifecycle integration result for runtime scope roots; diagnostics only, no materialization or release execution.")]
    internal readonly struct RuntimeScopeLifecycleResult
    {
        public RuntimeScopeLifecycleResult(
            RuntimeContentScope scope,
            RuntimeContentOwner owner,
            RuntimeRootRegistryOperationResult enterRootResult,
            RuntimeRootRegistryOperationResult exitRootResult,
            RuntimeScopeContext context,
            int rootCount,
            string source,
            string reason)
        {
            Scope = scope;
            Owner = owner;
            EnterRootResult = enterRootResult;
            ExitRootResult = exitRootResult;
            Context = context;
            RootCount = rootCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeContentScope Scope { get; }

        public RuntimeContentOwner Owner { get; }

        public RuntimeRootRegistryOperationResult EnterRootResult { get; }

        public RuntimeRootRegistryOperationResult ExitRootResult { get; }

        public RuntimeScopeContext Context { get; }

        public int RootCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool HasOwner => Owner.IsValid;

        public bool HasEnterRootResult => EnterRootResult != null;

        public bool HasExitRootResult => ExitRootResult != null;

        public bool HasContext => Context.IsValid;

        public bool Executed => HasEnterRootResult || HasExitRootResult || HasContext;

        public bool Rejected => EnterRootResult is { Rejected: true }
            || ExitRootResult is { Rejected: true };

        public bool Applied => EnterRootResult is { Applied: true }
            || ExitRootResult is { Applied: true };

        public string DiagnosticStatus
        {
            get
            {
                if (Rejected)
                {
                    return "Rejected";
                }

                if (Applied)
                {
                    return "Applied";
                }

                if (Executed)
                {
                    return "Observed";
                }

                return "NotExecuted";
            }
        }

        public string EnterStatus => EnterRootResult != null ? EnterRootResult.Status.ToString() : "None";

        public string ExitStatus => ExitRootResult != null ? ExitRootResult.Status.ToString() : "None";

        public string ContextStatus => HasContext ? "Available" : "None";

        public string OwnerText => HasOwner ? Owner.StableText : string.Empty;

        public string ToDiagnosticString()
        {
            string ownerText = HasOwner ? Owner.StableText : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();

            return $"scope='{Scope}' owner='{ownerText}' status='{DiagnosticStatus}' enterRoot='{EnterStatus}' exitRoot='{ExitStatus}' context='{ContextStatus}' rootCount='{RootCount}' source='{sourceText}' reason='{reasonText}'";
        }

        public static RuntimeScopeLifecycleResult None(RuntimeContentScope scope, string source, string reason)
        {
            return new RuntimeScopeLifecycleResult(
                scope,
                default(RuntimeContentOwner),
                null,
                null,
                default(RuntimeScopeContext),
                0,
                source,
                reason);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
