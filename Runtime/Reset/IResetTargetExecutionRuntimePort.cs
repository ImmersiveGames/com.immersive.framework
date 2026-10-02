using System.Threading.Tasks;

namespace Immersive.Framework.Reset
{
    internal interface IResetTargetExecutionRuntimePort
    {
        Task<ResetSelectionExecutionRuntimeResult> ExecuteResetTargetAsync(
            ResetTarget target,
            string source,
            string reason);

    }
}
