using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputRigApplicator
    {
        private readonly CameraOutputBinding binding;

        private bool hasAppliedRequest;
        private bool hasAppliedDefault;
        private CameraRequestId appliedRequestId;
        private CinemachineCamera appliedCamera;

        public CameraOutputRigApplicator(CameraOutputBinding binding)
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException(
                    "CameraOutputRigApplicator requires a valid output binding.",
                    nameof(binding));
            }

            this.binding = binding;
        }

        public CameraOutputBinding Binding => binding;
        public bool HasAppliedRequest => hasAppliedRequest;
        public bool HasAppliedDefault => hasAppliedDefault;
        public CameraRequestId AppliedRequestId => appliedRequestId;
        public CinemachineCamera AppliedCamera => appliedCamera;

        public CameraOutputApplyResult Apply(
            CameraOutputContext context,
            CameraRigReference defaultRig,
            bool forceDefault)
        {
            if (context == null)
            {
                return Blocked(
                    default,
                    "camera.output-apply.context.missing",
                    "Camera output application requires a CameraOutputContext.");
            }

            if (context.OutputId != binding.OutputId)
            {
                return Blocked(
                    default,
                    "camera.output-apply.output-mismatch",
                    $"Camera output context '{context.OutputId}' does not match binding '{binding.OutputId}'.");
            }

            if (!defaultRig.IsValid)
            {
                return Blocked(
                    default,
                    "camera.output-apply.default-rig.invalid",
                    $"Camera output '{binding.OutputId}' requires an explicit valid Default Camera Rig.");
            }

            if (forceDefault || !context.HasWinner)
            {
                return ApplyDefault(defaultRig);
            }

            return ApplyWinner(context.Winner);
        }

        public CameraOutputApplyResult Clear()
        {
            CinemachineCamera previous = appliedCamera;

            if (appliedCamera != null)
            {
                appliedCamera.enabled = false;
            }

            hasAppliedRequest = false;
            hasAppliedDefault = false;
            appliedRequestId = default;
            appliedCamera = null;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Cleared,
                default,
                previous,
                null,
                Array.Empty<CameraIssue>(),
                previous != null
                    ? $"Camera output cleared. previousCamera='{previous.name}'."
                    : "Camera output was already clear.");
        }

        private CameraOutputApplyResult ApplyDefault(CameraRigReference defaultRig)
        {
            CameraRigComposer composer = defaultRig.Composer;

            if (composer == null)
            {
                return Blocked(
                    default,
                    "camera.output-apply.default-composer.missing",
                    "Default Camera Rig requires a materialized CameraRigComposer before it can be applied.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;

            if (targetCamera == null)
            {
                return Blocked(
                    default,
                    "camera.output-apply.default-cinemachine-camera.missing",
                    $"Default CameraRigComposer '{composer.name}' has no materialized CinemachineCamera.");
            }

            if (!targetCamera.gameObject.scene.IsValid())
            {
                return Blocked(
                    default,
                    "camera.output-apply.default-cinemachine-camera.scene-invalid",
                    $"Default CinemachineCamera '{targetCamera.name}' is not part of a valid loaded scene.");
            }

            if (hasAppliedDefault &&
                appliedCamera == targetCamera &&
                targetCamera.enabled)
            {
                return new CameraOutputApplyResult(
                    CameraOutputApplyKind.Preserved,
                    default,
                    targetCamera,
                    targetCamera,
                    Array.Empty<CameraIssue>(),
                    $"Camera output preserved Default Camera Rig. camera='{targetCamera.name}' output='{binding.OutputId}'.");
            }

            CinemachineCamera previous = appliedCamera;

            if (previous != null && previous != targetCamera)
            {
                previous.enabled = false;
            }

            targetCamera.enabled = true;

            hasAppliedRequest = false;
            hasAppliedDefault = true;
            appliedRequestId = default;
            appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
                default,
                previous,
                targetCamera,
                Array.Empty<CameraIssue>(),
                $"Camera output applied Default Camera Rig. camera='{targetCamera.name}' output='{binding.OutputId}'.");
        }

        private CameraOutputApplyResult ApplyWinner(CameraRequest winner)
        {
            if (!winner.IsValid)
            {
                return Blocked(
                    winner,
                    "camera.output-apply.winner.invalid",
                    "Camera output application rejected an invalid winner.");
            }

            if (winner.OutputId != binding.OutputId)
            {
                return Blocked(
                    winner,
                    "camera.output-apply.winner-output-mismatch",
                    $"Winning request output '{winner.OutputId}' does not match binding '{binding.OutputId}'.");
            }

            CameraRigComposer composer = winner.Rig.Composer;

            if (composer == null)
            {
                return Blocked(
                    winner,
                    "camera.output-apply.composer.missing",
                    "Winning camera request requires a materialized CameraRigComposer before it can be applied.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;

            if (targetCamera == null)
            {
                return Blocked(
                    winner,
                    "camera.output-apply.cinemachine-camera.missing",
                    $"CameraRigComposer '{composer.name}' has no materialized CinemachineCamera.");
            }

            if (!targetCamera.gameObject.scene.IsValid())
            {
                return Blocked(
                    winner,
                    "camera.output-apply.cinemachine-camera.scene-invalid",
                    $"CinemachineCamera '{targetCamera.name}' is not part of a valid loaded scene.");
            }

            if (hasAppliedRequest &&
                appliedRequestId == winner.RequestId &&
                appliedCamera == targetCamera &&
                targetCamera.enabled)
            {
                return new CameraOutputApplyResult(
                    CameraOutputApplyKind.Preserved,
                    winner,
                    targetCamera,
                    targetCamera,
                    Array.Empty<CameraIssue>(),
                    $"Camera output preserved current winner. request='{winner.RequestId}' camera='{targetCamera.name}'.");
            }

            CinemachineCamera previous = appliedCamera;

            if (previous != null && previous != targetCamera)
            {
                previous.enabled = false;
            }

            targetCamera.enabled = true;

            hasAppliedRequest = true;
            hasAppliedDefault = false;
            appliedRequestId = winner.RequestId;
            appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
                winner,
                previous,
                targetCamera,
                Array.Empty<CameraIssue>(),
                $"Camera output applied winner. request='{winner.RequestId}' camera='{targetCamera.name}' output='{binding.OutputId}'.");
        }

        private CameraOutputApplyResult Blocked(
            CameraRequest request,
            string code,
            string message)
        {
            string normalized =
                message.NormalizeTextOrFallback(
                    "Camera output application was blocked.");

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Blocked,
                request,
                appliedCamera,
                appliedCamera,
                new[]
                {
                    CameraIssue.Blocking(code, normalized)
                },
                normalized);
        }
    }
}
