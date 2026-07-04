using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// API status: Experimental. Runtime bridge that adapts an IUnityResettable MonoBehaviour into IResetParticipant.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12J internal bridge for IUnityResettable component reset participation.")]
    internal sealed class UnityResettableComponentParticipant : IResetParticipant
    {
        private readonly MonoBehaviour _component;
        private readonly IUnityResettable _resettable;
        private readonly IUnityResettableMetadata _metadata;

        public UnityResettableComponentParticipant(MonoBehaviour component, IUnityResettable resettable)
        {
            _component = component;
            _resettable = resettable ?? throw new ArgumentNullException(nameof(resettable));
            _metadata = resettable as IUnityResettableMetadata;
        }

        public bool TryCreateResetParticipantDescriptor(
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
                    $"Unity resettable component '{ResolveComponentName()}' requires a valid ResetSubject.");
                return false;
            }

            if (_component == null)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.StaleOwner,
                    "Unity resettable component registration requires a live MonoBehaviour owner.");
                return false;
            }

            string normalizedParticipantId = _resettable.ResetParticipantId.NormalizeText();
            if (string.IsNullOrWhiteSpace(normalizedParticipantId))
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidParticipant,
                    $"Unity resettable component '{ResolveComponentName()}' requires a non-empty ResetParticipantId.");
                return false;
            }

            ResetParticipantRequiredness requiredness = _metadata?.ResetRequiredness ?? ResetParticipantRequiredness.Required;
            if (!Enum.IsDefined(typeof(ResetParticipantRequiredness), requiredness)
                || requiredness == ResetParticipantRequiredness.Unknown)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidParticipant,
                    $"Unity resettable component '{normalizedParticipantId}' requires explicit reset requiredness.");
                return false;
            }

            try
            {
                descriptor = new ResetParticipantDescriptor(
                    ResetParticipantId.From(normalizedParticipantId),
                    subject.SubjectId,
                    requiredness,
                    _metadata?.ResetOrder ?? 0,
                    ResolveDisplayName(normalizedParticipantId),
                    ResolveSource(),
                    ResolveReason());
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = ResetIssue.Error(ResetIssueKind.InvalidParticipant, exception.Message);
                return false;
            }
        }

        public ResetParticipantResult Reset(ResetContext context)
        {
            return _resettable.Reset(context);
        }

        private string ResolveDisplayName(string participantIdFallback)
        {
            string metadataDisplayName = _metadata?.ResetDisplayName;
            if (!string.IsNullOrWhiteSpace(metadataDisplayName))
            {
                return metadataDisplayName.Trim();
            }

            return participantIdFallback.NormalizeTextOrFallback(ResolveComponentName());
        }

        private string ResolveSource()
        {
            string metadataSource = _metadata?.ResetSource;
            return metadataSource.NormalizeTextOrFallback(ResolveComponentName());
        }

        private string ResolveReason()
        {
            string metadataReason = _metadata?.ResetReason;
            return metadataReason.NormalizeTextOrFallback("unity-resettable-component");
        }

        private string ResolveComponentName()
        {
            if (_component != null)
            {
                return _component.GetType().Name;
            }

            return _resettable.GetType().Name;
        }
    }
}
