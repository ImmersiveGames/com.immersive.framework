using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-046 typed Manager-Provisioned Join outcome (I1).")]
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
