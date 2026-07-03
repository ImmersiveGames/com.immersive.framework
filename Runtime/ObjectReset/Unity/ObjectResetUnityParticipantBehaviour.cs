using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ObjectEntry;
using UnityEngine;
namespace Immersive.Framework.ObjectReset.Unity
{
    /// <summary>
    /// API status: Experimental. Unity-side base class for Object Reset participants.
    /// It binds an explicit ObjectEntryDeclaration to the core Object Reset participant contract without using GameObject names, hierarchy paths or scene searches as identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F15B Unity Object Reset participant base; no concrete physical reset adapter yet.")]
    public abstract class ObjectResetUnityParticipantBehaviour : MonoBehaviour, IObjectResetParticipant
    {
        [Header("Object Reset Target")]
        [SerializeField] private ObjectEntryDeclaration targetDeclaration;

        [Header("Participant")]
        [SerializeField] private string participantId;
        [SerializeField] private ObjectResetParticipantRequiredness requiredness = ObjectResetParticipantRequiredness.Required;
        [SerializeField] private int order;
        [SerializeField] private string displayName;
        [SerializeField] private string source = "ObjectResetUnityParticipant";
        [SerializeField] private string reason = "unity-object-reset-participant";

        public ObjectEntryDeclaration TargetDeclaration => targetDeclaration;

        public string ParticipantIdText => participantId;

        public ObjectResetParticipantRequiredness Requiredness => requiredness;

        public int Order => order;

        public string DisplayName => displayName;

        public string Source => source;

        public string Reason => reason;

        public bool HasTargetDeclaration => targetDeclaration != null;

        public ObjectResetParticipantDescriptor GetObjectResetDescriptor()
        {
            if (TryCreateObjectResetDescriptor(out var descriptor, out string issue))
            {
                return descriptor;
            }

            throw new InvalidOperationException(issue);
        }

        public bool TryCreateObjectResetDescriptor(out ObjectResetParticipantDescriptor descriptor, out string issue)
        {
            descriptor = default;
            issue = string.Empty;

            if (targetDeclaration == null)
            {
                issue = "Object Reset Unity participant requires an explicit Object Entry Declaration target.";
                return false;
            }

            if (!targetDeclaration.TryCreateDescriptor(out var objectEntryDescriptor, out issue))
            {
                return false;
            }

            if (!objectEntryDescriptor.HasOwnerIdentity)
            {
                issue = "Object Reset Unity participant target declaration must resolve an owner identity.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ObjectResetParticipantRequiredness), requiredness)
                || requiredness == ObjectResetParticipantRequiredness.Unknown)
            {
                issue = "Object Reset Unity participant requiredness must be explicit.";
                return false;
            }

            try
            {
                var target = ObjectResetTarget.FromDescriptor(objectEntryDescriptor);
                var id = ObjectResetParticipantId.From(ResolveParticipantIdText(objectEntryDescriptor));
                descriptor = new ObjectResetParticipantDescriptor(
                    id,
                    target,
                    requiredness,
                    order,
                    ResolveDisplayName(objectEntryDescriptor),
                    ResolveSource(),
                    ResolveReason());
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = exception.Message;
                return false;
            }
        }

        public bool SupportsResolvedTarget(ObjectResetRequest request, ObjectEntryDescriptor resolvedTarget)
        {
            return TryCreateObjectResetDescriptor(out var descriptor, out _)
                && descriptor.SupportsResolvedTarget(request, resolvedTarget);
        }

        public abstract ObjectResetParticipantResult ResetObject(ObjectResetContext context);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            ObjectEntryDeclaration qaTargetDeclaration,
            string qaParticipantId,
            ObjectResetParticipantRequiredness qaRequiredness,
            int qaOrder,
            string qaDisplayName,
            string qaSource,
            string qaReason)
        {
            targetDeclaration = qaTargetDeclaration;
            participantId = qaParticipantId;
            requiredness = qaRequiredness;
            order = qaOrder;
            displayName = qaDisplayName;
            source = qaSource;
            reason = qaReason;
        }
#endif

        private string ResolveParticipantIdText(ObjectEntryDescriptor objectEntryDescriptor)
        {
            if (!string.IsNullOrWhiteSpace(participantId))
            {
                return participantId.Trim();
            }

            return $"{objectEntryDescriptor.Id.StableText}:{GetType().FullName}";
        }

        private string ResolveDisplayName(ObjectEntryDescriptor objectEntryDescriptor)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return $"{objectEntryDescriptor.DisplayName}:{GetType().Name}";
        }

        private string ResolveSource()
        {
            return source.NormalizeTextOrFallback(nameof(ObjectResetUnityParticipantBehaviour));
        }

        private string ResolveReason()
        {
            return reason.NormalizeTextOrFallback("unity-object-reset-participant");
        }
    }
}
