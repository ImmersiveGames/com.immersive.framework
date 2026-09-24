using System.Threading.Tasks;

namespace Immersive.Framework.Reset
{
    internal interface IResetSelectionExecutionRuntimePort
    {
        Task<ResetSelectionExecutionRuntimeResult> ExecuteResetSelectionAsync(
            ResetSelectionConfig selection,
            string source,
            string reason);
    }
}
