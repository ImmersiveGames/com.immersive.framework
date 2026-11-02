using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Unity-serializable authoring reference that transports only an ObjectEntryId.
    /// ObjectEntryId remains the canonical identity type.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Unity-serializable identity-only authoring reference for ObjectEntryId.")]
    public struct ObjectEntryIdReference
    {
        [SerializeField, HideInInspector]
        private string objectEntryIdText;

        public ObjectEntryIdReference(ObjectEntryId objectEntryId)
        {
            if (!objectEntryId.IsValid)
            {
                throw new ArgumentException("Object Entry ID reference requires a valid ObjectEntryId.", nameof(objectEntryId));
            }

            objectEntryIdText = objectEntryId.Value.Value;
        }

        public ObjectEntryId ObjectEntryId => TryGetObjectEntryId(out ObjectEntryId value) ? value : default;

        public bool IsValid => TryGetObjectEntryId(out _);

        public bool TryGetObjectEntryId(out ObjectEntryId value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(objectEntryIdText))
            {
                return false;
            }

            try
            {
                value = Immersive.Framework.ObjectEntry.ObjectEntryId.From(objectEntryIdText.Trim());
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
