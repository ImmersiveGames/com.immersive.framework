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
        private readonly CameraOutputBinding _binding;

        private bool _hasAppliedRequest;
        private bool _hasAppliedDefault;
        private CameraRequestId _appliedRequestId;
        private CinemachineCamera _appliedCamera;

        public CameraOutputRigApplicator(CameraOutputBinding binding)
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException(
                    "CameraOutputRigApplicator requires a valid output binding.",
                    nameof(binding));
            }

            this._binding = binding;
        }

        public CameraOutputBinding Binding => _binding;
        public bool HasAppliedRequest => _hasAppliedRequest;
        public bool HasAppliedDefault => _hasAppliedDefault;
        public CameraRequestId AppliedRequestId => _appliedRequestId;
        public CinemachineCamera AppliedCamera => _appliedCamera;

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

            if (context.OutputId != _binding.OutputId)
            {
                return Blocked(
                    default,
                    "camera.output-apply.output-mismatch",
                    $"Camera output context '{context.OutputId}' does not match binding '{_binding.OutputId}'.");
            }

            if (!defaultRig.IsValid)
            {
                return Blocked(
                    default,
                    "camera.output-apply.default-rig.invalid",
                    $"Camera output '{_binding.OutputId}' requires an explicit valid Default Camera Rig.");
            }

            if (forceDefault || !context.HasWinner)
            {
                return ApplyDefault(defaultRig);
            }

            return ApplyWinner(context.Winner);
        }

        public CameraOutputApplyResult Clear()
        {
            CinemachineCamera previous = _appliedCamera;

            if (_appliedCamera != null)
            {
                _appliedCamera.enabled = false;
            }

            _hasAppliedRequest = false;
            _hasAppliedDefault = false;
            _appliedRequestId = default;
            _appliedCamera = null;

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

            if (_hasAppliedDefault &&
                _appliedCamera == targetCamera &&
                targetCamera.enabled)
            {
                return new CameraOutputApplyResult(
                    CameraOutputApplyKind.Preserved,
                    default,
                    targetCamera,
                    targetCamera,
                    Array.Empty<CameraIssue>(),
                    $"Camera output preserved Default Camera Rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
            }

            CinemachineCamera previous = _appliedCamera;

            if (previous != null && previous != targetCamera)
            {
                previous.enabled = false;
            }

            targetCamera.enabled = true;

            _hasAppliedRequest = false;
            _hasAppliedDefault = true;
            _appliedRequestId = default;
            _appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
                default,
                previous,
                targetCamera,
                Array.Empty<CameraIssue>(),
                $"Camera output applied Default Camera Rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
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

            if (winner.OutputId != _binding.OutputId)
            {
                return Blocked(
                    winner,
                    "camera.output-apply.winner-output-mismatch",
                    $"Winning request output '{winner.OutputId}' does not match binding '{_binding.OutputId}'.");
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

            if (_hasAppliedRequest &&
                _appliedRequestId == winner.RequestId &&
                _appliedCamera == targetCamera &&
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

            CinemachineCamera previous = _appliedCamera;

            if (previous != null && previous != targetCamera)
            {
                previous.enabled = false;
            }

            targetCamera.enabled = true;

            _hasAppliedRequest = true;
            _hasAppliedDefault = false;
            _appliedRequestId = winner.RequestId;
            _appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
                winner,
                previous,
                targetCamera,
                Array.Empty<CameraIssue>(),
                $"Camera output applied winner. request='{winner.RequestId}' camera='{targetCamera.name}' output='{_binding.OutputId}'.");
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
                _appliedCamera,
                _appliedCamera,
                new[]
                {
                    CameraIssue.Blocking(code, normalized)
                },
                normalized);
        }
    }
}
