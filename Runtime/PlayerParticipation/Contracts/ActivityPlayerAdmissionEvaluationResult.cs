using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable Activity-scoped evaluation of Projection + Requirements against
    /// current Session Player evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.6 Activity Player admission evaluation result.")]
    public sealed class ActivityPlayerAdmissionEvaluationResult
    {
        private readonly ActivityPlayerAdmissionSlotResult[] slots;

        internal ActivityPlayerAdmissionEvaluationResult(
            string activityName,
            string sessionContextId,
            ActivityParticipationProjectionMode projectionMode,
            ActivityParticipationZeroParticipantPolicy zeroParticipantPolicy,
            PlayerParticipationRequirementLevel requirementLevel,
            ActivityPlayerAdmissionEvaluationStatus status,
            ActivityPlayerAdmissionEvaluationCode code,
            ActivityPlayerAdmissionSlotResult[] slots,
            string message)
        {
            ActivityName = activityName.NormalizeTextOrFallback("<missing>");
            SessionContextId = sessionContextId.NormalizeText();
            ProjectionMode = projectionMode;
            ZeroParticipantPolicy = zeroParticipantPolicy;
            RequirementLevel = requirementLevel;
            Status = status;
            Code = code;
            this.slots = slots != null
                ? (ActivityPlayerAdmissionSlotResult[])slots.Clone()
                : Array.Empty<ActivityPlayerAdmissionSlotResult>();
            Message = message.NormalizeText();

            for (int index = 0; index < this.slots.Length; index++)
            {
                switch (this.slots[index].Status)
                {
                    case ActivityPlayerAdmissionSlotStatus.Satisfied:
                        SatisfiedSlotCount++;
                        break;
                    case ActivityPlayerAdmissionSlotStatus.PendingResolution:
                        PendingSlotCount++;
                        break;
                    case ActivityPlayerAdmissionSlotStatus.Blocked:
                        BlockedSlotCount++;
                        break;
                    case ActivityPlayerAdmissionSlotStatus.Failed:
                        FailedSlotCount++;
                        break;
                }
            }
        }

        public string ActivityName { get; }
        public string SessionContextId { get; }
        public ActivityParticipationProjectionMode ProjectionMode { get; }
        public ActivityParticipationZeroParticipantPolicy ZeroParticipantPolicy { get; }
        public PlayerParticipationRequirementLevel RequirementLevel { get; }
        public ActivityPlayerAdmissionEvaluationStatus Status { get; }
        public ActivityPlayerAdmissionEvaluationCode Code { get; }
        public IReadOnlyList<ActivityPlayerAdmissionSlotResult> Slots => slots;
        public int ProjectedSlotCount => slots.Length;
        public int SatisfiedSlotCount { get; }
        public int PendingSlotCount { get; }
        public int BlockedSlotCount { get; }
        public int FailedSlotCount { get; }
        public string Message { get; }

        public bool CanActivate => Status == ActivityPlayerAdmissionEvaluationStatus.Satisfied;
        public bool IsPendingResolution => Status == ActivityPlayerAdmissionEvaluationStatus.PendingResolution;
        public bool IsBlocked => Status == ActivityPlayerAdmissionEvaluationStatus.Blocked;
        public bool IsFailed => Status == ActivityPlayerAdmissionEvaluationStatus.Failed;

        public string ToDiagnosticString()
        {
            return
                $"activity='{ActivityName}' session='{SessionContextId}' " +
                $"projection='{ProjectionMode}' zeroPolicy='{ZeroParticipantPolicy}' " +
                $"requirement='{RequirementLevel}' status='{Status}' code='{Code}' " +
                $"projected='{ProjectedSlotCount}' satisfied='{SatisfiedSlotCount}' " +
                $"pending='{PendingSlotCount}' blocked='{BlockedSlotCount}' failed='{FailedSlotCount}' " +
                $"message='{Message}'";
        }
    }
}
