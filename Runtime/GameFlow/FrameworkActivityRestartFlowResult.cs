using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// API status: Internal. Composite result for an Activity Restart flow executed under one Activity transition.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Composite Activity Restart flow result used by authored restart triggers.")]
    internal readonly struct FrameworkActivityRestartFlowResult
    {
        public FrameworkActivityRestartFlowResult(
            FrameworkActivityRequestResult clearResult,
            FrameworkActivityRequestResult reenterResult,
            string message)
        {
            ClearResult = clearResult;
            ReenterResult = reenterResult;
            Message = message.NormalizeText();
        }

        internal FrameworkActivityRequestResult ClearResult { get; }

        internal FrameworkActivityRequestResult ReenterResult { get; }

        internal string Message { get; }

        internal bool ClearSucceeded => ClearResult.Succeeded;

        internal bool ReenterSucceeded => ReenterResult.Succeeded;

        internal bool Succeeded => ClearSucceeded && ReenterSucceeded;

        internal static FrameworkActivityRestartFlowResult FailedClear(
            FrameworkActivityRequestResult clearResult,
            FrameworkActivityRequestResult reenterResult,
            string message)
        {
            return new FrameworkActivityRestartFlowResult(clearResult, reenterResult, message);
        }

        internal static FrameworkActivityRestartFlowResult FailedReenter(
            FrameworkActivityRequestResult clearResult,
            FrameworkActivityRequestResult reenterResult,
            string message)
        {
            return new FrameworkActivityRestartFlowResult(clearResult, reenterResult, message);
        }

        internal static FrameworkActivityRestartFlowResult Completed(
            FrameworkActivityRequestResult clearResult,
            FrameworkActivityRequestResult reenterResult,
            string message)
        {
            return new FrameworkActivityRestartFlowResult(clearResult, reenterResult, message);
        }
    }
}
