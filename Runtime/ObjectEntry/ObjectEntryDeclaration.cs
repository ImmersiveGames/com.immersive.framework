using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>Declares Object Entry requiredness and its optional stable boundary identity.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Entry/Object Entry Declaration")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Scene-authored Object Entry metadata admitted under its transaction owner.")]
    public sealed class ObjectEntryDeclaration : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string objectEntryId = string.Empty;
        [SerializeField] private ObjectEntryRequiredness requiredness = ObjectEntryRequiredness.Required;

        public ObjectEntryRequiredness Requiredness => requiredness;
        public bool HasObjectEntryId => TryGetObjectEntryId(out _);
        public ObjectEntryId ObjectEntryId => TryGetObjectEntryId(out ObjectEntryId id) ? id : default;

        public bool TryGetObjectEntryId(out ObjectEntryId id)
        {
            id = default;
            if (string.IsNullOrWhiteSpace(objectEntryId)) return false;
            try
            {
                id = Immersive.Framework.ObjectEntry.ObjectEntryId.From(objectEntryId.Trim());
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        internal bool TryCreateDescriptor(
            ObjectEntryScope scope,
            FrameworkIdentityKey ownerIdentity,
            out ObjectEntryDescriptor descriptor,
            out string issue)
        {
            descriptor = default;
            issue = string.Empty;
            if (!TryGetObjectEntryId(out ObjectEntryId id))
            {
                issue = "A valid Object Entry ID is required.";
                return false;
            }
            if (!Enum.IsDefined(typeof(ObjectEntryRequiredness), requiredness)
                || requiredness == ObjectEntryRequiredness.Unspecified)
            {
                issue = "Object Entry Requiredness must be explicit.";
                return false;
            }
            if (!ownerIdentity.IsValid || ownerIdentity.Domain != ObjectEntryDescriptor.GetExpectedOwnerDomain(scope))
            {
                issue = $"The admission transaction did not provide an owner matching scope '{scope}'.";
                return false;
            }

            try
            {
                descriptor = new ObjectEntryDescriptor(
                    id, scope, ObjectEntrySourceKind.SceneAuthored, requiredness, gameObject.name, ownerIdentity);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                issue = exception.Message;
                return false;
            }
        }

#if UNITY_EDITOR
        internal void ConfigureForQa(ObjectEntryId qaIdentity, ObjectEntryRequiredness qaRequiredness)
        {
            objectEntryId = qaIdentity.IsValid ? qaIdentity.Value.Value : string.Empty;
            requiredness = qaRequiredness;
        }
#endif
    }
}
