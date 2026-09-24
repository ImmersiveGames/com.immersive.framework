namespace Immersive.Framework.Pause
{
    internal interface IPauseRuntimePort
    {
        bool TryGetPauseSnapshot(out PauseSnapshot snapshot);

        PauseResult RequestPause(PauseRequest request);
    }
}
