using System.Threading.Tasks;

namespace Immersive.Framework.CycleReset
{
    internal interface IRouteCycleResetRuntimePort
    {
        Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason);
    }
}
