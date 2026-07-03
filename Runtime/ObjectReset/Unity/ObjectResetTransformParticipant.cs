using Immersive.Framework.ApiStatus;
using UnityEngine;
namespace Immersive.Framework.ObjectReset.Unity
{
    /// <summary>
    /// API status: Experimental. Unity Object Reset participant that restores an authored local Transform baseline.
    /// Identity comes from ObjectEntryDeclaration through ObjectResetUnityParticipantBehaviour; Transform is reset as local state only.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/Transform Reset Participant")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F15C Unity Transform reset participant with authored local baseline.")]
    public sealed class ObjectResetTransformParticipant : ObjectResetUnityParticipantBehaviour
    {
        [Header("Transform Target")]
        [SerializeField] private Transform targetTransformOverride;

        [Header("Authored Local Baseline")]
        [SerializeField] private bool baselineConfigured;
        [SerializeField] private bool resetLocalPosition = true;
        [SerializeField] private bool resetLocalRotation = true;
        [SerializeField] private bool resetLocalScale = true;
        [SerializeField] private Vector3 baselineLocalPosition;
        [SerializeField] private Vector3 baselineLocalEulerAngles;
        [SerializeField] private Vector3 baselineLocalScale = Vector3.one;

        public Transform TargetTransform => ResolveTargetTransform();

        public bool HasBaseline => baselineConfigured;

        public bool ResetLocalPosition => resetLocalPosition;

        public bool ResetLocalRotation => resetLocalRotation;

        public bool ResetLocalScale => resetLocalScale;

        public Vector3 BaselineLocalPosition => baselineLocalPosition;

        public Vector3 BaselineLocalEulerAngles => baselineLocalEulerAngles;

        public Vector3 BaselineLocalScale => baselineLocalScale;

        public override ObjectResetParticipantResult ResetObject(ObjectResetContext context)
        {
            var targetTransform = ResolveTargetTransform();
            if (targetTransform == null)
            {
                return ObjectResetParticipantResult.Failure(
                    context,
                    1,
                    context.Participant.Source,
                    context.Participant.Reason,
                    "Transform Reset participant could not resolve a Transform target.");
            }

            if (!baselineConfigured)
            {
                return ObjectResetParticipantResult.Failure(
                    context,
                    1,
                    context.Participant.Source,
                    context.Participant.Reason,
                    "Transform Reset participant requires an authored local baseline.");
            }

            if (resetLocalPosition)
            {
                targetTransform.localPosition = baselineLocalPosition;
            }

            if (resetLocalRotation)
            {
                targetTransform.localRotation = Quaternion.Euler(baselineLocalEulerAngles);
            }

            if (resetLocalScale)
            {
                targetTransform.localScale = baselineLocalScale;
            }

            return ObjectResetParticipantResult.Success(
                context,
                context.Participant.Source,
                context.Participant.Reason,
                "Transform Reset participant restored authored local Transform baseline.");
        }

        [ContextMenu("Capture Current Transform Baseline")]
        private void CaptureCurrentTransformBaseline()
        {
            var targetTransform = ResolveTargetTransform();
            if (targetTransform == null)
            {
                return;
            }

            baselineLocalPosition = targetTransform.localPosition;
            baselineLocalEulerAngles = targetTransform.localEulerAngles;
            baselineLocalScale = targetTransform.localScale;
            baselineConfigured = true;
        }

        private Transform ResolveTargetTransform()
        {
            return targetTransformOverride != null ? targetTransformOverride : transform;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureTransformBaselineForQa(
            Transform qaTargetTransform,
            bool qaBaselineConfigured,
            bool qaResetLocalPosition,
            bool qaResetLocalRotation,
            bool qaResetLocalScale,
            Vector3 qaBaselineLocalPosition,
            Vector3 qaBaselineLocalEulerAngles,
            Vector3 qaBaselineLocalScale)
        {
            targetTransformOverride = qaTargetTransform;
            baselineConfigured = qaBaselineConfigured;
            resetLocalPosition = qaResetLocalPosition;
            resetLocalRotation = qaResetLocalRotation;
            resetLocalScale = qaResetLocalScale;
            baselineLocalPosition = qaBaselineLocalPosition;
            baselineLocalEulerAngles = qaBaselineLocalEulerAngles;
            baselineLocalScale = qaBaselineLocalScale;
        }
#endif
    }
}
