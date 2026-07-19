using System.Threading.Tasks;

namespace Immersive.Framework.Reset
{
    internal interface IResetExecutionRuntimePort
    {
        Task<ResetExecutionResult> ExecuteResetAsync(
            ResetExecutionRequest request);
    }
}
