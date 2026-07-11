using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    public sealed class CameraOutputRigApplicator
    {
        private readonly CameraOutputBinding binding;

        private bool hasAppliedRequest;
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
        public CameraRequestId AppliedRequestId => appliedRequestId;
        public CinemachineCamera AppliedCamera => appliedCamera;

        public CameraOutputApplyResult Apply(CameraOutputContext context)
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

            if (!context.HasWinner)
            {
                return Clear();
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
