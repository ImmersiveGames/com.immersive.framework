using Immersive.Foundation.Common;

namespace Immersive.Framework.Camera.Cinemachine
{
    public enum CinemachineCameraOutputDiagnosticStatus
    {
        NotRun = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        Skipped = 30,
        Blocked = 40
    }

    /// <summary>
    /// Structured diagnostic emitted by Cinemachine output validation/application.
    /// </summary>
    public readonly struct CinemachineCameraOutputDiagnostic
    {
        public const string CameraOutputMissing = "camera-output-missing";
        public const string CinemachineCameraMissing = "cinemachine-camera-missing";
        public const string CinemachineBrainMissing = "cinemachine-brain-missing";
        public const string MultipleCinemachineBrains = "multiple-cinemachine-brains";
        public const string FollowTargetMissing = "follow-target-missing";
        public const string LookAtTargetMissing = "look-at-target-missing";
        public const string OutputApplied = "output-applied";
        public const string OptionalOutputSkipped = "optional-output-skipped";
        public const string RequiredOutputBlocked = "required-output-blocked";
        public const string BrainScopeMismatch = "cinemachine-brain-scope-mismatch";

        public CinemachineCameraOutputDiagnostic(
            CinemachineCameraOutputDiagnosticStatus status,
            string code,
            string message,
            string outputId,
            string displayName)
        {
            Status = status;
            Code = code.NormalizeTextOrFallback("cinemachine-output-diagnostic");
            Message = message.NormalizeTextOrFallback(Code);
            OutputId = outputId.NormalizeTextOrFallback("camera.output");
            DisplayName = displayName.NormalizeTextOrFallback(OutputId);
        }

        public CinemachineCameraOutputDiagnosticStatus Status { get; }

        public string Code { get; }

        public string Message { get; }

        public string OutputId { get; }

        public string DisplayName { get; }

        public bool IsSucceeded => Status == CinemachineCameraOutputDiagnosticStatus.Succeeded ||
                                   Status == CinemachineCameraOutputDiagnosticStatus.SucceededWithWarnings;

        public bool IsSkipped => Status == CinemachineCameraOutputDiagnosticStatus.Skipped;

        public bool IsBlocked => Status == CinemachineCameraOutputDiagnosticStatus.Blocked;

        public static CinemachineCameraOutputDiagnostic Succeeded(CinemachineCameraOutput output, string message = "")
        {
            return new CinemachineCameraOutputDiagnostic(
                CinemachineCameraOutputDiagnosticStatus.Succeeded,
                OutputApplied,
                message.NormalizeTextOrFallback("Cinemachine camera output applied."),
                output.OutputId,
                output.DisplayName);
        }

        public static CinemachineCameraOutputDiagnostic SucceededWithWarnings(CinemachineCameraOutput output, string code, string message)
        {
            return new CinemachineCameraOutputDiagnostic(
                CinemachineCameraOutputDiagnosticStatus.SucceededWithWarnings,
                code,
                message,
                output.OutputId,
                output.DisplayName);
        }

        public static CinemachineCameraOutputDiagnostic Skipped(CinemachineCameraOutput output, string code, string message)
        {
            return new CinemachineCameraOutputDiagnostic(
                CinemachineCameraOutputDiagnosticStatus.Skipped,
                code,
                message,
                output.OutputId,
                output.DisplayName);
        }

        public static CinemachineCameraOutputDiagnostic Blocked(CinemachineCameraOutput output, string code, string message)
        {
            return new CinemachineCameraOutputDiagnostic(
                CinemachineCameraOutputDiagnosticStatus.Blocked,
                code,
                message,
                output.OutputId,
                output.DisplayName);
        }

    }
}
