using System.Threading.Tasks;
using Immersive.Framework.Reset;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IResetExecutionRuntimePort
    {
        async Task<ResetExecutionResult> IResetExecutionRuntimePort.ExecuteResetAsync(
            ResetExecutionRequest request)
        {
            var executor = new ResetExecutor(ResetRegistry);
            return await executor.ExecuteAsync(request);
        }
    }
}
