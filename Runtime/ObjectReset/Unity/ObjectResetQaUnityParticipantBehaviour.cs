#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Immersive.Framework.ApiStatus;
using UnityEngine;
namespace Immersive.Framework.ObjectReset.Unity
{
    /// <summary>
    /// Development-only participant used by QA smokes to validate the Unity participant source boundary without adding a physical reset adapter.
    /// </summary>
    [AddComponentMenu("")]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F15B QA-only Unity Object Reset participant; no physical reset side effects.")]
    internal sealed class ObjectResetQaUnityParticipantBehaviour : ObjectResetUnityParticipantBehaviour
    {
        [SerializeField] private ObjectResetParticipantResultStatus resultStatus = ObjectResetParticipantResultStatus.Succeeded;

        public override ObjectResetParticipantResult ResetObject(ObjectResetContext context)
        {
            switch (resultStatus)
            {
                case ObjectResetParticipantResultStatus.Succeeded:
                    return ObjectResetParticipantResult.Success(
                        context,
                        context.Participant.Source,
                        context.Participant.Reason,
                        "QA Unity Object Reset participant succeeded.");
                case ObjectResetParticipantResultStatus.SkippedOptional:
                    return ObjectResetParticipantResult.Skipped(
                        context,
                        ObjectResetParticipantResultStatus.SkippedOptional,
                        context.Participant.Source,
                        context.Participant.Reason,
                        "QA Unity Object Reset participant skipped as optional.");
                case ObjectResetParticipantResultStatus.FailedBlocking:
                    return ObjectResetParticipantResult.BlockingFailure(
                        context,
                        1,
                        context.Participant.Source,
                        context.Participant.Reason,
                        "QA Unity Object Reset participant failed as required.");
                case ObjectResetParticipantResultStatus.FailedNonBlocking:
                    return ObjectResetParticipantResult.NonBlockingFailure(
                        context,
                        1,
                        context.Participant.Source,
                        context.Participant.Reason,
                        "QA Unity Object Reset participant failed without blocking.");
                default:
                    return ObjectResetParticipantResult.Failure(
                        context,
                        1,
                        context.Participant.Source,
                        context.Participant.Reason,
                        "QA Unity Object Reset participant failed.");
            }
        }

        internal void ConfigureResultForQa(ObjectResetParticipantResultStatus qaResultStatus)
        {
            resultStatus = qaResultStatus;
        }
    }
}
#endif
