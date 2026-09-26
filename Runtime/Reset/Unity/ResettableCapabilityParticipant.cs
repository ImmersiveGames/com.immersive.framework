using System;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>
    /// Internal IF-ADR-035 bridge from one existing local Reset capability to the existing
    /// <see cref="IResetParticipant"/> contract. The participant id is generated from the capability's
    /// deterministic position inside its Resettable boundary, so authored participant ids cannot collide
    /// and are not required. Execution delegates to the existing capability unchanged.
    /// </summary>
    internal sealed class ResettableCapabilityParticipant : IResetParticipant
    {
        private readonly MonoBehaviour _component;
        private readonly int _ordinal;

        internal ResettableCapabilityParticipant(MonoBehaviour component, int ordinal)
        {
            _component = component;
            _ordinal = ordinal;
        }

        internal MonoBehaviour Component => _component;

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
                    "Resettable capability registration requires a valid ResetSubject.");
                return false;
            }

            if (_component == null)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.StaleOwner,
                    "Resettable capability registration requires a live capability component.");
                return false;
            }

            ResolveMetadata(
                out ResetParticipantRequiredness requiredness,
                out int order,
                out string displayName);
            if (!Enum.IsDefined(typeof(ResetParticipantRequiredness), requiredness) ||
                requiredness == ResetParticipantRequiredness.Unknown)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidParticipant,
                    $"Resettable capability '{_component.GetType().Name}' requires explicit requiredness.");
                return false;
            }

            try
            {
                descriptor = new ResetParticipantDescriptor(
                    ResetParticipantId.From(CreateParticipantIdText()),
                    subject.SubjectId,
                    requiredness,
                    order,
                    displayName,
                    nameof(Resettable),
                    "resettable-capability");
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
            if (_component is UnityResetParticipantBehaviour behaviour)
            {
                return behaviour.Reset(context);
            }

            if (_component is IUnityResettable resettable)
            {
                return resettable.Reset(context);
            }

            return ResetParticipantResult.CreateFailed(
                context.Participant,
                1,
                nameof(Resettable),
                context.Reason,
                "Resettable capability component is missing or no longer implements a Reset capability.");
        }

        private string CreateParticipantIdText()
        {
            // The zero-padded ordinal keeps registry ordinal tie-break ordering equal to boundary
            // collection order. The type name is diagnostics only.
            return $"capability-{_ordinal:D4}-{_component.GetType().Name}";
        }

        private void ResolveMetadata(
            out ResetParticipantRequiredness requiredness,
            out int order,
            out string displayName)
        {
            if (_component is UnityResetParticipantBehaviour behaviour)
            {
                requiredness = behaviour.Requiredness;
                order = behaviour.Order;
                displayName = behaviour.DisplayName;
                return;
            }

            var metadata = _component as IUnityResettableMetadata;
            requiredness = metadata?.ResetRequiredness ?? ResetParticipantRequiredness.Required;
            order = metadata?.ResetOrder ?? 0;
            displayName = metadata?.ResetDisplayName.NormalizeTextOrFallback(_component.GetType().Name)
                ?? _component.GetType().Name;
        }
    }
}
