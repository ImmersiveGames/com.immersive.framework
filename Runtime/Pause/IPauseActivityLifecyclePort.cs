namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. Synchronous, mandatory precondition for Activity exit (IF-ADR-005
    /// Pause lifecycle cleanup). PrepareForActivityExit is not an observational lifecycle event:
    /// Activity/Route lifecycle must not commit the Activity exit unless this precondition
    /// succeeds. A successful call guarantees PauseRuntime is Running, with TimeScale, the
    /// physical Gate/Input posture and the currently registered Pause presentation fully
    /// restored through the canonical Pause pipeline. A failed call means the caller must not
    /// commit the Activity transition; no partial or best-effort cleanup is left in place.
    /// </summary>
    internal interface IPauseActivityLifecyclePort
    {
        bool PrepareForActivityExit(string source, string reason, out string diagnostic);
    }
}
