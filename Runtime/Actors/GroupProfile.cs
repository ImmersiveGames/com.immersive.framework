using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Optional reusable, game-neutral authoring configuration for Groups.
    /// A Profile has its own identity and presentation defaults; it does not create or own a Group.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GroupProfile",
        menuName = "Immersive Framework/Groups/Group Profile",
        order = 20)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 reusable Group Profile authoring surface.")]
    public sealed class GroupProfile : ScriptableObject
    {
        [Tooltip("Explicit canonical GroupProfileId. This identity is distinct from every GroupId.")]
        [SerializeField] private string groupProfileId;

        [SerializeField] private string displayName;

        [TextArea(2, 6)]
        [SerializeField] private string description;

        [SerializeField] private Sprite icon;

        public string GroupProfileIdText => groupProfileId.NormalizeText();

        public GroupProfileId GroupProfileId => GetRequiredGroupProfileId();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public Sprite Icon => icon;

        public bool TryGetGroupProfileId(out GroupProfileId resolvedGroupProfileId, out string issue)
        {
            string normalizedId = GroupProfileIdText;
            if (string.IsNullOrEmpty(normalizedId))
            {
                resolvedGroupProfileId = default;
                issue = $"GroupProfile '{name}' requires a non-empty GroupProfileId.";
                return false;
            }

            try
            {
                resolvedGroupProfileId = GroupProfileId.From(normalizedId);
                issue = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                resolvedGroupProfileId = default;
                issue = $"GroupProfile '{name}' has invalid GroupProfileId '{normalizedId}'. {exception.Message}";
                return false;
            }
        }

        public GroupProfileId GetRequiredGroupProfileId()
        {
            if (TryGetGroupProfileId(out GroupProfileId resolvedGroupProfileId, out string issue))
            {
                return resolvedGroupProfileId;
            }

            throw new InvalidOperationException(issue);
        }
    }
}
