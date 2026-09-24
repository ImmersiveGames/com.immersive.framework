using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Canonical phase for framework-core Activity Content Execution.
    /// This is a logical lifecycle phase only; it does not imply physical materialization, placement or gameplay behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10B Activity Content Execution contract phase; logical enter/exit only.")]
    public enum ActivityContentExecutionPhase
    {
        Unknown = 0,
        Enter = 10,
        Exit = 20
    }
}
