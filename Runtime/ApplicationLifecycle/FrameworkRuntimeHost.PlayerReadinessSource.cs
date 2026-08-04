using Immersive.Framework.PlayerParticipation;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        // This exact overload is selected by the existing Player preparation module.
        // It preserves the content-execution registration and additionally supplies
        // the same scoped lifecycle participant as a readiness participant source.
        internal void SetActivityContentExecutionParticipantSource(
            ActivityPlayerActorLifecycleParticipant participantSource)
        {
            _gameFlowRuntime?.SetActivityContentExecutionParticipantSource(
                participantSource);
            _gameFlowRuntime?.SetActivityReadinessParticipantSource(
                participantSource);
        }
    }
}
