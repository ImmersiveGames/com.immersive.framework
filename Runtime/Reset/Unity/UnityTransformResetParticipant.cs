using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
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
        private const float PositionTolerance = 0.0001f;
        private const float RotationToleranceDegrees = 0.01f;
        private const float ScaleTolerance = 0.0001f;
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

            Vector3 beforePosition = resolvedTarget.localPosition;
            Vector3 beforeEulerAngles = resolvedTarget.localEulerAngles;
            Vector3 beforeScale = resolvedTarget.localScale;
            CharacterController controller = resolvedTarget.GetComponent<CharacterController>();
            bool controllerOriginallyEnabled = controller != null && controller.enabled;
            if (controllerOriginallyEnabled)
            {
                controller.enabled = false;
            }

            try
            {
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
            }
            finally
            {
                if (controller != null)
                {
                    controller.enabled = controllerOriginallyEnabled;
                }
            }

            bool positionApplied = !resetPosition || Approximately(resolvedTarget.localPosition, baselineLocalPosition, PositionTolerance);
            bool rotationApplied = !resetRotation || Quaternion.Angle(Quaternion.Euler(resolvedTarget.localEulerAngles), Quaternion.Euler(baselineLocalEulerAngles)) <= RotationToleranceDegrees;
            bool scaleApplied = !resetScale || Approximately(resolvedTarget.localScale, baselineLocalScale, ScaleTolerance);
            bool verified = positionApplied && rotationApplied && scaleApplied;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FrameworkLogger.Create<UnityTransformResetParticipant>().Debug(
                "Unity Transform reset immediate verification.",
                LogFields.Field("participantId", ParticipantIdText),
                LogFields.Field("targetName", resolvedTarget.name),
                LogFields.Field("baselineLocalPosition", baselineLocalPosition.ToString()),
                LogFields.Field("beforeLocalPosition", beforePosition.ToString()),
                LogFields.Field("immediateAfterLocalPosition", resolvedTarget.localPosition.ToString()),
                LogFields.Field("characterControllerPresent", controller != null),
                LogFields.Field("characterControllerOriginallyEnabled", controllerOriginallyEnabled),
                LogFields.Field("positionApplied", positionApplied),
                LogFields.Field("rotationApplied", rotationApplied),
                LogFields.Field("scaleApplied", scaleApplied),
                LogFields.Field("verificationSucceeded", verified));
#endif

            if (!verified)
            {
                return ResetParticipantResult.CreateFailed(
                    CreateDescriptorForResult(context),
                    1,
                    nameof(UnityTransformResetParticipant),
                    context.Reason,
                    $"Unity Transform reset immediate verification failed. target='{resolvedTarget.name}' beforePosition='{beforePosition}' afterPosition='{resolvedTarget.localPosition}' beforeEuler='{beforeEulerAngles}' afterEuler='{resolvedTarget.localEulerAngles}' beforeScale='{beforeScale}' afterScale='{resolvedTarget.localScale}'.");
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

        private static bool Approximately(Vector3 left, Vector3 right, float tolerance)
        {
            return (left - right).sqrMagnitude <= tolerance * tolerance;
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
