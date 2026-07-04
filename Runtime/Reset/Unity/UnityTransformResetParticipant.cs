using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Synchronous Transform reset participant that captures and restores a local transform baseline.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Unity Transform Reset Participant")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12B Unity Transform reset participant using ResetSubject, not ObjectEntryDeclaration.")]
    public sealed class UnityTransformResetParticipant : UnityResetParticipantBehaviour
    {
        [Header("Transform Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool captureBaselineOnEnable = true;
        [SerializeField] private bool resetPosition = true;
        [SerializeField] private bool resetRotation = true;
        [SerializeField] private bool resetScale = true;

        [Header("Baseline")]
        [SerializeField] private Vector3 baselineLocalPosition;
        [SerializeField] private Vector3 baselineLocalEulerAngles;
        [SerializeField] private Vector3 baselineLocalScale = Vector3.one;

        #if UNITY_EDITOR
        private void Reset()
        {
            ConfigureForQa(
                "transform",
                ResetParticipantRequiredness.Required,
                0,
                "Transform",
                nameof(UnityTransformResetParticipant),
                "unity-transform-reset");
            target = transform;
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
            Transform resolvedTarget = ResolveTarget();
            if (resolvedTarget == null)
            {
                return;
            }

            baselineLocalPosition = resolvedTarget.localPosition;
            baselineLocalEulerAngles = resolvedTarget.localEulerAngles;
            baselineLocalScale = resolvedTarget.localScale;
        }

        public override ResetParticipantResult Reset(ResetContext context)
        {
            Transform resolvedTarget = ResolveTarget();
            if (resolvedTarget == null)
            {
                return ResetParticipantResult.CreateFailed(
                    CreateDescriptorForResult(context),
                    1,
                    nameof(UnityTransformResetParticipant),
                    context.Reason,
                    "Unity Transform reset failed because the target Transform is missing.");
            }

            if (resetPosition)
            {
                resolvedTarget.localPosition = baselineLocalPosition;
            }

            if (resetRotation)
            {
                resolvedTarget.localEulerAngles = baselineLocalEulerAngles;
            }

            if (resetScale)
            {
                resolvedTarget.localScale = baselineLocalScale;
            }

            return ResetParticipantResult.CreateSucceeded(
                CreateDescriptorForResult(context),
                nameof(UnityTransformResetParticipant),
                context.Reason,
                "Unity Transform reset completed.");
        }

        private Transform ResolveTarget()
        {
            return target != null ? target : transform;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureTransformForQa(
            Transform qaTarget,
            bool qaCaptureOnEnable,
            bool qaResetPosition,
            bool qaResetRotation,
            bool qaResetScale)
        {
            target = qaTarget;
            captureBaselineOnEnable = qaCaptureOnEnable;
            resetPosition = qaResetPosition;
            resetRotation = qaResetRotation;
            resetScale = qaResetScale;
            CaptureBaseline();
        }
#endif
    }
}
