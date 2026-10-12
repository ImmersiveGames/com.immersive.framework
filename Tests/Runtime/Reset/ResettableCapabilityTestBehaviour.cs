using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    public sealed class ResettableCapabilityTestBehaviour : MonoBehaviour, IUnityResettable, IUnityResettableMetadata
    {
        public int ResetCount { get; private set; }

        public ResetParticipantRequiredness ResetRequiredness => ResetParticipantRequiredness.Optional;

        public int ResetOrder => 17;

        public string ResetDisplayName => "Custom capability";

        public ResetParticipantResult Reset(ResetContext context)
        {
            ResetCount++;
            return ResetParticipantResult.CreateSucceeded(
                context.Participant,
                nameof(ResettableCapabilityTestBehaviour),
                context.Reason,
                "Custom Resettable capability executed.");
        }
    }
}
