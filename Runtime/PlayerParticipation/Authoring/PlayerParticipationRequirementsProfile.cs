using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable reusable Activity admission policy for projected Player Slots.
    /// Runtime evaluation and readiness state must remain outside this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerParticipationRequirementsProfile",
        menuName = "Immersive Framework/Player/Participation Requirements Profile",
        order = 20)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3C immutable Activity Player participation requirements Profile.")]
    public sealed class PlayerParticipationRequirementsProfile : ScriptableObject
    {
        [Header("Designer")]
        [SerializeField] private string displayName = "Player Participation — None";
        [TextArea(2, 6)]
        [SerializeField] private string description;

        [Header("Admission")]
        [Tooltip("Progressive requirement evaluated for every Slot selected by the Activity participation projection.")]
        [SerializeField] private PlayerParticipationRequirementLevel requirementLevel =
            PlayerParticipationRequirementLevel.None;

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public PlayerParticipationRequirementLevel RequirementLevel => requirementLevel;

        public bool IsExplicitNone => requirementLevel == PlayerParticipationRequirementLevel.None;

        public bool HasDefinedRequirementLevel =>
            System.Enum.IsDefined(typeof(PlayerParticipationRequirementLevel), requirementLevel);
    }
}
