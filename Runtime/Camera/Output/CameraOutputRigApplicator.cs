using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputRigApplicator : ICameraOutputApplication
    {
        private readonly CameraOutputBinding _binding;
        private readonly CinemachineBlendDefinition _baselineDefaultBlend;
        private readonly CinemachineBlenderSettings _baselineCustomBlends;

        private bool _hasAppliedRequest;
        private bool _hasAppliedFallback;
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
            _baselineDefaultBlend = binding.Brain.DefaultBlend;
            _baselineCustomBlends = binding.Brain.CustomBlends;
        }

        public CameraOutputBinding Binding => _binding;
        public bool HasAppliedRequest => _hasAppliedRequest;
        public bool HasAppliedFallback => _hasAppliedFallback;
        public bool HasAppliedNormalOccurrence => !_hasAppliedRequest && !_hasAppliedFallback && _appliedCamera != null;
        public CameraRequestId AppliedRequestId => _appliedRequestId;
        public CinemachineCamera AppliedCamera => _appliedCamera;

        public CameraOutputApplyResult Apply(
            CameraOutputContext context,
            CameraRigReference fallbackRig,
            bool coverWithFallback)
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

            if (!fallbackRig.IsValid)
            {
                return Blocked(
                    default,
                    "camera.output-apply.fallback-rig.invalid",
                    $"Camera output '{_binding.OutputId}' requires an explicit valid Fallback Camera Rig.");
            }

            if (coverWithFallback || !context.HasWinner)
            {
                return ApplyFallbackRig(fallbackRig);
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

            RestoreOutputBlendPolicy();

            _hasAppliedRequest = false;
            _hasAppliedFallback = false;
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

        public CameraOutputApplyResult ApplyFallbackRig(CameraRigReference fallbackRig)
        {
            CameraRigComposer composer = fallbackRig.Composer;

            if (composer == null)
            {
                return Blocked(
                    default,
                    "camera.output-apply.fallback-composer.missing",
                    "Fallback Camera Rig requires a materialized CameraRigComposer before it can be applied.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;

            if (targetCamera == null)
            {
                return Blocked(
                    default,
                    "camera.output-apply.fallback-cinemachine-camera.missing",
                    $"Fallback CameraRigComposer '{composer.name}' has no materialized CinemachineCamera.");
            }

            if (!targetCamera.gameObject.scene.IsValid())
            {
                return Blocked(
                    default,
                    "camera.output-apply.fallback-cinemachine-camera.scene-invalid",
                    $"Fallback CinemachineCamera '{targetCamera.name}' is not part of a valid loaded scene.");
            }

            if (targetCamera.OutputChannel != _binding.Brain.ChannelMask)
            {
                return Blocked(default, "camera.output-apply.fallback-channel.mismatch",
                    $"Fallback Camera '{targetCamera.name}' must use Output channel '{_binding.Brain.ChannelMask}'.");
            }

            if (_hasAppliedFallback &&
                _appliedCamera == targetCamera &&
                targetCamera.enabled)
            {
                return new CameraOutputApplyResult(
                    CameraOutputApplyKind.Preserved,
                    default,
                    targetCamera,
                    targetCamera,
                    Array.Empty<CameraIssue>(),
                    $"Camera output preserved Fallback Camera Rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
            }

            CinemachineCamera previous = _appliedCamera;

            RestoreOutputBlendPolicy();

            if (previous != null && previous != targetCamera)
            {
                previous.enabled = false;
            }

            targetCamera.enabled = true;

            _hasAppliedRequest = false;
            _hasAppliedFallback = true;
            _appliedRequestId = default;
            _appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
                default,
                previous,
                targetCamera,
                Array.Empty<CameraIssue>(),
                $"Camera output applied Fallback Camera Rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
        }

        public CameraOutputApplyResult ApplyNormalOccurrence(CameraRigReference occurrenceRig)
        {
            CameraRigComposer composer = occurrenceRig.Composer;
            if (composer == null)
            {
                return Blocked(default, "camera.output-apply.occurrence-composer.missing",
                    "Normal Camera Occurrence requires a materialized CameraRigComposer.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;
            if (targetCamera == null || !targetCamera.gameObject.scene.IsValid())
            {
                return Blocked(default, "camera.output-apply.occurrence-camera.invalid",
                    $"Normal Camera Occurrence '{composer.name}' requires a materialized CinemachineCamera in a valid loaded scene.");
            }

            if (targetCamera.OutputChannel != _binding.Brain.ChannelMask)
            {
                return Blocked(default, "camera.output-apply.occurrence-channel.mismatch",
                    $"Normal Camera Occurrence '{targetCamera.name}' must use Output channel '{_binding.Brain.ChannelMask}'.");
            }

            if (HasAppliedNormalOccurrence && _appliedCamera == targetCamera && targetCamera.enabled)
            {
                return new CameraOutputApplyResult(CameraOutputApplyKind.Preserved, default,
                    targetCamera, targetCamera, Array.Empty<CameraIssue>(),
                    $"Camera output preserved normal occurrence rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
            }

            CinemachineCamera previous = _appliedCamera;
            RestoreOutputBlendPolicy();
            if (previous != null && previous != targetCamera) previous.enabled = false;
            targetCamera.enabled = true;
            _hasAppliedRequest = false;
            _hasAppliedFallback = false;
            _appliedRequestId = default;
            _appliedCamera = targetCamera;
            return new CameraOutputApplyResult(CameraOutputApplyKind.Applied, default,
                previous, targetCamera, Array.Empty<CameraIssue>(),
                $"Camera output applied normal occurrence rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
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

            ApplyPresentationTransitionPolicy(
                winner.PresentationTransitionMode);

            if (previous != null && previous != targetCamera)
            {
                previous.enabled = false;
            }

            targetCamera.enabled = true;

            if (winner.PresentationTransitionMode ==
                    CameraPresentationTransitionMode.Cut)
            {
                _binding.Brain.ActiveBlend = null;
            }

            _hasAppliedRequest = true;
            _hasAppliedFallback = false;
            _appliedRequestId = winner.RequestId;
            _appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
                winner,
                previous,
                targetCamera,
                Array.Empty<CameraIssue>(),
                $"Camera output applied winner. request='{winner.RequestId}' camera='{targetCamera.name}' output='{_binding.OutputId}' transition='{winner.PresentationTransitionMode}'.");
        }

        private void ApplyPresentationTransitionPolicy(
            CameraPresentationTransitionMode transitionMode)
        {
            if (transitionMode == CameraPresentationTransitionMode.Cut)
            {
                _binding.Brain.CustomBlends = null;
                _binding.Brain.DefaultBlend =
                    new CinemachineBlendDefinition(
                        CinemachineBlendDefinition.Styles.Cut,
                        0f);
                return;
            }

            RestoreOutputBlendPolicy();
        }

        private void RestoreOutputBlendPolicy()
        {
            _binding.Brain.DefaultBlend = _baselineDefaultBlend;
            _binding.Brain.CustomBlends = _baselineCustomBlends;
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
