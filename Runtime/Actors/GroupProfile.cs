using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// The single authored definition of a logical Group identity and its optional presentation.
    /// Referencing or loading this asset does not create a runtime Group.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GroupProfile",
        menuName = "Immersive Framework/Groups/Group Profile",
        order = 20)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 authored Group identity and presentation.")]
    public sealed class GroupProfile : ScriptableObject
    {
        [Tooltip("Explicit canonical GroupId. Runtime Group creation remains an explicit operation.")]
        [SerializeField] private string groupId;

        [SerializeField] private string displayName;

        [TextArea(2, 6)]
        [SerializeField] private string description;

        [SerializeField] private Sprite icon;

        public string GroupIdText => groupId.NormalizeText();

        public GroupId GroupId => GetRequiredGroupId();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public Sprite Icon => icon;

        public bool TryGetGroupId(out GroupId resolvedGroupId, out string issue)
        {
            string normalizedId = GroupIdText;
            if (string.IsNullOrEmpty(normalizedId))
            {
                resolvedGroupId = default;
                issue = $"GroupProfile '{name}' requires a non-empty GroupId.";
                return false;
            }

            try
            {
                resolvedGroupId = GroupId.From(normalizedId);
                issue = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                resolvedGroupId = default;
                issue = $"GroupProfile '{name}' has invalid GroupId '{normalizedId}'. {exception.Message}";
                return false;
            }
        }

        public GroupId GetRequiredGroupId()
        {
            if (TryGetGroupId(out GroupId resolvedGroupId, out string issue))
            {
                return resolvedGroupId;
            }

            throw new InvalidOperationException(issue);
        }

        public bool TryValidate(out string issue)
        {
            return TryGetGroupId(out _, out issue);
        }
    }
}
