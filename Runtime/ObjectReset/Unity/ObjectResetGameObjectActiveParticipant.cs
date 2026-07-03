using Immersive.Framework.ApiStatus;
using UnityEngine;
namespace Immersive.Framework.ObjectReset.Unity
{
    /// <summary>
    /// API status: Experimental. Unity Object Reset participant that restores an authored GameObject activeSelf baseline.
    /// This adapter is intentionally primitive: it changes only activeSelf and does not reset children, components, physics, animation or gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/GameObject Active Reset Participant")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F16 GameObject activeSelf reset participant with authored baseline.")]
    public sealed class ObjectResetGameObjectActiveParticipant : ObjectResetUnityParticipantBehaviour
    {
        [Header("GameObject Target")]
        [SerializeField] private GameObject targetGameObjectOverride;

        [Header("Authored Active State Baseline")]
        [SerializeField] private bool baselineConfigured;
        [SerializeField] private bool baselineActiveSelf = true;

        public GameObject TargetGameObject => ResolveTargetGameObject();

        public bool HasBaseline => baselineConfigured;

        public bool BaselineActiveSelf => baselineActiveSelf;

        public override ObjectResetParticipantResult ResetObject(ObjectResetContext context)
        {
            var targetGameObject = ResolveTargetGameObject();
            if (targetGameObject == null)
            {
                return ObjectResetParticipantResult.Failure(
                    context,
                    1,
                    context.Participant.Source,
                    context.Participant.Reason,
                    "GameObject Active Reset participant could not resolve a GameObject target.");
            }

            if (!baselineConfigured)
            {
                return ObjectResetParticipantResult.Failure(
                    context,
                    1,
                    context.Participant.Source,
                    context.Participant.Reason,
                    "GameObject Active Reset participant requires an authored activeSelf baseline.");
            }

            targetGameObject.SetActive(baselineActiveSelf);
            return ObjectResetParticipantResult.Success(
                context,
                context.Participant.Source,
                context.Participant.Reason,
                "GameObject Active Reset participant restored authored activeSelf baseline.");
        }

        [ContextMenu("Capture Current Active State Baseline")]
        private void CaptureCurrentActiveStateBaseline()
        {
            var targetGameObject = ResolveTargetGameObject();
            if (targetGameObject == null)
            {
                return;
            }

            baselineActiveSelf = targetGameObject.activeSelf;
            baselineConfigured = true;
        }

        private GameObject ResolveTargetGameObject()
        {
            return targetGameObjectOverride != null ? targetGameObjectOverride : gameObject;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureActiveBaselineForQa(
            GameObject qaTargetGameObject,
            bool qaBaselineConfigured,
            bool qaBaselineActiveSelf)
        {
            targetGameObjectOverride = qaTargetGameObject;
            baselineConfigured = qaBaselineConfigured;
            baselineActiveSelf = qaBaselineActiveSelf;
        }
#endif
    }
}
