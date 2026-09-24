using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Base Unity behaviour for local, synchronous Reset participants.
    /// It has no ObjectEntryDeclaration target and receives the subject from UnityResetSubjectAdapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12B Unity reset participant without ObjectEntryDeclaration target.")]
    public abstract class UnityResetParticipantBehaviour : MonoBehaviour, IResetParticipant
    {
        [Header("Reset Participant")]
        [SerializeField] private string participantId = "participant";
        [SerializeField] private ResetParticipantRequiredness requiredness = ResetParticipantRequiredness.Required;
        [SerializeField] private int order;
        [SerializeField] private string displayName;
        [SerializeField] private string source = nameof(UnityResetParticipantBehaviour);
        [SerializeField] private string reason = "unity-reset-participant";

        public string ParticipantIdText => participantId;

        public ResetParticipantRequiredness Requiredness => requiredness;

        public int Order => order;

        public string DisplayName => ResolveDisplayName();

        public virtual bool TryCreateResetParticipantDescriptor(
            ResetSubject subject,
            out ResetParticipantDescriptor descriptor,
            out ResetIssue issue)
        {
            descriptor = default;
            issue = default;

            if (!subject.IsValid)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidSubject,
                    $"Unity reset participant '{ResolveDisplayName()}' requires a valid ResetSubject.");
                return false;
            }

            string normalizedParticipantId = participantId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedParticipantId))
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidParticipant,
                    $"Unity reset participant on GameObject '{ResolveGameObjectName()}' requires a non-empty Participant Id.");
                return false;
            }

            if (!Enum.IsDefined(typeof(ResetParticipantRequiredness), requiredness)
                || requiredness == ResetParticipantRequiredness.Unknown)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidParticipant,
                    $"Unity reset participant '{normalizedParticipantId}' requires explicit requiredness.");
                return false;
            }

            try
            {
                descriptor = new ResetParticipantDescriptor(
                    ResetParticipantId.From(normalizedParticipantId),
                    subject.SubjectId,
                    requiredness,
                    order,
                    ResolveDisplayName(),
                    source.NormalizeTextOrFallback(GetType().Name),
                    reason.NormalizeTextOrFallback("unity-reset-participant"));
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = ResetIssue.Error(ResetIssueKind.InvalidParticipant, exception.Message);
                return false;
            }
        }

        public abstract ResetParticipantResult Reset(ResetContext context);

        protected ResetParticipantDescriptor CreateDescriptorForResult(ResetContext context)
        {
            return context.Participant;
        }

        protected string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return !string.IsNullOrWhiteSpace(participantId)
                ? participantId.Trim()
                : GetType().Name;
        }

        protected string ResolveSource(string fallback)
        {
            return source.NormalizeTextOrFallback(fallback);
        }

        protected string ResolveReason(string fallback)
        {
            return reason.NormalizeTextOrFallback(fallback);
        }

        private string ResolveGameObjectName()
        {
            return gameObject != null && !string.IsNullOrWhiteSpace(gameObject.name)
                ? gameObject.name.Trim()
                : "<missing>";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            string qaParticipantId,
            ResetParticipantRequiredness qaRequiredness,
            int qaOrder,
            string qaDisplayName,
            string qaSource,
            string qaReason)
        {
            participantId = qaParticipantId;
            requiredness = qaRequiredness;
            order = qaOrder;
            displayName = qaDisplayName;
            source = qaSource;
            reason = qaReason;
        }
#endif
    }
}
