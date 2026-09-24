using System;
using Immersive.Framework.ActivityFlow;
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
            _activityParticipantSourceBindings.SetSources(
                participantSource,
                participantSource);
            _gameFlowRuntime?.SetActivityContentExecutionParticipantSource(
                participantSource);
            _gameFlowRuntime?.SetActivityReadinessParticipantSource(
                participantSource);
        }
    }

    internal sealed class ActivityParticipantSourceBindings
    {
        internal IActivityContentExecutionParticipantSource ContentSource { get; private set; }

        internal IActivityReadinessParticipantSource ReadinessSource { get; private set; }

        internal void SetContentSource(
            IActivityContentExecutionParticipantSource source)
        {
            ContentSource = source;
        }

        internal void SetSources(
            IActivityContentExecutionParticipantSource contentSource,
            IActivityReadinessParticipantSource readinessSource)
        {
            ContentSource = contentSource;
            ReadinessSource = readinessSource;
        }

        internal void ApplyTo(
            Action<IActivityContentExecutionParticipantSource> applyContent,
            Action<IActivityReadinessParticipantSource> applyReadiness)
        {
            if (applyContent == null)
            {
                throw new ArgumentNullException(nameof(applyContent));
            }

            if (applyReadiness == null)
            {
                throw new ArgumentNullException(nameof(applyReadiness));
            }

            applyContent(ContentSource);
            applyReadiness(ReadinessSource);
        }
    }
}
