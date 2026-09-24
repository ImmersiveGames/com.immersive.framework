using System.Threading.Tasks;
namespace Immersive.Framework.CycleReset { internal interface IActivityCycleResetRuntimePort { Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason); } }
