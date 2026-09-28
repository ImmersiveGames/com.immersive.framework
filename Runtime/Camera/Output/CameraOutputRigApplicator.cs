using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    internal readonly struct CameraOutputRigApplicatorSnapshot
    {
        internal CameraOutputRigApplicatorSnapshot(
            CinemachineCamera appliedCamera,
            bool appliedCameraEnabled,
            bool hasAppliedFallback,
            CinemachineBlendDefinition defaultBlend,
            CinemachineBlenderSettings customBlends)
        {
            AppliedCamera = appliedCamera;
            HasAppliedCamera = !ReferenceEquals(appliedCamera, null);
            AppliedCameraEnabled = appliedCameraEnabled;
            HasAppliedFallback = hasAppliedFallback;
            DefaultBlend = defaultBlend;
            CustomBlends = customBlends;
        }

        internal CinemachineCamera AppliedCamera { get; }
        internal bool HasAppliedCamera { get; }
        internal bool AppliedCameraEnabled { get; }
        internal bool HasAppliedFallback { get; }
        internal CinemachineBlendDefinition DefaultBlend { get; }
        internal CinemachineBlenderSettings CustomBlends { get; }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputRigApplicator
    {
        private readonly CameraOutputBinding _binding;
        private readonly CinemachineBlendDefinition _baselineDefaultBlend;
        private readonly CinemachineBlenderSettings _baselineCustomBlends;

        private bool _hasAppliedFallback;
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
        public bool HasAppliedFallback => _hasAppliedFallback;
        public bool HasAppliedNormalOccurrence => !_hasAppliedFallback && _appliedCamera != null;
        public CinemachineCamera AppliedCamera => _appliedCamera;

        internal CameraOutputRigApplicatorSnapshot CaptureSnapshot() =>
            new CameraOutputRigApplicatorSnapshot(
                _appliedCamera,
                _appliedCamera != null && _appliedCamera.enabled,
                _hasAppliedFallback,
                _binding.Brain.DefaultBlend,
                _binding.Brain.CustomBlends);

        internal CameraOccurrenceOutputResult RestoreSnapshot(
            CameraOutputRigApplicatorSnapshot snapshot)
        {
            if (snapshot.HasAppliedCamera && snapshot.AppliedCamera == null)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _appliedCamera,
                    "The previously applied Camera was destroyed before rollback and cannot be restored.");
            }

            CinemachineCamera previous = _appliedCamera;
            if (previous != null && previous != snapshot.AppliedCamera)
            {
                previous.enabled = false;
            }

            if (snapshot.AppliedCamera != null)
            {
                if (!snapshot.AppliedCamera.gameObject.scene.IsValid())
                {
                    return CameraOccurrenceOutputResult.Rejected(
                        previous,
                        "The previously applied Camera no longer belongs to a loaded Scene and cannot be restored.");
                }
                snapshot.AppliedCamera.enabled = snapshot.AppliedCameraEnabled;
            }

            _binding.Brain.DefaultBlend = snapshot.DefaultBlend;
            _binding.Brain.CustomBlends = snapshot.CustomBlends;
            _hasAppliedFallback = snapshot.HasAppliedFallback;
            _appliedCamera = snapshot.AppliedCamera;
            return CameraOccurrenceOutputResult.Applied(previous, _appliedCamera);
        }

        public CameraOutputApplyResult Clear()
        {
            CinemachineCamera previous = _appliedCamera;

            if (_appliedCamera != null)
            {
                _appliedCamera.enabled = false;
            }

            RestoreOutputBlendPolicy();

            _hasAppliedFallback = false;
            _appliedCamera = null;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Cleared,
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
                    "camera.output-apply.fallback-composer.missing",
                    "Fallback Camera Rig requires a materialized CameraRigComposer before it can be applied.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;

            if (targetCamera == null)
            {
                return Blocked(
                    "camera.output-apply.fallback-cinemachine-camera.missing",
                    $"Fallback CameraRigComposer '{composer.name}' has no materialized CinemachineCamera.");
            }

            if (!targetCamera.gameObject.scene.IsValid())
            {
                return Blocked(
                    "camera.output-apply.fallback-cinemachine-camera.scene-invalid",
                    $"Fallback CinemachineCamera '{targetCamera.name}' is not part of a valid loaded scene.");
            }

            if (targetCamera.OutputChannel != _binding.Brain.ChannelMask)
            {
                return Blocked("camera.output-apply.fallback-channel.mismatch",
                    $"Fallback Camera '{targetCamera.name}' must use Output channel '{_binding.Brain.ChannelMask}'.");
            }

            if (_hasAppliedFallback &&
                _appliedCamera == targetCamera &&
                targetCamera.enabled)
            {
                return new CameraOutputApplyResult(
                    CameraOutputApplyKind.Preserved,
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

            _hasAppliedFallback = true;
            _appliedCamera = targetCamera;

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Applied,
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
                return Blocked("camera.output-apply.occurrence-composer.missing",
                    "Normal Camera Occurrence requires a materialized CameraRigComposer.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;
            if (targetCamera == null || !targetCamera.gameObject.scene.IsValid())
            {
                return Blocked("camera.output-apply.occurrence-camera.invalid",
                    $"Normal Camera Occurrence '{composer.name}' requires a materialized CinemachineCamera in a valid loaded scene.");
            }

            if (targetCamera.OutputChannel != _binding.Brain.ChannelMask)
            {
                return Blocked("camera.output-apply.occurrence-channel.mismatch",
                    $"Normal Camera Occurrence '{targetCamera.name}' must use Output channel '{_binding.Brain.ChannelMask}'.");
            }

            if (HasAppliedNormalOccurrence && _appliedCamera == targetCamera && targetCamera.enabled)
            {
                return new CameraOutputApplyResult(CameraOutputApplyKind.Preserved,
                    targetCamera, targetCamera, Array.Empty<CameraIssue>(),
                    $"Camera output preserved normal occurrence rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
            }

            CinemachineCamera previous = _appliedCamera;
            RestoreOutputBlendPolicy();
            if (previous != null && previous != targetCamera) previous.enabled = false;
            targetCamera.enabled = true;
            _hasAppliedFallback = false;
            _appliedCamera = targetCamera;
            return new CameraOutputApplyResult(CameraOutputApplyKind.Applied,
                previous, targetCamera, Array.Empty<CameraIssue>(),
                $"Camera output applied normal occurrence rig. camera='{targetCamera.name}' output='{_binding.OutputId}'.");
        }

        internal CameraOccurrenceOutputResult ApplySessionOccurrence(CameraRigComposer composer)
        {
            if (composer == null || composer.CinemachineCamera == null)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _appliedCamera,
                    "A Session Camera Occurrence requires a materialized CameraRigComposer and CinemachineCamera.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;
            if (!targetCamera.gameObject.scene.IsValid() ||
                targetCamera.OutputChannel != _binding.Brain.ChannelMask)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _appliedCamera,
                    $"Session Camera Occurrence '{targetCamera.name}' must be in a loaded Scene and use Output channel '{_binding.Brain.ChannelMask}'.");
            }

            if (HasAppliedNormalOccurrence && _appliedCamera == targetCamera && targetCamera.enabled)
            {
                return CameraOccurrenceOutputResult.Preserved(targetCamera);
            }

            CinemachineCamera previous = _appliedCamera;
            RestoreOutputBlendPolicy();
            if (previous != null && previous != targetCamera) previous.enabled = false;
            targetCamera.enabled = true;
            _hasAppliedFallback = false;
            _appliedCamera = targetCamera;
            return CameraOccurrenceOutputResult.Applied(previous, targetCamera);
        }

        internal CameraOccurrenceOutputResult ApplyFallbackCoverage(CameraRigComposer composer)
        {
            if (composer == null || composer.CinemachineCamera == null)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _appliedCamera,
                    "Fallback coverage requires a materialized Fallback Camera Composer and CinemachineCamera.");
            }

            CinemachineCamera targetCamera = composer.CinemachineCamera;
            if (!targetCamera.gameObject.scene.IsValid() ||
                targetCamera.OutputChannel != _binding.Brain.ChannelMask)
            {
                return CameraOccurrenceOutputResult.Rejected(
                    _appliedCamera,
                    $"Fallback Camera '{targetCamera.name}' must be in a loaded Scene and use Output channel '{_binding.Brain.ChannelMask}'.");
            }

            if (_hasAppliedFallback && _appliedCamera == targetCamera && targetCamera.enabled)
            {
                return CameraOccurrenceOutputResult.Preserved(targetCamera);
            }

            CinemachineCamera previous = _appliedCamera;
            RestoreOutputBlendPolicy();
            if (previous != null && previous != targetCamera) previous.enabled = false;
            targetCamera.enabled = true;
            _hasAppliedFallback = true;
            _appliedCamera = targetCamera;
            return CameraOccurrenceOutputResult.Applied(previous, targetCamera);
        }

        private void RestoreOutputBlendPolicy()
        {
            _binding.Brain.DefaultBlend = _baselineDefaultBlend;
            _binding.Brain.CustomBlends = _baselineCustomBlends;
        }

        private CameraOutputApplyResult Blocked(
            string code,
            string message)
        {
            string normalized =
                message.NormalizeTextOrFallback(
                    "Camera output application was blocked.");

            return new CameraOutputApplyResult(
                CameraOutputApplyKind.Blocked,
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
