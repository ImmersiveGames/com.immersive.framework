namespace Immersive.Framework.Reset
{
    /// <summary>
    /// Internal semantic Reset target kinds introduced by IF-ADR-035.
    /// Object and Composition become resolvable when their typed authoring boundaries exist.
    /// </summary>
    internal enum ResetTargetKind
    {
        Unknown = 0,
        Object = 10,
        Composition = 20,
        CurrentActivity = 30,
        CurrentRoute = 40,
        StableReference = 50
    }
}
