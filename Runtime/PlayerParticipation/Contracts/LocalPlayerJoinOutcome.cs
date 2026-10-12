using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Bounded Manager-Provisioned Join outcome under IF-ADR-046.")]
    public sealed class LocalPlayerJoinOutcome
    {
        internal LocalPlayerJoinOutcome(
            LocalPlayerJoinOutcomeCode code,
            LocalPlayerJoinOutcomeCode? originalCode,
            LocalPlayerJoinAdmissionState admissionState)
        {
            Code = code;
            OriginalCode = originalCode;
            AdmissionState = admissionState;
        }

        public LocalPlayerJoinOutcomeCode Code { get; }
        public LocalPlayerJoinOutcomeCode? OriginalCode { get; }
        public LocalPlayerJoinAdmissionState AdmissionState { get; }
    }
}
