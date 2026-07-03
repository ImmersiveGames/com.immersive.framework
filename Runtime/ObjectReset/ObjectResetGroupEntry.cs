using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ObjectEntry;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Authoring entry for one logical target inside an Object Reset Group.
    /// Scene-authored entries may reference an ObjectEntryDeclaration directly; asset-authored entries should prefer the objectEntryId string.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F39A Object Reset Group target authoring entry.")]
    public sealed class ObjectResetGroupEntry
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private ObjectEntryDeclaration targetDeclaration;
        [SerializeField] private string objectEntryId;
        [SerializeField] private string reason;

        public bool Enabled => enabled;

        public ObjectEntryDeclaration TargetDeclaration => targetDeclaration;

        public string AuthoringObjectEntryId => objectEntryId;

        public string AuthoringReason => reason;

        public bool HasCustomReason => !string.IsNullOrWhiteSpace(reason);

        public string ResolvedObjectEntryIdText => ResolveObjectEntryIdText();

        public string ResolveObjectEntryIdText()
        {
            if (targetDeclaration != null && targetDeclaration.HasObjectEntryId)
            {
                return targetDeclaration.ObjectEntryIdText.Trim();
            }

            return objectEntryId.NormalizeText();
        }

        public string ResolveReason(string fallbackReason)
        {
            return reason.NormalizeTextOrFallback(fallbackReason.NormalizeTextOrFallback("Object Reset Group"));
        }
    }
}
