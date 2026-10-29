using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Authored definition of a logical Group identity and its optional reusable Profile reference.
    /// Loading or referencing this asset does not create a runtime Group.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GroupDefinition",
        menuName = "Immersive Framework/Groups/Group Definition",
        order = 10)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 authored Group identity definition.")]
    public sealed class GroupDefinitionAsset : ScriptableObject
    {
        [Tooltip("Explicit canonical GroupId. This value is the sole authored identity of the logical Group.")]
        [SerializeField] private string groupId;

        [Tooltip("Optional reusable, game-neutral Group Profile. A shared Profile does not merge Group identities.")]
        [SerializeField] private GroupProfile groupProfile;

        public string GroupIdText => groupId.NormalizeText();

        public GroupId GroupId => GetRequiredGroupId();

        public GroupProfile GroupProfile => groupProfile;

        public bool TryGetGroupId(out GroupId resolvedGroupId, out string issue)
        {
            string normalizedId = GroupIdText;
            if (string.IsNullOrEmpty(normalizedId))
            {
                resolvedGroupId = default;
                issue = $"GroupDefinitionAsset '{name}' requires a non-empty GroupId.";
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
                issue = $"GroupDefinitionAsset '{name}' has invalid GroupId '{normalizedId}'. {exception.Message}";
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
            if (!TryGetGroupId(out _, out issue))
            {
                return false;
            }

            if (groupProfile != null &&
                !groupProfile.TryGetGroupProfileId(out _, out string profileIssue))
            {
                issue = $"GroupDefinitionAsset '{name}' references an invalid GroupProfile. {profileIssue}";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
