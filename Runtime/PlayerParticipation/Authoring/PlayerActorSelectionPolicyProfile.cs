using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable Session product policy for Player Actor selection.
    /// Current Slot selections and conflict state remain in the Session runtime context.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerActorSelectionPolicyProfile",
        menuName = "Immersive Framework/Player/Actor Selection Policy Profile",
        order = 30)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3H immutable Player Actor selection policy Profile.")]
    public sealed class PlayerActorSelectionPolicyProfile : ScriptableObject
    {
        [Header("Designer")]
        [SerializeField] private string displayName = "Actor Selection — Allow Duplicates";
        [TextArea(2, 6)]
        [SerializeField] private string description;

        [Header("Selection")]
        [Tooltip("Explicit duplicate-selection rule evaluated by the Session participation runtime.")]
        [SerializeField] private PlayerActorSelectionDuplicatePolicy duplicatePolicy =
            PlayerActorSelectionDuplicatePolicy.AllowDuplicates;

        public string DisplayName => displayName.NormalizeText();

        public string Description => description.NormalizeText();

        public PlayerActorSelectionDuplicatePolicy DuplicatePolicy => duplicatePolicy;

        public bool HasDefinedDuplicatePolicy =>
            Enum.IsDefined(typeof(PlayerActorSelectionDuplicatePolicy), duplicatePolicy) &&
            duplicatePolicy != PlayerActorSelectionDuplicatePolicy.Unspecified;

        public bool AllowsDuplicates =>
            duplicatePolicy == PlayerActorSelectionDuplicatePolicy.AllowDuplicates;

        public bool RequiresUniqueActors =>
            duplicatePolicy == PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots;
    }
}
