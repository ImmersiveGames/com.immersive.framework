using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Synchronous GameObject active-state reset participant.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Unity GameObject Active Reset Participant")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12B Unity GameObject active reset participant using ResetSubject, not ObjectEntryDeclaration.")]
    public sealed class UnityGameObjectActiveResetParticipant : UnityResetParticipantBehaviour
    {
        [Header("GameObject Target")]
        [SerializeField] private GameObject target;
        [SerializeField] private bool captureBaselineOnEnable = true;
        [SerializeField] private bool baselineActive = true;

        #if UNITY_EDITOR
        private void Reset()
        {
            ConfigureForQa(
                "game-object-active",
                ResetParticipantRequiredness.Required,
                10,
                "GameObject Active",
                nameof(UnityGameObjectActiveResetParticipant),
                "unity-game-object-active-reset");
            target = gameObject;
            CaptureBaseline();
        }
        #endif

        private void OnEnable()
        {
            if (captureBaselineOnEnable)
            {
                CaptureBaseline();
            }
        }

        public void CaptureBaseline()
        {
            GameObject resolvedTarget = ResolveTarget();
            if (resolvedTarget != null)
            {
                baselineActive = resolvedTarget.activeSelf;
            }
        }

        public override ResetParticipantResult Reset(ResetContext context)
        {
            GameObject resolvedTarget = ResolveTarget();
            if (resolvedTarget == null)
            {
                return ResetParticipantResult.CreateFailed(
                    CreateDescriptorForResult(context),
                    1,
                    nameof(UnityGameObjectActiveResetParticipant),
                    context.Reason,
                    "Unity GameObject active reset failed because the target GameObject is missing.");
            }

            resolvedTarget.SetActive(baselineActive);
            return ResetParticipantResult.CreateSucceeded(
                CreateDescriptorForResult(context),
                nameof(UnityGameObjectActiveResetParticipant),
                context.Reason,
                "Unity GameObject active reset completed.");
        }

        private GameObject ResolveTarget()
        {
            return target != null ? target : gameObject;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureActiveStateForQa(GameObject qaTarget, bool qaCaptureOnEnable, bool qaBaselineActive)
        {
            target = qaTarget;
            captureBaselineOnEnable = qaCaptureOnEnable;
            baselineActive = qaBaselineActive;
        }
#endif
    }
}
