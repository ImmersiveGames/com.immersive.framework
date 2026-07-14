using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Immutable reusable product identity for an Actor option before any runtime Actor exists.
    /// Runtime selection, ActorId, owner scope, occupancy and presentation state remain outside this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActorProfile",
        menuName = "Immersive Framework/Actors/Actor Profile",
        order = 10)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3H immutable selectable Actor Profile authoring foundation.")]
    public sealed class ActorProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Canonical stable ActorProfileId authored only by this Profile.")]
        [SerializeField] private string actorProfileId = "actor-profile.player.default";
        [SerializeField] private string displayName = "Player Actor";
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Classification")]
        [Tooltip("Broad framework Actor category. This is not a project-specific class taxonomy.")]
        [SerializeField] private ActorKind actorKind = ActorKind.Player;
        [Tooltip("Broad framework Actor role. This is not a loadout, team or character class.")]
        [SerializeField] private ActorRole actorRole = ActorRole.Protagonist;

        [Header("Logical Composition")]
        [Tooltip("Canonical Logical Actor Host prefab. ActorProfile never instantiates it by itself.")]
        [SerializeField] private GameObject logicalActorHostPrefab;

        public string ActorProfileIdText => actorProfileId.NormalizeText();

        public ActorProfileId ActorProfileId => GetRequiredActorProfileId();

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public Sprite Icon => icon;

        public ActorKind ActorKind => actorKind;

        public ActorRole ActorRole => actorRole;

        public GameObject LogicalActorHostPrefab => logicalActorHostPrefab;

        public bool HasDefinedActorKind =>
            Enum.IsDefined(typeof(ActorKind), actorKind) && actorKind != ActorKind.Unknown;

        public bool HasDefinedActorRole =>
            Enum.IsDefined(typeof(ActorRole), actorRole) && actorRole != ActorRole.Unknown;

        public bool HasLogicalActorHostPrefab => logicalActorHostPrefab != null;

        public bool TryGetActorProfileId(
            out ActorProfileId resolvedActorProfileId,
            out string issue)
        {
            string normalizedId = ActorProfileIdText;
            if (string.IsNullOrEmpty(normalizedId))
            {
                resolvedActorProfileId = default;
                issue = $"ActorProfile '{name}' requires a non-empty ActorProfileId.";
                return false;
            }

            try
            {
                resolvedActorProfileId = ActorProfileId.From(normalizedId);
                issue = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                resolvedActorProfileId = default;
                issue = $"ActorProfile '{name}' has invalid ActorProfileId '{normalizedId}'. {exception.Message}";
                return false;
            }
        }

        public ActorProfileId GetRequiredActorProfileId()
        {
            if (TryGetActorProfileId(out ActorProfileId resolvedActorProfileId, out string issue))
            {
                return resolvedActorProfileId;
            }

            throw new InvalidOperationException(issue);
        }
    }
}
